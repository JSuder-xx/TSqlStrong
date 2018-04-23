using System;
using System.Collections.Generic;
using Microsoft.SqlServer.TransactSql.ScriptDom;
using LowSums;

namespace TSqlStrong.TypeSystem
{
    public class FunctionDataType : SubroutineDataType
    {
        private readonly Lazy<DataType> _typeCheckBody;
        private readonly IMaybe<DataType> _declaredReturnTypeMaybe;

        public FunctionDataType(string name, TSqlFragment sqlFragment, IMaybe<DataType> declaredReturnTypeMaybe, Lazy<DataType> typeCheckBody, IEnumerable<Parameter> parameters)
            : base(name, sqlFragment, parameters)
        {
            _declaredReturnTypeMaybe = declaredReturnTypeMaybe;            
            _typeCheckBody = typeCheckBody;
        }

        public DataType ReturnType_CompilingIfNecessary =>
            _declaredReturnTypeMaybe.Coalesce(() => _typeCheckBody.Value);
       
        public (IMaybe<DataType> declaredMaybe, DataType bodyExpression) GetReturnTypes_ForcingCompilationIfNotAlready() =>
            (declaredMaybe: _declaredReturnTypeMaybe, bodyExpression: _typeCheckBody.Value);
    }
}
