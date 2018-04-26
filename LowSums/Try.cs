using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LowSums
{
    public interface ITry<out TValue>
    {
        TResult Match<TResult>(Func<TValue, TResult> success, Func<string, TResult> failure);
    }

    public static class TryExtensions
    {
        public static ITry<T> Do<T>(this ITry<T> original, Action<T> execute)
        {
            if (original is Success<T> success)
                execute(success.Value);

            return original;
        }

        public static ITry<T> DoError<T>(this ITry<T> original, Action<string> execute)
        {
            if (original is Failure<T> error)
                execute(error.Message);

            return original;
        }

        public static T GetValue<T>(this ITry<T> value) =>        
            value.Match(
                success: (val) => val,
                failure: (message) => new InvalidOperationException(message).AsValue<T>()
            );
        

        /// <summary>
        /// Converts a value to an error message string if an error, otherwise returns an empty string.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string ToErrorMessage<T>(this ITry<T> value)
        {
            return value is Failure<T> error ? error.Message : String.Empty;
        }

        public static ITry<TNew> Select<TOriginal, TNew>(this ITry<TOriginal> original, Func<TOriginal, TNew> fn) =>        
            original.Match(
                success: (val) => Try.Success(fn(val)),
                failure: (err) => Try.Failure<TNew>(err)
            );

        /// <summary>
        /// Functor mapping over success. Pure function application. Errors propogate.
        /// </summary>
        public static ITry<T> SelectError<T>(this ITry<T> val, Func<string, string> fn) =>
            val.Match(
                success: (_) => val,
                failure: (err) => Try.Failure<T>(fn(err))
            );

        public static ITry<TNew> SelectMany<TOriginal, TNew>(this ITry<TOriginal> original, Func<TOriginal, ITry<TNew>> fn) =>
            original.Match(
                success: (val) => fn(val),
                failure: (err) => Try.Failure<TNew>(err)
            );
        
        /// <summary>
        /// Return the first success otherwise concatenate the error messages of both failures. 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="original"></param>
        /// <param name="fn"></param>
        /// <returns></returns>
        public static ITry<T> SelectFirstSuccess<T>(this ITry<T> original, Func<ITry<T>> fn) =>
            original.Match(
                success: (val) => original,
                failure: (firstError) =>
                    fn().Let(
                        secondResult => secondResult.Match(
                            success: (_) => secondResult,
                            failure: (secondError) => Try.Failure<T>(String.Concat(firstError, ", ", secondError))
                        )
                    )
            );

        /// <summary>
        /// If either succeed that is the result otherwise concatenate the errors of both.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        public static ITry<T> Or<T>(this ITry<T> left, ITry<T> right) =>
            SelectFirstSuccess(left, () => right);

        public static ITry<IEnumerable<T>> ToTryOfEnumerable<T>(this IEnumerable<ITry<T>> listOfTry) =>
            listOfTry.Aggregate(
                Try.Success(new T[] { }.AsEnumerable()),
                TryExtensions.Lift2(EnumerableExtensions.AppendFunc<T>())
            );

        /// <summary>
        /// Lift a function of two arguments into the Try space.
        /// </summary>
        public static Func<ITry<T1>, ITry<T2>, ITry<TResult>> Lift2<T1, T2, TResult>(Func<T1, T2, TResult> fn) =>
            (t1Try, t2Try) => t1Try.Apply2(t2Try, fn);

        public static ITry<TNew> Apply2<TOriginal1, TOriginal2, TNew>(this ITry<TOriginal1> first, ITry<TOriginal2> second, Func<TOriginal1, TOriginal2, TNew> fn) =>
            (first is Success<TOriginal1> firstSuccess)
                ? (second is Success<TOriginal2> secondSuccess)
                    ? Try.Success(fn(firstSuccess.Value, secondSuccess.Value))
                    : Try.Failure<TNew>((second as Failure<TOriginal2>).Message)
                : Try.Failure<TNew>((first as Failure<TOriginal1>).Message);
    }



    public class Try
    {
        public static readonly ITry<Unit> SuccessUnit = Success(Unit.unit);
        public static ITry<T> Success<T>(T val) => new Success<T>(val);
        public static ITry<T> Failure<T>(string error) => new Failure<T>(error);
    }

    public class Success<T> : ITry<T>
    {
        private readonly T _val;
        public Success(T val)
        {
            this._val = val;
        }

        public T Value => _val;

        public TResult Match<TResult>(Func<T, TResult> success, Func<string, TResult> error)
        {
            return success(this._val);
        }

        public override bool Equals(object obj)
        {
            if (obj is Success<T> otherSuccess)
            {
                return Value.Equals(otherSuccess.Value);
            }
            else
                return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }

    }

    public class Failure<T> : ITry<T>
    {
        private readonly string _message;

        public Failure(string message)
        {
            _message = message;
        }

        public TResult Match<TResult>(Func<T, TResult> some, Func<string, TResult> error)
        {
            return error(_message);
        }

        public string Message => _message;

        public override bool Equals(object obj)
        {
            if (obj is Failure<T> otherError)
            {
                return String.Equals(this.Message, otherError.Message);
            }
            else
                return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return Message.GetHashCode();
        }
    }
}
