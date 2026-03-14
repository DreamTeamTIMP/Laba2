namespace Laba2
{

    public abstract partial class VirtualMemoryArray
    {
        protected class Page(int number, int dataSize)
        {
            public int pageNumber = number;
            public bool modified = false;
            public DateTime lastAccess = DateTime.Now;
            public byte[] bitmap = new byte[Constants.BITMAP_SIZE];
            public byte[] data = new byte[dataSize];
        }
    }
}