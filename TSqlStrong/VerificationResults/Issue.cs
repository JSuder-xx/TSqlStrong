using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ScriptDom = Microsoft.SqlServer.TransactSql.ScriptDom;

namespace TSqlStrong.VerificationResults
{
    /// <summary>
    /// An in-memory Issue coupled to TSqlFragment references.
    /// </summary>
    public class Issue
    {
        private readonly ScriptDom.TSqlFragment _fragment;
        private readonly IssueLevel _level;
        private readonly string _message;

        public Issue(ScriptDom.TSqlFragment fragment, IssueLevel level, string message)
        {
            _fragment = fragment;
            _level = level;
            _message = message;
        }

        public ScriptDom.TSqlFragment Fragment => _fragment;
        public IssueLevel Level => _level;
        public string Message => _message;

        public static class Select 
        {
            public static string Message(Issue issue) => issue.Message;

            public static Func<Issue, IssueDTO> ToDTO(string fileName) =>
                (issue) =>
                    new IssueDTO()
                    {
                        FileName = fileName,
                        StartLine = issue.Fragment.StartLine,
                        StartColumn = issue.Fragment.StartColumn,
                        EndColumn = issue.Fragment.StartColumn + issue.Fragment.FragmentLength,
                        IssueLevel = issue.Level,
                        Message = issue.Message
                    };            
        }        
    }
    
    public static class IssueEnumerableExtension
    {
        public static IEnumerable<string> Messages(this IEnumerable<Issue> issues) => issues.Select(Issue.Select.Message);        
    }
}
