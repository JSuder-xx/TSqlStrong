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
    public class DomainDecoratorDataType : DataType
    {
        private string _domain = String.Empty;
        private DataType _decorates;

        public DomainDecoratorDataType(DataType decorates, string domain) : base()
        {
            _domain = domain;
            _decorates = decorates; 
        }

        public override int SizeOfDomain => 1;

        public static DomainDecoratorDataType Int(string domain) => new DomainDecoratorDataType(SqlDataType.Int, domain);
        
        public static DomainDecoratorDataType VarChar(string domain) => new DomainDecoratorDataType(SqlDataType.VarChar, domain);
        
        public string Domain => _domain;

        public static DataType From(DataType otherType, string domain) => new DomainDecoratorDataType(otherType, domain);

        public override bool Equals(object other) =>
            (other is DomainDecoratorDataType otherAsDomain)
                ? IsAssignableToDomain(otherAsDomain.Domain) && Decorates.Equals(otherAsDomain.Decorates)
                : false;

        public DataType Decorates => _decorates;

        public override int GetHashCode() => (Domain.GetHashCode() * 231) + Decorates.GetHashCode();
        
        public bool IsAssignableToDomain(string otherDomain) => String.Equals(_domain, otherDomain, StringComparison.InvariantCultureIgnoreCase);

        public override DataType Unwrapped => Decorates;

        public override string ToString() => $"{_decorates.ToString()}<{Domain}>";

        protected override ITry<Unit> OnCanCompareWith(DataType otherType) =>
            (otherType is DomainDecoratorDataType)
                // base behavior is that comparison is valid when assignable either way
                ? base.OnCanCompareWith(otherType)
                // if the other is not a domain then I just want to ensure that whoever I decorate can be compared with the other
                : Decorates.CanCompareWith(otherType);

        // SizedSqlDataType(Varchar, NVarChar)
        // Numeric(Float, Decimal, Money)
        // DateSqlDataType(Date, DateTime, Time)
        // Bit, Int

        protected override ITry<Unit> OnIsAssignableTo(DataType otherType) =>
            (otherType is NullableDataType otherAsNullable) ? IsAssignableTo(otherType.Unwrapped)
            : (otherType is DomainDecoratorDataType otherAsDomain)
                ? Decorates.IsAssignableTo(otherAsDomain.Decorates)
                    .SelectMany(_ =>
                        IsAssignableToDomain(otherAsDomain.Domain)
                            ? Try.SuccessUnit
                            : Try.Failure<Unit>(Messages.CannotAssignTo(this.ToString(), otherAsDomain.ToString()))
                    )
                : Decorates.IsAssignableTo(otherType);                
    }
}
