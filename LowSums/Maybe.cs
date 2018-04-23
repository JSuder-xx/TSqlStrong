using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LowSums
{
    public interface IMaybe<out T>
    {
        TResult Match<TResult>(Func<T, TResult> some, Func<TResult> none);
    }

    public static class MaybeExtensions
    {
        public static IMaybe<T> ToMaybe<T>(this T value) where T : class => value == null ? Maybe.None<T>() : Maybe.Some<T>(value);

        public static IMaybe<THopedFor> ToMaybe<T, THopedFor>(this T value)
            where T : class
            where THopedFor : class, T
            =>
            value is THopedFor asHopedFor ? Maybe.Some(asHopedFor) : Maybe.None<THopedFor>();
        
        public static T GetValueOrException<T>(this IMaybe<T> value) where T : class =>
            (value is Some<T> val)
                ? val.Value
                : (new ArgumentNullException("Expecting value but got None.")).AsValue<T>();

        public static ITry<T> ToTry<T>(this IMaybe<T> maybe, string errorMessage) =>
            maybe.Match(
                some: (val) => Try.Success<T>(val),
                none: () => Try.Failure<T>(errorMessage)
            );

        public static T Coalesce<T>(this IMaybe<T> value, T whenNull) =>
            value is Some<T> val
                ? val.Value
                : whenNull;

        public static T Coalesce<T>(this IMaybe<T> value, Func<T> whenNull) =>
            value is Some<T> val
                ? val.Value
                : whenNull();

        public static void Do<T>(this IMaybe<T> maybe, Action<T> some, Action none)
        {
            if (maybe is Some<T> someValue)
                some(someValue.Value);
            else
                none();
        }

        public static IMaybe<TNew> Select<TOriginal, TNew>(this IMaybe<TOriginal> original, Func<TOriginal, TNew> fn) =>
            original.Match(
                some: (val) => Maybe.Some(fn(val)),
                none: () => Maybe.None<TNew>()
            );        

        /// <summary>
        /// Produce another Maybe in the case where the current maybe is None. 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="original"></param>
        /// <param name="fn"></param>
        /// <returns></returns>
        public static IMaybe<T> SelectManyNone<T>(this IMaybe<T> original, Func<IMaybe<T>> fn) =>
            original is Some<T> ? original : fn();
        
        public static IMaybe<TNew> Apply2<TOriginal1, TOriginal2, TNew>(this IMaybe<TOriginal1> first, IMaybe<TOriginal2> second, Func<TOriginal1, TOriginal2, TNew> fn) =>
            ((first is Some<TOriginal1> firstSome) && (second is Some<TOriginal2> secondSome))
                ? Maybe.Some(fn(firstSome.Value, secondSome.Value))
                : Maybe.None<TNew>();        

        public static IMaybe<TNew> SelectMany<TOriginal, TNew>(this IMaybe<TOriginal> original, Func<TOriginal, IMaybe<TNew>> fn) =>
            original.Match(
                some: (val) => fn(val),
                none: () => Maybe.None<TNew>()
            );        

        /// <summary>
        /// Convert a maybe to an enumerable. A maybe holds either no value or one so this maps easily to either an empty enumerable or a singleton enumeration.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="maybe"></param>
        /// <returns></returns>
        public static IEnumerable<T> ToEnumerable<T>(this IMaybe<T> maybe) =>
            maybe.Match(
                some: (val) => new T[] { val },
                none: () => new T[] { }
            );        
    }

    public class Maybe
    {
        public static IMaybe<T> Some<T>(T val) => new Some<T>(val);
        public static IMaybe<T> None<T>() => new None<T>();        
    }

    public class Some<T> : IMaybe<T>
    {
        private T _val;
        public Some(T val)
        {
            this._val = val;
        }

        public static implicit operator Some<T>(T val) => new Some<T>(val);        

        public static implicit operator T(Some<T> val) => val.Value;

        public T Value => _val;

        public TResult Match<TResult>(Func<T, TResult> some, Func<TResult> none) => some(this._val);        
    }

    public class None<T> : IMaybe<T>
    {
        public None() { }
        public TResult Match<TResult>(Func<T, TResult> some, Func<TResult> none) => none();        
    }

}
