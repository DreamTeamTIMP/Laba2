namespace Laba2
{
    public static class Validator
    {
        public static void ValidateCreate(string fileName, long size, int maxStrSize)
        {
            ArgumentNullException.ThrowIfNull(fileName, "Имя файла было пустым");
            if (maxStrSize <= 0) throw new ArgumentException("String length must be positive");
            if (size <= 10000) throw new ArgumentException("Количество элементов должно быть >10000.");
        }
        public static void ValidateOpen(string fileName)
        {
            if (!File.Exists(fileName))
                throw new FileNotFoundException("Файл не найден.", fileName);

        }
        public static void ValidateInput(long index, object value, long arraySize, int strLength = 0)
        {
            ArgumentNullException.ThrowIfNull(value, "Значение для input было пустым");

            if (index < 0 || index >= arraySize)
                throw new IndexOutOfRangeException("Индекс вне границ массива.");

            if (value is string str)
            {
                if (str.Length > strLength)
                    throw new ArgumentException($"String too long. Max length is {strLength}");

            }
            if (value is int)
            {

            }
        }
        public static void ValidatePrint(long index)
        {

        }
    }
}