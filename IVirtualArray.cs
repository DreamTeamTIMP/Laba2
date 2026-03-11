namespace Laba2
{
    public interface IVirtualArray
    {
        void Create(string fileName, long size, int maxStrSize = 0);
        void Open(string fileName);
        void Input(long index, object value);
        object Print(long index);
        void Close();
    }
}