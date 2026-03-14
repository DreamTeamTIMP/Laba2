using System;
using System.IO;
using System.Text;

namespace Laba2
{
    public class VirtualCharArray : VirtualMemoryArray, IVirtualArray
    {
        private int _stringLength;          // фиксированная длина строки (в символах)

        protected override int ElementSize => _stringLength; // размер элемента в байтах

        public VirtualCharArray(string filename, long arraySize, int stringLength)
        {
            this.fileName = filename;
            _stringLength = stringLength;
            this.arraySize = arraySize;
            maxBufferPages = Constants.BUFFER_SIZE;
            elementsPerPage = Constants.ELEMS_PER_PAGE;

            // Вычисляем размер страницы данных (кратен 512, но не меньше 128 * stringLength)
            int rawDataSize = elementsPerPage * _stringLength;
            _pageDataSize = (rawDataSize + 511) / 512 * 512;

            if (!File.Exists(filename))
            {
                Create(filename, arraySize, stringLength);
            }
            else
            {
                Open(filename);
            }
        }

        public void Create(string fileName, long size, int maxStrSize)
        {
            Validator.ValidateCreate(fileName, size, maxStrSize);
            _stringLength = maxStrSize;
            arraySize = size;

            int pageCount = (int)((size + elementsPerPage - 1) / elementsPerPage);
            long totalFileSize = Constants.SIGNATURE_SIZE + Constants.LONG_SIZE + 1 + Constants.INT_SIZE +
                                 pageCount * (Constants.BITMAP_SIZE + _pageDataSize);
            Validator.CheckDiskSpace(fileName, totalFileSize);

            using (var stream = new FileStream(fileName, FileMode.Create))
            {
                stream.Write(Encoding.ASCII.GetBytes(Constants.SIGNATURE), 0, Constants.SIGNATURE_SIZE);
                stream.Write(BitConverter.GetBytes(size), 0, Constants.LONG_SIZE);
                stream.WriteByte((byte)Constants.ARRAY_TYPE_CHAR);
                stream.Write(BitConverter.GetBytes(maxStrSize), 0, Constants.INT_SIZE);

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
            fs = new FileStream(fileName, FileMode.Open, FileAccess.ReadWrite);
            using (var reader = new BinaryReader(fs, Encoding.ASCII, true))
            {
                byte[] sig = reader.ReadBytes(Constants.SIGNATURE_SIZE);
                if (sig[0] != 'V' || sig[1] != 'M')
                    throw new InvalidDataException("Invalid file signature");

                long fileSize = reader.ReadInt64();
                byte type = reader.ReadByte();
                int strLen = reader.ReadInt32();

                if (type != Constants.ARRAY_TYPE_CHAR)
                    throw new InvalidDataException("File type is not char array");
                if (strLen != _stringLength)
                    throw new InvalidDataException("String length mismatch");

                arraySize = fileSize;
                // _stringLength уже задан в конструкторе и совпадает с прочитанным
            }

            // Позиционируемся на начало данных
            fs.Seek(Constants.SIGNATURE_SIZE + Constants.LONG_SIZE + 1 + Constants.INT_SIZE, SeekOrigin.Begin);

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
            long pageOffset = Constants.SIGNATURE_SIZE + Constants.LONG_SIZE + 1 + Constants.INT_SIZE +
                              pageNumber * (Constants.BITMAP_SIZE + _pageDataSize);

            var page = new Page(pageNumber, _pageDataSize);
            fs.Seek(pageOffset, SeekOrigin.Begin);
            fs.Read(page.bitmap, 0, Constants.BITMAP_SIZE);
            fs.Read(page.data, 0, _pageDataSize);
            return page;
        }

        protected override void SavePageToFile(Page page)
        {
            long pageOffset = Constants.SIGNATURE_SIZE + Constants.LONG_SIZE + 1 + Constants.INT_SIZE +
                              page.pageNumber * (Constants.BITMAP_SIZE + _pageDataSize);

            fs.Seek(pageOffset, SeekOrigin.Begin);
            fs.Write(page.bitmap, 0, Constants.BITMAP_SIZE);
            fs.Write(page.data, 0, _pageDataSize);
            page.modified = false;
        }

        public void Input(long index, object value)
        {
            Validator.ValidateInput(index, (string)value, arraySize, _stringLength);
            string str = (string)value;
            string padded = str.PadRight(_stringLength, ' ');
            byte[] bytes = Encoding.ASCII.GetBytes(padded);
            WriteElementBytes(index, bytes);
        }

        public object Print(long index)
        {
            if (!IsElementWritten(index))
                return null;
            byte[] bytes = ReadElementBytes(index);
            string result = Encoding.ASCII.GetString(bytes).TrimEnd(' ');
            return result;
        }
    }
}