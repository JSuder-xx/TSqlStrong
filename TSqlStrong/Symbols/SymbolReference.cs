using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TSqlStrong.TypeSystem;

namespace TSqlStrong.Symbols
{
    /// <summary>
    /// OO discriminated union for a symbol reference with constructors TopLevelVariable | Column
    /// </summary>
    public interface ISymbolReference
    {
        TResult Match<TResult>(Func<string, TResult> topLevelVariable, Func<string, ColumnDataType, TResult> column);
    }

    /// <summary>
    /// Provides the factory (constructors) for ISymbolReference.
    /// </summary>
    public static class SymbolReference
    {
        public static ISymbolReference TopLevelVariable(string variableRef) => new TopLevelVariableReference(variableRef);
        public static ISymbolReference Column(string rowReference, ColumnDataType columnDataType) => new ColumnReference(rowReference, columnDataType);
        public static readonly ISymbolReference None = new TopLevelVariableReference("???");

        public static (IEnumerable<TopLevelVariableReference>, IEnumerable<ColumnReference>) Split(this IEnumerable<ISymbolReference> references) =>
            (references.OfType<TopLevelVariableReference>(), references.OfType<ColumnReference>());
    }
    
    public class TopLevelVariableReference : ISymbolReference
    {
        private readonly string _variable;
        public TopLevelVariableReference(string variable)
        {
            _variable = variable;
        }

        public string Variable => _variable;

        public override string ToString() => $"VariableReference({Variable})";

        public override bool Equals(object obj) =>
            obj is TopLevelVariableReference otherTopLevelVarialeRefeference
                ? String.Equals(otherTopLevelVarialeRefeference.Variable, _variable, StringComparison.InvariantCultureIgnoreCase)
                : base.Equals(obj);

        public override int GetHashCode() => _variable.GetHashCode();        

        public TResult Match<TResult>(Func<string, TResult> topLevelVariable, Func<string, ColumnDataType, TResult> column)
        {
            return topLevelVariable(this._variable);
        }
    }

    public class ColumnReference : ISymbolReference
    {
        private readonly string _rowReference;
        private readonly ColumnDataType _columnDataType;

        public ColumnReference(string rowReference, ColumnDataType columnDataType)
        {
            _rowReference = rowReference;
            _columnDataType = columnDataType;
        }

        public string RowReference => _rowReference;
        public ColumnDataType ColumnDataType => _columnDataType;

        public override int GetHashCode() => (_rowReference.GetHashCode() * 29) + _columnDataType.GetHashCode();

        public override string ToString() => $"ColumnReference({RowReference}, {ColumnDataType.Name})";

        public override bool Equals(object obj) =>
            obj is ColumnReference otherRef
                ? otherRef._columnDataType.Equals(_columnDataType) && String.Equals(otherRef._rowReference, _rowReference, StringComparison.InvariantCultureIgnoreCase)
                : base.Equals(obj);

        public TResult Match<TResult>(Func<string, TResult> topLevelVariable, Func<string, ColumnDataType, TResult> column)
        {
            return column(_rowReference, _columnDataType);
        }
    }

}
