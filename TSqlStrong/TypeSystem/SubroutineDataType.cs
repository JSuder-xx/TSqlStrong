using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.SqlServer.TransactSql.ScriptDom;
using LowSums;

namespace TSqlStrong.TypeSystem
{
    public abstract class SubroutineDataType : DataType
    {

        // TODO: Store input/output and other metadata?
        public class Parameter
        {
            public Parameter(string name, DataType dataType)
            {
                Name = name;
                DataType = dataType;
            }

            public readonly string Name;
            public readonly DataType DataType;
        }

        private readonly string _name;
        private readonly TSqlFragment _sqlFragment;
        private readonly IEnumerable<Parameter> _parameters;
        private readonly Lazy<Dictionary<string, Parameter>> _parameterDictionaryLazy;

        public SubroutineDataType(string name, TSqlFragment sqlFragment, IEnumerable<Parameter> parameters)
        {
            _name = name;
            _sqlFragment = sqlFragment;
            _parameters = parameters;
            _parameterDictionaryLazy = new Lazy<Dictionary<string, Parameter>>(() => _parameters.ToDictionary(it => it.Name.ToLower()));
        }

        public string Name => _name;

        public IEnumerable<Parameter> Parameters => _parameters;

        public IMaybe<Parameter> FindParameterMaybe(string parameterName) =>
            _parameterDictionaryLazy.Value.TryGetValue(parameterName.ToLower(), out Parameter result)
                ? result.ToMaybe()
                : Maybe.None<Parameter>();
        
        public TSqlFragment SqlFragment => _sqlFragment;
        

        protected sealed override ITry<Unit> OnCanCompareWith(DataType otherType) => Try.Failure<Unit>("Cannot compare subroutines with anything");

        protected sealed override ITry<Unit> OnIsAssignableTo(DataType otherType) => Try.Failure<Unit>("Cannot assign subroutines to anything.");
    }
}
