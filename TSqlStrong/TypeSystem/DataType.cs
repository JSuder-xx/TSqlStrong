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
        public ITry<Unit> IsAssignableTo(DataType otherType) =>
            this.OnIsAssignableTo(otherType);

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

        public virtual DataType Unwrapped => this;

        /// <summary>
        /// Keeep unwrapping a data type to get down to the inner-most type.
        /// </summary>
        /// <returns></returns>
        public DataType UnwrapToCore()
        {
            var current = this.Unwrapped;
            while (current.Unwrapped != current)            
                current = current.Unwrapped;
                            
            return current;
        }

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
            : (left is KnownSetDecoratorDataType leftAsWellKnown) && (right is KnownSetDecoratorDataType rightAsWellKnown) ? KnownSetDecoratorDataType.Difference(leftAsWellKnown, rightAsWellKnown)
            : (left is SqlDataType) && (right is KnownSetDecoratorDataType rightAsWellKnown2) ? rightAsWellKnown2.Invert()
            : left;

        public static IMaybe<DataType> Conjunction(DataType left, DataType right) =>
            (left is KnownSetDecoratorDataType leftWellKnown) && (!leftWellKnown.Include) && (right is KnownSetDecoratorDataType rightWellKnown) && (!rightWellKnown.Include)
                ? Maybe.Some(KnownSetDecoratorDataType.Union(leftWellKnown, rightWellKnown))
                : Maybe.None<DataType>();

        public static IMaybe<DataType> NegationOfConjunction(DataType left, DataType right) =>
            (left is KnownSetDecoratorDataType leftWellKnown) && (!leftWellKnown.Include) && (right is KnownSetDecoratorDataType rightWellKnown) && (!rightWellKnown.Include)
                ? Maybe.Some(KnownSetDecoratorDataType.Union(leftWellKnown, rightWellKnown).Invert())
                : Maybe.None<DataType>();

        public static IMaybe<DataType> Disjunction(DataType left, DataType right)
        {
            if ((left is ColumnDataType leftColumn) && (right is ColumnDataType rightColumn) && leftColumn.Name.Equals(rightColumn.Name))
                return Disjunction(left.Unwrapped, right.Unwrapped).Select((dataType) =>
                    new ColumnDataType(
                        leftColumn.Name,
                        dataType
                    )
                );

            if ((left is ColumnDataType) || (right is ColumnDataType))
                return Disjunction(
                    ColumnDataType.UnwrapIfColumnDataType(left), 
                    ColumnDataType.UnwrapIfColumnDataType(right)
                ).Select((dataType) =>
                    new ColumnDataType(
                        ColumnDataType.ColumnName.Anonymous.Instance,
                        dataType
                    )
                );

            if ((left is KnownSetDecoratorDataType leftWellKnown) 
                && (right is KnownSetDecoratorDataType rightWellKnown) 
                && (rightWellKnown.Include) && (leftWellKnown.Include)
                && (leftWellKnown.Decorates.Equals(rightWellKnown.Decorates)))
                return Return(leftWellKnown.UnionWith(rightWellKnown) as DataType);

            if ((left is NullDataType) && (right is NullDataType))
                return Return(NullDataType.Instance);

            if (left is NullDataType)
                return Return(right.ToNullable());

            if (right is NullDataType)
                return Return(left.ToNullable());

            if ((left is NullableDataType) || (right is NullableDataType))
                return Disjunction(
                    NullableDataType.UnwrapIfNull(left),
                    NullableDataType.UnwrapIfNull(right)
                ).Select(it => it.ToNullable());

            if ((left.UnwrapToCore() is SqlDataType leftSqlType)
                && (right.UnwrapToCore() is SqlDataType rightSqlType)
                && (leftSqlType.SqlDataTypeOption == rightSqlType.SqlDataTypeOption))
                return Return(leftSqlType);                

            return None();

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
