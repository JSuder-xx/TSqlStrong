using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ScriptDom = Microsoft.SqlServer.TransactSql.ScriptDom;
using LowSums;

namespace TSqlStrong.Ast
{
    /// <summary>
    /// A module for converting various AST objects to normalized names (ultimately for use by symbol management system).
    /// </summary>
    public static class Names
    {
        /// <summary>
        /// Returns a . delimited name from a schema object. 
        /// </summary>
        /// <param name="schemaObject"></param>
        /// <returns></returns>
        public static string GetFullTypeName(ScriptDom.SchemaObjectName schemaObject) =>
            schemaObject.SchemaIdentifier == null
                ? schemaObject.BaseIdentifier.Value
                : $"{schemaObject.SchemaIdentifier.Value}.{schemaObject.BaseIdentifier.Value}";

        public static ITry<string> GetSymbolName(ScriptDom.IdentifierOrValueExpression identifierOrValue) =>
            identifierOrValue.Identifier != null ? Try.Success(identifierOrValue.Identifier.Value)
            : !String.IsNullOrWhiteSpace(identifierOrValue.Value) ? Try.Success(identifierOrValue.Value)
            : Try.Failure<string>("We do not know how to process ValueExpression at this time");

        /// <summary>
        /// Returns a . delimited name from a multipart identifier.
        /// </summary>
        /// <param name="multiPart"></param>
        /// <returns></returns>
        public static string[] GetIdentifiers(ScriptDom.MultiPartIdentifier multiPart) => multiPart.Identifiers.Select(id => id.Value).ToArray();       

        /// <summary>
        /// Returns a . delimited name from a multipart identifier.
        /// </summary>
        /// <param name="multiPart"></param>
        /// <returns></returns>
        public static string GetFullTypeNameFromColumnReference(IEnumerable<ScriptDom.Identifier> identifiers)
        {
            var allIdentifiers = identifiers.Select(identifier => identifier.Value).ToArray();
            return allIdentifiers.Take(allIdentifiers.Length - 1).Delimit(".");
        }

        /// <summary>
        /// Returns just the column name. 
        /// </summary>
        /// <param name="multiPart"></param>
        /// <returns></returns>
        public static string GetColumnNameInColumnReference(IEnumerable<ScriptDom.Identifier> identifiers)
        {
            return identifiers.Last().Value;
        }
        
        public static string GetAlias(ScriptDom.SchemaObjectName schemaObject)
        {
            return schemaObject.BaseIdentifier.Value;
        }

        public static IEnumerable<string> GetTopLevelNames(string fullyQualifiedTypeName)
        {
            return fullyQualifiedTypeName.Contains(".")
                ? new[] { fullyQualifiedTypeName, fullyQualifiedTypeName.Split(new [] { "." }, StringSplitOptions.RemoveEmptyEntries).Last() }
                : new[] { fullyQualifiedTypeName };
        }
    }
}
