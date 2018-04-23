using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TSqlStrong.Logger
{
    public interface ILogger
    {
        void Enter(string blockName);
        void Exit(string scopeName = null);
        void Log(string message);
    }
}
