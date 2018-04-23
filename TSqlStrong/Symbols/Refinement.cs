using System.Linq;
using System.Collections.Generic;
using LowSums;
using TSqlStrong.TypeSystem;

namespace TSqlStrong.Symbols
{
    /// <summary>
    /// Knowledge gathered through flow analysis of a refined data type for a single symbol reference. 
    /// </summary>
    public class Refinement
    {
        private readonly ISymbolReference _reference;
        private readonly DataType _dataType;

        public Refinement(ISymbolReference reference, DataType dataType)
        {
            _reference = reference;
            _dataType = dataType;
        }

        public ISymbolReference Reference => _reference;
        public DataType DataType => _dataType;

        public Refinement WithNewDataType(DataType newType) =>
            new Refinement(_reference, newType);

        public override string ToString() => $"{Reference.ToString()}={DataType.ToString()}";

        public override bool Equals(object obj)
        {
            return obj is Refinement objAsRefinement
                ? objAsRefinement.Reference.Equals(_reference)
                    && objAsRefinement.DataType.Equals(DataType)
                : false;
        }

        public override int GetHashCode()
        {
            return Reference.GetHashCode() * 19 + DataType.GetHashCode();
        }

        public static Refinement FromTupe((ISymbolReference, DataType) tupe) => new Refinement(tupe.Item1, tupe.Item2);

        public static IMaybe<Refinement> Conjunction(ISymbolReference reference, DataType left, DataType right) =>
            DataType.Conjunction(left, right).Select(dt => new Refinement(reference, dt));

        public static IMaybe<Refinement> NegationOfConjunction(ISymbolReference reference, DataType left, DataType right) =>
            DataType.NegationOfConjunction(left, right).Select(dt => new Refinement(reference, dt));
        
        public static IMaybe<Refinement> Disjunction(ISymbolReference reference, DataType left, DataType right) =>
            DataType.Disjunction(left, right).Select(dataType => new Refinement(reference, dataType));

        public static IMaybe<Refinement> NegationOfDisjunction(ISymbolReference reference, DataType left, DataType right) =>
            Maybe.None<Refinement>();

        public static Refinement[] FromVariableNames(params (string, DataType)[] refinements)
        {
            return refinements.Select((tup) => new Refinement(SymbolReference.TopLevelVariable(tup.Item1), tup.Item2)).ToArray();
        }
    }

    public static class RefinementExtensions
    {
        public static IMaybe<Refinement> FirstWithVariable(this IEnumerable<Refinement> refinements, string variableName) =>
            refinements.SelectFirst(refinement =>
                refinement.Reference.Match(
                    topLevelVariable: (variable) => 
                        string.Compare(variable, variableName, System.StringComparison.InvariantCultureIgnoreCase) == 0
                            ? refinement.ToMaybe()
                            : Maybe.None<Refinement>(),
                    column: (_, __) => Maybe.None<Refinement>()
                )
            );
    }
}
