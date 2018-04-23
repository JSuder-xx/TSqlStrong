using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using TSqlStrong.VerificationResults;
using LowSums;

namespace TSqlStrong.TypeSystem
{
    public abstract class DataType
    {

        #region Public Instance

        /// <summary>
        /// Can this type be assigned to the other type? Generally this uses covariant typing or a test as 
        /// to whether the destination is a super type (less refined and therefore a WIDER domain). 
        /// </summary>
        /// <param name="otherType"></param>
        /// <returns></returns>
        public ITry<Unit> IsAssignableTo(DataType otherType) => this.OnIsAssignableTo(otherType);

        /// <summary>
        /// When this type is refining another type for type refinements. 
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public DataType Refine(DataType other) => OnRefine(other);

        /// <summary>Convenience utility to convert this data type to a nullable data type.</summary>
        public DataType ToNullable() => 
            this is NullableDataType ? this : new NullableDataType(this);

        public bool IsNullOrNullable =>
            this is NullableDataType || this is NullDataType;

        /// <summary>
        /// Is this type comparable with the other type?
        /// </summary>
        /// <param name="otherType"></param>
        /// <returns></returns>
        public ITry<Unit> CanCompareWith(DataType otherType) => this.OnCanCompareWith(otherType);

        /// <summary>
        /// A numerical representation of the membership of this type (considered as a set). 
        /// </summary>
        public virtual int SizeOfDomain => 0;

        #endregion

        #region Static (Pattern Matching)

        public static readonly Func<DataType, bool> CarriesNullValue = (DataType dataType) =>
            dataType is NullDataType || dataType is NullableDataType;

        public static DataType Subtract(DataType left, DataType right) =>
            (left is NullableDataType leftAsNullable) ? ((right is NullDataType) ? leftAsNullable.DataType : Subtract(leftAsNullable.DataType, right).ToNullable())
            : (left is SqlDataTypeWithKnownSet leftAsWellKnown) && (right is SqlDataTypeWithKnownSet rightAsWellKnown) ? SqlDataTypeWithKnownSet.Difference(leftAsWellKnown, rightAsWellKnown)
            : (left is SqlDataType) && (right is SqlDataTypeWithKnownSet rightAsWellKnown2) ? rightAsWellKnown2.Invert()
            : left;

        public static IMaybe<DataType> Conjunction(DataType left, DataType right) =>
            (left is SqlDataTypeWithKnownSet leftWellKnown) && (!leftWellKnown.Include) && (right is SqlDataTypeWithKnownSet rightWellKnown) && (!rightWellKnown.Include)
                ? Maybe.Some(SqlDataTypeWithKnownSet.Union(leftWellKnown, rightWellKnown))
                : Maybe.None<DataType>();

        public static IMaybe<DataType> NegationOfConjunction(DataType left, DataType right) =>
            (left is SqlDataTypeWithKnownSet leftWellKnown) && (!leftWellKnown.Include) && (right is SqlDataTypeWithKnownSet rightWellKnown) && (!rightWellKnown.Include)
                ? Maybe.Some(SqlDataTypeWithKnownSet.Union(leftWellKnown, rightWellKnown).Invert())
                : Maybe.None<DataType>();

        public static IMaybe<DataType> Disjunction(DataType left, DataType right)
        {
            if ((left is SqlDataTypeWithKnownSet leftWellKnown) 
                && (right is SqlDataTypeWithKnownSet rightWellKnown) 
                && (rightWellKnown.Include) && (leftWellKnown.Include)
                && (leftWellKnown.SqlDataTypeOption == rightWellKnown.SqlDataTypeOption))
                return Return(leftWellKnown.UnionWith(rightWellKnown) as DataType);
            if ((left is NullDataType) && (right is NullDataType))
                return Return(NullDataType.Instance);
            if (left is NullDataType)
                return Return(right.ToNullable());
            if (right is NullDataType)
                return Return(left.ToNullable());
            if ((left is NullableDataType) || (right is NullableDataType))
                return Disjunction(
                    GetInnerType(left),
                    GetInnerType(right)
                ).Select(it => it.ToNullable());
            if ((left is SqlDataType leftSqlType) 
                && (right is SqlDataType rightSqlType)
                && (leftSqlType.SqlDataTypeOption == rightSqlType.SqlDataTypeOption))
                return Return(left.SizeOfDomain > right.SizeOfDomain ? left : right);

            return None();

            DataType GetInnerType(DataType dataType) => dataType is NullableDataType asNullable ? GetInnerType(asNullable.DataType) : dataType;
            IMaybe<DataType> Return(DataType dataType) => dataType.ToMaybe();
            IMaybe<DataType> None() => Maybe.None<DataType>();
        }

        #endregion

        #region Protected Virtual

        protected virtual DataType OnRefine(DataType other) => this;

        protected virtual ITry<Unit> OnIsAssignableTo(DataType otherType) => Try.Failure<Unit>(Messages.CannotAssignTo(this.ToString(), otherType.ToString()));

        protected virtual ITry<Unit> OnCanCompareWith(DataType otherType) =>
            this.IsAssignableTo(otherType)
                .SelectFirstSuccess(() => otherType.IsAssignableTo(this))
                .SelectError(err => Messages.CannotCompare(this.ToString(), otherType.ToString(), because: err));

        #endregion

    }
}
