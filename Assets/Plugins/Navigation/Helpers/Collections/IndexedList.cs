using System.Collections.Generic;

namespace Vella.Common.Collections
{
    public class IndexedList<T> : List<T>
    {
        private int _current;

        public bool IsCyclic { get; set; }

        public int Index
        {
            get
            {
                ValidateCurrent();
                return _current;
            }
            set
            {
                _current = value;
                ValidateCurrent();
            }
        }

        public T CurrentOrDefault => Count > 0 ? this[Index] : default(T);

        public T Current => this[Index];

        public bool IsCurrentLast => Count > 0 && this[Index].Equals(this[Count - 1]);

        public IndexedList(bool isCyclic = false)
        {
            IsCyclic = isCyclic;
        }

        public IndexedList(int capacity, bool isCyclic = false) : base(capacity)
        {
            IsCyclic = isCyclic;
        }

        public IndexedList(IEnumerable<T> collection, bool isCyclic = false) : base(collection)
        {
            IsCyclic = isCyclic;
        }

        public bool Next()
        {
            var index = Index;
            Index = Index + 1;
            if (!IsCyclic)
                return Index == index + 1;
            return true;
        }

        public bool Previous()
        {
            var index = Index;
            Index = Index - 1;
            if (!IsCyclic)
                return Index == index - 1;
            return true;
        }

        private void ValidateCurrent()
        {
            if (_current == 0 || _current >= 0 && _current < Count)
                return;
            if (Count == 0)
                _current = 0;
            else if (IsCyclic)
            {
                if (_current < 0)
                    _current = Count - 1 - (-_current - 1) % Count;
                else
                    _current = _current % Count;
            }
            else
                _current = Compare(0, Count - 1, _current);
        }

        private static int Compare(int param0, int param1, int param2)
        {
            if (param2 < param0) return param0;
            return param2 <= param1 ? param2 : param1;
        }
    }
}