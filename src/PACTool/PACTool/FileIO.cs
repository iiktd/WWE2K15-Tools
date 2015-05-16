using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;

// this is needed to use DLLImport
using System.Runtime.InteropServices;
// DotNetZlib
using Ionic.Zlib;

namespace PACTool
{
    class FileIO
    {
        // this is where we load the functions from our unmanaged dll
        [DllImport("YukesBPE.dll")]
        public static extern int yukes_bpe(byte[] input, int insz, byte[] ouput, int outsz, int fill_outsz);


        public void ExtractFile(byte[] newFile, string filename)
        {
            using (BinaryWriter b = new BinaryWriter(File.Open(filename, FileMode.Create)))
            {
                foreach (var i in newFile) { b.Write(i); }
            }
        }

        public void DecompressBPE(byte[] file, string filename) 
        {
            byte[] align = new byte[4]; Array.Copy(file, 4, align, 0, 4); //used for byte alignment. But it's being fudged by the dll.
            byte[] compressed = new byte[4]; Array.Copy(file, 8, compressed, 0, 4);
            byte[] uncompressed = new byte[4]; Array.Copy(file, 12, uncompressed, 0, 4);
            byte[] input = new byte[BitConverter.ToInt32(compressed, 0)]; Array.Copy(file, 16, input, 0, file.Length - 16);
            byte[] output = new byte[BitConverter.ToInt32(uncompressed, 0)];
            int padding = BitConverter.ToInt32(uncompressed, 0) - BitConverter.ToInt32(compressed, 0);

            if (yukes_bpe(input, BitConverter.ToInt32(compressed, 0), output, BitConverter.ToInt32(uncompressed, 0), padding) == BitConverter.ToInt32(uncompressed, 0))
            {
                ExtractFile(output, filename); 
            }
        }

        public void DecompressZLIB(byte[] file, string filename) 
        {
            byte[] align = new byte[4]; Array.Copy(file, 4, align, 0, 4); //used for byte alignment. But it's being fudged by the dll.
            byte[] compressed = new byte[4]; Array.Copy(file, 8, compressed, 0, 4);
            byte[] uncompressed = new byte[4]; Array.Copy(file, 12, uncompressed, 0, 4);
            byte[] input = new byte[BitConverter.ToInt32(compressed, 0)]; Array.Copy(file, 16, input, 0, file.Length - 16);
            byte[] output = new byte[BitConverter.ToInt32(uncompressed, 0)];

            

            //output = DeflateStream.UncompressBuffer(input);
            output = ZlibStream.UncompressBuffer(input);
            ExtractFile(output, filename);
        }
    }
}
