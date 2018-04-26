using System;
using System.Collections.Generic;
using System.Linq;
using LowSums;

namespace TSqlStrong.TypeSystem
{
    /// <summary>
    /// Represents the structure of a row of data. 
    /// </summary>
    public class RowDataType : DataType
    {
        private readonly ColumnDataType[] _columns;

        public static readonly RowDataType EmptyRow = new RowDataType();

        public RowDataType() : this(new ColumnDataType[] { }) { }

        public RowDataType(IEnumerable<ColumnDataType> columnDataTypes)
        {
            _columns = columnDataTypes.ToArray();
        }

        public RowDataType(params ColumnDataType[] columnDataTypes)
        {
            _columns = columnDataTypes;
        }

        public IEnumerable<ColumnDataType> ColumnDataTypes => _columns;

        public static ITry<RowDataType> Join(RowDataType accumulator, RowDataType current) =>
            (accumulator.IsRowWithoutKnownStructure || current.IsRowWithoutKnownStructure) ? Try.Success(RowDataType.EmptyRow)
            : (accumulator.ColumnDataTypes.Count() != current.ColumnDataTypes.Count()) ? Try.Failure<RowDataType>($"The number of values {current.ColumnDataTypes.Count()} does not match the expected number {accumulator.ColumnDataTypes.Count()}.")
            : accumulator.ColumnDataTypes.Zip(current.ColumnDataTypes, ColumnDataType.Join)
                .ToTryOfEnumerable()
                .Select(columnDataTypes => new RowDataType(columnDataTypes));

        /// <summary>A Row without known structure functions like a bottom type.</summary>
        public bool IsRowWithoutKnownStructure => _columns.Length == 0;

        public IMaybe<ColumnDataType> FindColumn(string columnName) =>
            ColumnDataTypes
                .FirstOrDefault(col => col.Name.Matches(columnName))
                .ToMaybe();

        public override int SizeOfDomain => IsRowWithoutKnownStructure ? 0 : _columns.Sum(col => col.SizeOfDomain);

        public override string ToString() => $"RowType({ColumnDataTypes.CommaDelimit()})";

        public RowDataType MapNamedColumns(Func<string, DataType, IMaybe<DataType>> mapNamedColumnType)
        {
            return new RowDataType(_columns.Select(MapColumn));

            ColumnDataType MapColumn(ColumnDataType columnDataType) =>           
                (columnDataType.Name is ColumnDataType.ColumnName.BaseNamedColumn named)
                    ? mapNamedColumnType(named.Name, columnDataType.DataType)
                        .Match(
                            some: (newType) => columnDataType.WithNewDataType(newType),
                            none: () => columnDataType
                        )
                    : columnDataType;                                         
        }

        public RowDataType MapDataTypes(Func<DataType, DataType> map) =>
            new RowDataType(_columns.Select(columnDataType => columnDataType.WithNewDataType(map(columnDataType.DataType))));

        protected override ITry<Unit> OnIsAssignableTo(DataType otherType)
        {
            if (otherType is RowDataType otherRowType)
            {
                if (IsRowWithoutKnownStructure || otherRowType.IsRowWithoutKnownStructure)
                    return Try.SuccessUnit;

                if (_columns.Length != otherRowType.ColumnDataTypes.Count())
                    return Try.Failure<Unit>($"Unable to assign row with {_columns.Length} columns to row with {otherRowType.ColumnDataTypes.Count()} columns");

                return _columns.Zip(
                        otherRowType.ColumnDataTypes,
                        (myColumnType, otherColumnType) => myColumnType.IsAssignableTo(otherColumnType)
                    )
                    .ChooseErrors()
                    .ToMaybe()
                    .Match(
                        none: () => Try.SuccessUnit,
                        some: (issues) => Try.Failure<Unit>($"Unable to assign to other row because {issues.CommaDelimit()}")
                    );
            }
            else
                return base.OnIsAssignableTo(otherType);
        }
    }
}
