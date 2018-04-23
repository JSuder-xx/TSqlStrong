using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using FluentAssertions;
using TSqlStrong.Ast;


namespace TSqlStrongSpecifications
{
#pragma warning disable IDE1006 // Naming Styles
    public class describe_Names : NSpec.nspec
    {
        public void describe_GetAlias()
        {
            it["Should return a qualified name given a schema object with a schema name and value"] = () =>
                Names.GetAlias(ScriptDomMock.SchemaObjectName("MySchema", "MyObject"))
                    .Should().Be("MyObject");

            it["Should return a qualified name given a schema object without a schema name"] = () =>
                Names.GetAlias(ScriptDomMock.SchemaObjectName("MyObject"))
                    .Should().Be("MyObject");
        }

        public void describe_GetColumnNameInColumnReference()
        {
            it["given [x, y, z] returns z"] = () =>
                Names.GetColumnNameInColumnReference(ScriptDomMock.Identifiers("x", "y", "z"))
                    .Should().Be("z");

            it["given [x, y] returns y"] = () =>
                Names.GetColumnNameInColumnReference(ScriptDomMock.Identifiers("x", "y"))
                    .Should().Be("y");
        }

        public void describe_GetFullTypeNameFromColumnReference()
        {
            it["given [x, y, z] returns x.y"] = () =>
                Names.GetFullTypeNameFromColumnReference(ScriptDomMock.Identifiers("x", "y", "z"))
                    .Should().Be("x.y");

            it["given [x, y] returns x"] = () =>
                Names.GetFullTypeNameFromColumnReference(ScriptDomMock.Identifiers("x", "y"))
                    .Should().Be("x");
        }

        public void describe_GetFullTypeName()
        {
            it["Should return a qualified name given a schema object with a schema name and value"] = () =>
                Names.GetFullTypeName(ScriptDomMock.SchemaObjectName("MySchema", "MyObject"))
                    .Should().Be("MySchema.MyObject");

            it["Should return a qualified name given a schema object without a schema name"] = () =>
                Names.GetFullTypeName(ScriptDomMock.SchemaObjectName("MyObject"))
                    .Should().Be("MyObject");
        }

        public void describe_GetTopLevelNames()
        {
            it["returns a single value for an unqualified name"] = () =>
                Names.GetTopLevelNames("Bob").Should().BeEquivalentTo("Bob");

            it["returns both the name and the fully qualified name for a qualified name"] = () =>
                Names.GetTopLevelNames("SomeSchema.Bob").Should().BeEquivalentTo("Bob", "SomeSchema.Bob");
        }
    }
#pragma warning restore IDE1006 // Naming Styles
}
