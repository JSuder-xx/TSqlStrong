using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ScriptDom = Microsoft.SqlServer.TransactSql.ScriptDom;

using TSqlStrong.VerificationResults;
using LowSums;

namespace TSqlStrong.TypeSystem
{
    /// <summary>
    /// Representation of stock simple data types. 
    /// TODO: Support type parameters such as size of varchar and precision of floating point.
    /// </summary>
    public class SqlDataType : DataType
    {
        private ScriptDom.SqlDataTypeOption _sqlDataType;

        public readonly static SqlDataType Bit = new SqlDataType(ScriptDom.SqlDataTypeOption.Bit);
        public readonly static SqlDataType Int = new SqlDataType(ScriptDom.SqlDataTypeOption.Int);

        public readonly static SqlDataType VarChar = new SizedSqlDataType(SizedDataTypeOption.VarChar);
        public readonly static SqlDataType NVarChar = new SizedSqlDataType(SizedDataTypeOption.NVarChar);

        public readonly static SqlDataType Real = new SqlDataType(ScriptDom.SqlDataTypeOption.Real);
        public readonly static SqlDataType Decimal = new SqlDataType(ScriptDom.SqlDataTypeOption.Decimal);

        public readonly static SqlDataType Money = new SqlDataType(ScriptDom.SqlDataTypeOption.Money);

        public readonly static SqlDataType Date = new SqlDataType(ScriptDom.SqlDataTypeOption.Date);
        public readonly static SqlDataType Time = new SqlDataType(ScriptDom.SqlDataTypeOption.Time);
        public readonly static SqlDataType DateTime = new SqlDataType(ScriptDom.SqlDataTypeOption.DateTime);
        
        public SqlDataType(ScriptDom.SqlDataTypeOption sqlDataType)
        {            
            _sqlDataType = sqlDataType;
        }

        public ScriptDom.SqlDataTypeOption SqlDataTypeOption => _sqlDataType;

        public const int StaticWidth = Int32.MaxValue - 2;

        public override int SizeOfDomain => StaticWidth;

        public override bool Equals(object obj) =>
            obj is SqlDataType objAsSqlDataType 
                ? objAsSqlDataType.SqlDataTypeOption == SqlDataTypeOption
                : base.Equals(obj);

        public override int GetHashCode() =>
            _sqlDataType.GetHashCode();

        protected override ITry<Unit> OnIsAssignableTo(DataType otherType)
        {
            if (otherType is NullDataType)
                return Try.SuccessUnit;

            if (otherType is ColumnDataType)
                otherType = otherType.Unwrapped;
            if (otherType is NullableDataType)
                otherType = otherType.Unwrapped;

            if (otherType is SqlDataType otherSqlDataType) 
            {
                if (!SqlDataTypeOption.CanAssignTo(otherSqlDataType.SqlDataTypeOption))
                    return Fail();

                if (this.SizeOfDomain > otherType.SizeOfDomain)
                    return Fail();

                return Try.SuccessUnit;
            }
            else
                return base.OnIsAssignableTo(otherType);

            ITry<Unit> Fail() => Try.Failure<Unit>(Messages.CannotAssignTo(this.ToString(), otherType.ToString()));
        }

        public override string ToString() => SqlDataTypeOption.ToString();                        
    }

    public static class SqlDataTypeOptionExtensions
    {
        public static bool IsNumeric(this ScriptDom.SqlDataTypeOption source) =>
            // TODO: Might make sense to put in a dictionary
            (source == ScriptDom.SqlDataTypeOption.BigInt)
            || (source == ScriptDom.SqlDataTypeOption.Bit)
            || (source == ScriptDom.SqlDataTypeOption.Decimal)
            || (source == ScriptDom.SqlDataTypeOption.Float)
            || (source == ScriptDom.SqlDataTypeOption.Int)
            || (source == ScriptDom.SqlDataTypeOption.Money)
            || (source == ScriptDom.SqlDataTypeOption.Numeric)
            || (source == ScriptDom.SqlDataTypeOption.Real)
            || (source == ScriptDom.SqlDataTypeOption.SmallInt)
            || (source == ScriptDom.SqlDataTypeOption.SmallMoney)
            || (source == ScriptDom.SqlDataTypeOption.TinyInt);

        public static bool CanAssignTo(this ScriptDom.SqlDataTypeOption source, ScriptDom.SqlDataTypeOption dest) =>
            (source == dest)
            || ((source == ScriptDom.SqlDataTypeOption.VarChar) && (dest == ScriptDom.SqlDataTypeOption.NVarChar))
            || ((source == ScriptDom.SqlDataTypeOption.Int) && (dest == ScriptDom.SqlDataTypeOption.Float));
    }
}
