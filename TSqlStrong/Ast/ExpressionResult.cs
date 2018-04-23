using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TSqlStrong.TypeSystem;
using TSqlStrong.Symbols;

namespace TSqlStrong.Ast
{
    public class ExpressionResult
    {
        private readonly ISymbolReference _symbolReference;
        private readonly DataType _typeOfExpression;
        private readonly RefinementSetCases _refinementCases;

        public ExpressionResult(ISymbolReference symbolReference, DataType typeOfExpression, RefinementSetCases refinementCases)
        {
            _symbolReference = symbolReference;
            _typeOfExpression = typeOfExpression;
            _refinementCases = refinementCases;
        }

        public ExpressionResult(DataType typeOfExpression) : this(Symbols.SymbolReference.None, typeOfExpression, RefinementSetCases.Empty)
        { }

        public ExpressionResult(ISymbolReference symbolReference, DataType typeOfExpression) : this(symbolReference, typeOfExpression, RefinementSetCases.Empty)
        { }

        public ISymbolReference SymbolReference => _symbolReference;
        
        /// <summary>
        /// The type of the expression itself.
        /// </summary>
        public DataType TypeOfExpression => _typeOfExpression;
        
        /// <summary>
        /// Any refinements we can make to symbols based upon this expression (only makes sense if TypeOfExpresison is boolean)
        /// </summary>
        public RefinementSetCases RefinementCases => _refinementCases;
        
        public ExpressionResult WithNewTypeOfExpression(DataType typeOfExpression)
        {
            return new ExpressionResult(this.SymbolReference, typeOfExpression, this.RefinementCases);
        }

        public ExpressionResult WithNewSymbolReferenceAndTypeOfExpression(ISymbolReference symbolReference, DataType typeOfExpressin)
        {
            return new ExpressionResult(symbolReference, typeOfExpressin, this.RefinementCases);
        }
    }
}
