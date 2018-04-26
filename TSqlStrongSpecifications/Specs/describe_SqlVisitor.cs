using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using FluentAssertions;

using ScriptDom = Microsoft.SqlServer.TransactSql.ScriptDom;

using LowSums;
using TSqlStrong.Symbols;
using TSqlStrong.TypeSystem;
using TSqlStrong.Ast;
using TSqlStrong.VerificationResults;

using TSqlStrongSpecifications.Builders;

namespace TSqlStrongSpecifications
{
#pragma warning disable IDE1006 // Naming Styles
    public class describe_SqlVisitor : NSpec.nspec
    {
        #region Descriptions

        public void describe_SelectSetStatement()
        {
            ExpectSqlToHaveIssuesOnLines(
                @"
declare @myInt int;
declare @myText varchar(100);
select @myInt = @myText;
",
                4);

            ExpectSqlToBeFine(
                @"
declare @myInt int;
declare @myOtherInt int;
select @myInt = @myOtherInt;
");

        }

        public void describe_SelectStatement()
        {
            StackFrame FrameWithNullableIntX() => new StackFrame().WithSymbol("@x", SqlDataType.Int.ToNullable());

            context["where clause"] = () =>
            {
                GivenSql("select * from dbo.Master m where m.name = 'Bob'", () =>
                {
                    AndVerifyingWithNoTopFrame(() => ItShouldHaveErrorMessages(Messages.UnknownTypeForBinding("dbo.Master", "m"), Messages.UnableToFindColumn("m.name")));

                    AndVerifyingWithTopFrame(
                        StackFrameDefinitions.withMaster,
                        frameDescription: "containing dbo.Master",
                        expectations: () =>
                        {
                            ItShouldHaveNoIssues();
                            ItShouldReturnColumnNames(TableDefinitions.MasterColumnNames);
                        }
                    );
                });

                GivenSql("select * from dbo.Master m where m.name = 10", () =>                
                    AndVerifyingWithTopFrame(
                        StackFrameDefinitions.withMaster,
                        () => ItShouldHaveErrorMessageCount(1)
                    )
                );

                GivenSql("select * from dbo.Detail d where d.fruit = 'starfruit'", () =>                
                    AndVerifyingWithTopFrame(
                        StackFrameDefinitions.withMasterAndDetail,
                        () => ItShouldHaveErrorMessageCount(1)
                    )
                );

                GivenSql("select * from dbo.Detail d where d.fruit = 'apples'", () =>                
                    AndVerifyingWithTopFrame(
                        StackFrameDefinitions.withMasterAndDetail,
                        () => ItShouldHaveNoIssues()
                    )
                );
            };

            context["searched case"] = () =>
            {
                GivenSql("select case when 2 > 1 then 'alpha' else 'beta' end", () =>
                    AndVerifyingWithNoTopFrame(() =>
                        it["produces a column SqlDataTypeWithSet[Varchar]{alpha, beta}"] = () =>
                            ExpectSingleColumnRow<SqlDataTypeWithKnownSet>()
                                .Values
                                .Cast<string>()
                                .Should().BeEquivalentTo("alpha", "beta")
                    )
                );

                GivenSql("select case when 2 > 1 then 'alpha' when 3 > 1 then 'beta' else 'gamma' end", () =>
                    AndVerifyingWithNoTopFrame(() =>
                        it["produces a column SqlDataTypeWithSet[Varchar]{alpha, beta, gamma}"] = () =>
                            ExpectSingleColumnRow<SqlDataTypeWithKnownSet>()
                                .Values
                                .Cast<string>()
                                .Should().BeEquivalentTo("alpha", "beta", "gamma")
                    )
                );

                GivenSql("select case when 2 > 1 then 'alpha' when 3 > 1 then 'beta' else null end", () =>
                    AndVerifyingWithNoTopFrame(() =>
                        it["produces a column Nullable<SqlDataTypeWithSet[Varchar]{alpha, beta}>"] = () =>
                        {
                            var nullable = ExpectSingleColumnRow<NullableDataType>();
                            var sqlTypeWithKnownSet = nullable.DataType.Should().BeAssignableTo<SqlDataTypeWithKnownSet>().Which;
                            sqlTypeWithKnownSet.Values.Cast<string>().Should().BeEquivalentTo("alpha", "beta");
                        }
                    )
                );

                // case guards against zero
                GivenSql("select case when @x = 0 then -1 else 50 / @x end", () =>
                    AndVerifyingWithTopFrame(
                        FrameWithNullableIntX(),
                        () =>
                            it["produces a column SqlDataType[Int]"] = () =>
                                ExpectSingleColumnRow<SqlDataType>()
                                    .SqlDataTypeOption
                                    .Should().Be(ScriptDom.SqlDataTypeOption.Int)
                    )
                );

                // case guards against null and zero 
                GivenSql("select case when @x is null then -1 when @x = 0 then -2 else 50 / @x end", () =>
                    AndVerifyingWithTopFrame(
                        FrameWithNullableIntX(),
                        () =>
                            it["produces a column SqlDataType[Int]"] = () =>
                                ExpectSingleColumnRow<SqlDataType>()
                                    .SqlDataTypeOption
                                    .Should().Be(ScriptDom.SqlDataTypeOption.Int)
                    )
                );

                // throw error if null and possible zero
                GivenSql("select @x / 50", () =>
                    AndVerifyingWithTopFrame(
                        FrameWithNullableIntX(),
                        () => ItShouldHaveErrorMessages(Messages.BinaryOperationWithPossibleNull)
                    )
                );

                GivenSql("select case when @x is null then -1 else 50 / @x end", () =>
                    AndVerifyingWithTopFrame(
                        FrameWithNullableIntX(),
                        () => ItShouldHaveErrorMessages(Messages.DivideByZero)
                    )
                );

                GivenSql("select case when @x = 0 then -1 else 50 / @x end", () =>
                    AndVerifyingWithTopFrame(
                        FrameWithNullableIntX(),
                        () => ItShouldHaveErrorMessages(Messages.BinaryOperationWithPossibleNull)
                    )
                );

                GivenSql("select 50 / @x", () =>
                    AndVerifyingWithTopFrame(
                        new StackFrame().WithSymbol("@x", SqlDataType.Int),
                        () => ItShouldHaveErrorMessages(Messages.DivideByZero)
                    )
                );
            };

            context["simple case"] = () =>
            {
                GivenSql("select case @x when 1 then 'alpha' when 2 then 'beta' else 'gamma' end", () =>
                    AndVerifyingWithTopFrame(
                        FrameWithNullableIntX(),
                        () =>
                        it["produces a column SqlDataTypeWithSet[Varchar]{alpha, beta, gamma}"] = () =>
                            ExpectSingleColumnRow<SqlDataTypeWithKnownSet>()
                                .Values
                                .Cast<string>()
                                .Should().BeEquivalentTo("alpha", "beta", "gamma")
                    )
                );

                GivenSql("select case @x when 2 then 'alpha' when 3 then 'beta' else null end", () =>
                    AndVerifyingWithTopFrame(
                        FrameWithNullableIntX(),
                        () => 
                        it["produces a column Nullable<SqlDataTypeWithSet[Varchar]{alpha, beta}>"] = () =>
                        {
                            var nullable = ExpectSingleColumnRow<NullableDataType>();
                            var sqlTypeWithKnownSet = nullable.DataType.Should().BeAssignableTo<SqlDataTypeWithKnownSet>().Which;
                            sqlTypeWithKnownSet.Values.Cast<string>().Should().BeEquivalentTo("alpha", "beta");
                        }
                    )
                );

                // case guards against null and zero 
                GivenSql("select case @x when null then -1 when 0 then -2 else 50 / @x end", () =>
                    AndVerifyingWithTopFrame(
                        FrameWithNullableIntX(),
                        () =>
                            it["produces a column SqlDataType[Int]"] = () =>
                                ExpectSingleColumnRow<SqlDataType>()
                                    .SqlDataTypeOption
                                    .Should().Be(ScriptDom.SqlDataTypeOption.Int)
                    )
                );

                GivenSql("select case @x when null then -1 else 50 / @x end", () =>
                    AndVerifyingWithTopFrame(
                        FrameWithNullableIntX(),
                        () => ItShouldHaveErrorMessages(Messages.DivideByZero)
                    )
                );

                GivenSql("select case @x when 0 then -1 else 50 / @x end", () =>
                    AndVerifyingWithTopFrame(
                        FrameWithNullableIntX(),
                        () => ItShouldHaveErrorMessages(Messages.BinaryOperationWithPossibleNull)
                    )
                );
            };

            context["IsNull"] = () =>
            {
                ExpectSqlToBeFine("select isnull(null, 1);");
            };

            context["select literals"] = () =>
            {
                GivenSql("select 1, 'Bob'", () =>
                    AndVerifyingWithNoTopFrame(() =>
                        it["returns columns with type SqlDataTypeWithSet[Int], SqlDataTypeWithSet[VarChar]"] = () =>
                        {
                            var rowType = dataType.Should().BeOfType<RowDataType>().Which;
                            rowType.IsRowWithoutKnownStructure
                                .Should().Be(false);
                            rowType.IsAssignableTo(RowBuilder.WithAnonymousColumn(SqlDataType.Int).AndAnonymousColumn(SqlDataType.VarChar).CreateRow())
                                .Should().Be(success);
                        }
                    )
                );

                GivenSql("select 1 as MyInt, 'Bob' as Name", () =>
                    AndVerifyingWithNoTopFrame(() =>
                        it["returns columns with type SqlDataTypeWithSet[Int], SqlDataTypeWithSet[VarChar]"] = () =>
                        {
                            var rowType = dataType.Should().BeOfType<RowDataType>().Which;
                            rowType.IsRowWithoutKnownStructure
                                .Should().Be(false);
                            rowType.IsAssignableTo(RowBuilder.WithAnonymousColumn(SqlDataType.Int).AndAnonymousColumn(SqlDataType.VarChar).CreateRow())
                                .Should().Be(success);
                        }
                    )
                );

                GivenSql("select * from (select 1 as MyInt, 'Bob' as Name) d", () =>
                    AndVerifyingWithNoTopFrame(() =>
                    {
                        it["returns columns with type SqlDataTypeWithSet[Int], SqlDataTypeWithSet[VarChar]"] = () =>
                        {
                            var rowType = dataType.Should().BeOfType<RowDataType>().Which;
                            rowType.IsRowWithoutKnownStructure
                                .Should().Be(false);
                            rowType.IsAssignableTo(RowBuilder.WithAnonymousColumn(SqlDataType.Int).AndAnonymousColumn(SqlDataType.VarChar).CreateRow())
                                .Should().Be(success);
                        };

                        ItShouldReturnColumnNames("MyInt", "Name");
                    })
                );

                GivenSql("select deriv.x from (select 1 as MyInt, 'Bob' as Name) deriv", () =>
                    AndVerifyingWithNoTopFrame(() =>
                        ItShouldHaveErrorMessages(Messages.UnableToFindColumnInRow("deriv.x"))                            
                    )
                );

                GivenSql("select d.MyInt, 13 as ANumber from (select 1 as MyInt, 'Bob' as Name) d", () =>
                    AndVerifyingWithNoTopFrame(() =>
                        ItShouldReturnColumnNames("MyInt", "ANumber")
                    )
                );
            };

            context["union"] = () =>
            {
                GivenSql("select 1, 'apples' union select 2, 'oranges';", () => AndVerifyingWithTopFrame(() =>
                {
                    it["should evaluate to two columns"] = () => ExpectRowDataType().ColumnDataTypes.Count().Should().Be(2);
                    it["and the first should be Int{1, 2}"] = () => ExpectRowDataType().ColumnDataTypes.First().DataType.Should().BeOfType<SqlDataTypeWithKnownSet>().Which.Values.Should().BeEquivalentTo(1, 2);
                    it["and the second should be varchar{'apples', 'oranges'}"] = () => 
                        ExpectRowDataType().ColumnDataTypes.Second()
                        .DataType.Should().BeOfType<SqlDataTypeWithKnownSet>().Which
                        .Values.Cast<string>().Should().BeEquivalentTo("apples", "oranges");
                }));

                GivenSql("select 1 union select null;", () => AndVerifyingWithTopFrame(() =>
                {
                    it["should evaluate to one column"] = () => ExpectRowDataType().ColumnDataTypes.Count().Should().Be(1);
                    it["and it should be Nullable<Int{1}>"] = () =>
                        ExpectRowDataType().ColumnDataTypes.First().DataType
                        .Should().BeOfType<NullableDataType>().Which
                        .DataType.Should().BeOfType<SqlDataTypeWithKnownSet>().Which
                        .Values.Should().BeEquivalentTo(1);                        
                }));

                GivenSql("select 1 union select 'apples';", () => AndVerifyingWithTopFrame(() =>
                {
                    ItShouldHaveErrorMessages(Messages.UnableToJoinTypes("Int{1}", "VarChar{apples}"));
                }));

                GivenSql("select 1, 'apples' union select 2, 'oranges' union select 3, 'pears';", () => AndVerifyingWithTopFrame(() =>
                {
                    it["should evaluate to two columns"] = () => ExpectRowDataType().ColumnDataTypes.Count().Should().Be(2);
                    it["and the first should be Int{1, 2, 3}"] = () => ExpectRowDataType().ColumnDataTypes.First().DataType.Should().BeOfType<SqlDataTypeWithKnownSet>().Which.Values.Should().BeEquivalentTo(1, 2, 3);
                }));
           };

            context["name qualification"] = () =>
            {
                GivenSql("select * from dbo.Master where name = 'Bob'", () =>
                    AndVerifyingWithTopFrame(
                        StackFrameDefinitions.withMaster,
                        () =>
                        {
                            ItShouldHaveNoIssues();
                            ItShouldReturnColumnNames(TableDefinitions.MasterColumnNames);
                        }
                    )
                );

                GivenSql("select name from dbo.Master", () =>
                    AndVerifyingWithTopFrame(
                        StackFrameDefinitions.withMaster,
                        () =>
                        {
                            ItShouldHaveNoIssues();
                            ItShouldReturnColumnNames("name");
                        }
                    )
                );

                GivenSql("select * from Master where name = 'Bob'", () =>
                    AndVerifyingWithTopFrame(
                        StackFrameDefinitions.withMaster,
                        () =>
                        {
                            ItShouldHaveNoIssues();
                            ItShouldReturnColumnNames(TableDefinitions.MasterColumnNames);
                        })
                );
            };

            context["select exercising FROM clause: join, cross apply"] = () =>
            {
                GivenSql("select * from dbo.Master", () =>
                {
                    AndVerifyingWithNoTopFrame(() =>
                        ItShouldHaveErrorMessages(Messages.UnknownTypeForBinding("dbo.Master", "Master"))
                    );

                    AndVerifyingWithTopFrame(
                        new StackFrame().WithSymbol("dbo.Masterx", TableDefinitions.Master),
                        frameDescription: "dbo.Masterx given as name of master",
                        expectations: () =>
                            ItShouldHaveErrorMessages(Messages.UnknownTypeForBinding("dbo.Master", "Master"))
                    );

                    AndVerifyingWithTopFrame(
                        StackFrameDefinitions.withMaster,
                        frameDescription: "dbo.Master given as name of master",
                        expectations: () =>
                        {
                            ItShouldHaveNoIssues();
                            ItShouldReturnColumnNames(TableDefinitions.MasterColumnNames);
                        }
                    );
                });

                GivenSql("select * from dbo.Master m inner join dbo.Detail d on d.master_id = m.master_id", () =>
                {
                    AndVerifyingWithNoTopFrame(() =>
                        ItShouldHaveErrorMessages(
                            Messages.UnknownTypeForBinding(typeName: "dbo.Master", binding: "m"),
                            Messages.UnknownTypeForBinding(typeName: "dbo.Detail", binding: "d"),
                            Messages.UnableToFindColumn("d.master_id"),
                            Messages.UnableToFindColumn("m.master_id")
                        )
                    ); 

                    AndVerifyingWithTopFrame(
                        StackFrameDefinitions.withMasterAndDetail,
                        frameDescription: "dbo.Master and dbo.Detail register",
                        expectations: () =>
                        {
                            ItShouldHaveNoIssues();
                            ItShouldReturnColumnNames(TableDefinitions.MasterColumnNames.Concat(TableDefinitions.DetailColumnNames).ToArray());
                        }
                    );
                });

                GivenSql("select * from dbo.Master m inner join dbo.Detail d on d.detail_id = m.master_id and 1 = 1", () =>                
                    AndVerifyingWithTopFrame(
                        StackFrameDefinitions.withMasterAndDetail,
                        () => ItShouldHaveErrorMessageCount(1)
                    )
                );

                GivenSql("select * from dbo.Master m inner join dbo.Detail d on d.detail_id = m.master_id", () =>                
                    AndVerifyingWithTopFrame(
                        StackFrameDefinitions.withMasterAndDetail,
                        expectations: () => ItShouldHaveErrorMessageCount(1)
                    )
                );

                GivenSql("select * from dbo.Master m inner join dbo.Detail d on m.master_id = d.detail_id", () =>                
                    AndVerifyingWithTopFrame(
                        StackFrameDefinitions.withMasterAndDetail,
                        expectations: () => ItShouldHaveErrorMessageCount(1)
                    )
                );

                GivenSql("select * from dbo.Master m inner join (select * from dbo.Detail) derived on derived.master_id = m.master_id", () =>
                {
                    AndVerifyingWithNoTopFrame(() =>
                        ItShouldHaveErrorMessages(
                            Messages.UnknownTypeForBinding(typeName: "dbo.Master", binding: "m"),
                            Messages.UnknownTypeForBinding(typeName: "dbo.Detail", binding: "Detail"),
                            Messages.UnableToFindColumn("derived.master_id"),
                            Messages.UnableToFindColumn("m.master_id")
                        )
                    );

                    AndVerifyingWithTopFrame(
                        StackFrameDefinitions.withMasterAndDetail,
                        () =>
                        {
                            ItShouldHaveNoIssues();
                            ItShouldReturnColumnNames(TableDefinitions.MasterColumnNames.Concat(TableDefinitions.DetailColumnNames).ToArray());
                        }
                    );
                });

                GivenSql("select * from dbo.Master m cross apply (select * from dbo.Detail where Detail.master_id = m.master_id) det", () =>
                {
                    AndVerifyingWithTopFrame(
                        StackFrameDefinitions.withMasterAndDetail,
                        () =>
                        {
                            ItShouldHaveNoIssues();
                            ItShouldReturnColumnNames(TableDefinitions.MasterColumnNames.Concat(TableDefinitions.DetailColumnNames).ToArray());
                        }
                    );
                });
            };

            context["select exercising WHERE clause: type refinement"] = () =>
            {
                GivenSql("select * from dbo.Master m where m.nullableInt > 10", () =>
                {
                    AndVerifyingWithTopFrame(
                        StackFrameDefinitions.withMasterAndDetail,
                        () => ItShouldHaveErrorMessages(Messages.CannotCompareInequality(
                            TableDefinitions.NullableInt.ToString(),
                            SqlDataTypeWithKnownSet.Int(10).ToString()
                        ))
                    );
                });

                GivenSql("select * from dbo.Master m where (m.nullableInt is not null) and (m.nullableInt > 10)", () =>
                {
                    AndVerifyingWithTopFrame(
                        StackFrameDefinitions.withMasterAndDetail,
                        () => ItShouldHaveNoIssues("can compare (>) nullable integer with 10 on right side of conjunction after checking not null")
                    );
                });

                GivenSql("select * from dbo.Master m where (m.nullableInt is null) or (m.nullableInt > 10)", () =>
                {
                    AndVerifyingWithTopFrame(
                        StackFrameDefinitions.withMasterAndDetail,
                        () => ItShouldHaveNoIssues("can compare (>) nullable integer with 10 on right side of disjunction after checking null")
                    );
                });
            };

        }

        public void describe_CommonTableExpressions()
        {
            GivenSql(
                @"
with Recurse (Value)
as
(
    select 0

    union all

    select Value + 1
    from Recurse 
)
select top 10 * from Recurse",
                () => AndVerifyingWithTopFrame(() =>
                {                    
                    ItShouldHaveNoIssues();
                    it["should return a row with a single Int column"] = () =>
                    {
                        var columnDataTypes = ExpectRowDataType().ColumnDataTypes;
                        columnDataTypes.Count().Should().Be(1);
                        columnDataTypes.First().DataType.Should().BeOfType<SqlDataType>()
                            .Which.SqlDataTypeOption.Should().Be(ScriptDom.SqlDataTypeOption.Int);
                    };
                })            
            );

            ExpectSqlToHaveIssuesOnLines(
                @"
with Recurse (Value)
as
(
    -- ERROR: Missing the base case for recursion and therefore the Recurse symbol is not available
    select Value + 1
    from Recurse 
)
select top 10 * from Recurse",
                7, 6
            );

            ExpectSqlToBeFine(
                @"
with NonRecursive (Value)
as
(
    select 1
    union all
    select 2
)
select top 10 * from NonRecursive"
            );

            ExpectSqlToHaveIssuesOnLines(
                @"
with NonRecursive (Value)
as
(
    select 1
    union all
    select 'a'
)
select top 10 * from NonRecursive",
                7
            );

            ExpectSqlToBeFine(
                @"
with NonRecursive (Value)
as
(
    select 1 as 'Value' -- OK: Alias matches.
)
select top 10 * from NonRecursive"                
            );

            ExpectSqlToHaveIssuesOnLines(
                @"
with NonRecursive (Value)
as
(
    select 1 as 'CrazyValue' -- NOPE. Alias does not match.
)
select top 10 * from NonRecursive",
                2
            );

        }

        public void describe_CreateTable()
        {
            GivenSql("create table Stuff (Stuff_ID int not null identity(1, 1), Name varchar(100))", () =>
            {
                AndVerifyingWithTopFrame(                    
                    () =>
                    {
                        it["There should be one top level symbol that is a RowDataType with columns Stuff_ID, Name"] = () =>
                            stackFrame.CurrentFrameSymbols.Single().Value
                            .Should().BeOfType<SymbolTyping>().Which.ExpressionType
                            .Should().BeOfType<RowDataType>().Which.ColumnDataTypes.NameStrings()
                            .Should().BeEquivalentTo("Stuff_ID", "Name");
                    }
                );
            });

            GivenSql("create table Test (num int not null constraint my_constraint_name check((num = 1) or (num = 2) or (num = 3)))", () =>
            {
                AndVerifyingWithTopFrame(
                    () =>                     
                        it["There should be one top level symbol that is a RowDataType with column num"] = () =>
                        {
                            var rowType = stackFrame.CurrentFrameSymbols
                                .Single().Value
                                .Should().BeOfType<SymbolTyping>().Which.ExpressionType
                                .Should().BeOfType<RowDataType>().Which;

                            rowType.ColumnDataTypes.NameStrings()
                                .Should()
                                .BeEquivalentTo("num");

                            rowType.ColumnDataTypes.First().DataType
                                .Should().BeOfType<SqlDataTypeWithKnownSet>()
                                .Which.Values.Should()
                                .BeEquivalentTo(1, 2, 3);
                        }                    
                );
            });

            GivenSql("create table Test (fruit varchar(40) constraint fruit_constraint check ((fruit is null) or (fruit = 'apples') or (fruit = 'oranges')), num int not null constraint num_constraint_name check((num = 1) or (num = 2) or (num = 3)))", () =>
            {
                AndVerifyingWithTopFrame(
                    () => it["There should be one top level symbol that is a RowDataType with column num"] = () =>
                    {
                        issues.Count().Should().Be(0);
                        var rowType = stackFrame.CurrentFrameSymbols
                            .Single().Value.ExpressionType
                            .Should().BeOfType<RowDataType>().Which;

                        rowType.ColumnDataTypes.NameStrings()
                            .Should()
                            .BeEquivalentTo("fruit", "num");

                        var dataTypes = rowType.ColumnDataTypes.Select(ct => ct.DataType).ToArray();

                        var fruitAsNullable = dataTypes[0].Should().BeOfType<NullableDataType>().Which;
                        var fruitWellKnown = fruitAsNullable.DataType
                            .Should().BeOfType<SqlDataTypeWithKnownSet>().Which;
                        fruitWellKnown.Values.Cast<string>().Should().BeEquivalentTo("apples", "oranges");

                        dataTypes[1].Should().BeOfType<SqlDataTypeWithKnownSet>()
                            .Which.Values.Should()
                            .BeEquivalentTo(1, 2, 3);
                    }                    
                );
            });

            GivenSql("create table Stuff (Stuff_ID int not null identity(1, 1), Name varchar(100)); select *, 2 as Cool from Stuff", () =>            
                AndVerifyingWithTopFrame(() => ItShouldReturnColumnNames("Stuff_ID", "Name", "Cool"))
            );

            GivenSql(
                @"create table Master (master_id int not null); 
                create table Detail (detail_id int not null, master_id int not null foreign key references master(master_id));", () =>
            {
                AndVerifyingWithTopFrame(                    
                    () => it["should return two tables"] = () =>
                    {
                        var stackSymbols = stackFrame.CurrentFrameSymbols;
                        stackSymbols.Count().Should().Be(2);

                        var detailRowType = stackSymbols.First(symb => symb.Key == "DETAIL").Value.ExpressionType.Should().BeOfType<RowDataType>().Which;
                    }                    
                );
            });

            ExpectSqlToBeFine(@"create table Parent (id int not null, constraint PK_Parent primary key clustered (id)); 
create table Child(id int not null, Parent_id int not null foreign key references Parent(Parent_id), constraint PK_Child primary key clustered(id));");
        }

        public void describe_DropTable()
        {
            ExpectSqlToHaveIssuesOnLines(
                @"create table Something (id int not null);
create table Something (id int not null);",
                2
            );

            ExpectSqlToHaveIssuesOnLines(
                @"drop table Something;",
                1
            );

            ExpectSqlToBeFine(
                @"create table Something (id int not null);
drop table Something;
create table Something (id int not null);"                
            );

        }

        public void describe_InsertStatement_Values()
        {
            ExpectSqlToBeFine(
                @"create table LiteralValues(id int not null); 
                insert into LiteralValues values (1), (2);");

            ExpectSqlToHaveIssuesOnLines(
                @"create table LiteralValues(id int not null); 
                insert into LiteralValues values (1), ('a');",
                2);

            ExpectSqlToBeFine(
                @"create table LiteralValues(id int null); 
                insert into LiteralValues values (1), (2), (null);"
            );

            ExpectSqlToHaveIssuesOnLines(
                @"create table LiteralValues(id int not null); 
                insert into LiteralValues values (1), (2), (null);",
                2
            );
        }

        public void describe_InsertStatement_Select()
        {

            ExpectSqlToBeFine(
                @"create table Person(id int not null, firstName varchar(200)); 
                insert into Person(id, firstName) select 1, 'Bob';"
            );

            GivenSql(
                @"create table Person(id int not null, firstName varchar(200)); 
                insert into Person(id, firstName) select 1 as idx, 'Bob';",
                () =>
                    AndVerifyingWithTopFrame(
                        () => ItShouldHaveErrorMessages("Unable to assign to other row because Cannot assign column idx to Column('id')")
                    )
                );
        }

        public void describe_IfStatementReturningValue()
        {
            ExpectSqlToHaveIssuesOnLines(
                @"CREATE FUNCTION NoArgs() RETURNS INT AS
                BEGIN
                    IF (0 > 1) -- Unable to join return types of branches
                    BEGIN
                        RETURN 1;
                    END
                    ELSE 
                    BEGIN
                        return 'a';
                    END;
    
                    RETURN 1; -- unable to join return type 
                END;", 
                3, 12
            );                    
        }

        public void describe_IfStatement_And_SetVariableStatement()
        {
            GivenSql("if (@x = 1) begin set @y = @x end else begin set @z = @x end;", () =>
            {
                AndVerifyingWithTopFrame(
                    new StackFrame()
                        .WithSymbol("@x", SqlDataType.Int)
                        .WithSymbol("@y", SqlDataTypeWithKnownSet.Int(1))
                        .WithSymbol("@z", SqlDataType.Int),
                    () => ItShouldHaveNoIssues()                  
                );
            });

            GivenSql("if (@x = 2) begin set @y = @x end else begin set @z = @x end;", () =>
            {
                AndVerifyingWithTopFrame(
                    new StackFrame()
                        .WithSymbol("@x", SqlDataType.Int)
                        .WithSymbol("@y", SqlDataTypeWithKnownSet.Int(1))
                        .WithSymbol("@z", SqlDataType.Int),
                    () => ItShouldHaveErrorMessages(Messages.CannotAssignTo(SqlDataTypeWithKnownSet.Int(2).ToString(), SqlDataTypeWithKnownSet.Int(1).ToString()))
                );
            });

            GivenSql("if (@x = 0) begin set @z = 20 end else begin set @z = 10 / @x end;", () =>
            {
                AndVerifyingWithTopFrame(
                    new StackFrame()
                        .WithSymbol("@x", SqlDataType.Int)
                        .WithSymbol("@z", SqlDataType.Int),
                    () => ItShouldHaveNoIssues()
                );
            });

            GivenSql("if (@x = 1) begin set @z = 20 end else begin set @z = 10 / @x end;", () =>
            {
                AndVerifyingWithTopFrame(
                    new StackFrame()
                        .WithSymbol("@x", SqlDataType.Int)
                        .WithSymbol("@z", SqlDataType.Int),
                    () => ItShouldHaveErrorMessages(Messages.DivideByZero)
                );
            });
        }

        public void describe_BinaryExpression()
        {
            ExpectSqlToBeFine("select 1 + 2");
            ExpectSqlToHaveIssuesOnLines("select 1 + 'apples';", 1);
            ExpectSqlToHaveIssuesOnLines("select 'apples' + 1;", 1);
            ExpectSqlToHaveIssuesOnLines("select 1 + null;", 1);
            ExpectSqlToHaveIssuesOnLines("select null + 1;", 1);
        }

        public void describe_DeclareVariableStatements()
        {
            ExpectSqlToBeFine("declare @x int; set @x = 1; set @x = null;");

            ExpectSqlToBeFine("declare @x int = 1; set @x = 2; set @x = null;");

            ExpectSqlToHaveIssuesOnLines("declare @x int = 'apples';", 1);

            ExpectSqlToBeFine(
                @"declare @myInt int = 1; 
                declare @otherInt int; 
                if (20 > 10) 
                begin set @otherInt = 10 / @myInt; end;"
            );
        }

        public void describe_ScalarFunctionDeclarations()
        {
            ExpectSqlToHaveIssuesOnLines(
                @"create function sum(@x int, @y int) RETURNS INT AS
                BEGIN                    
                    return 'a'; -- !!!varchar value returned for INT
                END;",
                1
            );

            ExpectSqlToHaveIssuesOnLines(
                @"create function sum(@x int, @y int) RETURNS INT AS
                BEGIN
                    return @x + @z; -- !!!@z is undefined in scope
                END;",
                3
            );

            ExpectSqlToBeFine(
                @"create function sum(@x int, @y int) RETURNS INT AS
                BEGIN
                    return @x + @y; 
                END;"
            );

            ExpectSqlToBeFine(
                @"
CREATE FUNCTION NoArgs1() RETURNS INT AS
BEGIN    
    RETURN NoArgs2(); -- Forward call
END;
GO

CREATE FUNCTION NoArgs2() RETURNS INT AS
BEGIN
    RETURN NoArgs1(); -- Backward call
END;
GO"
            );

            ExpectSqlToHaveIssuesOnLines(
                @"CREATE FUNCTION NoArgs1() RETURNS INT AS
BEGIN    
    RETURN NoArgs2(); -- Forward call
END;
GO

CREATE FUNCTION NoArgs2() RETURNS INT AS
BEGIN
    RETURN NoArgs3(); -- !!!NoArgs3 is undefined
END;
GO",
                9
            );
        }

        public void describe_InlineTableValuedFunctionDeclarations()
        {
            ExpectSqlToBeFine(@"
create table Person (
    id int not null,
    firstName varchar(100),
    lastName varchar(100)
);
GO

create function PersonsWithLastName(@lastName varchar(100)) 
returns table
as
return select * from Person where lastName like @lastName;
go
");

            ExpectSqlToHaveIssuesOnLines(
                @"
create table Person (
    id int not null,
    firstName varchar(100),
    lastName varchar(100)
);
GO

create function PersonsWithLastName(@lastName varchar(100)) 
returns table
as
return select * from Person where lastName like @lastName;
go

select *
from
    -- ERROR: Mis-spelled
    PersonsWithLastNam('Smith');",
                18
                );


            ExpectSqlToBeFine(
                @"
create table Person (
    id int not null,
    firstName varchar(100),
    lastName varchar(100)
);
GO

create function PersonsWithLastName(@lastName varchar(100)) 
returns table
as
return select * from Person where lastName like @lastName;
go

select *
from
    PersonsWithLastName('Smith');");

            ExpectSqlToHaveIssuesOnLines(
                @"
create table Person (
    id int not null,
    firstName varchar(100),
    lastName varchar(100)
);
GO

create function PersonsWithLastName(@lastName varchar(100)) 
returns table
as
return select * from Person where lastName like @lastName;
go

select *
from
    -- ERROR: Parameter type mismatch
    PersonsWithLastName(1);",
                18
                );


        }
        
        public void describe_ProcedureDeclaration()
        {
            ExpectSqlToBeFine(
                @"create procedure dbo.ProcedureWithTwoParameters(@First int, @Second varchar(30))
as
begin
    select @First, @Second;
end;
go

exec dbo.ProcedureWithTwoParameters @First = 1, @Second = 'Bob';
"
            );

            ExpectSqlToHaveIssuesOnLines(
                @"create procedure dbo.ProcedureWithTwoParameters(@First int, @Second varchar(30))
as
begin
    select @First, @Second;
end;
go

exec dbo.ProcedureWithTwoParameters @First = 1, @Second = 2;
",
                8
            );

        }

        public void describe_DropProcedure()
        {
            ExpectSqlToHaveIssuesOnLines(@"drop procedure dbo.Something;", 1);

            ExpectSqlToBeFine(@"create procedure dbo.Something (@arg int) as begin select 1 end;
go
drop procedure dbo.Something;
go"         );

            ExpectSqlToBeFine(@"create procedure dbo.Something (@arg int) as begin select 1 end;
go
drop procedure dbo.Something;
go
create procedure dbo.Something (@arg int) as begin select 1 end;
go"         );

        }

        public void describe_AlterProcedure()
        {
            ExpectSqlToHaveIssuesOnLines(@"alter procedure dbo.Something (@arg int) as begin select 1 end;", 1);

            ExpectSqlToBeFine(@"create procedure dbo.Something (@arg int) as begin select 1 end;
go
alter procedure dbo.Something (@arg int) as begin select 2 end;
go"
            );
        }

        public void describe_ProcedureDeclarationAndTempTables()
        {
            ExpectSqlToBeFine(
                @"create procedure dbo.SelectFirstNameFromTemp 
as
begin
    select firstName from #Temp;
end;
GO

create procedure dbo.SelectFullNameFromTemp 
as
begin
    select fullName from #Temp;
end;
GO
"
            );
            
            ExpectSqlToBeFine(
                @"create procedure dbo.SelectFirstNameFromTemp 
as
begin
    select firstName from #Temp;
end;
GO

create procedure dbo.SelectFullNameFromTemp 
as
begin
    select fullName from #Temp;
end;
GO

create procedure dbo.CreateWithFirstName
as
begin
    create table #Temp (firstName varchar(200));
    exec dbo.SelectFirstNameFromTemp; -- OK
end;
GO

create procedure dbo.CreateWithFullName
as
begin
    create table #Temp (fullName varchar(200));
    exec dbo.SelectFullNameFromTemp; -- OK
end;
GO
"
            );

            ExpectSqlToHaveIssuesOnLines(
                @"create procedure dbo.SelectFirstNameFromTemp 
as
begin
    select firstName from #Temp;
end;
GO

create procedure dbo.CreateWithFullName
as
begin
    create table #Temp (fullName varchar(200));
    exec dbo.SelectFirstNameFromTemp; -- ERROR
end;
GO
",
                4, 12
            );
        }

        public void describe_Cursors()
        {
            ExpectSqlToHaveIssuesOnLines(
                @"OPEN notDefined",
                1
            );

            ExpectSqlToHaveIssuesOnLines(
                @"CLOSE notDefined",
                1
            );

            ExpectSqlToHaveIssuesOnLines(
                @"
DECLARE SomeCursor CURSOR FOR   
    SELECT 1
    UNION
    SELECT 2;  
OPEN notDefined",
                6                
            );

            ExpectSqlToBeFine(
                @"
DECLARE SomeCursor CURSOR FOR   
    SELECT 1
    UNION
    SELECT 2;  
OPEN SomeCursor
CLOSE SomeCursor");

            ExpectSqlToBeFine(
                @"
DECLARE SomeCursor CURSOR FOR   
    SELECT 1
    UNION
    SELECT 2;  

OPEN SomeCursor

DECLARE @myInt int;

FETCH NEXT FROM SomeCursor INTO @myInt;
");

            ExpectSqlToHaveIssuesOnLines(
                @"
DECLARE SomeCursor CURSOR FOR   
    SELECT 1
    UNION
    SELECT 2;  

OPEN SomeCursor

DECLARE @myVarChar varchar(20);

FETCH NEXT FROM SomeCursor INTO @myVarChar; -- ERROR: Incorrect type
",
                11);

            // Small but complete
            ExpectSqlToBeFine(
    @"
DECLARE SomeCursor CURSOR FOR   
    SELECT 1
    UNION
    SELECT 2;  

OPEN SomeCursor

DECLARE @myInt int;

FETCH NEXT FROM SomeCursor INTO @myInt;

WHILE @@FETCH_STATUS = 0  
BEGIN  
    PRINT @myInt;
   
    FETCH NEXT FROM SomeCursor INTO @myInt
END   
CLOSE SomeCursor;  
DEALLOCATE SomeCursor; 
");

            // Very slightly modified version of example from Microsoft documentation
            ExpectSqlToBeFine(
                @"
CREATE TABLE PURCHASING.VENDOR (
    VendorID int not null,
    Name nvarchar(100),
    PreferredVendorStatus int
);
GO

CREATE TABLE PURCHASING.ProductVendor (
    ProductID int not null,
    VendorID int not null
);
GO

CREATE TABLE Production.Product (
    ProductID int not null,
    Name nvarchar(100)
);
GO

DECLARE @vendor_id int, @vendor_name nvarchar(50),  
    @message nvarchar(80), @product nvarchar(50);  

-- Vendor Products Report 

DECLARE vendor_cursor CURSOR FOR   
SELECT VendorID, Name  
FROM Purchasing.Vendor  
WHERE PreferredVendorStatus = 1  
ORDER BY VendorID;  

OPEN vendor_cursor  

FETCH NEXT FROM vendor_cursor   
INTO @vendor_id, @vendor_name

WHILE @@FETCH_STATUS = 0  
BEGIN  
    PRINT @vendor_name;
   
    -- Declare an inner cursor based     
    -- on vendor_id from the outer cursor.  

    DECLARE product_cursor CURSOR FOR   
    SELECT v.Name  
    FROM Purchasing.ProductVendor pv, Production.Product v  
    WHERE pv.ProductID = v.ProductID AND  
    pv.VendorID = @vendor_id  -- Variable value from the outer cursor  

    OPEN product_cursor  
    FETCH NEXT FROM product_cursor INTO @product  

    IF @@FETCH_STATUS <> 0   
        PRINT '         <<None>>'       

    WHILE @@FETCH_STATUS = 0
    BEGIN      
        SELECT @message = N'         ' + Coalesce(@product, N'');
        PRINT @message  
        FETCH NEXT FROM product_cursor INTO @product  
    END  

    CLOSE product_cursor  
    DEALLOCATE product_cursor  
        -- Get the next vendor.  
    FETCH NEXT FROM vendor_cursor   
    INTO @vendor_id, @vendor_name  
END   
CLOSE vendor_cursor;  
DEALLOCATE vendor_cursor; 
");
        }

        #endregion

        #region Given/When/Then

        private static (ScriptDom.TSqlFragment, IList<ScriptDom.ParseError>) Parse(string sql)
        {
            using (var reader = new System.IO.StringReader(sql))
            {
                var scriptFragment = (new ScriptDom.TSql100Parser(true)).Parse(reader, out IList<ScriptDom.ParseError> errors);
                return (scriptFragment, errors);
            }
        }

        private void GivenSql(string sql, Action expectations)
        {
            context[sql] = () =>
            {
                before = () =>
                {
                    (sqlFragment, errors) = Parse(sql);
                    if (errors.Any())
                        throw new InvalidOperationException($"Error parsing SQL {errors.Delimit(",", err => err.Message)}");
                };
                expectations();
            };
        }

        private void ExpectSqlToBeFine(string sql)
        {
            GivenSql(
                sql,
                () => AndVerifyingWithTopFrame(() => ItShouldHaveNoIssues())
            );
        }

        private void ExpectSqlToHaveIssuesOnLines(string sql, params int[] lineNumbers)
        {
            GivenSql(
                sql,
                () => AndVerifyingWithTopFrame(() => ItShouldHaveIssuesOnLines(lineNumbers))
            );
        }

        /// <summary>
        /// Verifies that the data type return is a row with a single column and returns the data type of that column.
        /// </summary>
        /// <typeparam name="TTypeOfColumn"></typeparam>
        /// <returns></returns>
        private TTypeOfColumn ExpectSingleColumnRow<TTypeOfColumn>()
            where TTypeOfColumn : DataType
        {
            var rowType = dataType.Should().BeOfType<RowDataType>().Which;
            rowType.IsRowWithoutKnownStructure
                .Should().Be(false);
            rowType.ColumnDataTypes.Count().Should().Be(1);

            return rowType.ColumnDataTypes
                .First()
                .DataType
                .Should().BeAssignableTo<TTypeOfColumn>().Which;
        }

        private void AndVerifyingWithNoTopFrame(Action expectations)
        {
            context["when no frame given"] = () =>
            {
                before = () =>
                {
                    stackFrame = null;
                    (issues, expressionResult) = new SqlVisitor(null, new TSqlStrong.Logger.DebuggingLogger()).VisitAndReturnResults(sqlFragment);
                    dataType = expressionResult.TypeOfExpression;
                };
                expectations();
            };
        }

        private void AndVerifyingWithTopFrame(Action expectations, string frameDescription = "")
        {
            AndVerifyingWithTopFrame(new StackFrame(), expectations, frameDescription);
        }

        private void AndVerifyingWithTopFrame(StackFrame frame, Action expectations, string frameDescription = "")
        {
            context[$"with frame {frameDescription}"] = () =>
            {
                before = () =>
                {
                    stackFrame = frame.Clone();
                    var visitor = new SqlVisitor(stackFrame, new TSqlStrong.Logger.DebuggingLogger());
                    (_, expressionResult) = visitor.VisitAndReturnResults(sqlFragment);
                    dataType = expressionResult.TypeOfExpression;

                    var functionBodyTypeIssues = stackFrame.GetIssuesFromCompilingFunctionBodies().ToArray();
                    stackFrame.PerformTopLevelTypeCheckOfStoredProcedures();
                    issues = functionBodyTypeIssues.Concat(visitor.Issues);
                };
                
                expectations();
            };
        }

        private void ItShouldHaveNoIssues(string because = "")
        {
            it["should have no issues"] = () => issues.Messages().Should().BeEquivalentTo(new string[] { }, because: because);
        }

        private RowDataType ExpectRowDataType() => dataType.Should().BeOfType<RowDataType>().Which;

        private void ItShouldReturnColumnNames(params string[] columnNames)
        {
            it[$"should return columns {columnNames.CommaDelimit()}"] = () =>
                ExpectRowDataType().ColumnDataTypes.NameStrings()
                .Should().BeEquivalentTo(columnNames);
        }

        private void ItShouldHaveErrorMessages(params string[] errorMessages)
        {
            it[$"should have error messages {errorMessages.CommaDelimit()}"] = () =>
                issues
                .Messages()
                .Should().BeEquivalentTo(errorMessages);
        }

        private void ItShouldHaveErrorMessageCount(int count)
        {
            it[$"should have {count} error messages"] = () =>
                issues
                .Messages().Count()
                .Should().Be(count);
        }

        private void ItShouldHaveIssuesOnLines(params int[] lineNumbers)
        {
            it[$"should have error messages on lines {lineNumbers.Delimit(", ")}"] = () =>
                issues
                .Select(it => it.Fragment.StartLine)
                .Distinct()
                .Should().Equal(lineNumbers);
        }

        #endregion

        #region State (nasty)

        private readonly static ITry<Unit> success = Try.SuccessUnit;

        private StackFrame stackFrame;
        private ScriptDom.TSqlFragment sqlFragment = null;
        private IList<ScriptDom.ParseError> errors;
        private IEnumerable<Issue> issues = null;
        private ExpressionResult expressionResult = null;
        private DataType dataType = null;

        #endregion

        class DataTypeDefinitions
        {
            public static readonly DataType Fruit = SqlDataTypeWithKnownSet.VarChar("apples", "oranges", "bananas");
        }
        
        class TableDefinitions
        {
            private const string masterID = "master_id";
            private const string detailID = "detail_id";
            private const string fruit = "fruit";
            private const string name = "name";
            private const string nullableInt = "nullableInt";

            public static string[] ColumnNames(RowDataType row) =>
                 row.ColumnDataTypes.Select(col => col.Name is ColumnDataType.ColumnName.BaseNamedColumn named ? named.Name : String.Empty).ToArray();

            public static readonly RowDataType Master = RowBuilder
                .WithSchemaNamedColumn(masterID, SqlDataTypeWithDomain.Int("Master"))
                .AndSchemaNamedColumn(name, SqlDataType.VarChar)
                .AndSchemaNamedColumn(nullableInt, new NullableDataType(SqlDataType.Int))
                .CreateRow();

            public static readonly string[] MasterColumnNames = ColumnNames(Master);
            public static readonly DataType NullableInt = Master.FindColumn(nullableInt).GetValueOrException();

            public static readonly RowDataType Detail = RowBuilder
                .WithSchemaNamedColumn(detailID, SqlDataTypeWithDomain.Int("Detail"))
                .AndSchemaNamedColumn(masterID, SqlDataTypeWithDomain.Int("Master"))
                .AndSchemaNamedColumn(fruit, DataTypeDefinitions.Fruit)
                .CreateRow();

            public static readonly string[] DetailColumnNames = ColumnNames(Detail);
        }

        class StackFrameDefinitions
        {
            public static readonly StackFrame withMaster = new StackFrame()
                .WithSymbol("dbo.Master", TableDefinitions.Master);

            public static readonly StackFrame withMasterAndDetail = new StackFrame()
                .WithSymbol("dbo.Master", TableDefinitions.Master)
                .WithSymbol("dbo.Detail", TableDefinitions.Detail);
        }
    }
#pragma warning restore IDE1006 // Naming Styles
}
