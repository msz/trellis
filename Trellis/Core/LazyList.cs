using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Trellis.Core
{
    public class LazyList<T> : IList<T>
    {
        Id id;
        IDBCollection collection;
        string fieldName;

        bool wholeLoaded;
        Dictionary<int, T> loadedItems = new Dictionary<int, T>();

        bool commitWhole;
        List<ArrayModifyEvent> modifyEvents = new List<ArrayModifyEvent>();

        public LazyList(IDBCollection collection, Id id, string fieldName)
        {
            this.id = id;
            this.collection = collection;
            this.fieldName = fieldName;
        }
        public T this[int index]
        {
            get
            {
                if (!loadedItems.ContainsKey(index))
                    loadedItems[index] = (T) collection.ArrayElem(id, fieldName, index);
                return loadedItems[index];
            }
            set
            {
                modifyEvents.Add(new ArrayModifyEvent(ArrayModifyType.Set, value, index));
                loadedItems[index] = value;
            }
        }

        private void LoadWhole()
        {
            var items = (IEnumerable<T>)collection.GetFields(id, new[] { fieldName })[fieldName];
            var counter = 0;
            foreach(var item in items)
                loadedItems[counter++] = item;
        }

        public List<T> GetFullList()
        {
            if (!wholeLoaded)
            {
                LoadWhole();
                wholeLoaded = true;
            }
            var list = loadedItems
                .OrderBy(x => x.Key)
                .Select(x => x.Value)
                .ToList();
            foreach (var ev in modifyEvents)
            {
                var val = (T)ev.Value;
                switch (ev.Type)
                {
                    case ArrayModifyType.Set:
                        list[ev.Index] = val;
                        break;
                    case ArrayModifyType.Add:
                        list.Add(val);
                        break;
                    case ArrayModifyType.Remove:
                        list.Remove(val);
                        break;
                }
            }
            return list;
        }

        public int Count
        {
            get
            {
                return collection.ArraySize(id, fieldName)
                    + modifyEvents.Where(x => x.Type == ArrayModifyType.Add).Count()
                    - modifyEvents.Where(x => x.Type == ArrayModifyType.Remove).Count();
            }
        }

        public bool IsReadOnly
        {
            get
            {
                return false;
            }
        }

        public void Add(T item)
        {
            modifyEvents.Add(new ArrayModifyEvent(ArrayModifyType.Add, item));
        }

        public void Clear()
        {
            commitWhole = true;
            loadedItems.Clear();
            modifyEvents.Clear();
        }

        public bool Contains(T item)
        {
            return collection.ArrayContains(id, fieldName, item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            var list = GetFullList();
            list.CopyTo(array, arrayIndex);
        }

        public IEnumerator<T> GetEnumerator()
        {
            return GetFullList().GetEnumerator();
        }

        public int IndexOf(T item)
        {
            return GetFullList().IndexOf(item);
        }

        public void Insert(int index, T item)
        {
            GetFullList().Insert(index, item);
        }

        public bool Remove(T item)
        {
            modifyEvents.Add(new ArrayModifyEvent(ArrayModifyType.Remove, item));
            return true;
        }

        public void RemoveAt(int index)
        {
            GetFullList().RemoveAt(index);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)GetFullList()).GetEnumerator();
        }

        public bool CanCommitLazily
        {
            get
            {
                var removeEvents = modifyEvents.Where(x => x.Type == ArrayModifyType.Remove);
                var setEvents = modifyEvents.Where(x => x.Type == ArrayModifyType.Set);
                return !removeEvents.Any() && !setEvents.Any();
            }
        }

        public void Commit()
        {
            if (CanCommitLazily)
            {
                var addEvents = modifyEvents.Where(x => x.Type == ArrayModifyType.Add);
                collection.ArrayAppend(id, fieldName, addEvents.Select(x => x.Value).ToArray());
                return;
            }
            var list = GetFullList();
            collection.UpdateFields(id, new Dictionary<string, object> { { "List", list } });
        }

        public void Preload(params int[] indexes)
        {
            object[] values = collection.ArrayGet(id, fieldName, indexes);
            foreach (var kvp in indexes.Zip(values, (int k, object v) => new KeyValuePair<int, object>(k, v)))
                loadedItems[kvp.Key] = (T)kvp.Value;
        }
    }
}
