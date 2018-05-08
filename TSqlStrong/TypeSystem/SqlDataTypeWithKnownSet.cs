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
    /// A very basic (sophmoric) union type representation for SqlDataTypes. 
    /// </summary>
    public class SqlDataTypeWithKnownSet : SqlDataType
    {
        #region private state

        private readonly HashSet<Object> _set;
        private readonly bool _include;

        #endregion

        #region constructors and factories 

        public SqlDataTypeWithKnownSet(bool include, IEnumerable<Object> set, ScriptDom.SqlDataTypeOption typeOption) : base(typeOption)
        {
            _set = new HashSet<Object>(set);
            _include = include;
        }

        public static SqlDataTypeWithKnownSet IntIncludingSet(params int[] set) => new SqlDataTypeWithKnownSet(true, set.Cast<Object>(), ScriptDom.SqlDataTypeOption.Int);
        public static SqlDataTypeWithKnownSet IntExcludingSet(params int[] set) => new SqlDataTypeWithKnownSet(false, set.Cast<Object>(), ScriptDom.SqlDataTypeOption.Int);

        public static SqlDataTypeWithKnownSet VarCharIncludingSet(params string[] set) => new SqlDataTypeWithKnownSet(true, set, ScriptDom.SqlDataTypeOption.VarChar);
        public static SqlDataTypeWithKnownSet VarCharExcludingSet(params string[] set) => new SqlDataTypeWithKnownSet(false, set, ScriptDom.SqlDataTypeOption.VarChar);

        public static SqlDataTypeWithKnownSet NVarCharIncludingSet(params string[] set) => new SqlDataTypeWithKnownSet(true, set, ScriptDom.SqlDataTypeOption.NVarChar);

        public static SqlDataTypeWithKnownSet NumericIncludingSet(params decimal[] set) => new SqlDataTypeWithKnownSet(true, set.Cast<Object>(), ScriptDom.SqlDataTypeOption.Decimal);
        public static SqlDataTypeWithKnownSet NumericExcludingSet(params decimal[] set) => new SqlDataTypeWithKnownSet(false, set.Cast<Object>(), ScriptDom.SqlDataTypeOption.Decimal);

        public static SqlDataTypeWithKnownSet MoneyIncludingSet(params decimal[] set) => new SqlDataTypeWithKnownSet(true, set.Cast<Object>(), ScriptDom.SqlDataTypeOption.Money);
        public static SqlDataTypeWithKnownSet MoneyExcludingSet(params decimal[] set) => new SqlDataTypeWithKnownSet(false, set.Cast<Object>(), ScriptDom.SqlDataTypeOption.Money);

        public static SqlDataTypeWithKnownSet RealIncludingSet(params double[] set) => new SqlDataTypeWithKnownSet(true, set.Cast<Object>(), ScriptDom.SqlDataTypeOption.Float);
        public static SqlDataTypeWithKnownSet RealExcludingSet(params double[] set) => new SqlDataTypeWithKnownSet(false, set.Cast<Object>(), ScriptDom.SqlDataTypeOption.Float);

        #endregion

        public static SqlDataTypeWithKnownSet Difference(SqlDataTypeWithKnownSet left, SqlDataTypeWithKnownSet right) =>
            (left.SqlDataTypeOption != right.SqlDataTypeOption) 
                ? (new InvalidOperationException(nameof(Difference) + "When subtracting KnownSets they must be of the same value.")).AsValue<SqlDataTypeWithKnownSet>()
                : (left.Include)                
                    ? (right.Include)
                        ? new SqlDataTypeWithKnownSet(true, left.Values.Where(leftValue => !right.ContainsItem(leftValue)), left.SqlDataTypeOption)
                        : new SqlDataTypeWithKnownSet(true, left.Values.Concat(right.Values).Distinct(), left.SqlDataTypeOption)
                    : (right.Include)
                        ? new SqlDataTypeWithKnownSet(false, left.Values.Concat(right.Values).Distinct(), left.SqlDataTypeOption)
                        : (new InvalidOperationException(nameof(Difference) + "When subtracting KnownSets they must be of the same value.")).AsValue<SqlDataTypeWithKnownSet>();

        public static SqlDataTypeWithKnownSet operator-(SqlDataTypeWithKnownSet left, SqlDataTypeWithKnownSet right) => Difference(left, right);
        
        public static SqlDataTypeWithKnownSet Union(SqlDataTypeWithKnownSet left, SqlDataTypeWithKnownSet right) 
        {
            if (left.SqlDataTypeOption != right.SqlDataTypeOption)
                throw new InvalidOperationException(nameof(Union) + "When union KnownSets they must be of the same value.");
            if (left.Include != right.Include)
                throw new InvalidOperationException(nameof(Union) + "Must be same inclusion.");

            return new SqlDataTypeWithKnownSet(left.Include, left.Values.Concat(right.Values).Distinct(), left.SqlDataTypeOption);
        }

        public static SqlDataTypeWithKnownSet operator +(SqlDataTypeWithKnownSet left, SqlDataTypeWithKnownSet right) => Union(left, right);

        public SqlDataTypeWithKnownSet UnionWith(DataType other) =>
            (other is SqlDataTypeWithKnownSet otherAsKnownSet)
                ? Union(this, otherAsKnownSet)
                : this;

        #region public

        public SqlDataType UpCast() => new SqlDataType(SqlDataTypeOption);

        public bool ContainsItem(Object item) => _set.Contains(item);

        public override int SizeOfDomain => Include ? _set.Count() : SqlDataType.StaticWidth - _set.Count();

        public IEnumerable<Object> Values => _set.ToArray();

        public SqlDataTypeWithKnownSet NegateNumericValues() =>
            SqlDataTypeOption.IsNumeric()
                ? new SqlDataTypeWithKnownSet(Include, _set.ToArray().Select(NegateValue), SqlDataTypeOption)
                : (new InvalidOperationException($"Cannot negate numeric values of {SqlDataTypeOption.ToString()}")).AsValue<SqlDataTypeWithKnownSet>();

        public bool Include => _include;

        public SqlDataTypeWithKnownSet Invert() => new SqlDataTypeWithKnownSet(!Include, _set, SqlDataTypeOption);

        public override string ToString() => $"{base.ToString()}{(_include ? String.Empty : "~")}{{{_set.CommaDelimit()}}}";

        protected override DataType OnRefine(DataType other) =>
            this.Include ? this : DataType.Subtract(other, this.Invert());

        public override bool Equals(object other) =>        
            (other is SqlDataTypeWithKnownSet otherAsKnownSet)
                ? (otherAsKnownSet.SqlDataTypeOption != SqlDataTypeOption)
                    ? false
                    : Values.HaveEquivalentMembership(otherAsKnownSet.Values)
                : base.Equals(other);        

        public override int GetHashCode() => Values.GenerateListHashCode();

        #endregion

        #region private

        private static Object NegateValue(Object obj) =>
            (obj is int asInt) ? (-1 * asInt)
            : (obj is float asFloat) ? (-1 * asFloat)
            : (obj is decimal asDecimal) ? (-1 * asDecimal)
            : (obj is double asDouble) ? (-1 * asDouble)
            : (obj is Single asSingle) ? (-1 * asSingle)
            : (new InvalidOperationException($"")).AsValue<Object>();
        

        #endregion

        #region protected overrides

        protected override ITry<Unit> OnCanCompareWith(DataType otherType) =>
            !(otherType is SqlDataTypeWithKnownSet) && otherType is SqlDataType ? Try.SuccessUnit : base.OnCanCompareWith(otherType);

        protected override ITry<Unit> OnIsAssignableTo(DataType otherType) =>
            (otherType is SqlDataTypeWithKnownSet otherOfSameType) ? IsAssignableTo(otherOfSameType)
            : (otherType is SqlDataTypeWithDomain) ? Try.Failure<Unit>(Messages.CannotAssignTo(this.ToString(), otherType.ToString()))
            : base.OnIsAssignableTo(otherType);

        private ITry<Unit> IsAssignableTo(SqlDataTypeWithKnownSet otherType) =>
            (
                // {1,2} => {1,2,3} I can assign a positive source to a positive destination if all elements of the source are found in the destination
                Include && otherType.Include ? _set.All(it => otherType.ContainsItem(it))
                // {1,2} => ~{3} I can assign a positive source to a negative destination if no element of source is a member of the negative destination.
                : Include && !otherType.Include ? !_set.Any(it => otherType.ContainsItem(it))
                // ~{1, 2} => {3, 4} I can assign a negative source to a positive destination if no elements of the destination are in the source.
                : !Include && otherType.Include ? !otherType.Values.Any(it => ContainsItem(it))
                // ~{1, 2, 3} => ~{1, 2} I can assign a negative source to a negative destination if all elements of the destination are found in the source.
                : otherType.Values.All(it => ContainsItem(it))
            )
                ? Try.SuccessUnit
                : Try.Failure<Unit>(Messages.CannotAssignTo(this.ToString(), otherType.ToString()));

        #endregion
    }
}
