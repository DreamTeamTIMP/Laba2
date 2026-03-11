using System.Text;

namespace Laba2
{
    public class VirtualIntArray : VirtualMemoryArray, ICreator, IVirtualArray
    {
        protected override int ElementSize => Constants.INT_SIZE;
        public VirtualIntArray(string filename, long arraySize)
        {
            if (!File.Exists(filename))
                Create(filename, arraySize);


        }

        public void Close()
        {
            throw new NotImplementedException();
        }

        public void Create(string fileName, long size, int maxStrSize = 0)
        {
            fs = new FileStream(fileName, FileMode.Create);
            fs.Write(Encoding.ASCII.GetBytes("VM"), 0, Constants.SIGNATURE_SIZE);
            fs.Write(BitConverter.GetBytes(size), 0, Constants.LONG_SIZE);
            fs.Write(Encoding.ASCII.GetBytes("I"), 0, Constants.CHAR_SIZE);

            long totalDataBytes = Constants.INT_SIZE * size;
            int pageCount = (int)((totalDataBytes + (Constants.PAGE_DATA_SIZE - 1)) / Constants.PAGE_DATA_SIZE);

            for (int i = 0; i < pageCount; i++)
            {
                fs.Write(new byte[Constants.BITMAP_SIZE], 0, Constants.BITMAP_SIZE);
                fs.Write(new byte[Constants.PAGE_DATA_SIZE], 0, Constants.PAGE_DATA_SIZE);
            }
        }

        public void Input(long index, object value)
        {

        }

        public void Open(string fileName)
        {
            throw new NotImplementedException();
        }

        public object Print(long index)
        {
            throw new NotImplementedException();
        }
    }
}