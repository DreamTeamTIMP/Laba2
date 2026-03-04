using System.Text;

namespace lab2
{
    public class VirtualIntArray : VirtualMemoryArray
    {
        public VirtualIntArray(string filename, int size) 
        {
            if (!File.Exists(filename))
                CreateFile(filename, size);

        }
        protected override void CreateFile(string fileName,int size)
        {
            var fs = new FileStream(fileName, FileMode.Create);
            fs.Write(Encoding.ASCII.GetBytes("VM"), 0, 2);
            fs.Write(BitConverter.GetBytes(size), 0, 4);
            fs.Write(Encoding.ASCII.GetBytes("I"), 0, 1);

        }
    }
}
