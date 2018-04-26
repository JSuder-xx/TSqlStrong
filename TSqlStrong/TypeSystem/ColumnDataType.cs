using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.SqlServer.TransactSql.ScriptDom;
using LowSums;

namespace TSqlStrong.TypeSystem
{
    public partial class ColumnDataType : DataType
    {
        private readonly ColumnName.Base _name;
        private readonly DataType _dataType;
        private readonly IMaybe<TSqlFragment> _definingLocationMaybe;

        public ColumnDataType(ColumnName.Base name, DataType dataType, IMaybe<TSqlFragment> definingLocation)
        {
            _name = name;            
            _dataType = dataType;
            _definingLocationMaybe = definingLocation;
        }

        public ColumnDataType(ColumnName.Base name, DataType dataType) : this(name, dataType, Maybe.None<TSqlFragment>())
        {
        }

        public ColumnName.Base Name => _name;

        public DataType DataType => _dataType;
        public IMaybe<TSqlFragment> DefiningLocationMaybe => _definingLocationMaybe;

        public ColumnDataType WithNewDataType(DataType dataType) =>
            new ColumnDataType(_name, dataType, _definingLocationMaybe);

        public static ITry<ColumnDataType> Join(ColumnDataType left, ColumnDataType right) =>
            DataType.Disjunction(left.DataType, right.DataType)
                .ToTry(VerificationResults.Messages.UnableToJoinTypes(left.DataType.ToString(), right.DataType.ToString()))
                .Select(dataType =>
                    new ColumnDataType(
                        name: ColumnName.Anonymous.Instance,
                        dataType: dataType,
                        definingLocation: Maybe.None<TSqlFragment>()
                    )
                );
        
        public override string ToString() => $"{Name.ToString()} {DataType.ToString()}";
        
        public override int SizeOfDomain => DataType.SizeOfDomain;

        public static DataType UnwrapIfColumnDataType(DataType dataType) =>
            dataType is ColumnDataType columnDataType ? columnDataType.DataType : dataType;

        protected override ITry<Unit> OnIsAssignableTo(DataType otherType) =>
            (otherType is ColumnDataType otherColumnDataType)
                ? DataType.IsAssignableTo(otherColumnDataType.DataType)
                    .SelectMany(_ => Name.IsAssignableTo(otherColumnDataType.Name))
                : DataType.IsAssignableTo(otherType);

        protected override ITry<Unit> OnCanCompareWith(DataType otherType) =>
            (otherType is ColumnDataType otherColumnDataType)
                ? DataType.CanCompareWith(otherColumnDataType.DataType)
                : DataType.CanCompareWith(otherType);
    }
}
