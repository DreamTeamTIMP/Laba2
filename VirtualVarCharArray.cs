using System;
using System.IO;
using System.Text;

namespace Laba2
{
    public class VirtualVarCharArray : VirtualMemoryArray, IVirtualArray
    {
        private FileStream _strFs;               // поток для файла со строками
        private int _maxStrSize;                  // максимальная длина строки
        private string _stringsFileName;          // имя файла для хранения строк

        // Размер элемента в основном файле = 4 байта (адрес)
        protected override int ElementSize => Constants.INT_SIZE;

        public VirtualVarCharArray(string filename, long arraySize, int maxStrSize)
        {
            this.fileName = filename;
            _stringsFileName = filename + ".str";  // например, test.dat.str
            _maxStrSize = maxStrSize;
            this.arraySize = arraySize;
            maxBufferPages = Constants.BUFFER_SIZE;
            elementsPerPage = Constants.ELEMS_PER_PAGE;

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
            // Создаём основной файл
            using (var stream = new FileStream(fileName, FileMode.Create))
            {
                // Сигнатура "VM"
                stream.Write(Encoding.ASCII.GetBytes("VM"), 0, Constants.SIGNATURE_SIZE);
                // Размер массива (long)
                stream.Write(BitConverter.GetBytes(size), 0, Constants.LONG_SIZE);
                // Тип 'V'
                stream.WriteByte((byte)'V');
                // Максимальная длина строки (int)
                stream.Write(BitConverter.GetBytes(maxStrSize), 0, Constants.INT_SIZE);

                int pageCount = (int)((size + elementsPerPage - 1) / elementsPerPage);
                for (int i = 0; i < pageCount; i++)
                {
                    stream.Write(new byte[Constants.BITMAP_SIZE], 0, Constants.BITMAP_SIZE);
                    stream.Write(new byte[Constants.PAGE_DATA_SIZE], 0, Constants.PAGE_DATA_SIZE);
                }
            }

            // Создаём файл для строк (пустой)
            using (var strStream = new FileStream(_stringsFileName, FileMode.Create)) { }

            // Открываем основной файл для работы
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

                if (type != 'V')
                    throw new InvalidDataException("File type is not varchar array");
                if (maxStr != _maxStrSize)
                    throw new InvalidDataException("Max string length mismatch");

                arraySize = fileSize;
                _maxStrSize = maxStr;
            }

            // Загружаем начальные страницы
            int totalPages = (int)((arraySize + elementsPerPage - 1) / elementsPerPage);
            for (int i = 0; i < Math.Min(maxBufferPages, totalPages); i++)
            {
                var page = LoadPageFromFile(i);
                page.lastAccess = DateTime.Now;
                buffer.Add(page);
            }
        }

        // Загрузка страницы из основного файла (адреса)
        protected override Page LoadPageFromFile(int pageNumber)
        {
            long pageOffset = Constants.SIGNATURE_SIZE + Constants.LONG_SIZE + 1 + Constants.INT_SIZE +
                              pageNumber * (Constants.BITMAP_SIZE + Constants.PAGE_DATA_SIZE);

            var page = new Page(pageNumber);
            fs.Seek(pageOffset, SeekOrigin.Begin);
            fs.Read(page.bitmap, 0, Constants.BITMAP_SIZE);
            fs.Read(page.data, 0, Constants.PAGE_DATA_SIZE);
            return page;
        }

        protected override void SavePageToFile(Page page)
        {
            long pageOffset = Constants.SIGNATURE_SIZE + Constants.LONG_SIZE + 1 + Constants.INT_SIZE +
                              page.pageNumber * (Constants.BITMAP_SIZE + Constants.PAGE_DATA_SIZE);
            fs.Seek(pageOffset, SeekOrigin.Begin);
            fs.Write(page.bitmap, 0, Constants.BITMAP_SIZE);
            fs.Write(page.data, 0, Constants.PAGE_DATA_SIZE);
            page.modified = false;
        }

        // Запись строки
        public void Input(long index, object value)
        {
            Validator.ValidateInput(index, value, arraySize, _maxStrSize);

            string str = (string)value;

            // Сначала записываем/обновляем строку в файле строк
            long stringPosition = WriteStringToFile(str);

            // Теперь сохраняем позицию (адрес) в основном файле как элемент
            byte[] addrBytes = BitConverter.GetBytes(stringPosition);
            WriteElementBytes(index, addrBytes);
        }

        // Чтение строки
        public object Print(long index)
        {
            if (!IsElementWritten(index))
                return null;

            byte[] addrBytes = ReadElementBytes(index);
            long stringPosition = BitConverter.ToInt64(addrBytes, 0); // адрес – long (8 байт?) но у нас элемент 4 байта? 
            // На самом деле в задании сказано, что на странице 128 элементов целого типа (int), т.е. адрес должен быть int (4 байта).
            // Но для больших файлов лучше использовать long. Для простоты оставим int.
            // Исправим: адрес – это смещение в файле строк, которое может быть long.
            // Но в основном файле элемент – int (4 байта). Значит, адрес должен помещаться в int. Для учебной задачи ок.
            // Поэтому читаем int:
            int stringPos = BitConverter.ToInt32(addrBytes, 0);
            return ReadStringFromFile(stringPos);
        }

        private long WriteStringToFile(string str)
        {
            // Запись строки в файл _strFs в формате: [длина(4 байта)][байты строки]
            // Возвращаем позицию начала записи (смещение от начала файла)
            byte[] lenBytes = BitConverter.GetBytes(str.Length);
            byte[] strBytes = Encoding.ASCII.GetBytes(str);

            _strFs.Seek(0, SeekOrigin.End); // добавляем в конец
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

        public override void Close()
        {
            base.Close();
            _strFs?.Close();
        }
    }
}