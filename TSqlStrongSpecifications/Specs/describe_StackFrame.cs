using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using FluentAssertions;

using ScriptDom = Microsoft.SqlServer.TransactSql.ScriptDom;

using LowSums;

using TSqlStrong.Symbols;
using TSqlStrong.TypeSystem;

using TSqlStrongSpecifications.Builders;

namespace TSqlStrongSpecifications
{
#pragma warning disable IDE1006 // Naming Styles
    class describe_StackFrame : NSpec.nspec
    {
        private StackFrame _frame = null;

        public void describe_GivenATopFrame()
        {
            before = () =>
            {
                _frame = new StackFrame();
            };

            it["LastFrame is null"] = () =>
            {
                _frame.LastFrame.Should().Be(null);
                _frame.LookupTypeOfSymbolMaybe("a").Should().BeOfType<None<SymbolTyping>>();
            };

            context["when empty"] = () =>
            {
                it["Lookup returns None<DataType>"] = () =>
                    _frame.LookupTypeOfSymbolMaybe("a").Should().BeOfType<None<SymbolTyping>>();

                it["LookupColumn returns None<ColumnDataType>"] = () =>
                    _frame.LookupColumnDataTypeByNameMaybe("a").Should().BeOfType<None<(string, ColumnDataType)>>();

                it["GetTypesInCurrentFrame<DataType> should return empty"] = () =>
                    _frame.GetReadTypesInCurrentFrame<DataType>().Should().BeEquivalentTo();

                it["GetTypesInCurrentFrame<ColumnDataType> should return empty"] = () =>
                    _frame.GetReadTypesInCurrentFrame<ColumnDataType>().Should().BeEquivalentTo();
            };

            context["when myRow, myInt, myOtherInt, and myVarCharEnum symbols added"] = () =>
            {
                before = () =>
                    _frame = new StackFrame() 
                        .WithSymbol("myInt", SqlDataType.Int)
                        .WithSymbol("myOtherInt", SqlDataType.Int)
                        .WithSymbol("myVarCharEnum", SqlDataTypeWithKnownSet.VarChar("Apples", "Oranges", "Bananas"))
                        .WithSymbol("myRow", RowBuilder.WithAliasedColumn("id", SqlDataType.Int).AndAliasedColumn("count", SqlDataType.Int).CreateRow());

                it["Lookup(myInt) returns Some<SqlDataType>"] = () =>
                    _frame.LookupTypeOfSymbolMaybe("myInt")
                        .Should().BeOfType<Some<SymbolTyping>>()
                        .Which.Value.ExpressionType
                        .Should().BeOfType<SqlDataType>()
                        .Which.SqlDataTypeOption
                        .Should().Be(ScriptDom.SqlDataTypeOption.Int);

                it["Lookup(MYINT) returns Some<SqlDataType>"] = () =>
                    _frame.LookupTypeOfSymbolMaybe("MYINT")
                        .Should().BeOfType<Some<SymbolTyping>>(because: "symbol table lookups should be case insensitive")
                        .Which.Value.ExpressionType
                        .Should().BeOfType<SqlDataType>()
                        .Which.SqlDataTypeOption
                        .Should().Be(ScriptDom.SqlDataTypeOption.Int);

                it["Lookup(myVarCharEnum) returns Some<SqlDataTypeWithKnownSet"] = () =>
                    _frame.LookupTypeOfSymbolMaybe("myVarCharEnum")
                        .Should().BeOfType<Some<SymbolTyping>>()
                        .Which.Value.ExpressionType
                        .Should().BeOfType<SqlDataTypeWithKnownSet>()
                        .Which.Values.Cast<string>()
                        .Should().BeEquivalentTo("Apples", "Oranges", "Bananas");

                it["GetTypesInCurrentFrame<SqlDataType> should return all of the simple types"] = () =>
                    _frame.GetReadTypesInCurrentFrame<SqlDataType>().Count().Should().Be(3);

                it["GetTypesInCurrentFrame<RowDataType> should return the single row"] = () =>
                    _frame.GetReadTypesInCurrentFrame<RowDataType>().Single().ColumnDataTypes.NameStrings()
                        .Should().BeEquivalentTo("id", "count");

                context["when a new frame is nested with myInt shadowing myInt from parent and a new symbol myMoney"] = () =>
                {
                    before = () =>
                        _frame = new StackFrame(_frame)
                            .WithSymbol("myInt", SqlDataTypeWithDomain.Int("XYZ"))
                            .WithSymbol("myMoney", SqlDataType.Money);

                    it["Lookup(myInt) returns the new int type"] = () =>
                        _frame.LookupTypeOfSymbolMaybe("myInt")
                            .Should().BeOfType<Some<SymbolTyping>>()
                            .Which.Value.ExpressionType
                            .Should().BeOfType<SqlDataTypeWithDomain>()
                            .Which.Domain
                            .Should().Be("XYZ");

                    it["Lookup(myMoney) returns myMoney (new symbol at this level)"] = () =>
                        _frame.LookupTypeOfSymbolMaybe("myMoney")
                            .Should().BeOfType<Some<SymbolTyping>>()
                            .Which.Value.ExpressionType
                            .Should().BeOfType<SqlDataType>()
                            .Which.SqlDataTypeOption
                            .Should().Be(ScriptDom.SqlDataTypeOption.Money);

                    context["when popping the current frame off the stack to return to the prior"] = () =>
                    {
                        before = () => _frame = _frame.LastFrame;

                        it["Lookup(myInt) returns the original int type"] = () =>
                            _frame.LookupTypeOfSymbolMaybe("myInt")
                                .Should().BeOfType<Some<SymbolTyping>>()
                                .Which.Value.ExpressionType
                                .Should().BeOfType<SqlDataType>()
                                .Which.SqlDataTypeOption
                                .Should().Be(ScriptDom.SqlDataTypeOption.Int);

                        it["Lookup(myMoney) returns None<DataType>"] = () =>
                            _frame.LookupTypeOfSymbolMaybe("myMoney")
                                .Should().BeOfType<None<SymbolTyping>>();
                    };
                };
            };
        }
    }
#pragma warning restore IDE1006 // Naming Styles
}
