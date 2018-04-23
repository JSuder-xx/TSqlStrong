using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LowSums
{
    public static class LetExtensions
    {
        /// <summary>
        /// Bind a single value to an argument for multiple re-use in a function body. 
        /// </summary>
        /// <typeparam name="TValue"></typeparam>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="value"></param>
        /// <param name="getValue"></param>
        /// <returns></returns>
        public static TResult Let<TValue, TResult>(this TValue value, Func<TValue, TResult> getValue) =>
            getValue(value);

        public static void SideEffect<T>(this T value)
        {
        }

        /// <summary>
        /// Bind two values to argument for multiple re-use in a function body. 
        /// </summary>
        /// <typeparam name="TValue"></typeparam>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="value"></param>
        /// <param name="getValue"></param>
        /// <returns></returns>
        public static TResult Let<TValue1, TValue2, TResult>(this TValue1 value1, TValue2 value2, Func<TValue1, TValue2, TResult> getValue) =>
            getValue(value1, value2);
    }
}
