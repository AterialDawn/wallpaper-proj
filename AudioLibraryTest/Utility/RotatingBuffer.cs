using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace player.Utility
{
    class RotatingBuffer<T>
    {
        private T[] elementArray;
        List<T> returnList = new List<T>();
        private int startIndex = 0;
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
            startIndex = 0;
        }

        public void RotateElements()
        {
            startIndex = (startIndex + 1) % count;
        }

        public T[] ToArray()
        {
            returnList.Clear();
            for (int i = 0; i < count; i++)
            {
                int index = (i + startIndex) % count;
                returnList.Add(elementArray[index]);
            }
            return returnList.ToArray();
        }

        public T GetCurrent()
        {
            return elementArray[startIndex];
        }

        public T GetNext()
        {
            int index = (startIndex + 1) % count;
            return elementArray[index];
        }
        public T GetLast()
        {
            int index = startIndex - 1;
            if (index < 0) index = count - 1;
            return elementArray[index];
        }

        public IEnumerable<T> Enumerate()
        {
            for (int i = 0; i < count; i++)
            {
                int index = (i + startIndex) % count;
                yield return elementArray[index];
            }
        }
    }
}
