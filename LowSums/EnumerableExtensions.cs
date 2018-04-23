using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LowSums
{
    public static class EnumerableExtensions
    {
        public static IEnumerable<(IMaybe<TA>, IMaybe<TB>, TKey)> OuterJoin<TA, TB, TKey, TResult>(
            this IEnumerable<TA> aEnumerable,
            IEnumerable<TB> bEnumerable,
            Func<TA, TKey> selectKeyA,
            Func<TB, TKey> selectKeyB
        )
        {
            var aLookup = aEnumerable.ToLookup(selectKeyA);
            var bLookup = bEnumerable.ToLookup(selectKeyB);

            return (
                from key in new HashSet<TKey>(aLookup.Select(GetKey).Union(aLookup.Select(GetKey)))
                from a in PromoteToMaybe(aLookup[key])
                from b in PromoteToMaybe(bLookup[key])
                select (a, b, key)
            );

            TKey GetKey<T>(IGrouping<TKey, T> grouping) => grouping.Key;
            IEnumerable<IMaybe<T>> PromoteToMaybe<T>(IEnumerable<T> enumerable) => enumerable.Select(Maybe.Some).DefaultIfEmpty(Maybe.None<T>());
        }

        /// <summary>
        /// Only return the items in the list that have a value.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <returns></returns>
        public static IEnumerable<T> Choose<T>(this IEnumerable<IMaybe<T>> list) where T: class
        {
            return list
                .OfType<Some<T>>()
                .Select(it => it.Value);                
        }

        public static IEnumerable<T> WhenTrue<T>(this bool expression, Func<T> produce) => expression ? new T[] { produce() }  : new T[] { };

        /// <summary>
        /// Only return the items in the list that succeeded.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <returns></returns>
        public static IEnumerable<T> Choose<T>(this IEnumerable<ITry<T>> list) where T : class
        {
            return list
                .OfType<Success<T>>()
                .Select(it => it.Value);
        }

        /// <summary>
        /// Only return the errors in the list.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <returns></returns>
        public static IEnumerable<string> ChooseErrors<T>(this IEnumerable<ITry<T>> list) where T : class
        {
            return list
                .OfType<Failure<T>>()
                .Select(it => it.Message);
        }

        public static void Do<T>(this IEnumerable<T> list, Action<T> act)
        {
            foreach(var item in list)
            {
                act(item);
            }
        }

        public static IMaybe<TResult> SelectFirst<TItem, TResult>(this IEnumerable<TItem> list, Func<TItem, IMaybe<TResult>> fn)
        {
            foreach(var item in list)
            {
                var result = fn(item);
                if (result is Some<TResult> someResult)
                    return someResult;
            }

            return Maybe.None<TResult>();
        }

        public static int GenerateListHashCode<T>(this IEnumerable<T> list)
        {
            return list.Aggregate(271, (hashCode, cur) => hashCode * 17 + cur.GetHashCode());
        }

        /// <summary>
        /// Returns true when both enumerables have equivalent items (Set equal or without regard for order).
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="thisEnumerable"></param>
        /// <param name="otherEnumerable"></param>
        /// <returns></returns>
        public static bool HaveEquivalentMembership<T>(this IEnumerable<T> thisEnumerable, IEnumerable<T> otherEnumerable)
        {
            var thisArray = thisEnumerable.ToArray();
            var otherArray = otherEnumerable.ToArray();
            if (thisArray.Length != otherArray.Length)
                return false;

            var otherHash = new HashSet<T>(otherArray);

            return thisArray.All(thisItem => otherHash.Contains(thisItem));
        }

        public static ITry<TResult> SelectFirst<TItem, TResult>(this IEnumerable<TItem> list, Func<TItem, ITry<TResult>> fn)
        {
            foreach (var item in list)
            {
                var result = fn(item);
                if (result is Success<TResult> someResult)
                    return someResult;
            }

            return Try.Failure<TResult>("Unable to find success");
        }

        public static IEnumerable<T> ChooseNonNull<T>(this IEnumerable<T> list) where T : class
        {
            return list.Where(it => it != null);
        }

        public static string Delimit<T>(this IEnumerable<T> list, string with, Func<T, string> selector = null)
        {
            selector = selector ?? ((T val) => val.ToString());
            return String.Join(with, list.Select(selector).ToArray());
        }

        public static string CommaDelimit<T>(this IEnumerable<T> list, Func<T, string> selector = null)
        {
            return list.Delimit(", ", selector);
        }

        /// <summary>
        /// Unit for IEnumerable.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="val"></param>
        /// <returns></returns>
        public static IEnumerable<T> ToEnumerable<T>(this T val)
        {
            return new[] { val };
        }

        public static Func<IEnumerable<T>, T, IEnumerable<T>> AppendFunc<T>() =>
            Append;

        public static IEnumerable<T> Append<T>(this IEnumerable<T> enumerable, T value) =>
            enumerable.Concat(value.ToEnumerable());

        public static T Second<T>(this IEnumerable<T> val) =>
            val.Skip(1).First();


        /// <summary>
        /// Converts a list to a Maybe where it is Some when the list is non-empty and None when empty.
        /// </summary>
        /// <typeparam name="TItem"></typeparam>
        /// <param name="list"></param>
        /// <returns></returns>
        public static IMaybe<IEnumerable<TItem>> ToMaybe<TItem>(this IEnumerable<TItem> list) =>        
            list.Any()
                ? Maybe.Some(list)
                : Maybe.None<IEnumerable<TItem>>();                            

        public static (IEnumerable<TRefined>, IEnumerable<TBase>) PartitionByClass<TBase, TRefined>(this IEnumerable<TBase> list)
            where TRefined: TBase
            where TBase: class =>        
            (list.Where(it => it is TRefined).Cast<TRefined>(), list.Where(it => !(it is TRefined)));
        

        public static (IEnumerable<TRefined>, IEnumerable<TBase>) PartitionByInterface<TBase, TRefined>(this IEnumerable<TBase> list) =>        
            (list.Where(it => it is TRefined).Cast<TRefined>(), list.Where(it => !(it is TRefined)));


        public static (IEnumerable<T>, IEnumerable<T>) Partition<T>(this IEnumerable<T> list, Func<T, bool> pred) =>
            (list.Where(pred), list.Where(pred.Not()));        
    }
}
