using System;
using System.Collections.Generic;
using System.Linq;
using LowSums;

using TSqlStrong.TypeSystem;

namespace TSqlStrong.Symbols
{
    /// <summary>
    /// The collected knowledge of type refinements for symbols. 
    /// </summary>
    public class RefinementSet
    {
        /// <summary>Null/zero/identity object with no refinements</summary>
        public static readonly RefinementSet Empty = new RefinementSet(new Refinement[] { });

        private readonly IEnumerable<Refinement> _refinements;

        public RefinementSet(IEnumerable<Refinement> refinements)
        {
            _refinements = refinements.ToArray();
        }

        public RefinementSet(params (string, DataType)[] refinements)
        {
            _refinements = Refinement.FromVariableNames(refinements);
        }

        public static RefinementSet From(params (string, DataType)[] refinements)
        {
            return new RefinementSet(refinements);
        }

        public IEnumerable<Refinement> Refinements => _refinements.ToArray();

        public override string ToString() => $"Refinements({Refinements.CommaDelimit()})";

        /// <summary>
        /// Join a left and a right refinement set together.
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <param name="outer">Produce a refinement from a single refinement that belongs to either the left or the right BUT not both. You will want to return either nothing or this refinement.</param>
        /// <param name="inner">Produce refinement by combining both</param>
        /// <returns></returns>
        public static RefinementSet Join(
            RefinementSet left, 
            RefinementSet right,
            Func<Refinement, IMaybe<Refinement>> outer,
            Func<ISymbolReference, DataType, DataType, IMaybe<Refinement>> inner
        ) =>
            new RefinementSet(
                left.Refinements
                .Concat(right.Refinements)
                .GroupBy(refinement => refinement.Reference)
                .SelectMany(refinementsGroupedByName =>
                {
                    var refinementsArray = refinementsGroupedByName.ToArray();
                    return refinementsArray.Length == 1
                        ? outer(refinementsArray[0]).ToEnumerable()
                        : inner(refinementsArray[0].Reference, refinementsArray[0].DataType, refinementsArray[1].DataType).ToEnumerable();
                })
            );
    }
}