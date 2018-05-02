using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using ScriptDom = Microsoft.SqlServer.TransactSql.ScriptDom;
using LowSums;
using TSqlStrong.TypeSystem;

namespace TSqlStrongCLI
{

    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length != 1)
            {
                Console.WriteLine("Pass the filename containing the Sql as the first argument to the command line.");
                return;
            }

            var fileName = args[0];
            if (!System.IO.File.Exists(fileName))
            {
                Console.WriteLine($"Unable to find {fileName}");
                return;
            }

            try
            {
                var issues = TSqlStrong.Ast.TypeChecker.Parse(System.IO.File.ReadAllText(fileName))
                    .Select(TSqlStrong.VerificationResults.Issue.Select.ToDTO(fileName))
                    .Select(TSqlStrong.VerificationResults.IssueDTO.ToConsoleString)
                    .Distinct();
                var time = DateTime.Now.ToShortTimeString();
                foreach (var issue in issues)
                {
                    Console.WriteLine(issue);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"'{fileName}':0:0: error: Parse error ${ex.Message}");
            }
        }

        /// <summary>
        /// Used during development of project to get a hierarchy 
        /// </summary>
        /// <param name="fileName"></param>
        private static void EmitDomClassHierarchy(string fileName)
        {
            var scriptDomAssembly = System.Reflection.Assembly.GetAssembly(typeof(Microsoft.SqlServer.TransactSql.ScriptDom.QueryExpression));
            System.IO.File.WriteAllText(fileName, TSqlStrong.Tools.ClassHiearchyReflection.GetClassHierarchyForAssembly(scriptDomAssembly).ToString());
            System.Console.ReadLine();
        }
    }


}

