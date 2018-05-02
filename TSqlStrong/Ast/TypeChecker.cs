using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using ScriptDom = Microsoft.SqlServer.TransactSql.ScriptDom;
using LowSums;
using TSqlStrong.TypeSystem;

namespace TSqlStrong.Ast
{
    public class TypeChecker
    {
        /// <summary>
        /// Parse the given SQL and return a list of issues.
        /// </summary>
        /// <param name="sql"></param>
        /// <returns></returns>
        public static IEnumerable<VerificationResults.Issue> Parse(string sql)
        {
            using (var reader = new System.IO.StringReader(sql))
            {
                var scriptFragment = (new ScriptDom.TSql100Parser(true)).Parse(reader, out IList<ScriptDom.ParseError> errors);

                var topFrame = new Symbols.StackFrame();
                var visitor = new SqlVisitor(
                    topFrame,
                    new Logger.DebuggingLogger()
                );

                visitor.VisitAndReturnResults(scriptFragment);

                var functionBodyTypeIssues = topFrame.GetIssuesFromCompilingFunctionBodies().ToArray();
                topFrame.PerformTopLevelTypeCheckOfStoredProcedures();
                return functionBodyTypeIssues.Concat(visitor.Issues);
            }
        }
    }
}
