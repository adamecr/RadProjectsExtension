using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace net.adamec.dev.vs.extension.radprojects.utils
{
    /// <summary>
    /// <see cref="IEnumerator{T}"/> class extensions
    /// </summary>
    public static class EnumeratorExtensions
    {
        /// <summary>
        /// Transforms the <paramref name="enumerator"/> to <see cref="IEnumerable{T}"/>
        /// </summary>
        /// <typeparam name="T">Type  of objects held in enumerator/enumerable</typeparam>
        /// <param name="enumerator">Enumerator to transforms</param>
        /// <returns><see cref="IEnumerable{T}"/> based on given <paramref name="enumerator"/></returns>
        public static IEnumerable<T> ToEnumerable<T>(this IEnumerator<T> enumerator)
        {
            while (enumerator.MoveNext())
                yield return enumerator.Current;
        }

        /// <summary>
        /// Transforms the <paramref name="enumerator"/> to <see cref="IEnumerable{T}"/>
        /// </summary>
        /// <typeparam name="T">Type of objects held in enumerator/enumerable</typeparam>
        /// <param name="enumerator">Enumerator to transforms</param>
        /// <returns><see cref="IEnumerable{T}"/> based on given <paramref name="enumerator"/></returns>
        public static IEnumerable<T> ToEnumerable<T>(this IEnumerator enumerator)
        {
            while (enumerator.MoveNext())
                yield return (T)enumerator.Current;
        }

        /// <summary>
        /// Transforms the <paramref name="enumerator"/> to <see cref="List{T}"/>
        /// </summary>
        /// <typeparam name="T">Type of objects held in enumerator/list</typeparam>
        /// <param name="enumerator">Enumerator to transforms</param>
        /// <returns><see cref="List{T}"/> based on given <paramref name="enumerator"/></returns>
        public static List<T> ToList<T>(this IEnumerator enumerator)
        {
            return enumerator.ToEnumerable<T>().ToList();
        }
    }
}
