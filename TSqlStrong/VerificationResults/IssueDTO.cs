using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TSqlStrong.VerificationResults
{
    public class IssueDTO
    {

        public string FileName { get; set; }
        public int StartLine { get; set; }
        public int StartColumn { get; set; }
        public int EndColumn { get; set; }
        public IssueLevel IssueLevel { get; set; }
        public string Message { get; set; }

        public string ToConsoleString() =>
            $"{FileName}({StartLine},{StartColumn}-{EndColumn}): {IssueLevel.ToString().ToLower()}: {Message}";

        public static string ToConsoleString(IssueDTO issue) => issue.ToConsoleString();

    }
}
