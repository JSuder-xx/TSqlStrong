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
        public readonly static SqlDataType Numeric = new SqlDataType(ScriptDom.SqlDataTypeOption.Real);
        public readonly static SqlDataType VarChar = new SqlDataType(ScriptDom.SqlDataTypeOption.VarChar);
        public readonly static SqlDataType NVarChar = new SqlDataType(ScriptDom.SqlDataTypeOption.NVarChar);
        public readonly static SqlDataType Money = new SqlDataType(ScriptDom.SqlDataTypeOption.Money);
        public readonly static SqlDataType Date = new SqlDataType(ScriptDom.SqlDataTypeOption.Date);
        public readonly static SqlDataType Time = new SqlDataType(ScriptDom.SqlDataTypeOption.Time);
        public readonly static SqlDataType DateTime = new SqlDataType(ScriptDom.SqlDataTypeOption.DateTime);

        public static ScriptDom.SqlDataTypeOption GetSqlDataTypeOptionFor<TCLT>()
        {
            var cltType = typeof(TCLT);
            if (cltType == typeof(int))
                return ScriptDom.SqlDataTypeOption.Int;
            else if (cltType == typeof(string))
                return ScriptDom.SqlDataTypeOption.VarChar;
            else if (cltType == typeof(double))
                return ScriptDom.SqlDataTypeOption.Real;
            else if (cltType == typeof(decimal))
                return ScriptDom.SqlDataTypeOption.Numeric;
            else
                throw new ArgumentException($"Unsupported type {cltType.FullName}");
        }       
        
        public SqlDataType(ScriptDom.SqlDataTypeOption sqlDataType)
        {            
            _sqlDataType = sqlDataType;
        }
        
        public ScriptDom.SqlDataTypeOption SqlDataTypeOption => _sqlDataType;

        public const int StaticWidth = Int32.MaxValue - 2;

        public override int SizeOfDomain => StaticWidth;

        protected override ITry<Unit> OnIsAssignableTo(DataType otherType)
        {
            if (otherType is NullDataType)
                return Try.SuccessUnit;

            if (otherType is NullableDataType otherAsNullable)
                return IsAssignableTo(otherAsNullable.DataType);

            if (otherType is ColumnDataType columnDataType)
                otherType = columnDataType.DataType;

            if (otherType is SqlDataType otherSqlDataType) 
            {
                if (SqlDataTypeOption != otherSqlDataType.SqlDataTypeOption)
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
}
