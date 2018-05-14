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
    public class KnownSetDecoratorDataType : DataType
    {
        #region private state

        private readonly HashSet<Object> _set;
        private readonly bool _include;
        private readonly DataType _decorates;

        #endregion

        #region constructors and factories 

        public KnownSetDecoratorDataType(bool include, IEnumerable<Object> set, DataType decorates) : base()
        {
            _set = new HashSet<Object>(set);
            _include = include;
            _decorates = decorates;
        }

        public static KnownSetDecoratorDataType IntIncludingSet(params int[] set) => new KnownSetDecoratorDataType(true, set.Cast<Object>(), SqlDataType.Int);
        public static KnownSetDecoratorDataType IntExcludingSet(params int[] set) => new KnownSetDecoratorDataType(false, set.Cast<Object>(), SqlDataType.Int);

        public static KnownSetDecoratorDataType VarCharIncludingSet(params string[] set) => 
            new KnownSetDecoratorDataType(true, set, new SizedSqlDataType(SizedDataTypeOption.VarChar, set.Max(it => it.Length)));
        public static KnownSetDecoratorDataType VarCharExcludingSet(params string[] set) =>
            new KnownSetDecoratorDataType(false, set, new SizedSqlDataType(SizedDataTypeOption.VarChar, set.Max(it => it.Length)));

        public static KnownSetDecoratorDataType NVarCharIncludingSet(params string[] set) => new KnownSetDecoratorDataType(true, set, SqlDataType.NVarChar);

        public static KnownSetDecoratorDataType NumericIncludingSet(params decimal[] set) => new KnownSetDecoratorDataType(true, set.Cast<Object>(), SqlDataType.Decimal);
        public static KnownSetDecoratorDataType NumericExcludingSet(params decimal[] set) => new KnownSetDecoratorDataType(false, set.Cast<Object>(), SqlDataType.Decimal);

        public static KnownSetDecoratorDataType MoneyIncludingSet(params decimal[] set) => new KnownSetDecoratorDataType(true, set.Cast<Object>(), SqlDataType.Money);
        public static KnownSetDecoratorDataType MoneyExcludingSet(params decimal[] set) => new KnownSetDecoratorDataType(false, set.Cast<Object>(), SqlDataType.Money);

        public static KnownSetDecoratorDataType RealIncludingSet(params double[] set) => new KnownSetDecoratorDataType(true, set.Cast<Object>(), SqlDataType.Real);
        public static KnownSetDecoratorDataType RealExcludingSet(params double[] set) => new KnownSetDecoratorDataType(false, set.Cast<Object>(), SqlDataType.Real);

        #endregion

        #region public

        public static KnownSetDecoratorDataType Difference(KnownSetDecoratorDataType left, KnownSetDecoratorDataType right) =>
            (!left.Decorates.Equals(right.Decorates))
                ? (new InvalidOperationException(nameof(Difference) + "When subtracting KnownSets they must be of the same value.")).AsValue<KnownSetDecoratorDataType>()
                : (left.Include)
                    ? (right.Include)
                        ? new KnownSetDecoratorDataType(true, left.Values.Where(leftValue => !right.ContainsItem(leftValue)), left._decorates)
                        : new KnownSetDecoratorDataType(true, left.Values.Concat(right.Values).Distinct(), left._decorates)
                    : (right.Include)
                        ? new KnownSetDecoratorDataType(false, left.Values.Concat(right.Values).Distinct(), left._decorates)
                        : (new InvalidOperationException(nameof(Difference) + "When subtracting KnownSets they must be of the same value.")).AsValue<KnownSetDecoratorDataType>();

        public static KnownSetDecoratorDataType operator -(KnownSetDecoratorDataType left, KnownSetDecoratorDataType right) => Difference(left, right);

        public static KnownSetDecoratorDataType Union(KnownSetDecoratorDataType left, KnownSetDecoratorDataType right)
        {
            if (!left.Decorates.Equals(right.Decorates))
                throw new InvalidOperationException(nameof(Union) + "When union KnownSets they must be of the same value.");
            if (left.Include != right.Include)
                throw new InvalidOperationException(nameof(Union) + "Must be same inclusion.");

            return new KnownSetDecoratorDataType(left.Include, left.Values.Concat(right.Values).Distinct(), left._decorates);
        }

        public static KnownSetDecoratorDataType operator +(KnownSetDecoratorDataType left, KnownSetDecoratorDataType right) => Union(left, right);

        public KnownSetDecoratorDataType UnionWith(DataType other) =>
            (other is KnownSetDecoratorDataType otherAsKnownSet)
                ? Union(this, otherAsKnownSet)
                : this;

        public DataType UpCast() => _decorates;

        public DataType Decorates => _decorates;

        public override DataType Unwrapped => Decorates;

        public bool ContainsItem(Object item) => _set.Contains(item);

        public override int SizeOfDomain => Include ? _set.Count() : SqlDataType.StaticWidth - _set.Count();

        public IEnumerable<Object> Values => _set.ToArray();

        public KnownSetDecoratorDataType NegateNumericValues() =>
            new KnownSetDecoratorDataType(Include, _set.ToArray().Select(NegateValue), _decorates);

        public bool Include => _include;

        public KnownSetDecoratorDataType Invert() => new KnownSetDecoratorDataType(!Include, _set, _decorates);

        public override string ToString() => $"{_decorates.ToString()}{(_include ? String.Empty : "~")}{{{_set.CommaDelimit()}}}";

        protected override DataType OnRefine(DataType other) =>
            this.Include ? this : DataType.Subtract(other, this.Invert());

        public override bool Equals(object other) =>        
            (other is KnownSetDecoratorDataType otherAsKnownSet)
                ? (!otherAsKnownSet.Decorates.Equals(Decorates))
                    ? false
                    : Values.HaveEquivalentMembership(otherAsKnownSet.Values)
                : false;        

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
            (otherType is KnownSetDecoratorDataType)
                // base behavior is that comparison is valid when assignable either way
                ? base.OnCanCompareWith(otherType)
                // if the other is not a known set then I just want to ensure that whoever I decorate can be compared with the other
                : Decorates.CanCompareWith(otherType);                                

        protected override ITry<Unit> OnIsAssignableTo(DataType otherType) =>
            (otherType is NullableDataType otherAsNullable) ? IsAssignableTo(otherType.Unwrapped)
            : (otherType is KnownSetDecoratorDataType otherOfSameType) ? IsAssignableTo(otherOfSameType)
            : (otherType is DomainDecoratorDataType) ? Try.Failure<Unit>(Messages.CannotAssignTo(this.ToString(), otherType.ToString()))
            : _decorates.IsAssignableTo(otherType);

        private ITry<Unit> IsAssignableTo(KnownSetDecoratorDataType otherType) =>
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
