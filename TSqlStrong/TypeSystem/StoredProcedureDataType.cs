using System;
using System.Collections.Generic;
using Microsoft.SqlServer.TransactSql.ScriptDom;
using TSqlStrong.Symbols;
using TSqlStrong.VerificationResults;
using LowSums;

namespace TSqlStrong.TypeSystem
{
    public class StoredProcedureDataType : SubroutineDataType
    {
        private readonly Lazy<bool> _referencesTempTable;
        private readonly Func<StackFrame, IEnumerable<Issue>> _getIssuesAtApplication;

        public StoredProcedureDataType(string name, TSqlFragment sqlFragment, IEnumerable<Parameter> parameters, Lazy<bool> referencesTempTable, Func<StackFrame, IEnumerable<Issue>> getIssuesAtApplication)
            : base(name, sqlFragment, parameters)
        {
            _referencesTempTable = referencesTempTable;
            _getIssuesAtApplication = getIssuesAtApplication;
        }

        public bool ReferencesTempTable => _referencesTempTable.Value;

        public IEnumerable<Issue> GetIssuesAtApplication(StackFrame frame) => _getIssuesAtApplication(frame);

        public void PerformTopLevelTypeCheck()
        {
            _referencesTempTable.Value.SideEffect();
        }
    }
}
