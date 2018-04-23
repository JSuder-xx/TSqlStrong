using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LowSums
{
    public static class FuncExtensions
    {
        public static Func<T, bool> Not<T>(this Func<T, bool> original) =>
            (val) => !original(val);
    }
}
