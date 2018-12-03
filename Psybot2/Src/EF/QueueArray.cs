using System;

namespace Psybot2.Src.EF
{
    internal sealed class QueueArray<T>
    {
        private T[] array;
        private int size;
        private int nextInsertIndex;
        private int ava;

        public T this[int index]
        {
            get => array[index];
        }

        public QueueArray(int capacity)
        {
            size = capacity;
            array = new T[size];
        }

        public void Insert(T value)
        {
            array[nextInsertIndex] = value;
            nextInsertIndex = (nextInsertIndex + 1) % size;
            if (ava < size)
                ava++;
        }

        public bool Contains(T value)
        {
            for (int i = 0; i < ava; i++)
            {
                if (array[i].Equals(value))
                    return true;
            }
            return false;
        }

        public void Reset()
        {
            nextInsertIndex = 0;
            ava = 0;
        }

    }
}
