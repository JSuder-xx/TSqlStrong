using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TSqlStrong.Logger
{
    public class LambdaLogger : BaseLogger
    {
        private readonly Action<string> _writeLine;

        public LambdaLogger(Action<string> writeLine)
        {
            _writeLine = writeLine;
        }

        protected override void OnWriteLine(string line)
        {
            _writeLine(line);
        }
    }
}
