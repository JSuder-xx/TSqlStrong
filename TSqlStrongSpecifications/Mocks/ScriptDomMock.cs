using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ScriptDom = Microsoft.SqlServer.TransactSql.ScriptDom;

namespace TSqlStrongSpecifications
{
    public static class ScriptDomMock
    {
        public static ScriptDom.Identifier Identifier(string id)
        {
            return new ScriptDom.Identifier() { Value = id };
        }

        public static ScriptDom.SchemaObjectName SchemaObjectName(ScriptDom.Identifier schemaIdentifier, ScriptDom.Identifier baseIdentifier)
        {
            var schemaObjectName = new Moq.Mock<ScriptDom.SchemaObjectName>();

            schemaObjectName.SetupGet(it => it.BaseIdentifier).Returns(baseIdentifier);
            schemaObjectName.SetupGet(it => it.SchemaIdentifier).Returns(schemaIdentifier);
                    
            return schemaObjectName.Object;
        }

        public static ScriptDom.SchemaObjectName SchemaObjectName(string schemaIdentifier, string baseIdentifier)
        {
            return SchemaObjectName(Identifier(schemaIdentifier), Identifier(baseIdentifier));
        }

        public static ScriptDom.SchemaObjectName SchemaObjectName(ScriptDom.Identifier baseIdentifier)
        {
            var schemaObjectName = new Moq.Mock<ScriptDom.SchemaObjectName>();

            schemaObjectName.SetupGet(it => it.BaseIdentifier).Returns(baseIdentifier);
            schemaObjectName.SetupGet(it => it.SchemaIdentifier).Returns((ScriptDom.Identifier)null);

            return schemaObjectName.Object;
        }

        public static ScriptDom.SchemaObjectName SchemaObjectName(string baseIdentifier)
        {
            return SchemaObjectName(Identifier(baseIdentifier));
        }

        public static IEnumerable<ScriptDom.Identifier> Identifiers(params string[] ids)
        {
            return ids.Select(Identifier);
        }
    }
}
