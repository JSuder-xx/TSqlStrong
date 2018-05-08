using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using FluentAssertions;

using ScriptDom = Microsoft.SqlServer.TransactSql.ScriptDom;

using LowSums;
using TSqlStrong.Ast;
using TSqlStrong.TypeSystem;
using TSqlStrong.Symbols;
using TSqlStrongSpecifications.Builders;

namespace TSqlStrongSpecifications
{
#pragma warning disable IDE1006 // Naming Styles
    class describe_RefinementSet : NSpec.nspec
    {
        #region descriptions

        public void describe_Negation()
        {
        }

        public void describe_Conjunction()
        {
            WhenTakingConjunction(
                of: RefinementSet.Empty,
                with: RefinementSet.Empty,
                itProduces: Refinement.FromVariableNames()
            );

            WhenTakingConjunction(
                of: RefinementSet.From(("x", SqlDataType.Int)),
                with: RefinementSet.Empty,
                itProduces: Refinement.FromVariableNames(("x", SqlDataType.Int))
            );

            WhenTakingConjunction(
                of: RefinementSet.Empty,
                with: RefinementSet.From(("x", SqlDataType.Int)),
                itProduces: Refinement.FromVariableNames(("x", SqlDataType.Int))
            );

            WhenTakingConjunction(
                of: RefinementSet.From(("x", SqlDataType.Int)),
                with: RefinementSet.From(("y", SqlDataType.Int)),
                itProduces: Refinement.FromVariableNames(("x", SqlDataType.Int), ("y", SqlDataType.Int))
            );

            WhenTakingConjunction(
                of: RefinementSet.From(("x", SqlDataTypeWithKnownSet.IntExcludingSet(1, 2))),
                with: RefinementSet.From(("x", SqlDataTypeWithKnownSet.IntExcludingSet(3, 4))),
                itProduces: Refinement.FromVariableNames(("x", SqlDataTypeWithKnownSet.IntExcludingSet(1, 2, 3, 4)))
            );

            WhenTakingConjunction(
                of: RefinementSet.From(("x", SqlDataTypeWithKnownSet.IntIncludingSet(1, 2))),
                with: RefinementSet.From(("x", SqlDataTypeWithKnownSet.IntIncludingSet(3, 4))),
                itProduces: Refinement.FromVariableNames()
            );

            WhenTakingConjunction(
                of: RefinementSet.From(("x", SqlDataTypeWithKnownSet.IntIncludingSet(1, 2))),
                with: RefinementSet.From(("x", NullDataType.Instance)),
                itProduces: Refinement.FromVariableNames()
            );
        }

        public void describe_Join()
        {
            it["When a symbol is found in both there should only be an inner join"] = () =>
            {
                var outerRefinements = new List<Refinement>();
                var innerCount = 0;
                RefinementSet.Join(
                    RefinementSet.From(("x", NullDataType.Instance)),
                    RefinementSet.From(("x", SqlDataType.Int)),
                    outer: (refinement) =>
                    {
                        outerRefinements.Add(refinement);
                        return refinement.ToMaybe();
                    },
                    inner: (reference, lDataType, rDataType) =>
                    {
                        innerCount++;
                        return Maybe.None<Refinement>();
                    });

                innerCount.Should().Be(1);
                outerRefinements.Count().Should().Be(0);
            };

            it["When a symbol is found in both and another symbol is only in one there should be an inner join and one outer join"] = () =>
            {
                var outerRefinements = new List<Refinement>();
                var innerCount = 0;
                RefinementSet.Join(
                    RefinementSet.From(("x", NullDataType.Instance), ("y", NullDataType.Instance)),
                    RefinementSet.From(("x", SqlDataType.Int)),
                    outer: (refinement) =>
                    {
                        outerRefinements.Add(refinement);
                        return refinement.ToMaybe();
                    },
                    inner: (reference, lDataType, rDataType) =>
                    {
                        innerCount++;
                        return Maybe.None<Refinement>();
                    });

                innerCount.Should().Be(1);
                outerRefinements.Count().Should().Be(1);
            };
        }

        public void describe_Disjunction()
        {
            WhenTakingDisjunction(
                of: RefinementSet.Empty,
                with: RefinementSet.Empty,
                itProduces: Refinement.FromVariableNames()
            );

            WhenTakingDisjunction(
                of: RefinementSet.From(
                    ("y", SqlDataType.VarChar),
                    ("x", SqlDataTypeWithKnownSet.IntIncludingSet(1, 2))
                ),
                with: RefinementSet.Empty,
                itProduces: Refinement.FromVariableNames()                
            );

            WhenTakingDisjunction(
                of: RefinementSet.From(("x", SqlDataTypeWithKnownSet.IntIncludingSet(1, 2))),
                with: RefinementSet.From(("x", new NullableDataType(SqlDataTypeWithKnownSet.IntIncludingSet(3, 4)))),
                itProduces: Refinement.FromVariableNames(("x", new NullableDataType(SqlDataTypeWithKnownSet.IntIncludingSet(1, 2, 3, 4))))
            );
        }

        #endregion

        #region assertion helpers

        private void WhenTakingDisjunction(
            RefinementSet of,
            RefinementSet with,
            params Refinement[] itProduces
        )
        {
            it[$"Disjunction of {of.ToString()} and {with.ToString()} should yield {new RefinementSet(itProduces).ToString()}"] = () =>
                new RefinementSetCases(of, RefinementSet.Empty).Disjunction(new RefinementSetCases(with, RefinementSet.Empty)).Positive.Refinements.Should().BeEquivalentTo(itProduces);
        }

        private void WhenTakingConjunction(
            RefinementSet of,
            RefinementSet with,
            params Refinement[] itProduces
        )
        {
            it[$"Conjunction of {of.ToString()} and {with.ToString()} should yield {new RefinementSet(itProduces).ToString()}"] = () =>
                new RefinementSetCases(of, RefinementSet.Empty).Conjunction(new RefinementSetCases(with, RefinementSet.Empty)).Positive.Refinements.Should().BeEquivalentTo(itProduces);
        }

        #endregion
    }
#pragma warning restore IDE1006 // Naming Styles
}


