using System;
using System.Collections.Generic;
using System.Linq;
using LowSums;
using TSqlStrong.TypeSystem;

namespace TSqlStrong.Symbols
{
    /// <summary>
    /// This is the symbol stack. It is a linked list of frames. 
    /// </summary>
    public class StackFrame
    {
        private readonly StackFrame _lastFrame = null;
        private readonly CaseSensitivity _caseSensitivity = CaseSensitivity.CaseInsensitive;
        private Dictionary<string, SymbolTyping> _frameSymbols = new Dictionary<string, SymbolTyping>();

        public StackFrame() { }

        public StackFrame(StackFrame lastFrame) : this()
        {
            _lastFrame = lastFrame;
            _caseSensitivity = lastFrame != null ? lastFrame.CaseSensitivity : CaseSensitivity.CaseInsensitive;
        }

        public StackFrame(StackFrame lastFrame, CaseSensitivity overrideCaseSensitivity) : this(lastFrame)
        {
            _caseSensitivity = overrideCaseSensitivity;
        }

        public CaseSensitivity CaseSensitivity => _caseSensitivity;

        public IEnumerable<KeyValuePair<string, SymbolTyping>> CurrentFrameSymbols => _frameSymbols;

        public StackFrame LastFrame => _lastFrame;

        public int Depth => _lastFrame == null ? 0 : 1 + _lastFrame.Depth;

        public override string ToString() =>
            $"Depth: {Depth.ToString()}; Symbols: [{String.Join(", ", CurrentFrameSymbols.Select(it => it.Key).ToArray())}]";

        public StackFrame Clone()
        {
            var newFrame = new StackFrame(LastFrame, CaseSensitivity);
            _frameSymbols.Do((keyValue) => newFrame._frameSymbols.Add(keyValue.Key, keyValue.Value));
            return newFrame;
        }

        public bool ContainsSymbol(string symbolName) =>
            LookupTypeOfSymbolMaybe(symbolName).Match(
                some: (_) => true,
                none: () => false
            );

        public StackFrame WithSymbol(string symbolName, TypeSystem.DataType dataType) =>
            WithSymbol(symbolName, new SymbolTyping(dataType));

        public StackFrame ReplaceSymbol(string symbolName, TypeSystem.DataType dataType) =>
            ReplaceSymbol(symbolName, new SymbolTyping(dataType));

        public StackFrame WithSymbol(string symbolName, SymbolTyping symbolTyping)
        {
            Ast.Names.GetTopLevelNames(symbolName)
                .Do(name =>
                {
                    var key = name.ToCaseSensitivityNormalizedString(_caseSensitivity);
                    if (_frameSymbols.ContainsKey(key))
                    {
                        var existing = _frameSymbols[key];
                        throw new InvalidOperationException($"In stack frame, key '{key}' already exists as '{existing.ToString()}'. Attemtping to add symbol typing {symbolTyping.ToString()}");
                    }
                    _frameSymbols.Add(key, symbolTyping);
                });

            return this;
        }

        public StackFrame ReplaceSymbol(string symbolName, SymbolTyping symbolTyping)
        {
            Ast.Names.GetTopLevelNames(symbolName)
                .Do(name =>
                {
                    var key = name.ToCaseSensitivityNormalizedString(_caseSensitivity);
                    _frameSymbols[key] = symbolTyping;
                });

            return this;
        }

        public void RemoveSymbol(string symbolName) =>
            Ast.Names.GetTopLevelNames(symbolName)
                .Do(name =>
                {
                    var key = name.ToCaseSensitivityNormalizedString(_caseSensitivity);
                    if (_frameSymbols.ContainsKey(key))
                        _frameSymbols.Remove(key);
                });            

        public IMaybe<SymbolTyping> LookupTypeOfSymbolMaybe(string symbolName) =>
            String.IsNullOrWhiteSpace(symbolName) ? Maybe.None<SymbolTyping>()
                 : (_frameSymbols.TryGetValue(symbolName.ToCaseSensitivityNormalizedString(_caseSensitivity), out SymbolTyping value)) ? Maybe.Some(value)
                 : (_lastFrame == null) ? Maybe.None<SymbolTyping>()
                 : this._lastFrame.LookupTypeOfSymbolMaybe(symbolName);

        public IEnumerable<T> GetReadTypesInCurrentFrame<T>() where T : TypeSystem.DataType => _frameSymbols.Values.Select(it => it.ExpressionType).OfType<T>();

        public IMaybe<(string, ColumnDataType)> LookupColumnDataTypeByNameMaybe(params string[] identifiers) =>
            identifiers.Length == 1
                ? LookupUnQualifiedColumnDataType(identifiers.First())
                : LookupQualifiedColumnDataType(identifiers.Take(identifiers.Length - 1).Delimit("."), identifiers.Last());

        public void RefineInPlace(IEnumerable<Refinement> refinements)
        {
            refinements
                .GroupBy(refinement =>
                    refinement.Reference is TopLevelVariableReference variable ? variable.Variable
                    : refinement.Reference is ColumnReference columnReference ? columnReference.RowReference
                    : String.Empty
                )
                .Where(grouping => !String.IsNullOrWhiteSpace(grouping.Key))
                .Do(refinementsGroupedBySymbolEntry =>
                {
                    var symbolTableName = refinementsGroupedBySymbolEntry.Key;
                    this.LookupTypeOfSymbolMaybe(symbolTableName)
                        .Match(
                            some: (symbolTyping) =>
                            {
                                ReplaceSymbol(
                                    symbolTableName,
                                    symbolTyping.ExpressionType is RowDataType rowDataType
                                        ? RefineRow(rowDataType, refinementsGroupedBySymbolEntry)
                                        : RefineVariable(symbolTyping.ExpressionType, refinementsGroupedBySymbolEntry)
                                );

                                return Unit.unit;
                            },
                            none: () =>
                            {
                                throw new InvalidOperationException($"Unable to find symbol {symbolTableName} in symbol table.");
                            }
                        );
                });

            DataType RefineVariable(DataType originalDataType, IEnumerable<Refinement> variableRefinements) =>
                variableRefinements.Aggregate(
                    TypeSystem.ColumnDataType.UnwrapIfColumnDataType(originalDataType),
                    (currentType, refinement) => refinement.DataType.Refine(currentType)
                );

            DataType RefineRow(RowDataType originalRow, IEnumerable<Refinement> columnRefinements)
            {
                var refinementsGroupedByName = columnRefinements
                    .Where(refinement =>
                        refinement.Reference.Match(
                            topLevelVariable: (_) => false,
                            column: (_, columnDataType) =>
                                columnDataType.Name is ColumnDataType.ColumnName.BaseNamedColumn
                        )
                    )
                    .GroupBy(refinement => ((refinement.Reference as ColumnReference).ColumnDataType.Name as ColumnDataType.ColumnName.BaseNamedColumn).Name);

                var columnNameToRefinement = refinementsGroupedByName
                    .Select(grouping =>
                    {
                        return grouping.Aggregate((acc, cur) =>
                            acc.DataType.SizeOfDomain < cur.DataType.SizeOfDomain
                                ? acc
                                : cur
                        );
                    })
                    .ToDictionary(getNameOfRefinement);

                return originalRow.MapNamedColumns((columnName, originalDataType) =>
                    (columnNameToRefinement.TryGetValue(columnName, out Refinement columnRefinement))
                    ? Maybe.Some(columnRefinement.DataType) //.Refine(originalDataType))
                    : Maybe.None<DataType>()
                );

                string getNameOfRefinement(Refinement refinement) =>
                    ((refinement.Reference as ColumnReference).ColumnDataType.Name as ColumnDataType.ColumnName.BaseNamedColumn).Name;
            }
        }

        public StackFrame NewFrameFromRefinements(IEnumerable<Refinement> refinements)
        {
            var newFrame = new StackFrame(this);
            newFrame.RefineInPlace(refinements);
            return newFrame;
        }

        public void PerformTopLevelTypeCheckOfStoredProcedures()
        {
            GetReadTypesInCurrentFrame<StoredProcedureDataType>().Do(sproc => sproc.PerformTopLevelTypeCheck());
        }

        public IEnumerable<VerificationResults.Issue> GetIssuesFromCompilingFunctionBodies() =>            
            GetReadTypesInCurrentFrame<FunctionDataType>().SelectMany(functionDataType =>
            { 
                var (declaredMaybe, bodyExpression) = functionDataType.GetReturnTypes_ForcingCompilationIfNotAlready();
                return declaredMaybe.Match<IEnumerable<TSqlStrong.VerificationResults.Issue>>(
                    none: () =>
                        new TSqlStrong.VerificationResults.Issue[] { },
                    some: (declared) =>
                        bodyExpression.IsAssignableTo(declared).Match(
                            success: (_) =>
                                new TSqlStrong.VerificationResults.Issue[] { },
                            failure: (error) =>
                                new TSqlStrong.VerificationResults.Issue(
                                    functionDataType.SqlFragment,
                                    TSqlStrong.VerificationResults.IssueLevel.Error,
                                    error
                                ).ToEnumerable()
                        )
                );
            });

        private IEnumerable<(string SymbolName, RowDataType RowType)> GetRowTypes() =>
            _frameSymbols
            .Where(it => it.Value.ExpressionType is RowDataType)
            .Select(it => (SymbolName: it.Key, RowType: it.Value.ExpressionType as RowDataType));

        private IMaybe<(string, ColumnDataType)> LookupUnQualifiedColumnDataType(string columnName) =>
            GetRowTypes()
            .SelectFirst(rowNameRowTypePair =>
                rowNameRowTypePair.RowType
                    .FindColumn(columnName)
                    .Select(columnDataType => (rowNameRowTypePair.SymbolName, columnDataType))
            )
            .SelectManyNone(() =>
                (_lastFrame == null)
                    ? Maybe.None<(string, ColumnDataType)>()
                    : this._lastFrame.LookupUnQualifiedColumnDataType(columnName)
            );

        private IMaybe<(string, ColumnDataType)> LookupQualifiedColumnDataType(string rowReference, string columnName) =>
            LookupTypeOfSymbolMaybe(rowReference)
                .SelectMany(symbolTyping =>
                    (symbolTyping.ExpressionType is RowDataType rowType)
                        ? rowType.FindColumn(columnName).Select(columnDataType => (rowReference, columnDataType))
                        : Maybe.None<(string, ColumnDataType)>()
                );
    }
}
