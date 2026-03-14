using System.IO;
using System.Text;

namespace Laba2
{
    public class VirtualIntArray : VirtualMemoryArray, IVirtualArray
    {
        protected override int ElementSize => Constants.INT_SIZE;

        public VirtualIntArray(string filename, long arraySize)
        {
            this.fileName = filename;
            this.arraySize = arraySize;
            maxBufferPages = Constants.BUFFER_SIZE;
            elementsPerPage = Constants.ELEMS_PER_PAGE;
            _pageDataSize = Constants.PAGE_DATA_SIZE; // 512 байт

            if (!File.Exists(filename))
            {
                Create(filename, arraySize);
            }
            else
            {
                Open(filename);
            }
        }

        public void Create(string fileName, long size, int maxStrSize = 0)
        {
            Validator.ValidateCreate(fileName, size, 1);
            arraySize = size;

            int pageCount = (int)((size * Constants.INT_SIZE + (_pageDataSize - 1)) / _pageDataSize);
            long totalFileSize = Constants.SIGNATURE_SIZE + Constants.LONG_SIZE + 1 +
                                 pageCount * (Constants.BITMAP_SIZE + _pageDataSize);
            Validator.CheckDiskSpace(fileName, totalFileSize);

            using (var stream = new FileStream(fileName, FileMode.Create))
            {
                stream.Write(Encoding.ASCII.GetBytes(Constants.SIGNATURE), 0, Constants.SIGNATURE_SIZE);
                stream.Write(BitConverter.GetBytes(size), 0, Constants.LONG_SIZE);
                stream.WriteByte((byte)Constants.ARRAY_TYPE_INT);

                for (int i = 0; i < pageCount; i++)
                {
                    stream.Write(new byte[Constants.BITMAP_SIZE], 0, Constants.BITMAP_SIZE);
                    stream.Write(new byte[_pageDataSize], 0, _pageDataSize);
                }
            }

            fs = new FileStream(fileName, FileMode.Open, FileAccess.ReadWrite);
        }

        public void Open(string fileName)
        {
            Validator.ValidateOpen(fileName);
            fs = new FileStream(fileName, FileMode.Open, FileAccess.ReadWrite);

            using (var reader = new BinaryReader(fs, Encoding.ASCII, true))
            {
                byte[] sig = reader.ReadBytes(Constants.SIGNATURE_SIZE);
                if (sig[0] != 'V' || sig[1] != 'M')
                    throw new InvalidDataException("Неверная сигнатура");

                long fileSize = reader.ReadInt64();
                byte type = reader.ReadByte();
                if (type != Constants.ARRAY_TYPE_INT)
                    throw new InvalidDataException("Тип файла не int");

                arraySize = fileSize;
            }

            fs.Seek(Constants.SIGNATURE_SIZE + Constants.LONG_SIZE + Constants.CHAR_SIZE, SeekOrigin.Begin);

            int totalPages = (int)((arraySize + elementsPerPage - 1) / elementsPerPage);
            for (int i = 0; i < Math.Min(maxBufferPages, totalPages); i++)
            {
                var page = LoadPageFromFile(i);
                page.lastAccess = DateTime.Now;
                buffer.Add(page);
            }
        }

        protected override Page LoadPageFromFile(int pageNumber)
        {
            long pageOffset = Constants.SIGNATURE_SIZE + Constants.LONG_SIZE + Constants.CHAR_SIZE +
                              pageNumber * (Constants.BITMAP_SIZE + _pageDataSize);

            var page = new Page(pageNumber, _pageDataSize);
            fs.Seek(pageOffset, SeekOrigin.Begin);
            fs.Read(page.bitmap, 0, Constants.BITMAP_SIZE);
            fs.Read(page.data, 0, _pageDataSize);
            return page;
        }

        protected override void SavePageToFile(Page page)
        {
            long pageOffset = Constants.SIGNATURE_SIZE + Constants.LONG_SIZE + Constants.CHAR_SIZE +
                              page.pageNumber * (Constants.BITMAP_SIZE + _pageDataSize);

            fs.Seek(pageOffset, SeekOrigin.Begin);
            fs.Write(page.bitmap, 0, Constants.BITMAP_SIZE);
            fs.Write(page.data, 0, _pageDataSize);
            page.modified = false;
        }

        public void Input(long index, object value)
        {
            Validator.ValidateInput(index, (int)value, arraySize);
            int intValue = (int)value;
            WriteElementBytes(index, BitConverter.GetBytes(intValue));
        }

        public object Print(long index)
        {
            if (!IsElementWritten(index))
                return null;
            byte[] bytes = ReadElementBytes(index);
            return BitConverter.ToInt32(bytes, 0);
        }
    }
}