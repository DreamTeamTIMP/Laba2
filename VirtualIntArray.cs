using System.IO;
using System.Text;

namespace Laba2
{
    public class VirtualIntArray : VirtualMemoryArray, ICreator, IVirtualArray
    {
        protected override int ElementSize => Constants.INT_SIZE;
        public VirtualIntArray(string filename, long arraySize)
        {
            this.fileName = filename;
            this.arraySize = arraySize;
            maxBufferPages = Constants.BUFFER_SIZE;
            elementsPerPage = Constants.ELEMS_PER_PAGE;

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

            using (var stream = new FileStream(fileName, FileMode.Create))
            {
                stream.Write(Encoding.ASCII.GetBytes("VM"), 0, Constants.SIGNATURE_SIZE);
                stream.Write(BitConverter.GetBytes(size), 0, Constants.LONG_SIZE);
                // Тип 'I'
                stream.WriteByte((byte)'I');

                int pageCount = (int)((size * Constants.INT_SIZE + (Constants.PAGE_DATA_SIZE - 1)) / Constants.PAGE_DATA_SIZE);

                for (int i = 0; i < pageCount; i++)
                {
                    stream.Write(new byte[Constants.BITMAP_SIZE], 0, Constants.BITMAP_SIZE);
                    stream.Write(new byte[Constants.PAGE_DATA_SIZE], 0, Constants.PAGE_DATA_SIZE);
                }
            }

            // Открываем файл для дальнейшей работы
            fs = new FileStream(fileName, FileMode.Open, FileAccess.ReadWrite);
            }

        public void Input(long index, object value)
        {
            Validator.ValidateInput(index, (int)value, arraySize);

            int intValue = (int)value;

            WriteElementBytes(index, BitConverter.GetBytes(intValue));
        }

        public void Open(string fileName)
        {
            Validator.ValidateOpen(fileName);

            fs = new FileStream(fileName, FileMode.Open, FileAccess.ReadWrite);

            using (var reader = new BinaryReader(fs, Encoding.ASCII, true))
            {
                // Проверка сигнатуры
                byte[] sig = reader.ReadBytes(Constants.SIGNATURE_SIZE);
                if (sig[0] != 'V' || sig[1] != 'M')
                    throw new InvalidDataException("Неверная сигнатура");

                // Чтение заголовка
                long fileSize = reader.ReadInt64();
                byte type = reader.ReadByte();

                if (type != 'I')
                    throw new InvalidDataException("Тип файла не int");

                arraySize = fileSize;
            }

            fs.Seek(Constants.SIGNATURE_SIZE + Constants.LONG_SIZE + Constants.CHAR_SIZE, SeekOrigin.Begin);

            // Загружаем первые страницы в буфер (не более maxBufferPages)
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
                              pageNumber * (Constants.BITMAP_SIZE + Constants.PAGE_DATA_SIZE);

            var page = new Page(pageNumber);

            fs.Seek(pageOffset, SeekOrigin.Begin);
            fs.Read(page.bitmap, 0, Constants.BITMAP_SIZE);
            fs.Read(page.data, 0, Constants.PAGE_DATA_SIZE);

            return page;
        }

        // Переопределение сохранения страницы в файл
        protected override void SavePageToFile(Page page)
        {
            long pageOffset = Constants.SIGNATURE_SIZE + Constants.LONG_SIZE + Constants.CHAR_SIZE +
                              page.pageNumber * (Constants.BITMAP_SIZE + Constants.PAGE_DATA_SIZE);

            fs.Seek(pageOffset, SeekOrigin.Begin);
            fs.Write(page.bitmap, 0, Constants.BITMAP_SIZE);
            fs.Write(page.data, 0, Constants.PAGE_DATA_SIZE);
            page.modified = false;
        }


        public object Print(long index)
        {

            if (!IsElementWritten(index))
                return null; // ничего не записано

            byte[] bytes = ReadElementBytes(index);

            return BitConverter.ToInt32(bytes, 0);
        }


    }
}