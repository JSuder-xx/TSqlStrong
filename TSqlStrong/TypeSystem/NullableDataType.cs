using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LowSums;

namespace TSqlStrong.TypeSystem
{
    /// <summary>
    /// Decorate another data type with nullability.
    /// </summary>
    public class NullableDataType : DataType
    {
        private readonly DataType _dataType;

        public NullableDataType(DataType dataType) 
        {
            _dataType = dataType;
        }

        public DataType DataType => _dataType;
                
        public override string ToString() => $"Nullable<{DataType.ToString()}>";

        public override int GetHashCode() => DataType.GetHashCode() * 19;

        public override int SizeOfDomain => DataType.SizeOfDomain + 1;

        public override DataType Unwrapped => DataType;

        public override bool Equals(object other) =>
            other is NullableDataType otherAsNullableDataType
                ? this.DataType.Equals(otherAsNullableDataType.DataType)
                : false;

        public static DataType UnwrapIfNull(DataType dataType) => 
            dataType is NullableDataType asNullable 
                ? asNullable.DataType 
                : dataType;

        protected override ITry<Unit> OnIsAssignableTo(DataType otherType) =>
            otherType is NullableDataType otherAsNullable
                ? DataType.IsAssignableTo(otherAsNullable.DataType)
                : Try.Failure<Unit>($"Cannot assign a nullable {DataType.ToString()} to a {otherType.ToString()}");
    }
}
