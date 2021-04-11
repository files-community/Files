using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Files.UserControls.Selection
{
    public class GenericItemsCollection<T> : ICollection<T>
    {
        private readonly IList baseList;

        public GenericItemsCollection(IList baseList)
        {
            this.baseList = baseList;
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

        public bool Remove(T item)
        {
            if (baseList.Contains(item))
            {
                baseList.Remove(item);
                return true;
            }
            return false;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return baseList.GetEnumerator();
        }
    }
}