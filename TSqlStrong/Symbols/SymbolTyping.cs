using TSqlStrong.TypeSystem;

namespace TSqlStrong.Symbols
{
    /// <summary>
    /// The full typing knowledge for a symbol.
    /// </summary>
    public class SymbolTyping
    {
        private readonly DataType _declaredType;
        private readonly DataType _expressionType;

        /// <summary>
        /// The typing knowledge for a symbol.
        /// </summary>
        /// <param name="declaredType">The T-SQL defined data type used when symbol is assigned to.</param>
        /// <param name="expressionType">The refined knowledge of this type used when this symbol is in an expression.</param>
        public SymbolTyping(DataType declaredType, DataType expressionType)
        {
            _declaredType = declaredType;
            _expressionType = expressionType;
        }

        public SymbolTyping(DataType declaredType) : this(declaredType, null)
        {
        }

        public override string ToString() =>
            DeclaredType == ExpressionType
                ? $"({ExpressionType.ToString()})"
                : $"(Declared: {DeclaredType}, Expr: {ExpressionType})";

        /// <summary>
        /// The type this symbol was declared to be. This is also called the IN type as it controls what
        /// can be ASSIGNED TO this symbol or when this symbol is on the left side of an assignment.
        /// </summary>
        public DataType DeclaredType => _declaredType;

        /// <summary>
        /// The type this symbol was refined to be (through flow typing) in the current context. This is also called
        /// the OUT type as it dictates the type of the value when it is an expression or when this symbol is on the right
        /// side of an assignment.
        /// </summary>
        public DataType ExpressionType => _expressionType ?? DeclaredType;
    }
}
