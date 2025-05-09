// SPDX-License-Identifier: BSD-2-Clause

using System;
using System.Collections;
using System.Collections.Generic;

namespace ClassicUO.Utility.Collections
{
    public static class CollectionHelper
    {
        public static IReadOnlyCollection<T> ReifyCollection<T>(IEnumerable<T> source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            IReadOnlyCollection<T> result = source as IReadOnlyCollection<T>;

            if (result != null)
            {
                return result;
            }

            ICollection<T> collection = source as ICollection<T>;

            if (collection != null)
            {
                return new CollectionWrapper<T>(collection);
            }

            ICollection nongenericCollection = source as ICollection;

            if (nongenericCollection != null)
            {
                return new NongenericCollectionWrapper<T>(nongenericCollection);
            }

            return new List<T>(source);
        }

        private sealed class NongenericCollectionWrapper<T> : IReadOnlyCollection<T>
        {
            private readonly ICollection _collection;

            public NongenericCollectionWrapper(ICollection collection)
            {
                if (collection == null)
                {
                    throw new ArgumentNullException(nameof(collection));
                }

                _collection = collection;
            }

            public int Count => _collection.Count;

            public IEnumerator<T> GetEnumerator()
            {
                foreach (T item in _collection)
                {
                    yield return item;
                }
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return _collection.GetEnumerator();
            }
        }

        private sealed class CollectionWrapper<T> : IReadOnlyCollection<T>
        {
            private readonly ICollection<T> _collection;

            public CollectionWrapper(ICollection<T> collection)
            {
                if (collection == null)
                {
                    throw new ArgumentNullException(nameof(collection));
                }

                _collection = collection;
            }

            public int Count => _collection.Count;

            public IEnumerator<T> GetEnumerator()
            {
                return _collection.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return _collection.GetEnumerator();
            }
        }
    }
}