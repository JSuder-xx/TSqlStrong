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
        private static readonly Dictionary<string, (string, Func<IEnumerable<string>, DataType>)> _table = new(string, DataType)[]
        {
            ("int", SqlDataType.Int),
            ("float", SqlDataType.Real),
            ("bit", SqlDataType.Bit),
            ("date", SqlDataType.Date),
            ("time", SqlDataType.Time),
            ("datetime", SqlDataType.DateTime)
        }

        .Select(tup => (tup.Item1, new Func<IEnumerable<string>, DataType>(_ => tup.Item2)))
        .Union(new (string, Func<IEnumerable<string>, DataType>)[] {
            (
                "varchar", 
                (IEnumerable<string> parameters) => 
                    new SizedSqlDataType(
                        SizedDataTypeOption.VarChar, 
                        parameters.Any() 
                            ? parameters.First().Let(size =>
                                String.Equals(size, "max", StringComparison.InvariantCultureIgnoreCase)
                                    ? Int32.MaxValue
                                    : Convert.ToInt32(size)
                            )
                            : Int32.MaxValue
                    )
            ),
            (
                "nvarchar",
                (IEnumerable<string> parameters) =>
                    new SizedSqlDataType(
                        SizedDataTypeOption.NVarChar,
                        parameters.Any()
                            ? parameters.First().Let(size =>
                                String.Equals(size, "max", StringComparison.InvariantCultureIgnoreCase)
                                    ? Int32.MaxValue
                                    : Convert.ToInt32(size)
                            )
                            : Int32.MaxValue
                    )
            )
        })
        .ToDictionary(tup => tup.Item1);

        public static IMaybe<Func<IEnumerable<string>, DataType>> Lookup(string name) =>
            _table.TryGetValue(name.ToLower(), out (string, Func<IEnumerable<string>, DataType>) result)
                ? result.Item2.ToMaybe()
                : Maybe.None<Func<IEnumerable<string>, DataType>>();
    }
}


