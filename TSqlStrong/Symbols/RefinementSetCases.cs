using LowSums;

namespace TSqlStrong.Symbols
{
    /// <summary>
    /// Representation of the refinement knowledge for both the positive and negative cases.
    /// </summary>
    public class RefinementSetCases
    {
        public static readonly RefinementSetCases Empty = new RefinementSetCases(RefinementSet.Empty, RefinementSet.Empty);
        private readonly RefinementSet _positive;
        private readonly RefinementSet _negative;

        public RefinementSetCases(RefinementSet positive, RefinementSet negative)
        {
            _positive = positive;
            _negative = negative;
        }
        public RefinementSet Positive => _positive;
        public RefinementSet Negative => _negative;

        public RefinementSetCases Negate() => new RefinementSetCases(Negative, Positive);

        public RefinementSetCases Conjunction(RefinementSetCases other) =>
            Conjunction(this, other);

        public RefinementSetCases Disjunction(RefinementSetCases other) =>
            Disjunction(this, other);

        public static RefinementSetCases Conjunction(RefinementSetCases left, RefinementSetCases right) =>
            new RefinementSetCases(
                RefinementSet.Join(
                    left.Positive, 
                    right.Positive,
                    outer: (refinement) => refinement.ToMaybe(),
                    inner: (reference, leftType, rightType) => Refinement.Conjunction(reference, leftType, rightType)
                ),

                RefinementSet.Join(
                    left.Positive,
                    right.Positive,
                    outer: (refinement) => Maybe.None<Refinement>(), // symbols in either left or right but not both LOSE their refinement due to the indecision
                    inner: (reference, leftType, rightType) => Refinement.NegationOfConjunction(reference, leftType, rightType)
                )
            );

        public static RefinementSetCases Disjunction(RefinementSetCases left, RefinementSetCases right) =>
            new RefinementSetCases(
                RefinementSet.Join(
                    left.Positive,
                    right.Positive,
                    outer: (refinement) => Maybe.None<Refinement>(), // symbols in either left or right but not both LOSE their refinement due to the indecision
                    inner: (reference, leftType, rightType) => Refinement.Disjunction(reference, leftType, rightType)
                ),
                RefinementSet.Join(
                    left.Positive,
                    right.Positive,
                    outer: (refinement) =>                         
                        // DeMorgan's
                        refinement.DataType is TypeSystem.KnownSetDecoratorDataType wellKnown
                            ? refinement.WithNewDataType(wellKnown.Invert()).ToMaybe()
                            : Maybe.None<Refinement>(),
                    inner: (reference, leftType, rightType) => Refinement.NegationOfDisjunction(reference, leftType, rightType)
                )
            );
    }
}
