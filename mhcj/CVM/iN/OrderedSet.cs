// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.Collections
{
    internal sealed class OrderedSet<T> :HashSet<T>, IEnumerable<T>
    {
        private readonly ArrayBuilder<T> _list;

        public OrderedSet()
        {
            _list = new ArrayBuilder<T>();
        }

        public OrderedSet(IEnumerable<T> items)
            : this()
        {
            AddRange(items);
        }

        public void AddRange(IEnumerable<T> items)
        {
            foreach (var item in items)
            {
                Add(item);
            }
        }

        public new bool Add(T item)
        {
            if (base.Add(item))
            {
                _list.Add(item);
                return true;
            }

            return false;
        }

        public new int Count
        {
            get
            {
                return _list.Count;
            }
        }

        public new  bool Contains(T item)
        {
            return base.Contains(item);
        }

        public new IEnumerator<T> GetEnumerator()
        {
            return _list.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _list.GetEnumerator();
        }

        public new  void Clear()
        {
            base.Clear();
            _list.Clear();
        }
    }
}
