using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assets._Project.Develop.Runtime.Utilities
{
    public class Buffer<T>
    {
        public T[] Items;
        public int Count;

        public Buffer(int initialSize)
        {
            Items = new T[initialSize];
            Count = 0;
        }
    }
}
