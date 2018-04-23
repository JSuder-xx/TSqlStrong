using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LowSums
{
    public static class StringExtensions
    {
        public static bool IsMoreThanWhitespace(this string val) => !String.IsNullOrWhiteSpace(val);
    }
}
