using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TSqlStrong.Logger
{
    public abstract class BaseLogger : ILogger
    {
        private const string Indentation = "  ";
        private Stack<(string scope, string indentation)> _scopes = new Stack<(string scope, string indentation)>();

        public void Enter(string scope)
        {
            WriteLine($"{scope}.Enter");
            _scopes.Push((scope, String.Concat(CurrentIndentiation(), Indentation)));
        }

        public void Exit(string expectedScopeName = null)
        {
            var (scope, _) = _scopes.Pop();
            if ((expectedScopeName != null) && !String.Equals(expectedScopeName, scope))
                throw new InvalidOperationException($"Expected to exit scope {expectedScopeName} but found scope {scope}");

            WriteLine($"{scope}.Exit");
        }

        public void Log(string message)
        {
            WriteLine(message);
        }

        private void WriteLine(string message)
        {
            OnWriteLine(String.Concat(CurrentIndentiation(), message));
        }

        private string CurrentIndentiation() =>
            _scopes.Count() == 0
                ? String.Empty
                : _scopes.Peek().indentation;

        protected abstract void OnWriteLine(string line);
    }
}
