namespace Laba2
{
    internal static class Constants
    {
        public const int PAGE_DATA_SIZE = 512;//байт данных на странице
        public const int ELEMS_PER_PAGE = 128;//кол-во элементов на странице
        public const int INT_SIZE = 4;
        public const int SIGNATURE_SIZE = 2;
        public const int LONG_SIZE = 8;
        public const int BYTE = 16;
        public const int CHAR_SIZE = 1;
        public const int BITMAP_SIZE = 16;
        public const int BUFFER_SIZE = 3;//минимум 3 страницы в памяти 
    }
}