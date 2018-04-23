using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TSqlStrong.Logger
{
    public sealed class NullLogger : ILogger
    {
        public static readonly ILogger Instance = new NullLogger();

        void ILogger.Enter(string blockName) { }

        void ILogger.Exit(string scope) { }

        void ILogger.Log(string message) { }
    }
}
