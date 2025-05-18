using System;
using System.Collections.Generic;

namespace player.Utility
{
    class RotatingBuffer<T>
    {
        public int Index { get; set; }

        private T[] elementArray;
        List<T> returnList = new List<T>();
        private int count = 0;

        public int Count { get { return count; } }

        public RotatingBuffer(int count)
        {
            if (count == 0) throw new ArgumentException("count");
            elementArray = new T[count];
            this.count = count;
        }

        public void Set(T[] elements)
        {
            if (elements.Length != elementArray.Length) throw new ArgumentException("elements");

            elementArray = elements;
        }

        public void Reset()
        {
            Index = 0;
        }

        public void RotateElements()
        {
            Index = (Index + 1) % count;
        }

        public T[] ToArray()
        {
            returnList.Clear();
            for (int i = 0; i < count; i++)
            {
                int index = (i + Index) % count;
                returnList.Add(elementArray[index]);
            }
            return returnList.ToArray();
        }

        public T GetCurrent()
        {
            return elementArray[Index];
        }

        public T GetNext()
        {
            int index = (Index + 1) % count;
            return elementArray[index];
        }
        public T GetLast()
        {
            int index = Index - 1;
            if (index < 0) index = count - 1;
            return elementArray[index];
        }

        public IEnumerable<T> Enumerate()
        {
            for (int i = 0; i < count; i++)
            {
                int index = (i + Index) % count;
                yield return elementArray[index];
            }
        }
    }
}
