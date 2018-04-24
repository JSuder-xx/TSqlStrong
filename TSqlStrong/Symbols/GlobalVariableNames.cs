using System;
using System.Collections.Generic;
using System.Linq;
using LowSums;
using TSqlStrong.TypeSystem;

namespace TSqlStrong.Symbols
{
    static class GlobalVariableNames
    {
        private static readonly Dictionary<string, (string, DataType)> _table = new(string, DataType)[]
        {
            ("fetch_status", SqlDataType.Int),
            ("error", SqlDataType.Int),
            ("identity", SqlDataType.Int),
            ("rowcount", SqlDataType.Int)
        }.ToDictionary(tup => $"@@{tup.Item1}");

        public static IMaybe<DataType> Lookup(string name) =>
            _table.TryGetValue(name.ToLower(), out (string, DataType) result)
                ? result.Item2.ToMaybe()
                : Maybe.None<DataType>();
    }
}
