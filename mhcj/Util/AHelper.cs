using System;
using System.Linq;
using CVM.Collections.Immutable;



namespace CVM
{
    public static  class AHelper
    {

        public static bool IsNullOrWhiteSpace(this string c)
        {
            return string.IsNullOrEmpty(c)||c.Contains(' ')|| c.Contains('\t') || c.Contains('\r') || c.Contains( '\n');
        }
        public static bool IsWhiteSpace(this char c)
        {
            return c == ' ' || c == '\t' || c == '\r' || c == '\n';
        }
        private static readonly object loc = new object();
        public static T CompareExchange<T>(ref T a, T b, T c)
        {
            lock (loc)
            {
                var d = a;
                if (a == null && c == null)
                {
                    a = b;
                }
                if (a.Equals(c))
                {
                    a = b;
                }
             

                return d;
            }
        }
        public static T Exchange<T>(ref T a, T b)
        {
           lock (loc)
            {
                var d = a;

                a = b;


                return d;
            }
        }

        internal static int Increment(ref int size)
        {
            lock (loc)
            {
                var d = size;

                ++size;

                return d;
            }
        }

        internal static int Decrement(ref int size)
        {
            lock (loc)
            {
                var d = size;

                --size;

                return d;
            }
        }

        internal static int Add(ref int rwlock, int v)
        {
            lock (loc)
            {
                var a = rwlock;
                var b = rwlock + v;
                rwlock = b;

                return a;
            }
            }
        internal static long Add(ref long rwlock, long v)
        {
            lock (loc)
            {
                var a = rwlock;
                var b = rwlock + v;
                rwlock = b;

                return a;
            }
        }

        internal static bool Initialize<T>(ref T cachedDiagnostics, T newSet) 
        {
            
            var copy = cachedDiagnostics;

            lock(loc)
            {
                cachedDiagnostics = newSet;
            }
            if(copy.Equals(cachedDiagnostics))
            {
                return true;
            }
            return false;

        }

        internal static long Increment(ref long s_nextId)
        {
            lock (loc)
            {
               

                var d=s_nextId++;

                return d;
            }
        }

        /// <summary>
        /// Obtains the value for the specified key from a dictionary, or adds a new value to the dictionary where the key did not previously exist.
        /// </summary>
        /// <typeparam name="TKey">The type of key stored by the dictionary.</typeparam>
        /// <typeparam name="TValue">The type of value stored by the dictionary.</typeparam>
        /// <typeparam name="TArg">The type of argument supplied to the value factory.</typeparam>
        /// <param name="location">The variable or field to atomically update if the specified <paramref name="key"/> is not in the dictionary.</param>
        /// <param name="key">The key for the value to retrieve or add.</param>
        /// <param name="valueFactory">The function to execute to obtain the value to insert into the dictionary if the key is not found.</param>
        /// <param name="factoryArgument">The argument to pass to the value factory.</param>
        /// <returns>The value obtained from the dictionary or <paramref name="valueFactory"/> if it was not present.</returns>
        public static TValue GetOrAdd<TKey, TValue, TArg>(ref Collections.Immutable.ImmutableDictionary<TKey, TValue> location, TKey key, Func<TKey, TArg, TValue> valueFactory, TArg factoryArgument)
        {
            Requires.NotNull(valueFactory, valueFactory.ToString());

            var map = location;
            Requires.NotNull(map, (location).ToString());

            TValue value;
            if (map.TryGetValue(key, out value))
            {
                return value;
            }

            value = valueFactory(key, factoryArgument);
            return GetOrAdd(ref location, key, value);
        }
        /// <summary>
        /// Obtains the value for the specified key from a dictionary, or adds a new value to the dictionary where the key did not previously exist.
        /// </summary>
        /// <typeparam name="TKey">The type of key stored by the dictionary.</typeparam>
        /// <typeparam name="TValue">The type of value stored by the dictionary.</typeparam>
        /// <param name="location">The variable or field to atomically update if the specified <paramref name="key"/> is not in the dictionary.</param>
        /// <param name="key">The key for the value to retrieve or add.</param>
        /// <param name="valueFactory">
        /// The function to execute to obtain the value to insert into the dictionary if the key is not found.
        /// This delegate will not be invoked more than once.
        /// </param>
        /// <returns>The value obtained from the dictionary or <paramref name="valueFactory"/> if it was not present.</returns>
        public static TValue GetOrAdd<TKey, TValue>(ref Collections.Immutable.ImmutableDictionary<TKey, TValue> location, TKey key, Func<TKey, TValue> valueFactory)
        {
            Requires.NotNull(valueFactory, valueFactory.ToString());

               var map = location;
            Requires.NotNull(map, location.ToString());

            TValue value;
            if (map.TryGetValue(key, out value))
            {
                return value;
            }

            value = valueFactory(key);
            return GetOrAdd(ref location, key, value);
        }
        /// <summary>
        /// Obtains the value for the specified key from a dictionary, or adds a new value to the dictionary where the key did not previously exist.
        /// </summary>
        /// <typeparam name="TKey">The type of key stored by the dictionary.</typeparam>
        /// <typeparam name="TValue">The type of value stored by the dictionary.</typeparam>
        /// <param name="location">The variable or field to atomically update if the specified <paramref name="key"/> is not in the dictionary.</param>
        /// <param name="key">The key for the value to retrieve or add.</param>
        /// <param name="value">The value to add to the dictionary if one is not already present.</param>
        /// <returns>The value obtained from the dictionary or <paramref name="value"/> if it was not present.</returns>
        public static TValue GetOrAdd<TKey, TValue>(ref Collections.Immutable.ImmutableDictionary<TKey, TValue> location, TKey key, TValue value)
        {
            var priorCollection = location;
            bool successful;
            do
            {
                Requires.NotNull(priorCollection, location.ToString());
                TValue oldValue;
                if (priorCollection.TryGetValue(key, out oldValue))
                {
                    return oldValue;
                }

                var updatedCollection = priorCollection.Add(key, value);
                var interlockedResult = CompareExchange(ref location, updatedCollection, priorCollection);
                successful = object.ReferenceEquals(priorCollection, interlockedResult);
                priorCollection = interlockedResult; // we already have a volatile read that we can reuse for the next loop
            }
            while (!successful);

            // We won the race-condition and have updated the collection.
            // Return the value that is in the collection (as of the Interlocked operation).
            return value;
        }
        //public static TValue GetOrAdd<TKey, TValue>(ref System.Collections.Immutable.ImmutableDictionary<TKey, TValue> location, TKey key, TValue value)
        //{
        //    throw new NotImplementedException();
        //}
        //public static TValue GetOrAdd<TKey, TValue>(ref System.Collections.Immutable.ImmutableDictionary<TKey, TValue> location, TKey key, Func<TKey, TValue> valueFactory)
        //{
        //    TValue value;

        //    var wer = valueFactory.Invoke(key);

        //    if(location.ContainsKey(key))
        //    {
        //        value= location[key];
        //    }else
        //    {
        //        value= wer;
        //    }
        //    location.TryAdd(key, wer);
        //    return value;

        //}

        //internal static TValue GetOrAdd<TKey, TValue>(ref ImmutableDictionary<TKey, TValue> memberModels, TKey attribute, Func<TKey, ParameterSyntax, ParameterSymbol,TValue> p, Binder binder)
        //{
        //    throw new NotImplementedException();
        //}
    }
}
