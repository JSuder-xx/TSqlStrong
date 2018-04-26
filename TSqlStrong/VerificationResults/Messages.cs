using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LowSums;

namespace TSqlStrong.VerificationResults
{
    public static class Messages
    {
        public const string DivideByZero = "Divide by zero";

        public const string ExpectingASingleExpressionInRow = "Expecting a single expression in row.";

        public const string BinaryOperationWithPossibleNull = "Attempted a binary operation (+, -, /, *) with a possibly null value.";

        public const string ColumnCountMismatchInUnion = "Column count mismatch in union";

        public const string ColumnCountDoesNotMatchCTE = "Column count does not match the specification of CTE.";

        public static string UnknownGlobalVariable(string variableName) => $"Unknown global variable {variableName}.";

        public static string UnknownCursor(string name) => $"Unknown cursor {name}";

        public static string TooManyVariablesSpecifiedForFetch(int columnsInCursor, int variableCount) =>
            $"Fetch specified {variableCount} variables but the cursor only has {columnsInCursor} columns";

        public static string TableAlreadyExists(string tableName) =>
            $"Table {tableName} already exists in the current scope.";

        public static string TableDoesNotExist(string tableName) =>
            $"Table {tableName} does not exist in the current scope.";

        public const string ExpectingFunction = "Expecting function.";
        public const string ExpectingProcedure = "Expecting procedure.";
        public static string ProcedureExec(IEnumerable<string> evaluationContext, string procedureName) =>
            $"Error executing procedure {procedureName} in the context of ({evaluationContext.Delimit(":")}). This probably means that temporary table structure as defined as this point are incompatible with the structure expected by {procedureName} or something {procedureName} calls.";

        public static string UnableToJoinTypes(string left, string right) =>
            $"Unable to join types {left} and {right}";

        public static string CallWithIncorrectNumberOfArguments(string functionName, int actual, int expecting) =>
            $"Expected {expecting} arguments but received {actual} for function '{functionName}'.";

        public static string BinaryMathWithIncompatibleTypes(string leftType, string rightType) =>
            $"Binary math with incompatible values of type {leftType} and {rightType}.";

        public static string ParameterIssue(string parameterName, string message) =>
            $"Parameter {parameterName}: {message}";

        public static string ExecuteParameterDoesNotExistForProcedure(string procedureName) =>
            $"Not a parameter of {procedureName}";

        public static string UnknownFunction(string functionName) =>
            $"Unknown function {functionName}.";

        public static string UnknownProcedure(string procName) =>
            $"Unknown procedure {procName}.";

        public static string CannotAssignColumnName(string source, string destination) =>
            $"Cannot assign column {source} to {destination}";

        public static string UnknownTypeForBinding(string typeName, string binding) =>
            $"Unknown type {typeName} in trying to bind to {binding}";

        public static string UnableToFindColumn(string columnName) =>
            $"Unable to find column {columnName}";

        public static string UnableToFindVariable(string variableName) =>
            $"Unable to find variable {variableName}";

        public static bool IsTempTableIssue(string message) =>
            // TODO: Replace with regex
            message.ToLower().Contains("unknown type #");

        public static string CannotAssignRowToAnotherDueToMissingColumn(string columnName) =>
            $"Target row does not include {columnName}";

        public static string CannotAssignTo(string from, string dest) =>
            $"Cannot assign value of type {from} to {dest}.";

        public static string CannotCompare(string from, string dest, string because = "") =>
            $"Cannot compare {from} and {dest}{(String.IsNullOrEmpty(because) ? "" : $" because <<{because}>>")}.";

        public static string UnknownRowType(string rowTypeName) =>
            $"Row type {rowTypeName} not found.";

        public static string ExpectingRowTypeButGot(string typeName) =>
            $"Expecting a row type but got {typeName}.";

        public static string UnableToFindColumnInRow(string columnName) =>
            $"Unable to find column {columnName}";

        public static string ColumnsOfSameNameAreNotAssignable(string columnName) =>
            $"Columns of same name {columnName} are not assignable.";

        public static string CannotCompareInequality(string leftType, string rightType) =>
            $"Cannot compare inequalities of {leftType} and {rightType}.";
    }
}
