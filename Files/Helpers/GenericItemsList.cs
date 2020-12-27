using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Files.UserControls.Selection
{
    public class GenericItemsList<T> : IList<T>
    {
        private readonly IList baseList;

        public GenericItemsList(IList baseList)
        {
            this.baseList = baseList;
        }

        public T this[int index]
        {
            get => (T)baseList[index];
            set => baseList[index] = value;
        }

        public int Count => baseList.Count;

        public bool IsReadOnly => baseList.IsReadOnly;

        public void Add(T item)
        {
            baseList.Add(item);
        }

        public void Clear()
        {
            baseList.Clear();
        }

        public bool Contains(T item)
        {
            return baseList.Contains(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            baseList.CopyTo(array, arrayIndex);
        }

        public IEnumerator<T> GetEnumerator()
        {
            return baseList.Cast<T>().GetEnumerator();
        }

        public int IndexOf(T item)
        {
            return baseList.IndexOf(item);
        }

        public void Insert(int index, T item)
        {
            baseList.Insert(index, item);
        }

        public bool Remove(T item)
        {
            if (baseList.Contains(item))
            {
                baseList.Remove(item);
                return true;
            }
            return false;
        }

        public void RemoveAt(int index)
        {
            baseList.RemoveAt(index);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return baseList.GetEnumerator();
        }
    }
}