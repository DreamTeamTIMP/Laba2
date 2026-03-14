namespace Laba2
{
    public static class Validator
    {
        /// <summary>
        /// Проверка параметров создания массива.
        /// </summary>
        public static void ValidateCreate(string fileName, long size, int maxStrSize)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                throw new ArgumentNullException(nameof(fileName), "Имя файла не может быть пустым.");

            if (size <= 10000)
                throw new ArgumentException("Количество элементов должно быть больше 10000.");

            if (maxStrSize <= 0)
                throw new ArgumentException("Длина строки должна быть положительным числом.");
        }
        /// <summary>
        /// Проверка существования файла при открытии.
        /// </summary>
        public static void ValidateOpen(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                throw new ArgumentNullException(nameof(fileName), "Имя файла не может быть пустым.");

            if (!File.Exists(fileName))
                throw new FileNotFoundException("Файл не найден.", fileName);
        }
        /// <summary>
        /// Проверка индекса элемента.
        /// </summary>
        public static void ValidateIndex(long index, long arraySize)
        {
            if (index < 0 || index >= arraySize)
                throw new IndexOutOfRangeException($"Индекс {index} выходит за границы массива (0..{arraySize - 1}).");
        }
        /// <summary>
        /// Проверка значения для записи.
        /// </summary>
        /// <param name="index">Индекс элемента.</param>
        /// <param name="value">Значение.</param>
        /// <param name="arraySize">Размер массива.</param>
        /// <param name="strLength">Максимальная длина строки (для строковых типов).</param>
        public static void ValidateInput(long index, object value, long arraySize, int strLength = 0)
        {
            ValidateIndex(index, arraySize);

            if (value == null)
                throw new ArgumentNullException(nameof(value), "Значение не может быть null.");

            if (value is string str)
            {
                if (strLength <= 0)
                    throw new InvalidOperationException("Внутренняя ошибка: для строкового массива не задана допустимая длина.");

                if (str.Length > strLength)
                    throw new ArgumentException($"Длина строки ({str.Length}) превышает максимально допустимую ({strLength}).");
            }
            else
            {
                throw new ArgumentException($"Неподдерживаемый тип значения: {value.GetType()}. Ожидалось int или string.");
            }
        }
        /// <summary>
        /// Проверка наличия достаточного свободного места на диске.
        /// </summary>
        /// <param name="filePath">Путь к файлу (для определения диска).</param>
        /// <param name="requiredBytes">Требуемое количество байт.</param>
        public static void CheckDiskSpace(string filePath, long requiredBytes)
        {
            try
            {
                var drive = new DriveInfo(Path.GetPathRoot(Path.GetFullPath(filePath)) ?? "");
                if (drive.IsReady && drive.AvailableFreeSpace < requiredBytes)
                {
                    throw new IOException($"Недостаточно свободного места на диске {drive.Name}. " +
                                          $"Требуется {requiredBytes} байт, доступно {drive.AvailableFreeSpace} байт.");
                }
            }
            catch (ArgumentException ex)
            {
                throw new IOException("Не удалось определить диск для проверки свободного места.", ex);
            }
        }
    }
}