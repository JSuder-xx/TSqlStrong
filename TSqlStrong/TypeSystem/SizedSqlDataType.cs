using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LowSums;
using ScriptDom = Microsoft.SqlServer.TransactSql.ScriptDom;

namespace TSqlStrong.TypeSystem
{
    /// <summary>
    /// The SQL data types that have a simple scalar size.
    /// </summary>
    public enum SizedDataTypeOption
    {
        VarChar,
        NVarChar,
        Binary,
        VarBinary
    }

    public static class SizedDataTypeOptionExtensions
    {
        public static ScriptDom.SqlDataTypeOption ToSqlDataTypeOption(this SizedDataTypeOption value) =>
            value == SizedDataTypeOption.Binary ? ScriptDom.SqlDataTypeOption.Binary
            : value == SizedDataTypeOption.VarBinary ? ScriptDom.SqlDataTypeOption.VarBinary
            : value == SizedDataTypeOption.VarChar ? ScriptDom.SqlDataTypeOption.VarChar
            : ScriptDom.SqlDataTypeOption.NVarChar;
    }

    public class SizedSqlDataType : SqlDataType
    {
        private readonly int _size;
        public SizedSqlDataType(SizedDataTypeOption option, int size) 
            : base(option.ToSqlDataTypeOption())
        {
            _size = size;
        }

        public SizedSqlDataType(SizedDataTypeOption option)
            : this(option, Int32.MaxValue)
        {
        }

        public int Size => _size;

        public override string ToString() => $"{base.ToString()}[{Size.ToString()}]";

        protected override ITry<Unit> OnIsAssignableTo(DataType otherType) =>
            NullableDataType.UnwrapIfNull(otherType).Let(otherNotNull =>
                otherNotNull is SizedSqlDataType otherAsSized
                    ? base.OnIsAssignableTo(otherNotNull).SelectMany((_) =>
                        (otherAsSized.Size < Size)
                        ? Try.Failure<Unit>($"Cannot assign {this.ToString()} to {otherAsSized.ToString()} because data could be lost.")
                        : Try.SuccessUnit
                    )
                    : base.OnIsAssignableTo(otherNotNull)
            );
    }
}
