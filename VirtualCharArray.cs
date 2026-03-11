using System;
using System.IO;
using System.Text;

namespace Laba2
{
    public class VirtualCharArray : VirtualMemoryArray, IVirtualArray
    {
        private int _stringLength;          // фиксированная длина строки (в символах)

        // Реализация абстрактного свойства базового класса
        protected override int ElementSize => _stringLength; // 1 байт на символ (ASCII)

        
        /// <param name="filename">Имя файла (расширение .dat будет добавлено автоматически)</param>
        /// <param name="arraySize">Количество элементов ( > 10000 )</param>
        /// <param name="stringLength">Фиксированная длина строки в символах</param>
        public VirtualCharArray(string filename, long arraySize, int stringLength)
        {
            this.fileName = filename;
            _stringLength = stringLength;
            this.arraySize = arraySize;
            maxBufferPages = Constants.BUFFER_SIZE;
            elementsPerPage = Constants.ELEMS_PER_PAGE;

            if (!File.Exists(filename))
            {
                Create(filename, arraySize, stringLength);
            }
            else
            {
                Open(filename);
            }
        }

        // Создание нового файла
        public void Create(string fileName, long size, int maxStrSize)
        {
            Validator.ValidateCreate(fileName, size, maxStrSize);

            _stringLength = maxStrSize;
            arraySize = size;

            using (var stream = new FileStream(fileName, FileMode.Create))
            {
                // Сигнатура "VM"
                stream.Write(Encoding.ASCII.GetBytes("VM"), 0, Constants.SIGNATURE_SIZE);
                // Размер массива (long)
                stream.Write(BitConverter.GetBytes(size), 0, Constants.LONG_SIZE);
                // Тип 'C'
                stream.WriteByte((byte)'C');
                // Длина строки (int)
                stream.Write(BitConverter.GetBytes(maxStrSize), 0, Constants.INT_SIZE);

                // Количество страниц
                int pageCount = (int)((size + elementsPerPage - 1) / elementsPerPage);

                for (int i = 0; i < pageCount; i++)
                {
                    // Битовая карта (16 байт нулей)
                    stream.Write(new byte[Constants.BITMAP_SIZE], 0, Constants.BITMAP_SIZE);
                    // Данные страницы (512 байт нулей)
                    stream.Write(new byte[Constants.PAGE_DATA_SIZE], 0, Constants.PAGE_DATA_SIZE);
                }
            }

            // Открываем файл для дальнейшей работы
            fs = new FileStream(fileName, FileMode.Open, FileAccess.ReadWrite);
        }

        // Открытие существующего файла
        public void Open(string fileName)
        {
            
            fs = new FileStream(fileName, FileMode.Open, FileAccess.ReadWrite);
            using (var reader = new BinaryReader(fs, Encoding.ASCII, true))
            {
                // Проверка сигнатуры
                byte[] sig = reader.ReadBytes(Constants.SIGNATURE_SIZE);
                if (sig[0] != 'V' || sig[1] != 'M')
                    throw new InvalidDataException("Invalid file signature");

                // Чтение заголовка
                long fileSize = reader.ReadInt64();
                byte type = reader.ReadByte();
                int strLen = reader.ReadInt32();

                if (type != 'C')
                    throw new InvalidDataException("File type is not char array");
                if (strLen != _stringLength)
                    throw new InvalidDataException("String length mismatch");

                arraySize = fileSize;
                _stringLength = strLen;
            }

            // Позиционируемся на начало данных (после заголовка)
            fs.Seek(Constants.SIGNATURE_SIZE + Constants.LONG_SIZE + 1 + Constants.INT_SIZE, SeekOrigin.Begin);

            // Загружаем первые страницы в буфер (не более maxBufferPages)
            int totalPages = (int)((arraySize + elementsPerPage - 1) / elementsPerPage);
            for (int i = 0; i < Math.Min(maxBufferPages, totalPages); i++)
            {
                var page = LoadPageFromFile(i);
                page.lastAccess = DateTime.Now;
                buffer.Add(page);
            }
        }

        // Переопределение загрузки страницы из файла
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

        // Переопределение сохранения страницы в файл
        protected override void SavePageToFile(Page page)
        {
            long pageOffset = Constants.SIGNATURE_SIZE + Constants.LONG_SIZE + 1 + Constants.INT_SIZE +
                              page.pageNumber * (Constants.BITMAP_SIZE + Constants.PAGE_DATA_SIZE);

            fs.Seek(pageOffset, SeekOrigin.Begin);
            fs.Write(page.bitmap, 0, Constants.BITMAP_SIZE);
            fs.Write(page.data, 0, Constants.PAGE_DATA_SIZE);
            page.modified = false;
        }

        // Реализация интерфейса IVirtualArray
        public void Input(long index, object value)
        {
            Validator.ValidateInput(index, (string)value, arraySize,_stringLength);

            string str = (string)value;
            
            // Дополняем пробелами до фиксированной длины
            string padded = str.PadRight(_stringLength, ' ');
            byte[] bytes = Encoding.ASCII.GetBytes(padded);

            WriteElementBytes(index, bytes);
        }

        public object Print(long index)
        {
            if (!IsElementWritten(index))
                return null; // ничего не записано

            byte[] bytes = ReadElementBytes(index);
            string result = Encoding.ASCII.GetString(bytes).TrimEnd(' ');
            return result;
        }
    }
}