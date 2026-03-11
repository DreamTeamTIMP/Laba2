namespace Laba2
{

    public abstract partial class VirtualMemoryArray
    {
        protected class Page
        {
            public int pageNumber;
            public bool modified;
            public DateTime lastAccess;
            public byte[] bitmap = new byte[Constants.BITMAP_SIZE];//битовая карта(1 бит)
            public byte[] data = new byte[Constants.PAGE_DATA_SIZE]; //данные страницы

            public Page(int number)
            {
                pageNumber = number;
                modified = false;
                lastAccess = DateTime.Now;
            }
        }
    }
}