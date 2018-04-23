using System.Collections.Generic;
using System.Linq;

namespace TSqlStrong.TypeSystem
{
    public static class ColumnDataTypeEnumerableExtension
    {
        public static IEnumerable<ColumnDataType.ColumnName.Base> Names(this IEnumerable<ColumnDataType> columnDataTypes) =>
            columnDataTypes.Select(ct => ct.Name);

        public static IEnumerable<string> NameStrings(this IEnumerable<ColumnDataType> columnDataTypes) =>
            columnDataTypes.Select(ct => ct.Name is ColumnDataType.ColumnName.BaseNamedColumn named ? named.Name : "_");
    }
}
