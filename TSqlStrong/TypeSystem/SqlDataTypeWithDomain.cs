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
    /// Represents a named domain space for a simple data type. For example, the primary key of a table is of a type that
    /// represents the set of identities for that table. Foreign keys to said table should be of that same type.
    /// </summary>
    public class SqlDataTypeWithDomain : SqlDataType
    {
        private string _domain = String.Empty;

        public SqlDataTypeWithDomain(ScriptDom.SqlDataTypeOption sqlDataType, string domain) : base(sqlDataType)
        {
            _domain = domain;
        }

        public override int SizeOfDomain => 1;

        public new static SqlDataType Int(string domain) => new SqlDataTypeWithDomain(ScriptDom.SqlDataTypeOption.Int, domain);
        
        public new static SqlDataType VarChar(string domain) => new SqlDataTypeWithDomain(ScriptDom.SqlDataTypeOption.VarChar, domain);
        
        public string Domain => _domain;

        public static DataType From(DataType otherType, string domain) =>
            otherType is NullableDataType otherAsNullable ? From(otherAsNullable.DataType, domain).ToNullable()
            : otherType is SqlDataType asSqlType ? new SqlDataTypeWithDomain(asSqlType.SqlDataTypeOption, domain)
            : otherType;

        public override bool Equals(object other) =>
            (other is SqlDataTypeWithDomain otherAsDomain)
                ? IsAssignableToDomain(otherAsDomain.Domain) && SqlDataTypeOption == otherAsDomain.SqlDataTypeOption
                : base.Equals(other);

        public override int GetHashCode() => (Domain.GetHashCode() * 231) + SqlDataTypeOption.GetHashCode();
        
        public bool IsAssignableToDomain(string otherDomain) => String.Equals(_domain, otherDomain, StringComparison.InvariantCultureIgnoreCase);

        public override string ToString() => $"{base.ToString()}<{Domain}>";

        protected override ITry<Unit> OnCanCompareWith(DataType otherType) =>
            !(otherType is SqlDataTypeWithDomain) && otherType is SqlDataType ? Try.SuccessUnit : base.OnCanCompareWith(otherType);
        
        protected override ITry<Unit> OnIsAssignableTo(DataType otherType) =>        
            (otherType is SqlDataTypeWithDomain otherSqlDataType)
                ? base.OnIsAssignableTo(otherSqlDataType)
                    .SelectMany(_ =>
                        IsAssignableToDomain(otherSqlDataType.Domain)
                            ? Try.SuccessUnit
                            : Try.Failure<Unit>(Messages.CannotAssignTo(this.ToString(), otherSqlDataType.ToString()))
                    )
                : (otherType is SqlDataTypeWithKnownSet) 
                    ? Try.Failure<Unit>(Messages.CannotAssignTo(this.ToString(), otherType.ToString()))
                    : base.OnIsAssignableTo(otherType);                
    }
}
