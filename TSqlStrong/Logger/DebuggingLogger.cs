using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TSqlStrong.Logger
{
    public class DebuggingLogger : BaseLogger
    {
        public static readonly ILogger Instance = new DebuggingLogger();

        protected override void OnWriteLine(string line)
        {
            System.Diagnostics.Debug.WriteLine(line);
        }
    }
}
