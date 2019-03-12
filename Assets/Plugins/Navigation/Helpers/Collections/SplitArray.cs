namespace Vella.Common.Collections
{
    /// <summary>
    /// Alternative to 2D array that splits the memory size into many 1D arrays.
    /// May avoid the large object heap and causing fragmentation / OutOfMemoryException.
    /// At the cost of additional overhead and processing.
    /// </summary>
    public class SplitArray<T>
    {
        public SplitArray(int sizeX, int sizeY)
        {
            LengthX = sizeX;
            LengthY = sizeY;
            _nodes = new IndexedList<T[]>(sizeX);

            for (int ix = 0; ix < sizeX; ++ix)
            {
                _nodes.Add(new T[sizeY]);
            }
        }

        public int LengthX;
        public int LengthY;

        private readonly IndexedList<T[]> _nodes;

        public T this[int indexX, int indexY]
        {
            get { return Get(indexX, indexY); }
            set { Set(indexX, indexY, value); }
        }

        private void Set(int x, int y, T value)
        {
            _nodes[x][y] = value;
        }

        private T Get(int x, int y)
        {
            return _nodes[x][y];
        }

        public int GetLength(int dimension)
        {
            return dimension > 1 ? 0 : dimension == 1 ? LengthY : LengthX;
        }
    }


}
