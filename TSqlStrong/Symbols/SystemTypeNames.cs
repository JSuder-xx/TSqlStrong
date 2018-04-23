using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LowSums;
using TSqlStrong.TypeSystem;

namespace TSqlStrong.Symbols
{
    public static class SystemTypeNames
    {
        private static readonly Dictionary<string, (string, DataType)> _table = new(string, DataType)[]
        {
            ("int", SqlDataType.Int),
            ("varchar", SqlDataType.VarChar),
            ("float", SqlDataType.Numeric),
            ("bit", SqlDataType.Bit),
            ("date", SqlDataType.Date),
            ("time", SqlDataType.Time),
            ("datetime", SqlDataType.DateTime)
            // TODO: Obviously expand this list
        }.ToDictionary(tup => tup.Item1);

        public static IMaybe<DataType> Lookup(string name) =>
            _table.TryGetValue(name.ToLower(), out (string, DataType) result)
                ? result.Item2.ToMaybe()
                : Maybe.None<DataType>();
    }
}


