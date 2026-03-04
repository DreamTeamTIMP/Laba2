namespace lab2
{
    public interface IVirtualArray //для всех типов виртуальных массивов
    {
        void Create(string fleName, long size, int extraParam = 0);
        void Open(string fileName);
        void Input(long index, object value);
        object Print(long index);
        void Close();
    }
    public interface ICreater
    {
        void CreateFile(string fileName, int size);
        void CreatePage(int count);

    }

    public abstract class VirtualMemoryArray : IVirtualArray
    {
        protected const int PAGE_DATA_SIZE = 512;//байт данных на странице
        protected const int ELEMS_PER_PAGE = 128;//кол-во элементов на странице
        protected const int BITMAP_SIZE = 16;
        protected const int BUFFER_SIZE = 3;//минимум 3 страницы в памяти 

        protected FileStream fs;
        protected string fileName;
        protected long arraySize;
        protected int elemetSize;
        protected int totalPages;
        protected List<Page> buffer = new List<Page>(); //буфер страниц
        protected class Page
        {
            public int pageNumber;
            public bool modified;
            public DateTime lastAccess;
            public byte[] bitmap = new byte[BITMAP_SIZE];//битовая карта(1 бит)
            public byte[] data = new byte[PAGE_DATA_SIZE]; //данные страницы

            public Page(int number)
            {
                pageNumber = number;
                modified = false;
                lastAccess = DateTime.Now;
            }
        }
        public void Create(string fleName, long size, int extraParam = 0)
        {
            // Реализация будет позже
        }
        public void Open(string fileName)
        {
            // Реализация будет позже
        }
        public void Input(long index, object value)
        {
            // Реализация будет позже
        }
        public object Print(long index)
        {
            // Реализация будет позже
            return 0;
        }
        public void Close()
        {
            // Реализация будет позже
        }

    }
}
