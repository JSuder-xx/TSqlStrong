using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LowSums
{
    public static class ExceptionExtension
    {
        /// <summary>
        /// Treat an Exception as a value of type T in order to use in an expression.
        /// Example:
        /// int result = someDivisor != 0 
        ///   ? someDividend / someDivisor 
        ///   : (new InvalidOperationException("Division by zero")).AsValue<int>();
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="exception"></param>
        /// <returns></returns>
        public static T AsValue<T>(this Exception exception)
        {            
            throw exception;
        }
    }
}
