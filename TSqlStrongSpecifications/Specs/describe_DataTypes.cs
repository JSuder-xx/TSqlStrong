using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using FluentAssertions;

using ScriptDom = Microsoft.SqlServer.TransactSql.ScriptDom;

using LowSums;
using TSqlStrong.TypeSystem;
using TSqlStrong.Symbols;
using TSqlStrongSpecifications.Builders;

namespace TSqlStrongSpecifications
{
#pragma warning disable IDE1006 // Naming Styles
    class describe_DataTypes : NSpec.nspec
    {
        #region descriptions

        public void describe_CanCompareWith()
        {
            context["varchar"] = () =>
            {
                context["with a known set"] = () =>
                {
                    GoodTypeComparison(DomainDecoratorDataType.Int("X"), KnownSetDecoratorDataType.IntIncludingSet(1, 2));

                    GoodTypeComparison(KnownSetDecoratorDataType.VarCharIncludingSet("apples", "oranges"), SqlDataType.VarChar);
                    GoodTypeComparison(KnownSetDecoratorDataType.VarCharIncludingSet("apples", "oranges"), KnownSetDecoratorDataType.VarCharIncludingSet("apples"));
                    GoodTypeComparison(KnownSetDecoratorDataType.VarCharIncludingSet("apples"), KnownSetDecoratorDataType.VarCharIncludingSet("apples", "oranges"));

                    BadTypeComparison(KnownSetDecoratorDataType.VarCharIncludingSet("apples"), KnownSetDecoratorDataType.VarCharIncludingSet("oranges"));
                    BadTypeComparison(KnownSetDecoratorDataType.VarCharIncludingSet("apples", "bananas"), KnownSetDecoratorDataType.VarCharIncludingSet("oranges", "grapes"));
                };
            };

            context["integers"] = () =>
            {
                context["without a domain"] = () =>
                {
                    GoodTypeComparison(SqlDataType.Int, new SqlDataType(ScriptDom.SqlDataTypeOption.Int));
                    GoodTypeComparison(SqlDataType.Int, DomainDecoratorDataType.Int("X"));
                    GoodTypeComparison(SqlDataType.Int, KnownSetDecoratorDataType.IntIncludingSet(1, 2));

                    BadTypeComparison(SqlDataType.Int, SqlDataType.VarChar);
                    BadTypeComparison(SqlDataType.Int, new RowDataType());
                };

                context["with a domain"] = () =>
                {
                    GoodTypeComparison(DomainDecoratorDataType.Int("X"), SqlDataType.Int);
                    GoodTypeComparison(DomainDecoratorDataType.Int("X"), DomainDecoratorDataType.Int("X"));
                    GoodTypeComparison(DomainDecoratorDataType.Int("X"), KnownSetDecoratorDataType.IntIncludingSet(1, 2));

                    BadTypeComparison(DomainDecoratorDataType.Int("X"), DomainDecoratorDataType.Int("Y"));
                };

                context["with a known set"] = () =>
                {
                    GoodTypeComparison(KnownSetDecoratorDataType.IntIncludingSet(1, 2), SqlDataType.Int);
                    GoodTypeComparison(KnownSetDecoratorDataType.IntIncludingSet(1), KnownSetDecoratorDataType.IntIncludingSet(1, 2), "you can compare two sets so long as one is a subset of the other");
                    GoodTypeComparison(KnownSetDecoratorDataType.IntIncludingSet(1, 2), KnownSetDecoratorDataType.IntIncludingSet(1), "you can compare two sets so long as one is a subset of the other");
                    GoodTypeComparison(KnownSetDecoratorDataType.IntIncludingSet(1, 2), DomainDecoratorDataType.Int("X"));

                    BadTypeComparison(KnownSetDecoratorDataType.IntIncludingSet(1, 2), KnownSetDecoratorDataType.IntIncludingSet(3, 4), "cannot compare two entirely disjoint sets");
                };
            };

            context["ColumDataType"] = () =>
            {
                GoodTypeComparison(new ColumnDataType(ColumnDataType.ColumnName.Anonymous.Instance, SqlDataType.Int), SqlDataType.Int);
                GoodTypeComparison(new ColumnDataType(ColumnDataType.ColumnName.Anonymous.Instance, SqlDataType.VarChar), SqlDataType.VarChar);
                GoodTypeComparison(new ColumnDataType(new ColumnDataType.ColumnName.Aliased("x", CaseSensitivity.CaseInsensitive), SqlDataType.Int), SqlDataType.Int);
                GoodTypeComparison(new ColumnDataType(new ColumnDataType.ColumnName.Schema("x", CaseSensitivity.CaseInsensitive), SqlDataType.Int), SqlDataType.Int);
                GoodTypeComparison(new ColumnDataType(ColumnDataType.ColumnName.Anonymous.Instance, KnownSetDecoratorDataType.VarCharIncludingSet("x", "y", "z")), KnownSetDecoratorDataType.VarCharIncludingSet("x"));

                BadTypeComparison(new ColumnDataType(ColumnDataType.ColumnName.Anonymous.Instance, SqlDataType.VarChar), SqlDataType.Int);
                BadTypeComparison(new ColumnDataType(new ColumnDataType.ColumnName.Aliased("x", CaseSensitivity.CaseInsensitive), SqlDataType.VarChar), SqlDataType.Int);
                BadTypeComparison(new ColumnDataType(new ColumnDataType.ColumnName.Schema("x", CaseSensitivity.CaseInsensitive), SqlDataType.VarChar), SqlDataType.Int);
                BadTypeComparison(new ColumnDataType(ColumnDataType.ColumnName.Anonymous.Instance, KnownSetDecoratorDataType.VarCharIncludingSet("x", "y", "z")), KnownSetDecoratorDataType.VarCharIncludingSet("q"));
            };
        }

        public void describe_IsAssignableTo()
        {
            context["VarChar"] = () =>
            {
                context["without a domain"] = () =>
                {
                    GoodTypeAssignment(SqlDataType.VarChar, SqlDataType.VarChar);
                    GoodTypeAssignment(new SizedSqlDataType(SizedDataTypeOption.VarChar, 50), new SizedSqlDataType(SizedDataTypeOption.VarChar, 50));
                    GoodTypeAssignment(new SizedSqlDataType(SizedDataTypeOption.VarChar, 50), new SizedSqlDataType(SizedDataTypeOption.VarChar, 100));
                    GoodTypeAssignment(SqlDataType.VarChar, SqlDataType.NVarChar, "An NVarChar can hold the representation of a VarChar");

                    BadTypeAssignment(SqlDataType.VarChar, SqlDataType.Int);
                    BadTypeAssignment(new SizedSqlDataType(SizedDataTypeOption.VarChar, 100), new SizedSqlDataType(SizedDataTypeOption.VarChar, 50));
                };
            };

            context["NVarChar"] = () =>
            {
                context["without a domain"] = () =>
                {
                    GoodTypeAssignment(SqlDataType.NVarChar, SqlDataType.NVarChar);

                    BadTypeAssignment(SqlDataType.NVarChar, SqlDataType.VarChar);
                    BadTypeAssignment(SqlDataType.NVarChar, SqlDataType.Int);
                };
            };

            context["Integers"] = () =>
            {
                context["without a domain"] = () =>
                {
                    GoodTypeAssignment(SqlDataType.Int, new SqlDataType(ScriptDom.SqlDataTypeOption.Int));

                    BadTypeAssignment(SqlDataType.Int, SqlDataType.VarChar);
                    BadTypeAssignment(SqlDataType.Int, DomainDecoratorDataType.Int("X"), because: "there is no way to vouch for a domain");
                    BadTypeAssignment(SqlDataType.Int, KnownSetDecoratorDataType.IntIncludingSet(1, 2, 3));
                    BadTypeAssignment(SqlDataType.Int, new RowDataType());
                };

                context["with a domain"] = () =>
                {
                    GoodTypeAssignment(DomainDecoratorDataType.Int("X"), SqlDataType.Int);
                    GoodTypeAssignment(DomainDecoratorDataType.Int("X"), DomainDecoratorDataType.Int("X"));

                    BadTypeAssignment(DomainDecoratorDataType.Int("X"), KnownSetDecoratorDataType.IntIncludingSet(1, 2));
                    BadTypeAssignment(DomainDecoratorDataType.Int("X"), DomainDecoratorDataType.Int("Y"));
                };

                context["with a known set"] = () =>
                {
                    GoodTypeAssignment(KnownSetDecoratorDataType.IntIncludingSet(1, 2), SqlDataType.Int);
                    GoodTypeAssignment(KnownSetDecoratorDataType.IntIncludingSet(1), KnownSetDecoratorDataType.IntIncludingSet(1, 2), because: "you can assign a subset to a superset");

                    BadTypeAssignment(KnownSetDecoratorDataType.IntIncludingSet(1, 2), DomainDecoratorDataType.Int("X"));
                    BadTypeAssignment(KnownSetDecoratorDataType.IntIncludingSet(1, 2), KnownSetDecoratorDataType.IntIncludingSet(1), because: "cannot assign a super set to a subset");
                    BadTypeAssignment(KnownSetDecoratorDataType.IntIncludingSet(1, 2), KnownSetDecoratorDataType.IntIncludingSet(3, 4), because: "cannot assign sets with no common elements");
                };
            };

            context["Nullable<T>"] = () =>
            {
                GoodTypeAssignment(SqlDataType.Int, SqlDataType.Int.ToNullable());
                GoodTypeAssignment(DomainDecoratorDataType.Int("x"), DomainDecoratorDataType.Int("x").ToNullable());

                GoodTypeAssignment(SqlDataType.Int.ToNullable(), SqlDataType.Int.ToNullable());
                GoodTypeAssignment(DomainDecoratorDataType.Int("x").ToNullable(), DomainDecoratorDataType.Int("x").ToNullable());

                GoodTypeAssignment(new SizedSqlDataType(SizedDataTypeOption.VarChar, 10).ToNullable(), new SizedSqlDataType(SizedDataTypeOption.VarChar, 12).ToNullable());
                GoodTypeAssignment(new SizedSqlDataType(SizedDataTypeOption.VarChar, 10), new SizedSqlDataType(SizedDataTypeOption.VarChar, 12).ToNullable());

                BadTypeAssignment(SqlDataType.Int, SqlDataType.VarChar.ToNullable());
                BadTypeAssignment(SqlDataType.Int.ToNullable(), SqlDataType.Int, "A null cannot fit inside of a non-null");

                BadTypeAssignment(new SizedSqlDataType(SizedDataTypeOption.VarChar, 12), new SizedSqlDataType(SizedDataTypeOption.VarChar, 10).ToNullable());
            };

            context["ColumDataType"] = () =>
            {
                GoodTypeAssignment(new ColumnDataType(ColumnDataType.ColumnName.Anonymous.Instance, SqlDataType.Int), SqlDataType.Int);
                GoodTypeAssignment(new ColumnDataType(ColumnDataType.ColumnName.Anonymous.Instance, SqlDataType.VarChar), SqlDataType.VarChar);
                GoodTypeAssignment(new ColumnDataType(new ColumnDataType.ColumnName.Aliased("x", CaseSensitivity.CaseInsensitive), SqlDataType.Int), SqlDataType.Int);
                GoodTypeAssignment(new ColumnDataType(new ColumnDataType.ColumnName.Schema("x", CaseSensitivity.CaseInsensitive), SqlDataType.Int), SqlDataType.Int);

                BadTypeAssignment(new ColumnDataType(ColumnDataType.ColumnName.Anonymous.Instance, SqlDataType.VarChar), SqlDataType.Int);
                BadTypeAssignment(new ColumnDataType(new ColumnDataType.ColumnName.Aliased("x", CaseSensitivity.CaseInsensitive), SqlDataType.VarChar), SqlDataType.Int);
                BadTypeAssignment(new ColumnDataType(new ColumnDataType.ColumnName.Schema("x", CaseSensitivity.CaseInsensitive), SqlDataType.VarChar), SqlDataType.Int);
            };

            context["RowDataType"] = () =>
            {
                var anon = String.Empty;

                var rowWithIDAndCountSchemaNames = RowBuilder
                    .WithSchemaNamedColumn("id", SqlDataType.Int)
                    .AndSchemaNamedColumn("count", SqlDataType.Int)
                    .CreateRow();

                var rowWithCountAndIDSchemaNames = RowBuilder
                    .WithSchemaNamedColumn("count", SqlDataType.Int)
                    .AndSchemaNamedColumn("id", SqlDataType.Int)
                    .CreateRow();

                var rowWithIDAndCountAliased = RowBuilder
                    .WithAliasedColumn("id", SqlDataType.Int)
                    .AndAliasedColumn("count", SqlDataType.Int)
                    .CreateRow();

                var rowWithCountAndIDAliased = RowBuilder
                    .WithAliasedColumn("count", SqlDataType.Int)
                    .AndAliasedColumn("id", SqlDataType.Int)
                    .CreateRow();

                var rowWithAnonymousIntInt = RowBuilder
                    .WithAnonymousColumn(SqlDataType.Int)
                    .AndAnonymousColumn(SqlDataType.Int)
                    .CreateRow();

                var rowWithAnonymousIntVarChar = RowBuilder
                    .WithAnonymousColumn(SqlDataType.Int)
                    .AndAnonymousColumn(SqlDataType.VarChar)
                    .CreateRow();

                var rowWithAnonymousVarCharInt = RowBuilder
                    .WithAnonymousColumn(SqlDataType.VarChar)
                    .AndAnonymousColumn(SqlDataType.Int)
                    .CreateRow();

                // empty rows
                GoodTypeAssignment(new RowDataType(), new RowDataType());
                GoodTypeAssignment(RowBuilder.EmptyRow, rowWithIDAndCountSchemaNames);
                GoodTypeAssignment(rowWithIDAndCountSchemaNames, RowBuilder.EmptyRow);

                GoodTypeAssignment(rowWithAnonymousIntVarChar, rowWithAnonymousIntVarChar);
                GoodTypeAssignment(rowWithIDAndCountSchemaNames, rowWithAnonymousIntInt);
                GoodTypeAssignment(rowWithAnonymousIntInt, rowWithIDAndCountSchemaNames);
                GoodTypeAssignment(rowWithIDAndCountSchemaNames, rowWithCountAndIDSchemaNames, because: "Schema names do not have to match because a user could be selecting from one schema object into another");

                BadTypeAssignment(rowWithAnonymousVarCharInt, rowWithAnonymousIntVarChar, because: "The types do not align");
                BadTypeAssignment(rowWithAnonymousVarCharInt, rowWithIDAndCountSchemaNames, because: "The types do not align");
                BadTypeAssignment(rowWithAnonymousVarCharInt, rowWithIDAndCountSchemaNames, because: "The types do not align");

                BadTypeAssignment(rowWithIDAndCountAliased, rowWithCountAndIDAliased, because: "Aliases are explicitly applied by author and therefore must match the destination");
                BadTypeAssignment(rowWithIDAndCountAliased, rowWithCountAndIDSchemaNames, because: "Aliases are explicitly applied by author and therefore must match the destination");
                BadTypeAssignment(rowWithCountAndIDAliased, rowWithIDAndCountAliased, because: "Aliases are explicitly applied by author and therefore must match the destination");
                BadTypeAssignment(rowWithCountAndIDAliased, rowWithIDAndCountSchemaNames, because: "Aliases are explicitly applied by author and therefore must match the destination");
            };

            context["ColumnName"] = () =>
            {
                GoodColumnNameAssignment(ColumnDataType.ColumnName.Anonymous.Instance, ColumnDataType.ColumnName.Anonymous.Instance);
                GoodColumnNameAssignment(ColumnDataType.ColumnName.Anonymous.Instance, new ColumnDataType.ColumnName.Aliased("x", CaseSensitivity.CaseInsensitive));
                GoodColumnNameAssignment(ColumnDataType.ColumnName.Anonymous.Instance, new ColumnDataType.ColumnName.Schema("x", CaseSensitivity.CaseInsensitive));

                GoodColumnNameAssignment(new ColumnDataType.ColumnName.Schema("x", CaseSensitivity.CaseInsensitive), new ColumnDataType.ColumnName.Schema("x", CaseSensitivity.CaseInsensitive));
                GoodColumnNameAssignment(new ColumnDataType.ColumnName.Schema("x", CaseSensitivity.CaseInsensitive), new ColumnDataType.ColumnName.Schema("y", CaseSensitivity.CaseInsensitive), because: "Schema names do not have to match");

                GoodColumnNameAssignment(new ColumnDataType.ColumnName.Aliased("x", CaseSensitivity.CaseInsensitive), new ColumnDataType.ColumnName.Aliased("x", CaseSensitivity.CaseInsensitive));
                GoodColumnNameAssignment(new ColumnDataType.ColumnName.Aliased("x", CaseSensitivity.CaseInsensitive), new ColumnDataType.ColumnName.Schema("x", CaseSensitivity.CaseInsensitive));
                GoodColumnNameAssignment(new ColumnDataType.ColumnName.Aliased("x", CaseSensitivity.CaseInsensitive), ColumnDataType.ColumnName.Anonymous.Instance);
                BadColumnNameAssignment(new ColumnDataType.ColumnName.Aliased("x", CaseSensitivity.CaseInsensitive), new ColumnDataType.ColumnName.Aliased("y", CaseSensitivity.CaseInsensitive), because: "an alias in the source expressing an _intention_ and so must match the dest");
                BadColumnNameAssignment(new ColumnDataType.ColumnName.Aliased("x", CaseSensitivity.CaseInsensitive), new ColumnDataType.ColumnName.Schema("y", CaseSensitivity.CaseInsensitive), because: "an alias in the source expressing an _intention_ and so must match the dest");
            };
        }

        public void describe_Disjunction()
        {
            context["a type with itself"] = () =>
            {
                GoodDisjunction(NullDataType.Instance, NullDataType.Instance, NullDataType.Instance);
                GoodDisjunction(SqlDataType.Int, SqlDataType.Int, SqlDataType.Int);
            };

            context["a type and null produces nullable"] = () =>
            {
                GoodDisjunction(SqlDataType.Int, NullDataType.Instance, SqlDataType.Int.ToNullable());
                GoodDisjunction(SqlDataType.VarChar, NullDataType.Instance, SqlDataType.VarChar.ToNullable());
                GoodDisjunction(
                    KnownSetDecoratorDataType.VarCharIncludingSet("a", "b"),
                    NullDataType.Instance,
                    KnownSetDecoratorDataType.VarCharIncludingSet("a", "b").ToNullable()
                );
            };

            context["columns"] = () =>
            {
                GoodDisjunction(
                    new ColumnDataType(ColumnDataType.ColumnName.Anonymous.Instance, KnownSetDecoratorDataType.IntIncludingSet(1, 2)),
                    KnownSetDecoratorDataType.IntIncludingSet(3, 4),
                    new ColumnDataType(ColumnDataType.ColumnName.Anonymous.Instance, KnownSetDecoratorDataType.IntIncludingSet(1, 2, 3, 4))
                );
            };

            context["known set"] = () =>
            {
                GoodDisjunction(KnownSetDecoratorDataType.IntIncludingSet(1, 2), KnownSetDecoratorDataType.IntIncludingSet(3, 4), KnownSetDecoratorDataType.IntIncludingSet(1, 2, 3, 4));
                GoodDisjunction(KnownSetDecoratorDataType.IntIncludingSet(1, 2), SqlDataType.Int, SqlDataType.Int);
                GoodDisjunction(SqlDataType.Int, KnownSetDecoratorDataType.IntIncludingSet(1, 2), SqlDataType.Int);
                GoodDisjunction(KnownSetDecoratorDataType.VarCharIncludingSet("a", "b"), KnownSetDecoratorDataType.VarCharIncludingSet("c", "d"), KnownSetDecoratorDataType.VarCharIncludingSet("a", "b", "c", "d"));
                GoodDisjunction(
                    KnownSetDecoratorDataType.VarCharIncludingSet("a", "b"), 
                    KnownSetDecoratorDataType.VarCharIncludingSet("c", "d").ToNullable(), 
                    KnownSetDecoratorDataType.VarCharIncludingSet("a", "b", "c", "d").ToNullable()
                );
                GoodDisjunction(KnownSetDecoratorDataType.IntExcludingSet(1, 2), KnownSetDecoratorDataType.IntExcludingSet(3, 4), SqlDataType.Int);
            };

            context["incompatible types"] = () =>
            {
                BadDisjunction(SqlDataType.Int, SqlDataType.VarChar);
            };
        }

        public void describe_Conjunction()
        {
            context["known set"] = () =>
            {
                GoodConjunction(KnownSetDecoratorDataType.IntExcludingSet(1, 2), KnownSetDecoratorDataType.IntExcludingSet(3, 4), KnownSetDecoratorDataType.IntExcludingSet(1, 2, 3, 4));
                BadConjunction(KnownSetDecoratorDataType.IntIncludingSet(1, 2), KnownSetDecoratorDataType.IntExcludingSet(3, 4));
                BadConjunction(KnownSetDecoratorDataType.IntIncludingSet(1, 2), KnownSetDecoratorDataType.IntIncludingSet(3, 4));
                BadConjunction(KnownSetDecoratorDataType.IntExcludingSet(1, 2), KnownSetDecoratorDataType.IntIncludingSet(3, 4));
            };
        }

        public void describe_Subtract()
        {
            context["a nullable type - null produces the type"] = () =>
            {
                Subtraction(SqlDataType.Int.ToNullable(), NullDataType.Instance, SqlDataType.Int);
                Subtraction(KnownSetDecoratorDataType.IntIncludingSet(1, 2, 3).ToNullable(), NullDataType.Instance, KnownSetDecoratorDataType.IntIncludingSet(1, 2, 3));
                Subtraction(DomainDecoratorDataType.Int("Bob").ToNullable(), NullDataType.Instance, DomainDecoratorDataType.Int("Bob"));
            };

            context["KnownSet"] = () =>
            {
                Subtraction(KnownSetDecoratorDataType.IntIncludingSet(1, 2, 3), KnownSetDecoratorDataType.IntIncludingSet(1), KnownSetDecoratorDataType.IntIncludingSet(2, 3));
            };

            context["Domain"] = () =>
            {
                Subtraction(DomainDecoratorDataType.Int("Bob"), DomainDecoratorDataType.Int("Jane"), DomainDecoratorDataType.Int("Bob"));
            };
        }

        #endregion

        #region assertion helpers

        private readonly static ITry<Unit> success = Try.SuccessUnit;

        private void BadColumnNameAssignment(ColumnDataType.ColumnName.Base source, ColumnDataType.ColumnName.Base dest, string because = "")
        {
            it[$"BAD: {source.ToString()} => {dest.ToString()}"] = () =>
                source.IsAssignableTo(dest).Should().NotBe(success, because: because);
        }

        private void GoodColumnNameAssignment(ColumnDataType.ColumnName.Base source, ColumnDataType.ColumnName.Base dest, string because = "")
        {
            it[$"GOOD: {source.ToString()} => {dest.ToString()}"] = () =>
                source.IsAssignableTo(dest).Should().Be(success, because: because);
        }

        private void BadTypeAssignment(DataType source, DataType dest, string because = "")
        {
            it[$"BAD: {source.ToString()} => {dest.ToString()}"] = () =>
                source.IsAssignableTo(dest).Should().NotBe(success, because: because);
        }

        private void GoodTypeAssignment(DataType source, DataType dest, string because = "")
        {
            it[$"GOOD: {source.ToString()} => {dest.ToString()}"] = () =>
                source.IsAssignableTo(dest).Should().Be(success, because: because);
        }

        private void GoodTypeComparison(DataType left, DataType right, string because = "")
        {
            it[$"GOOD: {left.ToString()} = {right.ToString()}"] = () =>
                left.CanCompareWith(right)
                .Should().Be(success, because: because);
        }

        private void BadTypeComparison(DataType left, DataType right, string because = "")
        {
            it[$"BAD: {left.ToString()} = {right.ToString()}"] = () =>
                left.CanCompareWith(right)                                
                .Should().NotBe(success, because);
        }

        private void GoodDisjunction(DataType left, DataType right, DataType result, string because = "")
        {
            it[$"GOOD: {left.ToString()} disjunction {right.ToString()} = {result.ToString()}"] = () =>
                DataType.Disjunction(left, right).GetValueOrException().Should().Be(result);            
        }

        private void BadDisjunction(DataType left, DataType right, string because = "")
        {
            it[$"BAD: {left.ToString()} disjunction {right.ToString()} = Nothing"] = () =>            
                DataType.Disjunction(left, right)
                    .ToEnumerable().Count().Should().Be(0, because);            
        }

        private void GoodConjunction(DataType left, DataType right, DataType result, string because = "")
        {
            it[$"GOOD: {left.ToString()} conjunction {right.ToString()} = {result.ToString()}"] = () =>            
                DataType.Conjunction(left, right).GetValueOrException().Should().Be(result);            
        }

        private void BadConjunction(DataType left, DataType right, string because = "")
        {
            it[$"BAD: {left.ToString()} conjunction {right.ToString()} = Nothing"] = () =>
                DataType.Conjunction(left, right)
                    .ToEnumerable().Count().Should().Be(0, because);        
        }

        private void Subtraction(DataType left, DataType right, DataType result)
        {
            it[$"{left.ToString()} - {right.ToString()} = ${result.ToString()}"] = () =>           
                DataType.Subtract(left, right).Should().Be(result);            
        }

        #endregion
    }
#pragma warning restore IDE1006 // Naming Styles
}
