namespace System.Collections.Generic {
    public class List<T> {
        private T[] _value;

        public int Count = 0;

        public List(int initsize = 256) {
            _value = new T[initsize];
        }

        public List(T[] t) {
            _value = t;
        }

        public T this[int index] {
            get {
                return _value[index];
            }
            set {
                _value[index] = value;
            }
        }

        /// <summary>
        /// Ensure the internal array has enough capacity
        /// </summary>
        private void EnsureCapacity(int minCapacity) {
            if (_value.Length >= minCapacity)
                return;
                
            // Double the size or use minCapacity, whichever is larger
            int newCapacity = _value.Length * 2;
            if (newCapacity < minCapacity)
                newCapacity = minCapacity;
            
            // Allocate new array and copy existing items
            T[] newArray = new T[newCapacity];
            for (int i = 0; i < Count; i++) {
                newArray[i] = _value[i];
            }
            
            // Dispose old array and use new one
            _value.Dispose();
            _value = newArray;
        }

        public void Add(T t) {
            // Ensure we have room for one more item
            EnsureCapacity(Count + 1);
            
            _value[Count] = t;
            Count++;
        }

        public void Insert(int index, T item, bool internalMove = false) {
            //Broken
            //if (index == IndexOf(item)) return;

            if (!internalMove) {
                Count++;
                // Ensure capacity for the new item
                EnsureCapacity(Count);
            }

            if (internalMove) {
                int _index = IndexOf(item);
                for (int i = _index; i < Count - 1; i++) {
                    _value[i] = _value[i + 1];
                }
            }

            for (int i = Count - 1; i > index; i--) {
                _value[i] = _value[i - 1];
            }
            _value[index] = item;
        }

        public T[] ToArray() {
            T[] array = new T[Count];
            for (int i = 0; i < Count; i++) {
                array[i] = this[i];
            }
            return array;
        }

        public int IndexOf(T item) {
            for (int i = 0; i < Count; i++) {
                T first = this[i];
                T second = item;

                if (this[i] == item)
                    return i;
            }

            return -1;
        }
        public bool Remove(T item) {
            int at = IndexOf(item);

            if (at < 0)
                return false;

            RemoveAt(at);

            return true;
        }

        public void RemoveAt(int index) {
            Count--;

            for (int i = index; i < Count; i++) {
                _value[i] = _value[i + 1];
            }

            _value[Count] = default(T);
        }

        public override void Dispose() {
            _value.Dispose();
            base.Dispose();
        }

        public void Clear() {
            Count = 0;
        }
    }
}