using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.SqlServer.TransactSql.ScriptDom;

using LowSums;
using TSqlStrong.Logger;
using TSqlStrong.TypeSystem;
using TSqlStrong.VerificationResults;
using TSqlStrong.Symbols;

namespace TSqlStrong.Ast
{
    public class SqlVisitor : TSqlFragmentVisitor
    {

        #region Private Variables 

        private StackFrame _currentFrame;
        private ExpressionResult _lastExpressionResult = new ExpressionResult(UnknownDataType.Instance);
        private ILogger _diagnosticLogger;

        private Stack<string> _evaluationContext = new Stack<string>();
        private bool _prefixIssuesWithEvaluationContext = false;
        private List<Issue> _issues = new List<Issue>();

        #endregion

        #region Constructors 

        public SqlVisitor() : this(null) { }

        public SqlVisitor(StackFrame frame) : this(frame, NullLogger.Instance)
        {
            _currentFrame = frame;
        }

        public SqlVisitor(StackFrame frame, ILogger diagnosticLogger)
        {
            _currentFrame = frame;
            _diagnosticLogger = diagnosticLogger;
        }

        #endregion

        #region Public 

        /// <summary>
        /// Visits the element and returns the issues encountered while visiting.
        /// </summary>
        /// <param name="fragment"></param>
        /// <returns></returns>
        public (IEnumerable<Issue> issues, ExpressionResult expressionResult) VisitAndReturnResults(TSqlFragment fragment)
        {
            if (fragment == null)
                throw new ArgumentNullException("Cannot visit a null fragment");

            var lastIssues = _issues;
            var theseIssues = new List<Issue>();
            _issues = theseIssues;
            fragment.Accept(this);

            _issues = lastIssues;
            _issues.AddRange(theseIssues);
            return (theseIssues.ToArray(), this.LastExpressionResult);
        }

        public IEnumerable<Issue> Issues => _issues.ToArray();

        public ExpressionResult LastExpressionResult => _lastExpressionResult;

        #endregion

        #region Visitors 

        #region Computational Unit Declaration

        public override void ExplicitVisit(CreateFunctionStatement node)
        {
            var name = Names.GetFullTypeName(node.Name);

            var topFrameWhenCompilingBody = _currentFrame;
            var parameters = node.Parameters.Select(CreateSubroutineParameterFromProcedureParameterFragment);
            
            _currentFrame.WithSymbol(
                Names.GetFullTypeName(node.Name),
                new FunctionDataType(
                    name: name,
                    sqlFragment: node.Name,
                    declaredReturnTypeMaybe: 
                        (node.ReturnType == null) || (node.ReturnType is SelectFunctionReturnType)
                            // if a select function return type then the user has not explicitly declared a return type, it is implict via processing the expression
                            ? Maybe.None<DataType>()
                            : CreateDataTypeFromFunctionReturnType(node.ReturnType),
                    parameters: parameters,
                    typeCheckBody: new Lazy<DataType>(() =>                    
                    {
                        var lastFrame = _currentFrame;
                        // create a new frame for the body
                        _currentFrame = new StackFrame(topFrameWhenCompilingBody);
                        _evaluationContext.Push(name);
                        try
                        {
                            // register the parameters so they are in scope during type-check of the body
                            parameters.Do(parameter => _currentFrame.WithSymbol(parameter.Name, parameter.DataType));
                            return VisitAndReturnResults(
                                node.ReturnType is SelectFunctionReturnType
                                    ? (TSqlFragment)node.ReturnType
                                    : node.StatementList
                            ).expressionResult.TypeOfExpression;
                        }
                        finally
                        {
                            _currentFrame = lastFrame;
                            _evaluationContext.Pop();
                        }                        
                    })
                )
            );

            // TODO: node.Options
        }

        public override void ExplicitVisit(AlterProcedureStatement node)
        {
            var procedure = CreateStoredProcedureDataType(node);
            if (!_currentFrame.ContainsSymbol(procedure.Name))
                LogError(node, Messages.UnknownProcedure(procedure.Name));

            _currentFrame.ReplaceSymbol(procedure.Name, procedure);
        }

        public override void ExplicitVisit(CreateOrAlterProcedureStatement node)
        {
            var procedure = CreateStoredProcedureDataType(node);
            _currentFrame.ReplaceSymbol(procedure.Name, procedure);
        }

        public override void ExplicitVisit(CreateProcedureStatement node)
        {
            var procedure = CreateStoredProcedureDataType(node);
            _currentFrame.WithSymbol(procedure.Name, procedure);
        }

        #endregion

        #region Table DDL

        public override void ExplicitVisit(DropTableStatement node)
        {
            DropObjects(node);
        }

        public override void ExplicitVisit(DropProcedureStatement node)
        {
            DropObjects(node);
        }

        public override void ExplicitVisit(CreateTableStatement node)
        {
            _diagnosticLogger.Enter("CreateTableStatement");

            var tableName = Names.GetFullTypeName(node.SchemaObjectName);
            if (_currentFrame.ContainsSymbol(tableName))
                LogError(node, Messages.TableAlreadyExists(tableName));
            else
                _currentFrame.WithSymbol(
                    tableName,
                    DecorateRowDataTypeWithConstraints(tableName, node.Definition.TableConstraints, new RowDataType(node.Definition.ColumnDefinitions.Select(CreateColumnDataTypeFromColumnDefinition).ToArray()))
                );                       

            _lastExpressionResult = _lastExpressionResult.WithNewTypeOfExpression(VoidDataType.Instance);

            _diagnosticLogger.Exit();
        }

        #endregion

        #region Statements

        public override void ExplicitVisit(SelectStatement node)
        {
            if (node.WithCtesAndXmlNamespaces != null)
            {
                if (node.WithCtesAndXmlNamespaces.CommonTableExpressions.Count() > 0)
                {
                    _currentFrame = new StackFrame(_currentFrame);
                    try
                    {
                        node.WithCtesAndXmlNamespaces.CommonTableExpressions.Do(cte =>
                        {
                            // recursive calls must be a binary query with the base case in the first
                            if (cte.QueryExpression is BinaryQueryExpression binaryQuery)
                            {
                                var (_, firstResult) = VisitAndReturnResults(binaryQuery.FirstQueryExpression);
                                if (firstResult.TypeOfExpression is RowDataType firstAsRow)
                                    _currentFrame.WithSymbol(
                                        cte.ExpressionName.Value, 
                                        ApplyNamesToRowType(cte, firstAsRow)
                                    );
                            }

                            var (_, result) = VisitAndReturnResults(cte.QueryExpression);
                            if (result.TypeOfExpression is RowDataType rowDataType)
                            {
                                if (rowDataType.ColumnDataTypes.Count() != cte.Columns.Count())
                                    LogError(node, Messages.ColumnCountDoesNotMatchCTE);
                                else
                                {
                                    foreach(var pair in rowDataType.ColumnDataTypes.Zip(cte.Columns, (columnDataType, cteColumn) => (columnDataType, cteColumn)))
                                    {
                                        if (pair.columnDataType.Name is ColumnDataType.ColumnName.BaseNamedColumn asNamed)
                                        {
                                            if (!String.Equals(asNamed.Name, pair.cteColumn.Value, StringComparison.InvariantCultureIgnoreCase))
                                                LogError(
                                                    pair.columnDataType.DefiningLocationMaybe.Coalesce(pair.cteColumn), 
                                                    Messages.CannotAssignColumnName(
                                                        source: pair.columnDataType.ToString(),
                                                        destination: pair.cteColumn.Value
                                                    )
                                                );
                                        }
                                    }

                                    _currentFrame.ReplaceSymbol(
                                        cte.ExpressionName.Value,
                                        ApplyNamesToRowType(cte, rowDataType)
                                    );
                                }
                            }
                            else
                                LogError(cte.QueryExpression, "Internal Error: Expecting Row Type");
                        });

                        // Finally, now that the environment has been prepared, go ahead and type check the expression itself
                        _lastExpressionResult = _lastExpressionResult.WithNewTypeOfExpression(VisitAndReturnResults(node.QueryExpression).expressionResult.TypeOfExpression);
                    }
                    finally
                    {
                        _currentFrame = _currentFrame.LastFrame;
                    }
                }
                // TODO: Handle XmlNamespaces                
            }
            else
                _lastExpressionResult = _lastExpressionResult.WithNewTypeOfExpression(VisitAndReturnResults(node.QueryExpression).expressionResult.TypeOfExpression);

            RowDataType ApplyNamesToRowType(CommonTableExpression cte, RowDataType rowDataType) =>
                new RowDataType(
                    cte.Columns.Zip(
                        rowDataType.ColumnDataTypes,
                        (columnNameIdentifier, columnDataType) =>
                            new ColumnDataType(
                                new ColumnDataType.ColumnName.Aliased(columnNameIdentifier.Value, CaseSensitivity.CaseInsensitive),
                                columnDataType.DataType,
                                columnDataType.DefiningLocationMaybe
                            )
                    )
                );
        }

        public override void ExplicitVisit(RowValue node)
        {
            _lastExpressionResult = _lastExpressionResult.WithNewTypeOfExpression(
                new RowDataType(
                    node.ColumnValues.Select(colummValue =>
                        new ColumnDataType(
                            ColumnDataType.ColumnName.Anonymous.Instance,
                            VisitAndReturnResults(colummValue).expressionResult.TypeOfExpression,
                            colummValue.ToMaybe()
                        )
                    )
                )
            );
        }

        public override void ExplicitVisit(ValuesInsertSource node)
        {
            var typeCheckedRowValues = node.RowValues
                .Select(rowValue =>
                    (
                        rowNode: rowValue,
                        rowDataType: VisitAndReturnResults(rowValue)
                            .expressionResult
                            .TypeOfExpression
                            .Let(dataType =>
                                dataType is RowDataType rowDataType
                                    ? rowDataType
                                    : LogIssueAndReturn(
                                        rowValue,
                                        IssueLevel.Error,
                                        Messages.ExpectingRowTypeButGot(dataType.ToString()),
                                        RowDataType.EmptyRow
                                    )
                            )
                    )
                )
                .Where(it => !it.rowDataType.IsRowWithoutKnownStructure);

            _lastExpressionResult = _lastExpressionResult.WithNewTypeOfExpression(
                typeCheckedRowValues
                    .Aggregate(
                        typeCheckedRowValues.First().rowDataType,
                        (accumulatorDataType, nodeAndRowDataType) => 
                            RowDataType.Join(accumulatorDataType, nodeAndRowDataType.rowDataType).Match(
                                success: it => it,
                                failure: (errorMessage) =>
                                    LogIssueAndReturn(
                                        location: nodeAndRowDataType.rowNode,
                                        issueLevel: IssueLevel.Error,
                                        message: errorMessage,
                                        val: RowDataType.EmptyRow
                                    )
                            )
                    )
            );
        }

        public override void ExplicitVisit(InsertSpecification node)
        {
            _currentFrame = new StackFrame(_currentFrame);
            try
            {
                var (_, targetTableExpressionResult) = VisitAndReturnResults(node.Target);
                var (_, sourceExpressionResult) = VisitAndReturnResults(node.InsertSource);
                sourceExpressionResult.TypeOfExpression.IsAssignableTo(targetTableExpressionResult.TypeOfExpression).DoError(error =>
                    LogIssue(node.Target, IssueLevel.Error, error)
                );
            }
            finally
            {
                _currentFrame = _currentFrame.LastFrame;
            }
        }

        public override void ExplicitVisit(InsertStatement node)
        {
            VisitAndReturnResults(node.InsertSpecification);
            _lastExpressionResult = _lastExpressionResult.WithNewTypeOfExpression(VoidDataType.Instance);
        }

        public override void ExplicitVisit(PrintStatement node)
        {
            VisitAndReturnResults(node.Expression);
            _lastExpressionResult = ExpressionResult.Statement;
        }

        public override void ExplicitVisit(DeclareCursorStatement node)
        {
            _currentFrame.WithSymbol(
                node.Name.Value,
                VisitAndReturnResults(node.CursorDefinition.Select).expressionResult.TypeOfExpression
            );
        }

        public override void ExplicitVisit(FetchCursorStatement node)
        {
            var name = Names.GetSymbolName(node.Cursor.Name).GetValue();
            _currentFrame.LookupTypeOfSymbolMaybe(name).Do(
                none: () =>
                {
                    LogError(node.Cursor.Name, Messages.UnknownCursor(name));
                },
                some: (cursorTyping) =>
                {
                    if (!(cursorTyping.ExpressionType is RowDataType rowDataType))
                        LogError(node.Cursor, "Internal error: Expecting a row type.");
                    else if (node.IntoVariables.Count() > rowDataType.ColumnDataTypes.Count())
                        LogError(
                            node.IntoVariables.Skip(rowDataType.ColumnDataTypes.Count()).First(), 
                            Messages.TooManyVariablesSpecifiedForFetch(columnsInCursor: rowDataType.ColumnDataTypes.Count(), variableCount: node.IntoVariables.Count())
                        );
                    else
                    {
                        foreach(var pair in node.IntoVariables.Zip(rowDataType.ColumnDataTypes, (variable, columnDataType) => (variable, columnDataType)))
                        {
                            var (_, variableExpression) = VisitAndReturnResults(pair.variable);
                            pair.columnDataType.DataType.IsAssignableTo(variableExpression.TypeOfExpression).DoError(error => LogError(pair.variable, error));                            
                        }
                    }                        
                }
            );

            _lastExpressionResult = ExpressionResult.Statement;
        }

        public override void ExplicitVisit(OpenCursorStatement node)
        {
            CursorStatementWithNoEffects(node);
        }

        public override void ExplicitVisit(DeallocateCursorStatement node)
        {
            CursorStatementWithNoEffects(node);
        }

        public override void ExplicitVisit(CloseCursorStatement node)
        {
            CursorStatementWithNoEffects(node);
        }

        private void CursorStatementWithNoEffects(CursorStatement node)
        {
            var name = Names.GetSymbolName(node.Cursor.Name).GetValue();            
            _currentFrame.LookupTypeOfSymbolMaybe(name).Do(
                some: (_) => { },
                none: () =>
                {
                    LogError(node.Cursor.Name, Messages.UnknownCursor(name));
                }
            );

            _lastExpressionResult = ExpressionResult.Statement;
        }

        #endregion

        #region Row Expressions

        public override void ExplicitVisit(QuerySpecification node)
        {
            _currentFrame = new StackFrame(_currentFrame);
            try
            {
                _diagnosticLogger.Enter("QuerySpecification");

                if (node.FromClause != null)
                {
                    _diagnosticLogger.Enter("FromClause");
                    node.FromClause.Accept(this);
                    _diagnosticLogger.Exit();
                }

                if (node.WhereClause != null)
                {
                    _diagnosticLogger.Enter("WhereClause");
                    node.WhereClause.Accept(this);
                    _diagnosticLogger.Exit("WhereClause");
                }

                _diagnosticLogger.Enter("SelectElements[]");


                var rowResult = new RowDataType(
                        node.SelectElements
                        .SelectMany(selectElement => SelectElementToColumnTypes(selectElement))
                        .ToArray()
                    );

                _lastExpressionResult = node.SelectElements.Any(select => select is SelectSetVariable)
                    // if setting variables then this is a statement 
                    ? new ExpressionResult(VoidDataType.Instance)
                    // otherwise an expression
                    : new ExpressionResult(rowResult);
                _diagnosticLogger.Exit();

                _diagnosticLogger.Exit();
            }
            finally
            {
                _currentFrame = _currentFrame.LastFrame;
            }
        }

        public override void ExplicitVisit(BinaryQueryExpression node)
        {
            var (_, firstExpressionResult) = VisitAndReturnResults(node.FirstQueryExpression);
            var (_, secondExpressionResult) = VisitAndReturnResults(node.SecondQueryExpression);
            var firstDataType = firstExpressionResult.TypeOfExpression;

            ExpectTypeEvaluatingExpression<DataType, RowDataType>(
                node: node.FirstQueryExpression,
                errorMessage: ExpectingRowTypeMessage,
                val: firstExpressionResult.TypeOfExpression,
                whenOfProperType: (firstAsRowType) =>
                    ExpectTypeEvaluatingExpression<DataType, RowDataType>(
                        node: node.SecondQueryExpression,
                        errorMessage: ExpectingRowTypeMessage,
                        val: secondExpressionResult.TypeOfExpression,
                        whenOfProperType: (secondAsRowType) =>
                        {
                            if (firstAsRowType.ColumnDataTypes.Count() != secondAsRowType.ColumnDataTypes.Count())
                                LogError(node, Messages.ColumnCountMismatchInUnion);
                            else
                                _lastExpressionResult = _lastExpressionResult.WithNewTypeOfExpression(
                                    new RowDataType(
                                        firstAsRowType.ColumnDataTypes.Zip(
                                            secondAsRowType.ColumnDataTypes,
                                            (first, second) =>
                                                new ColumnDataType(
                                                    ColumnDataType.ColumnName.TakeNamed(first.Name, second.Name),
                                                    DataType.Disjunction(first.DataType, second.DataType).Coalesce(() =>
                                                    {
                                                        var errorReportLocationCandidates = new[] { second.DefiningLocationMaybe, first.DefiningLocationMaybe }.Choose();
                                                        LogError(
                                                            (errorReportLocationCandidates.Any()) ? errorReportLocationCandidates.First() : node.SecondQueryExpression,
                                                            Messages.UnableToJoinTypes(first.DataType.ToString(), second.DataType.ToString())
                                                        );
                                                        
                                                        return UnknownDataType.Instance;
                                                    }),
                                                    Maybe.None<TSqlFragment>()
                                                )
                                        )
                                    )
                                );
                        }
                    )
            );

            string ExpectingRowTypeMessage(DataType dataType) => Messages.ExpectingRowTypeButGot(dataType.ToString());
        }

        public override void ExplicitVisit(QueryDerivedTable node)
        {
            _diagnosticLogger.Enter("QueryDerivedTable");

            node.QueryExpression.Accept(this);
            _currentFrame.WithSymbol(node.Alias.Value, _lastExpressionResult.TypeOfExpression);

            _diagnosticLogger.Exit("QueryDerivedTable");
        }

        public override void ExplicitVisit(SchemaObjectFunctionTableReference node)
        {
            var alias = node.Alias == null ? Names.GetAlias(node.SchemaObject) : node.Alias.Value;
            var name = Names.GetFullTypeName(node.SchemaObject);
            var maybeDataType = _currentFrame.LookupTypeOfSymbolMaybe(name);
            _lastExpressionResult = _lastExpressionResult.WithNewSymbolReferenceAndTypeOfExpression(
                SymbolReference.None,
                maybeDataType.Match(
                    some: dataType =>
                    {
                        if (dataType.ExpressionType is FunctionDataType asFunctionDataType)
                        {
                            CheckPositionalAppliedParameters(
                                asFunctionDataType,
                                node,
                                name,
                                GetAppliedParameterDataTypes(node.Parameters)
                            );

                            return asFunctionDataType.ReturnType_CompilingIfNecessary;
                        }                            
                        else
                        {
                            LogIssue(node, IssueLevel.Error, "Expecting function data type");
                            return RowDataType.EmptyRow;
                        }                          
                    },
                    none: () =>
                    {
                        LogIssue(node, IssueLevel.Warning, Messages.UnknownTypeForBinding(typeName: name, binding: alias));
                        return new RowDataType(new TypeSystem.ColumnDataType[] { });
                    }
                )
            );

            _currentFrame.WithSymbol(alias, _lastExpressionResult.TypeOfExpression);
        }

        public override void ExplicitVisit(NamedTableReference node)
        {
            var alias = node.Alias == null ? Names.GetAlias(node.SchemaObject) : node.Alias.Value;
            _diagnosticLogger.Log($"NamedTableReference: Alias = {alias}");

            var typeName = Names.GetFullTypeName(node.SchemaObject);
            var maybeDataType = _currentFrame.LookupTypeOfSymbolMaybe(typeName);

            _lastExpressionResult = _lastExpressionResult.WithNewSymbolReferenceAndTypeOfExpression(
                SymbolReference.None,  
                maybeDataType.Match(
                    some: dataType => dataType.ExpressionType,
                    none: () =>
                    {
                        LogIssue(node, IssueLevel.Warning, Messages.UnknownTypeForBinding(typeName: typeName, binding: alias));
                        return new RowDataType(new TypeSystem.ColumnDataType[] { });
                    }
                )
            );

            _currentFrame.WithSymbol(alias, _lastExpressionResult.TypeOfExpression);
        }

        #endregion

        #region Call Routines

        public override void ExplicitVisit(FunctionCall node)
        {
            var functionNameIdentifier = node.FunctionName.Value;
            if (String.Equals(functionNameIdentifier, "isnull", StringComparison.InvariantCultureIgnoreCase))
            {
                var parameterDataTypes = GetAppliedParameterDataTypes(node.Parameters);
                if (parameterDataTypes.Length != 2)
                    ErrorApplyingFunction(Messages.CallWithIncorrectNumberOfArguments(functionNameIdentifier, expecting: 2, actual: parameterDataTypes.Length));
                else
                {
                    var anyNotNull = parameterDataTypes.Select(tup => tup.dataType).Any(DataType.CarriesNullValue.Not());
                    var disjunction = DataType.Disjunction(parameterDataTypes[0].dataType, parameterDataTypes[1].dataType).Coalesce(parameterDataTypes[0].dataType);
                    _lastExpressionResult = _lastExpressionResult.WithNewTypeOfExpression(
                        anyNotNull
                            ? NullableDataType.UnwrapIfNull(disjunction)
                            : disjunction
                    );
                }
            }
            else
            {
                _currentFrame.LookupTypeOfSymbolMaybe(functionNameIdentifier).Do(
                    some: (symbolTyping) =>
                    {
                        if (symbolTyping.DeclaredType is FunctionDataType asFunction)
                        {
                            CheckPositionalAppliedParameters(asFunction, routineFragment: node.FunctionName, routineName: node.FunctionName.Value, appliedParameterDataTypes: GetAppliedParameterDataTypes(node.Parameters));
                            _lastExpressionResult = _lastExpressionResult.WithNewTypeOfExpression(asFunction.ReturnType_CompilingIfNecessary);
                        }
                        else
                            ErrorApplyingFunction(Messages.ExpectingFunction);
                    },
                    none: () =>
                        ErrorApplyingFunction(Messages.UnknownFunction(functionNameIdentifier))
                );
                base.ExplicitVisit(node);
            }

            void ErrorApplyingFunction(string message, TSqlFragment location = null)
            {
                LogError(location ?? node.FunctionName, message);
                _lastExpressionResult = _lastExpressionResult.WithNewTypeOfExpression(UnknownDataType.Instance);
            }
        }

        public override void ExplicitVisit(ExecuteStatement node)
        {
            var executableEntity = node.ExecuteSpecification.ExecutableEntity;
            if (executableEntity is ExecutableProcedureReference procedureReference)
            {
                var procedureName = Names.GetFullTypeName(procedureReference.ProcedureReference.ProcedureReference.Name);

                var maybeDataType = _currentFrame.LookupTypeOfSymbolMaybe(procedureName);
                maybeDataType.Do(
                    some: (procedureSymbolTyping) =>
                    {
                        if (procedureSymbolTyping.DeclaredType is StoredProcedureDataType asProcedureDataType)
                        {
                            if (asProcedureDataType.Parameters.Count() != executableEntity.Parameters.Count())
                                LogError(
                                    procedureReference.ProcedureReference.ProcedureReference,
                                    Messages.CallWithIncorrectNumberOfArguments(
                                        procedureName,
                                        expecting: asProcedureDataType.Parameters.Count(),
                                        actual: executableEntity.Parameters.Count()
                                    )
                                );
                            else
                                executableEntity.Parameters.Do(executeParameter =>                                
                                    asProcedureDataType.FindParameterMaybe(executeParameter.Variable.Name)
                                    .Do(
                                        some: declaredParameterForAppliedParameter =>                                        
                                            VisitAndReturnResults(executeParameter.ParameterValue).expressionResult.TypeOfExpression.IsAssignableTo(declaredParameterForAppliedParameter.DataType)
                                                .DoError(message =>
                                                    LogError(executeParameter, Messages.ParameterIssue(parameterName: declaredParameterForAppliedParameter.Name, message: message))
                                                ),
                                        none: () =>                                        
                                            LogError(executeParameter, Messages.ExecuteParameterDoesNotExistForProcedure(procedureName: procedureName))                                       
                                    )                                   
                                );

                            if (asProcedureDataType.ReferencesTempTable)
                            {
                                var prefix = _prefixIssuesWithEvaluationContext;
                                _prefixIssuesWithEvaluationContext = true;
                                var issuesAtApplication = asProcedureDataType.GetIssuesAtApplication(_currentFrame);
                                _prefixIssuesWithEvaluationContext = prefix;

                                if (issuesAtApplication.Any())
                                {
                                    LogError(node, Messages.ProcedureExec(evaluationContext: _evaluationContext.Reverse(), procedureName: procedureName));
                                    _issues.AddRange(issuesAtApplication);
                                }
                            }
                                
                        }
                        else
                            LogIssue(node, IssueLevel.Error, Messages.ExpectingProcedure);
                    },
                    none: () =>
                    {
                        LogIssue(node, IssueLevel.Error, Messages.UnknownProcedure(procedureName));
                    });
            }

            _lastExpressionResult = _lastExpressionResult.WithNewTypeOfExpression(VoidDataType.Instance);
        }

        #endregion

        #region Imperative 

        public override void ExplicitVisit(ReturnStatement node)
        {
            _lastExpressionResult = _lastExpressionResult.WithNewTypeOfExpression(
                VisitAndReturnResults(node.Expression).expressionResult.TypeOfExpression
            );
        }

        public override void ExplicitVisit(StatementList node)
        {
            var typesOfStatements = node.Statements.Select(statement =>
                    (statement, VisitAndReturnResults(statement).expressionResult.TypeOfExpression)
                )
                .Where(tup => !(tup.TypeOfExpression is VoidDataType))
                .ToArray();
            
            _lastExpressionResult = _lastExpressionResult.WithNewTypeOfExpression(
                (typesOfStatements.Length == 0) ? VoidDataType.Instance
                : (typesOfStatements.Length == 1) ? typesOfStatements[0].TypeOfExpression
                : typesOfStatements.Aggregate(
                    typesOfStatements.First().TypeOfExpression,
                    (acc, cur) =>
                        DataType.Disjunction(acc, cur.TypeOfExpression)
                            .Coalesce(() =>
                                LogIssueAndReturn(
                                    cur.statement,
                                    IssueLevel.Error,
                                    Messages.UnableToJoinTypes(acc.ToString(), cur.TypeOfExpression.ToString()),
                                    UnknownDataType.Instance
                                )
                             )
                )
            );
        }

        public override void ExplicitVisit(IfStatement node)
        {
            var (_, predicateExpressionResult) = VisitAndReturnResults(node.Predicate);

            var originalFrame = _currentFrame;

            _currentFrame = originalFrame.NewFrameFromRefinements(predicateExpressionResult.RefinementCases.Positive.Refinements);
            var (_, thenExpressionResult) = VisitAndReturnResults(node.ThenStatement);

            if (node.ElseStatement == null)
                _lastExpressionResult = _lastExpressionResult.WithNewTypeOfExpression(thenExpressionResult.TypeOfExpression);
            else
            {
                _currentFrame = originalFrame.NewFrameFromRefinements(predicateExpressionResult.RefinementCases.Negative.Refinements);
                var (_, elseExpressionResult) = VisitAndReturnResults(node.ElseStatement);
                                
                _lastExpressionResult = _lastExpressionResult.WithNewTypeOfExpression(
                    (thenExpressionResult.TypeOfExpression == VoidDataType.Instance) && (elseExpressionResult.TypeOfExpression == VoidDataType.Instance)
                        ? VoidDataType.Instance
                        : (thenExpressionResult.TypeOfExpression != VoidDataType.Instance) && (elseExpressionResult.TypeOfExpression != VoidDataType.Instance)
                            ? DataType.Disjunction(thenExpressionResult.TypeOfExpression, elseExpressionResult.TypeOfExpression)
                                .Coalesce(() =>
                                    LogIssueAndReturn(
                                        node, 
                                        IssueLevel.Error, 
                                        Messages.UnableToJoinTypes(thenExpressionResult.TypeOfExpression.ToString(), elseExpressionResult.TypeOfExpression.ToString()), 
                                        UnknownDataType.Instance)
                                )
                            : thenExpressionResult.TypeOfExpression ?? elseExpressionResult.TypeOfExpression
                );
            }

            _currentFrame = originalFrame;
        }

        public override void ExplicitVisit(SelectSetVariable node)
        {
            var variableSymbolTypingMaybe = _currentFrame.LookupTypeOfSymbolMaybe(node.Variable.Name);
            var (_, expressionResult) = VisitAndReturnResults(node.Expression);

            variableSymbolTypingMaybe.Do(
                some: (variableSymbolTyping) =>
                    CheckAssignment(
                        source: expressionResult.TypeOfExpression,
                        destination: variableSymbolTyping.DeclaredType,
                        location: node.Variable
                    ),
                none: () =>
                    LogError(node.Variable, Messages.UnableToFindVariable(node.Variable.Name))
            );

            _lastExpressionResult = ExpressionResult.Statement;
        }

        public override void ExplicitVisit(SetVariableStatement node)
        {
            var (_, expressionResult) = VisitAndReturnResults(node.Expression);
            var variableSymbolTypingMaybe = _currentFrame.LookupTypeOfSymbolMaybe(node.Variable.Name);

            var sourceDataType = expressionResult.TypeOfExpression;
            if (expressionResult.TypeOfExpression is RowDataType expressionAsRowType)
            {
                if (expressionAsRowType.ColumnDataTypes.Count() != 1)
                {
                    LogIssue(node.Expression, IssueLevel.Error, Messages.ExpectingASingleExpressionInRow);
                    return;
                }

                sourceDataType = expressionAsRowType.ColumnDataTypes.Single();
            }

            // TODO: Handle type effects of node.AssignmentKind, for example {1, 2} + int => int i.e. we lose precision with operations.
            variableSymbolTypingMaybe.Do(
                some: (variableSymbolTyping) =>
                    CheckAssignment(
                        source: sourceDataType,
                        destination: variableSymbolTyping.DeclaredType,
                        location: node.Variable
                    ),
                none: () =>
                    LogIssue(node.Variable, IssueLevel.Error, Messages.UnableToFindVariable(node.Variable.Name))
            );

            _lastExpressionResult = ExpressionResult.Statement;
        }

        public override void ExplicitVisit(DeclareVariableElement node)
        {
            if (node.Value == null)
                _currentFrame.WithSymbol(node.VariableName.Value, ResolveDataTypeReference(node.DataType).ToNullable());
            else
            {
                var (_, valueExpressionResult) = VisitAndReturnResults(node.Value);
                var symbolTyping = new SymbolTyping(
                    declaredType: ResolveDataTypeReference(node.DataType).ToNullable(),
                    expressionType: valueExpressionResult.TypeOfExpression
                );
                CheckAssignment(
                    source: valueExpressionResult.TypeOfExpression,
                    destination: symbolTyping.DeclaredType,
                    location: node.Value
                );
                
                _currentFrame.WithSymbol(node.VariableName.Value, symbolTyping);
            }
        }

        public override void ExplicitVisit(DeclareTableVariableBody node)
        {
            _currentFrame.WithSymbol(
                node.VariableName.Value,
                new RowDataType(node.Definition.ColumnDefinitions.Select(CreateColumnDataTypeFromColumnDefinition).ToArray())
            );
        }

        #endregion

        #region Type Refining Expression Operators

        private static readonly HashSet<BooleanComparisonType> EqualityBooleanComparisonTypeSet = new HashSet<BooleanComparisonType>(new BooleanComparisonType[]
        {
            BooleanComparisonType.Equals,
            BooleanComparisonType.NotEqualToExclamation,
            BooleanComparisonType.NotEqualToBrackets
        });

        private static readonly HashSet<BooleanComparisonType> InequalityBooleanComparisonTypeSet = new HashSet<BooleanComparisonType>(new BooleanComparisonType[]
        {
            BooleanComparisonType.GreaterThan,
            BooleanComparisonType.LessThan,
            BooleanComparisonType.GreaterThanOrEqualTo,
            BooleanComparisonType.LessThanOrEqualTo,
        });

        public override void ExplicitVisit(BooleanIsNullExpression node)
        {
            var (_, expressionResult) = this.VisitAndReturnResults(node.Expression);

            if ((expressionResult.SymbolReference != SymbolReference.None) && (ColumnDataType.UnwrapIfColumnDataType(expressionResult.TypeOfExpression) is NullableDataType nullableExpressionType))
            {
                var isNotNullRefinement = CreateForType(nullableExpressionType.DataType);
                var isNullRefinement = CreateForType(NullDataType.Instance);

                _lastExpressionResult = new ExpressionResult(
                    SymbolReference.None,
                    SqlDataType.Bit,
                    new RefinementSetCases(
                        positive: node.IsNot ? isNotNullRefinement : isNullRefinement,
                        negative: node.IsNot ? isNullRefinement : isNotNullRefinement
                    )
                );
            }
            else
                _lastExpressionResult = new ExpressionResult(SqlDataType.Bit);

            RefinementSet CreateForType(DataType dataType) =>
                new RefinementSet(new Refinement[] { new Refinement(expressionResult.SymbolReference, dataType) });

        }

        public override void ExplicitVisit(BooleanComparisonExpression node)
        {
            _diagnosticLogger.Enter("BooleanComparisonExpression");

            var (_, leftResult)  = this.VisitAndReturnResults(node.FirstExpression);
            var (_, rightResult) = this.VisitAndReturnResults(node.SecondExpression);

            if (EqualityBooleanComparisonTypeSet.Contains(node.ComparisonType))
            {
                leftResult.TypeOfExpression.CanCompareWith(rightResult.TypeOfExpression)
                    .DoError(error => LogError(node, error));

                var positiveRefinementSet = new RefinementSet(
                    RefineSymbolForEquality(leftResult, rightResult)
                    .Concat(RefineSymbolForEquality(rightResult, leftResult))
                );                    
                var negativeRefinementSet = new RefinementSet(
                    RefineSymbolForInEquality(leftResult, rightResult)
                    .Concat(RefineSymbolForInEquality(rightResult, leftResult))
                );

                var isEqual = node.ComparisonType == BooleanComparisonType.Equals;
                _lastExpressionResult = new ExpressionResult(
                    SymbolReference.None,
                    SqlDataType.Bit,
                    new RefinementSetCases(
                        positive: isEqual ? positiveRefinementSet : negativeRefinementSet,
                        negative: isEqual ? negativeRefinementSet : positiveRefinementSet
                    )
                );
            }
            else if (InequalityBooleanComparisonTypeSet.Contains(node.ComparisonType))
            {
                var leftDataType = ColumnDataType.UnwrapIfColumnDataType(leftResult.TypeOfExpression);
                var rightDataType = ColumnDataType.UnwrapIfColumnDataType(rightResult.TypeOfExpression);
                if (
                    (leftDataType is NullableDataType)
                    || (rightDataType is NullableDataType)
                    || ((leftDataType is NullDataType) ^ (rightDataType is NullDataType))
                )
                    LogError(
                        node,                    
                        Messages.CannotCompareInequality(leftType: leftResult.TypeOfExpression.ToString(), rightType: rightResult.TypeOfExpression.ToString())
                    );

                _lastExpressionResult = _lastExpressionResult.WithNewTypeOfExpression(SqlDataType.Bit);
            }
            else
                _lastExpressionResult = _lastExpressionResult.WithNewTypeOfExpression(SqlDataType.Bit);

            _diagnosticLogger.Exit();
        }

        public override void ExplicitVisit(BinaryExpression node)
        {
            var (_, leftResult) = this.VisitAndReturnResults(node.FirstExpression);
            var (__, rightResult) = this.VisitAndReturnResults(node.SecondExpression);

            var leftType = ColumnDataType.UnwrapIfColumnDataType(leftResult.TypeOfExpression);
            var rightType = ColumnDataType.UnwrapIfColumnDataType(rightResult.TypeOfExpression);

            var nullOperand = (leftType.IsNullOrNullable || rightType.IsNullOrNullable);
            if (nullOperand)
                LogIssue(node, IssueLevel.Warning, Messages.BinaryOperationWithPossibleNull);

            leftType = NullableDataType.UnwrapIfNull(leftType);
            rightType = NullableDataType.UnwrapIfNull(rightType);

            if (!(leftType is SqlDataType leftAsSqlType) || !(rightType is SqlDataType rightAsSqlType) || (leftAsSqlType.SqlDataTypeOption != rightAsSqlType.SqlDataTypeOption))
                LogError(node, Messages.BinaryOperationWithIncompatibleTypes(leftType.ToString(), rightType.ToString()));
            else
            {
                if (node.BinaryExpressionType == BinaryExpressionType.Divide)
                {
                    var divisorIsNotZero = (
                        (rightType is SqlDataTypeWithKnownSet knownSet)
                        && (
                            // either if excludes zero
                            (
                                (!knownSet.Include)
                                && knownSet.Values.Any(val =>
                                    val != null && val.Equals(0)
                                )
                            )
                            ||
                            // OR it includes values and does not contain zero
                            (
                                (knownSet.Include)
                                && !knownSet.Values.Any(val =>
                                    val != null && val.Equals(0)
                                )
                            )
                        )
                    );

                    if (!divisorIsNotZero)
                        // possible divide by zero
                        LogIssue(node.SecondExpression, IssueLevel.Warning, Messages.DivideByZero);

                    _lastExpressionResult = _lastExpressionResult.WithNewTypeOfExpression(leftResult.TypeOfExpression);
                }
                else
                    _lastExpressionResult = _lastExpressionResult.WithNewTypeOfExpression(new SqlDataType(leftAsSqlType.SqlDataTypeOption));                   
            }

        }

        public override void ExplicitVisit(BooleanBinaryExpression node)
        {
            var (_, leftResult) = this.VisitAndReturnResults(node.FirstExpression);

            // set the current frame based on the results of the left
            _currentFrame = _currentFrame.NewFrameFromRefinements(
                (
                    node.BinaryExpressionType == BooleanBinaryExpressionType.And
                        ? leftResult.RefinementCases.Positive
                        : leftResult.RefinementCases.Negative
                ).Refinements
            );

            try
            {
                var (_, rightResult) = this.VisitAndReturnResults(node.SecondExpression);

                var refinementCases = node.BinaryExpressionType == BooleanBinaryExpressionType.And
                    ? RefinementSetCases.Conjunction(leftResult.RefinementCases, rightResult.RefinementCases)
                    : RefinementSetCases.Disjunction(leftResult.RefinementCases, rightResult.RefinementCases);

                _lastExpressionResult = new ExpressionResult(
                    SymbolReference.None,
                    SqlDataType.Bit,
                    refinementCases
                );
            }
            finally
            {
                _currentFrame = _currentFrame.LastFrame;
            }
        }

        public override void ExplicitVisit(SearchedCaseExpression node)
        {
            var originalFrame = _currentFrame;

            RefinementSetCases workingRefinementCases = null;
            IMaybe<DataType> thenDataTypeDisjunction = null;
            node.WhenClauses.Do(whenClause =>
            {
                // anything we have proven thus far should be applied here
                _currentFrame = workingRefinementCases == null ? _currentFrame : _currentFrame.NewFrameFromRefinements(workingRefinementCases.Positive.Refinements);
                var (_, whenConditionExpressionResult) = VisitAndReturnResults(whenClause.WhenExpression);

                // when processsing THEN we do so with refinements of everything we know AND the when
                _currentFrame = _currentFrame.NewFrameFromRefinements(
                    (
                        workingRefinementCases == null
                            ? whenConditionExpressionResult.RefinementCases
                            : RefinementSetCases.Conjunction(
                                workingRefinementCases,
                                whenConditionExpressionResult.RefinementCases
                            )
                    ).Positive.Refinements
                );
                var (_, thenExpressionResult) = VisitAndReturnResults(whenClause.ThenExpression);

                // the refinements are now the existing refinements AND not of when because the when failed or fell through...
                workingRefinementCases = workingRefinementCases == null
                    ? whenConditionExpressionResult.RefinementCases.Negate()
                    : RefinementSetCases.Conjunction(workingRefinementCases, whenConditionExpressionResult.RefinementCases.Negate());
                thenDataTypeDisjunction = thenDataTypeDisjunction == null
                    ? thenExpressionResult.TypeOfExpression.ToMaybe()
                    : thenDataTypeDisjunction.SelectMany(acc => DataType.Disjunction(acc, thenExpressionResult.TypeOfExpression));
            });

            // now process the else clause... 
            _currentFrame = _currentFrame.NewFrameFromRefinements(workingRefinementCases.Positive.Refinements);
            var (_, elseExpressionResult) = VisitAndReturnResults(node.ElseExpression);
            thenDataTypeDisjunction = thenDataTypeDisjunction == null
                ? elseExpressionResult.TypeOfExpression.ToMaybe()
                : thenDataTypeDisjunction.SelectMany(acc => DataType.Disjunction(acc, elseExpressionResult.TypeOfExpression));

            _lastExpressionResult = new ExpressionResult(
                    SymbolReference.None,
                    thenDataTypeDisjunction.Coalesce(UnknownDataType.Instance),
                    RefinementSetCases.Empty
                );
        }

        public override void ExplicitVisit(SimpleCaseExpression node)
        {
            RefinementSetCases refinementsFromWhen = null;
            IMaybe<DataType> thenDataTypeDisjunction = null;

            var (_, inputExpressionResult) = VisitAndReturnResults(node.InputExpression);

            var inputSymbolReference = inputExpressionResult.SymbolReference;
            var inputType = inputExpressionResult.TypeOfExpression;
            var canRefineInputSymbolReference = (inputSymbolReference != SymbolReference.None)
                && ((inputType is NullableDataType) || ((inputType is SqlDataTypeWithKnownSet inputAsWellKnown) && inputAsWellKnown.Include));
            Func<StackFrame> createCurrentFrameFromWhenRefinements =
                canRefineInputSymbolReference
                    ? new Func<StackFrame>(() => refinementsFromWhen == null ? _currentFrame : _currentFrame.NewFrameFromRefinements(refinementsFromWhen.Positive.Refinements))
                    : new Func<StackFrame>(() => _currentFrame);

            node.WhenClauses.Do(whenClause =>
            {
                // anything we have proven thus far is considered during evaluation of WHEN 
                _currentFrame = createCurrentFrameFromWhenRefinements();

                var (_, whenValueExpressionResult) = VisitAndReturnResults(whenClause.WhenExpression);

                var currentRefinementCases = canRefineInputSymbolReference
                    ? new RefinementSetCases(
                        new RefinementSet(RefineSymbolForEquality(inputExpressionResult, whenValueExpressionResult)),
                        new RefinementSet(RefineSymbolForInEquality(inputExpressionResult, whenValueExpressionResult))
                    )
                    : null;

                // when processsing THEN we do so with refinements of everything we know AND the when
                _currentFrame = currentRefinementCases == null
                    ? _currentFrame
                    : _currentFrame.NewFrameFromRefinements(
                        (
                            refinementsFromWhen == null
                                ? currentRefinementCases
                                : RefinementSetCases.Conjunction(
                                    refinementsFromWhen,
                                    currentRefinementCases
                                )
                        ).Positive.Refinements
                    );
                var (_, thenExpressionResult) = VisitAndReturnResults(whenClause.ThenExpression);

                // the refinements are now the existing refinements AND not of when because the when failed or fell through...
                refinementsFromWhen = currentRefinementCases == null ? null
                    : refinementsFromWhen == null ? currentRefinementCases.Negate()
                    : RefinementSetCases.Conjunction(refinementsFromWhen, currentRefinementCases.Negate());
                thenDataTypeDisjunction = thenDataTypeDisjunction == null
                    ? thenExpressionResult.TypeOfExpression.ToMaybe()
                    : thenDataTypeDisjunction.SelectMany(acc => DataType.Disjunction(acc, thenExpressionResult.TypeOfExpression));
            });

            // now process the else clause... 
            _currentFrame = refinementsFromWhen == null ? _currentFrame : _currentFrame.NewFrameFromRefinements(refinementsFromWhen.Positive.Refinements);
            var (_, elseExpressionResult) = VisitAndReturnResults(node.ElseExpression);
            thenDataTypeDisjunction = thenDataTypeDisjunction == null
                ? elseExpressionResult.TypeOfExpression.ToMaybe()
                : thenDataTypeDisjunction.SelectMany(acc => DataType.Disjunction(acc, elseExpressionResult.TypeOfExpression));

            _lastExpressionResult = new ExpressionResult(
                    SymbolReference.None,
                    thenDataTypeDisjunction.Coalesce(UnknownDataType.Instance),
                    RefinementSetCases.Empty
                );
        }

        #endregion

        #region Complex Expressions

        public override void ExplicitVisit(CoalesceExpression node)
        {
            var expressionTypes = node.Expressions.Select(expression => VisitAndReturnResults(expression).expressionResult.TypeOfExpression);
            var anyNotNull = expressionTypes.Any(DataType.CarriesNullValue.Not());
            var disjunction = expressionTypes.Aggregate((acc, type) => DataType.Disjunction(acc, type).Coalesce(UnknownDataType.Instance));

            _lastExpressionResult = _lastExpressionResult.WithNewTypeOfExpression(
                anyNotNull
                    ? NullableDataType.UnwrapIfNull(disjunction)
                    : disjunction
            );
        }

        #endregion

        #region Terminal Values 

        public override void ExplicitVisit(CastCall node)
        {
            var (_, parameterExpression) = VisitAndReturnResults(node.Parameter);
            // TODO: Implement a check against all of the X's found here https://docs.microsoft.com/en-us/sql/t-sql/functions/cast-and-convert-transact-sql?view=sql-server-2017
            _lastExpressionResult = new ExpressionResult(ResolveDataTypeReference(node.DataType));
        }

        public override void Visit(IntegerLiteral node)
        {
            base.Visit(node);
            _lastExpressionResult = new ExpressionResult(SqlDataTypeWithKnownSet.Int(Convert.ToInt32(node.Value)));
        }

        public override void Visit(StringLiteral node)
        {
            base.Visit(node);
            _lastExpressionResult = new ExpressionResult(
                node.IsNational
                    ? SqlDataTypeWithKnownSet.NVarChar(node.Value)
                    : SqlDataTypeWithKnownSet.VarChar(node.Value)
            );
        }

        public override void Visit(NullLiteral node)
        {
            base.Visit(node);
            _lastExpressionResult = new ExpressionResult(NullDataType.Instance);
        }

        public override void Visit(NumericLiteral node)
        {
            base.Visit(node);
            _lastExpressionResult = new ExpressionResult(SqlDataTypeWithKnownSet.Numeric(Convert.ToDecimal(node.Value)));
        }

        public override void Visit(RealLiteral node)
        {
            base.Visit(node);
            _lastExpressionResult = new ExpressionResult(SqlDataTypeWithKnownSet.Real(Convert.ToDouble(node.Value)));
        }

        public override void Visit(MoneyLiteral node)
        {
            base.Visit(node);            
            _lastExpressionResult = new ExpressionResult(SqlDataTypeWithKnownSet.Money(Convert.ToDecimal(node.Value)));
        }

        public override void Visit(GlobalVariableExpression node)
        {
            _lastExpressionResult = _lastExpressionResult.WithNewTypeOfExpression(
                Symbols.GlobalVariableNames.Lookup(node.Name).Coalesce(() => 
                {
                    LogError(node, Messages.UnknownGlobalVariable(node.Name));
                    return UnknownDataType.Instance;
                })
            );
        }

        public override void Visit(VariableReference node)
        {
            base.Visit(node);

            _lastExpressionResult = _currentFrame.LookupTypeOfSymbolMaybe(node.Name)
                .Match(
                    some: (symbolTyping) =>
                        _lastExpressionResult.WithNewSymbolReferenceAndTypeOfExpression(SymbolReference.TopLevelVariable(node.Name), symbolTyping.ExpressionType),
                    none: () =>
                    {
                        LogError(node, Messages.UnableToFindVariable(node.Name));
                        return _lastExpressionResult.WithNewTypeOfExpression(UnknownDataType.Instance);
                    }
                );
        }

        public override void Visit(IdentifierLiteral node)
        {
            base.Visit(node);
            throw new NotImplementedException("IdentifierLiteral");
        }

        public override void Visit(BinaryLiteral node)
        {
            base.Visit(node);
            throw new NotImplementedException("BinaryLiteral");
        }

        public override void Visit(OdbcLiteral node)
        {
            base.Visit(node);
            throw new NotImplementedException("OdbcLiteral");
        }

        public override void Visit(DefaultLiteral node)
        {
            base.Visit(node);            
            throw new NotImplementedException("DefaultLiteral");
        }

        public override void Visit(MaxLiteral node)
        {
            base.Visit(node);
            throw new NotImplementedException("MaxLiteral");
        }

        public override void Visit(ColumnReferenceExpression node)
        {
            base.Visit(node);

            var identifiers = Names.GetIdentifiers(node.MultiPartIdentifier);

            _diagnosticLogger.Log($"ColumnReferenceExpression: {identifiers.Delimit(".")}");

            _lastExpressionResult = _currentFrame.LookupColumnDataTypeByNameMaybe(identifiers)
                .Match(
                    some: (rowAndColumn) => 
                        rowAndColumn.Item2 is ColumnDataType columnType && columnType.Name is ColumnDataType.ColumnName.BaseNamedColumn ncn 
                        ? _lastExpressionResult.WithNewSymbolReferenceAndTypeOfExpression(
                            symbolReference: SymbolReference.Column(rowAndColumn.Item1, columnType),
                            typeOfExpressin: rowAndColumn.Item2
                        )
                        : _lastExpressionResult.WithNewTypeOfExpression(rowAndColumn.Item2),                        
                    none: () =>                    
                        (identifiers.Count() == 1)
                            ? _currentFrame.LookupTypeOfSymbolMaybe(identifiers.First()).Match(
                                some: (typing) => _lastExpressionResult.WithNewSymbolReferenceAndTypeOfExpression(SymbolReference.TopLevelVariable(identifiers.First()), typing.ExpressionType),
                                none: () => UnableToFind()
                            )
                            : UnableToFind()                    
                );

            ExpressionResult UnableToFind()
            {
                LogError(node, Messages.UnableToFindColumnInRow(identifiers.Delimit(".")));
                return _lastExpressionResult.WithNewTypeOfExpression(UnknownDataType.Instance);
            }
        }

        #endregion

        #endregion

        #region Private Accessory 

        private (ScalarExpression sqlFragment, DataType dataType)[] GetAppliedParameterDataTypes(IEnumerable<ScalarExpression> parameters) =>
            parameters
                .Select(parameter =>
                    (sqlFragment: parameter, dataType: VisitAndReturnResults(parameter).expressionResult.TypeOfExpression)
                )
                .ToArray();

        private void CheckPositionalAppliedParameters(SubroutineDataType subRoutine, TSqlFragment routineFragment, string routineName, (ScalarExpression sqlFragment, DataType dataType)[] appliedParameterDataTypes)
        {
            if (subRoutine.Parameters.Count() != appliedParameterDataTypes.Length)
                LogError(
                    routineFragment,
                    Messages.CallWithIncorrectNumberOfArguments(
                        routineName,
                        expecting: subRoutine.Parameters.Count(),
                        actual: appliedParameterDataTypes.Length
                    )
                );
            else
            {
                foreach(var pair in appliedParameterDataTypes.Zip(subRoutine.Parameters, (appliedParameter, subroutineParameter) => (appliedParameter, subroutineParameter)))
                {
                    pair.appliedParameter.dataType.IsAssignableTo(pair.subroutineParameter.DataType)
                        .DoError(message =>
                            LogError(pair.appliedParameter.sqlFragment, Messages.ParameterIssue(parameterName: pair.subroutineParameter.Name, message: message))
                        );
                }
            }
        }

        private void DropObjects(DropObjectsStatement dropStatement)
        {
            var names = dropStatement.Objects.Select(Names.GetFullTypeName);
            var (namesDefined, namesNotDefined) = names.Partition(_currentFrame.ContainsSymbol);

            namesNotDefined.Do(name => LogError(dropStatement, Messages.TableDoesNotExist(name)));
            namesDefined.Do(_currentFrame.RemoveSymbol);

            _lastExpressionResult = _lastExpressionResult.WithNewTypeOfExpression(VoidDataType.Instance);
        }

        /// <summary>
        /// Creates a stored procedure from the Sql AST node but does NOT affect the symbol table.
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        private StoredProcedureDataType CreateStoredProcedureDataType(ProcedureStatementBody node)
        {
            var name = Names.GetFullTypeName(node.ProcedureReference.Name);
            var parameters = node.Parameters.Select(CreateSubroutineParameterFromProcedureParameterFragment);
            var topFrameWhenCompilingBody = _currentFrame;

            return new StoredProcedureDataType(
                    name: name,
                    sqlFragment: node.ProcedureReference,
                    parameters: parameters,
                    referencesTempTable: new Lazy<bool>(() =>
                    {
                        var originalIssues = _issues;

                        _issues = new List<Issue>();
                        var issuesFromTypeCheck = TypeCheckAndGetIssues(topFrameWhenCompilingBody);
                        _issues = originalIssues;

                        var referencesTempTable = issuesFromTypeCheck.Any(issue => Messages.IsTempTableIssue(issue.Message));
                        if (!referencesTempTable)
                            // if it references a temp table then we must type check at application
                            _issues.AddRange(issuesFromTypeCheck);

                        return referencesTempTable;

                    }),
                    getIssuesAtApplication: (stackFrame) => TypeCheckAndGetIssues(stackFrame)
                );


            IEnumerable<Issue> TypeCheckAndGetIssues(StackFrame stackFrame)
            {
                var lastFrame = _currentFrame;

                // create a new frame for the body
                _currentFrame = new StackFrame(stackFrame);
                _evaluationContext.Push(name);
                try
                {
                    // register the parameters so they are in scope during type-check of the body
                    parameters.Do(parameter => _currentFrame.WithSymbol(parameter.Name, parameter.DataType));
                    var (issues, _) = VisitAndReturnResults(node.StatementList);
                    return issues;
                }
                finally
                {
                    _currentFrame = lastFrame;
                    _evaluationContext.Pop();
                }
            }
        }
        
        private IEnumerable<ColumnDataType> SelectElementToColumnTypes(SelectElement selectElement)
        {
            if (selectElement is SelectScalarExpression selectScalarExpression)
            {
                var columName = selectScalarExpression.ColumnName;
                var (_, lastExpressionResult) = this.VisitAndReturnResults(selectScalarExpression.Expression);
                
                if (columName == null)
                {
                    return new [] 
                    {
                        lastExpressionResult.TypeOfExpression is ColumnDataType lastColumnType
                        ? lastColumnType
                        : new ColumnDataType(ColumnDataType.ColumnName.Anonymous.Instance, lastExpressionResult.TypeOfExpression, selectScalarExpression.Expression.ToMaybe())
                    };
                }
                else
                    return Names.GetSymbolName(columName).Match(
                        success: (columnNameAsString) =>
                            new[]
                            {
                                new ColumnDataType(
                                    new ColumnDataType.ColumnName.Aliased(columnNameAsString, _currentFrame.CaseSensitivity),
                                    ColumnDataType.UnwrapIfColumnDataType(lastExpressionResult.TypeOfExpression),
                                    selectScalarExpression.Expression.ToMaybe()
                                )
                            },
                        failure: (message) =>
                        {
                            throw new NotImplementedException(message);
                            //if (columName.ValueExpression is GlobalVariableExpression globalVariableExpression)
                            //    throw new NotImplementedException("Global variable reference in select.");
                        }
                    );
            }
            else if (selectElement is SelectSetVariable selectSetVariable)
            {
                var variableSymbolTypingMaybe = _currentFrame.LookupTypeOfSymbolMaybe(selectSetVariable.Variable.Name);
                var (_, expressionResult) = VisitAndReturnResults(selectSetVariable.Expression);

                variableSymbolTypingMaybe.Do(
                    some: (variableSymbolTyping) =>
                        CheckAssignment(
                            source: expressionResult.TypeOfExpression,
                            destination: variableSymbolTyping.DeclaredType,
                            location: selectSetVariable.Variable
                        ),
                    none: () =>
                        LogError(selectSetVariable.Variable, Messages.UnableToFindVariable(selectSetVariable.Variable.Name))
                );

                return new[]
                {
                    expressionResult.TypeOfExpression is ColumnDataType lastColumnType
                        ? lastColumnType
                        : new ColumnDataType(ColumnDataType.ColumnName.Anonymous.Instance, expressionResult.TypeOfExpression, selectSetVariable.ToMaybe())
                };
            }
            else if (selectElement is SelectStarExpression selectStarExpression)
            {
                return _currentFrame
                    .GetReadTypesInCurrentFrame<RowDataType>()
                    .SelectMany(rowType => rowType.ColumnDataTypes)
                    .ToArray();
            }
            else
                throw new InvalidOperationException($"Unknown select kind {selectElement.GetType().Name}");
        }

        private ColumnDataType CreateColumnDataTypeFromColumnDefinition(ColumnDefinition cd) =>
            new ColumnDataType(
                new ColumnDataType.ColumnName.Schema(cd.ColumnIdentifier.Value, _currentFrame.CaseSensitivity),
                DecorateDataTypeWithConstraints(cd.ColumnIdentifier.Value, cd.Constraints, ResolveDataTypeReference(cd.DataType).ToNullable()),
                cd.ToMaybe()
            );

        private SubroutineDataType.Parameter CreateSubroutineParameterFromProcedureParameterFragment(ProcedureParameter parameter) =>
            new SubroutineDataType.Parameter(
                name: parameter.VariableName.Value,
                dataType: ResolveDataTypeReference(parameter.DataType).Let(dataType =>
                    parameter.Nullable != null ? DecorateDataTypeWithNullableConstraint(parameter.Nullable, dataType) : dataType
                )
            );              

        private IMaybe<DataType> CreateDataTypeFromFunctionReturnType(FunctionReturnType returnType) =>
            returnType is ScalarFunctionReturnType asScalar ? CreateDataTypeFromFunctionReturnType(asScalar).ToMaybe()
            : returnType is SelectFunctionReturnType asSelect ? CreateDataTypeFromFunctionReturnType(asSelect).ToMaybe()
            : returnType is TableValuedFunctionReturnType asTVF ? CreateDataTypeFromFunctionReturnType(asTVF)
            : new ArgumentOutOfRangeException("Expecting scalar, select, or TVF function return type").AsValue<IMaybe<DataType>>();

        private DataType CreateDataTypeFromFunctionReturnType(ScalarFunctionReturnType returnType) =>
            ResolveDataTypeReference(returnType.DataType).ToNullable();

        private DataType CreateDataTypeFromFunctionReturnType(SelectFunctionReturnType returnType) =>
            VisitAndReturnResults(returnType.SelectStatement).expressionResult.TypeOfExpression;

        private IMaybe<DataType> CreateDataTypeFromFunctionReturnType(TableValuedFunctionReturnType returnType) =>
            (returnType.DeclareTableVariableBody?.Definition is TableDefinition)
                ? new RowDataType(returnType.DeclareTableVariableBody.Definition.ColumnDefinitions.Select(CreateColumnDataTypeFromColumnDefinition).ToArray()).ToMaybe()
                : Maybe.None<DataType>();
                        
        private DataType DecorateDataTypeWithConstraints(string columnName, IEnumerable<ConstraintDefinition> constraints, DataType originalDataType) =>
            constraints.FirstOrDefault() is ConstraintDefinition constraint
                ? DecorateDataTypeWithConstraints(columnName, constraints.Skip(1), DecorateDataTypeWithConstraint(columnName, constraint, originalDataType))
                : originalDataType;

        private RowDataType DecorateRowDataTypeWithConstraints(string tableName, IEnumerable<ConstraintDefinition> constraints, RowDataType rowDataType) =>
            constraints.FirstOrDefault() is ConstraintDefinition constraint
                ? DecorateRowDataTypeWithConstraints(tableName, constraints.Skip(1), DecorateRowDataTypeWithConstraint(tableName, constraint, rowDataType))
                : rowDataType;

        private RowDataType DecorateRowDataTypeWithConstraint(string tableName, ConstraintDefinition constraint, RowDataType rowDataType) =>
            constraint is UniqueConstraintDefinition asUniqueConstraint ? DecorateRowDataTypeWithUniqueConstraint(tableName, asUniqueConstraint, rowDataType)
            : rowDataType;

        private RowDataType DecorateRowDataTypeWithUniqueConstraint(string tableName, UniqueConstraintDefinition constraint, RowDataType rowDataType) =>
            constraint.Columns.Count() != 1 ? rowDataType // we only create a domain for primary keys based off a single column
            : Names.GetColumnNameInColumnReference(constraint.Columns.First().Column.MultiPartIdentifier.Identifiers).Let(columnName =>
                new RowDataType(
                    rowDataType.ColumnDataTypes.Select(ct => 
                        ct.Name.Matches(columnName) && ct.DataType is SqlDataType dataTypeAsSqlType
                            ? ct.WithNewDataType(new SqlDataTypeWithDomain(dataTypeAsSqlType.SqlDataTypeOption, $"{tableName}.{columnName}"))
                            : ct
                    )
                )
            );

        private DataType DecorateDataTypeWithConstraint(string columnName, ConstraintDefinition constraint, DataType originalDataType) =>
            (constraint is CheckConstraintDefinition asCheckConstraint) ? DecorateDataTypeWithCheckConstraint(columnName, asCheckConstraint, originalDataType)
            : (constraint is NullableConstraintDefinition asNullableConstraint) ? DecorateDataTypeWithNullableConstraint(asNullableConstraint, originalDataType)
            : (constraint is ForeignKeyConstraintDefinition asForeignKeyConstraint) ? DecorateDataTypeWithForeignKeyConstraint(columnName, asForeignKeyConstraint, originalDataType)
            : originalDataType;

        private DataType DecorateDataTypeWithCheckConstraint(string columnName, CheckConstraintDefinition constraint, DataType originalDataType)
        {
            _currentFrame = new StackFrame(_currentFrame, _currentFrame.CaseSensitivity);
            _currentFrame.WithSymbol(columnName, originalDataType);
            var (_, checkCondition) = VisitAndReturnResults(constraint.CheckCondition);

            var possiblyRefined = checkCondition.RefinementCases.Positive.Refinements.FirstWithVariable(columnName);
            _currentFrame = _currentFrame.LastFrame;

            return possiblyRefined is Some<Refinement> someRefinement
                ? someRefinement.Value.DataType
                : originalDataType;                   
        }

        private DataType DecorateDataTypeWithNullableConstraint(NullableConstraintDefinition constraint, DataType originalDataType) =>
            constraint.Nullable
                ? originalDataType is NullableDataType ? originalDataType : originalDataType.ToNullable()
                : originalDataType is NullableDataType asNullable ? asNullable.DataType : originalDataType;

        private DataType DecorateDataTypeWithForeignKeyConstraint(string columnName, ForeignKeyConstraintDefinition constraint, DataType originalDataType) =>
            // TODO: Need to verify that the referenced column is the primary key of the other table!!!
            constraint.ReferencedTableColumns.Count() != 1
                ? originalDataType
                : SqlDataTypeWithDomain.From(originalDataType, $"{Names.GetFullTypeName(constraint.ReferenceTableName)}.{constraint.ReferencedTableColumns.First().Value}");

        private DataType ResolveDataTypeReference(DataTypeReference reference) =>
            Names.GetFullTypeName(reference.Name)
                .Let(typeName =>
                    SystemTypeNames.Lookup(typeName)
                        .Coalesce(() =>
                            _currentFrame == null ? UnknownDataType.Instance
                            : _currentFrame.LookupTypeOfSymbolMaybe(Names.GetFullTypeName(reference.Name))
                                .Match<DataType>(
                                    some: id => id.ExpressionType,
                                    none: () => UnknownDataType.Instance
                                )
                        )
                );

        private void CheckAssignment(
            DataType source,
            DataType destination,
            TSqlFragment location
        )
        {
            source.IsAssignableTo(destination).DoError(error =>
                LogIssue(
                    location,
                    IssueLevel.Error,
                    Messages.CannotAssignTo(source.ToString(), destination.ToString())
                )
            );
        }

        private static IEnumerable<Refinement> RefineSymbolForEquality(ExpressionResult potentialSymbolCandidate, ExpressionResult otherExpression) =>
            (potentialSymbolCandidate.SymbolReference != SymbolReference.None) && (potentialSymbolCandidate.TypeOfExpression.SizeOfDomain > otherExpression.TypeOfExpression.SizeOfDomain)
                ? new Refinement(potentialSymbolCandidate.SymbolReference, otherExpression.TypeOfExpression).ToEnumerable()
                : new Refinement[] { };

        private static IEnumerable<Refinement> RefineSymbolForInEquality(ExpressionResult potentialSymbolCandidate, ExpressionResult otherExpression) =>
            (potentialSymbolCandidate.SymbolReference != SymbolReference.None)
                ? new Refinement(potentialSymbolCandidate.SymbolReference, DataType.Subtract(potentialSymbolCandidate.TypeOfExpression, otherExpression.TypeOfExpression)).ToEnumerable()
                : new Refinement[] { };

        private void ExpectTypeEvaluatingExpression<TValue, TExpected>(TSqlFragment node, Func<TValue, string> errorMessage, TValue val, Action<TExpected> whenOfProperType)
        {
            if (val is TExpected asExpected)
                whenOfProperType(asExpected);
            else
            {
                LogError(node, errorMessage(val));
                _lastExpressionResult = _lastExpressionResult.WithNewTypeOfExpression(UnknownDataType.Instance);
            }
        }

        private void LogIssue(TSqlFragment location, IssueLevel issueLevel, string message)
        {
            if (_prefixIssuesWithEvaluationContext)
                message = $"When called from {_evaluationContext.ToArray().Reverse().Delimit(": ")}: {message}";
            _issues.Add(new Issue(location, issueLevel, message));
        }

        private void LogError(TSqlFragment location, string error)
        {
            LogIssue(location, IssueLevel.Error, error);
        }

        private T LogIssueAndReturn<T>(TSqlFragment location, IssueLevel issueLevel, string message, T val)
        {
            LogIssue(location, issueLevel, message);
            return val;
        }

        #endregion
    }
}
