using System;
using System.IO;
using System.Text;

namespace Laba2
{
    public class VirtualVarCharArray : VirtualMemoryArray, IVirtualArray
    {
        private FileStream _strFs;
        private int _maxStrSize;
        private string _stringsFileName;

        protected override int ElementSize => Constants.INT_SIZE; // храним адрес как int

        public VirtualVarCharArray(string filename, long arraySize, int maxStrSize)
        {
            this.fileName = filename;
            _stringsFileName = filename + ".str";
            _maxStrSize = maxStrSize;
            this.arraySize = arraySize;
            maxBufferPages = Constants.BUFFER_SIZE;
            elementsPerPage = Constants.ELEMS_PER_PAGE;
            _pageDataSize = Constants.PAGE_DATA_SIZE; // 512 байт (128 int)

            if (!File.Exists(filename))
            {
                Create(filename, arraySize, maxStrSize);
            }
            else
            {
                Open(filename);
            }
        }

        public void Create(string fileName, long size, int maxStrSize)
        {
            Validator.ValidateCreate(fileName, size, maxStrSize);

            int pageCount = (int)((size + elementsPerPage - 1) / elementsPerPage);
            long totalFileSize = Constants.SIGNATURE_SIZE + Constants.LONG_SIZE + 1 + Constants.INT_SIZE +
                                 pageCount * (Constants.BITMAP_SIZE + _pageDataSize);
            Validator.CheckDiskSpace(fileName, totalFileSize);

            using (var stream = new FileStream(fileName, FileMode.Create))
            {
                stream.Write(Encoding.ASCII.GetBytes(Constants.SIGNATURE), 0, Constants.SIGNATURE_SIZE);
                stream.Write(BitConverter.GetBytes(size), 0, Constants.LONG_SIZE);
                stream.WriteByte((byte)Constants.ARRAY_TYPE_VARCHAR);
                stream.Write(BitConverter.GetBytes(maxStrSize), 0, Constants.INT_SIZE);

                for (int i = 0; i < pageCount; i++)
                {
                    stream.Write(new byte[Constants.BITMAP_SIZE], 0, Constants.BITMAP_SIZE);
                    stream.Write(new byte[_pageDataSize], 0, _pageDataSize);
                }
            }

            using (var strStream = new FileStream(_stringsFileName, FileMode.Create)) { }

            fs = new FileStream(fileName, FileMode.Open, FileAccess.ReadWrite);
            _strFs = new FileStream(_stringsFileName, FileMode.Open, FileAccess.ReadWrite);
        }

        public void Open(string fileName)
        {
            fs = new FileStream(fileName, FileMode.Open, FileAccess.ReadWrite);
            _stringsFileName = fileName + ".str";
            _strFs = new FileStream(_stringsFileName, FileMode.OpenOrCreate, FileAccess.ReadWrite);

            using (var reader = new BinaryReader(fs, Encoding.ASCII, true))
            {
                byte[] sig = reader.ReadBytes(Constants.SIGNATURE_SIZE);
                if (sig[0] != 'V' || sig[1] != 'M')
                    throw new InvalidDataException("Invalid signature");

                long fileSize = reader.ReadInt64();
                byte type = reader.ReadByte();
                int maxStr = reader.ReadInt32();

                if (type != Constants.ARRAY_TYPE_VARCHAR)
                    throw new InvalidDataException("File type is not varchar array");
                if (maxStr != _maxStrSize)
                    throw new InvalidDataException("Max string length mismatch");

                arraySize = fileSize;
                _maxStrSize = maxStr;
            }

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

        private long WriteStringToFile(string str)
        {
            byte[] lenBytes = BitConverter.GetBytes(str.Length);
            byte[] strBytes = Encoding.ASCII.GetBytes(str);
            _strFs.Seek(0, SeekOrigin.End);
            long position = _strFs.Position;
            _strFs.Write(lenBytes, 0, 4);
            _strFs.Write(strBytes, 0, strBytes.Length);
            _strFs.Flush();
            return position;
        }

        private string ReadStringFromFile(long position)
        {
            _strFs.Seek(position, SeekOrigin.Begin);
            byte[] lenBytes = new byte[4];
            _strFs.Read(lenBytes, 0, 4);
            int len = BitConverter.ToInt32(lenBytes, 0);
            byte[] strBytes = new byte[len];
            _strFs.Read(strBytes, 0, len);
            return Encoding.ASCII.GetString(strBytes);
        }

        public void Input(long index, object value)
        {
            Validator.ValidateInput(index, value, arraySize, _maxStrSize);
            string str = (string)value;

            long stringPosition = WriteStringToFile(str);
            if (stringPosition > int.MaxValue)
                throw new InvalidOperationException("String file too large, address exceeds 4 bytes");
            int addr = (int)stringPosition;
            byte[] addrBytes = BitConverter.GetBytes(addr);
            WriteElementBytes(index, addrBytes);
        }

        public object Print(long index)
        {
            if (!IsElementWritten(index))
                return null;
            byte[] addrBytes = ReadElementBytes(index);
            int addr = BitConverter.ToInt32(addrBytes, 0);
            return ReadStringFromFile(addr);
        }

        public override void Close()
        {
            base.Close();
            _strFs?.Close();
        }
    }
}