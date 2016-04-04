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
    public class FileIO
    {
        // this is where we load the functions from our unmanaged dll
        [DllImport("YukesBPE.dll")]
        public static extern int yukes_bpe(byte[] input, int insz, byte[] ouput, int outsz, int fill_outsz);

        public void ExtractFile(byte[] newFile, string filename)
        {
            (new FileInfo(filename)).Directory.Create();
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

            output = ZlibStream.UncompressBuffer(input);
            ExtractFile(output, filename);
        }

        public byte[] Decompress(byte[] file, string type)
        {
            byte[] align = new byte[4]; Array.Copy(file, 4, align, 0, 4); //used for byte alignment. But it's being fudged by the dll.
            byte[] compressed = new byte[4]; Array.Copy(file, 8, compressed, 0, 4);
            byte[] uncompressed = new byte[4]; Array.Copy(file, 12, uncompressed, 0, 4);
            byte[] input = new byte[BitConverter.ToInt32(compressed, 0)]; Array.Copy(file, 16, input, 0, file.Length - 16);
            byte[] output = new byte[BitConverter.ToInt32(uncompressed, 0)];
            int padding = BitConverter.ToInt32(uncompressed, 0) - BitConverter.ToInt32(compressed, 0);

            if (type == "BPE ") { yukes_bpe(input, BitConverter.ToInt32(compressed, 0), output, BitConverter.ToInt32(uncompressed, 0), padding); }
            if (type == "ZLIB") { output = ZlibStream.UncompressBuffer(input); }

            return output;
        }

        internal byte[] Compress(byte[] file, string type)
        {
            if (type == "BPE ") return CompressBPE(file);
            if (type == "ZLIB") return CompressZLIB(file);
            return file;
        }

        private byte[] CompressZLIB(byte[] file)
        {
            MemoryStream compressed = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(compressed);

            byte[] bytes = ZlibStream.CompressBuffer(file);

            Int32 size = (Int32)(file.Length);
            //What we need:
            //  4 bytes - "ZLIB" header
            //  4 bytes - something magical, assume 00 01 00 00, possibly version?
            //  4 bytes - compressed size
            //  4 bytes - uncompressed size

            writer.Write(Encoding.ASCII.GetBytes("ZLIB"));
            writer.Write((byte)0);
            writer.Write((byte)1);
            writer.Write((byte)0);
            writer.Write((byte)0);
            writer.Write((Int32)(bytes.Length));
            writer.Write((Int32)(size));

            writer.Write(bytes);

            writer.Close();
            return compressed.ToArray();
        }

        private byte[] CompressBPE(byte[] file)
        {
            MemoryStream compressed = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(compressed);

            Int16 size = (Int16)(file.Length);
            //What we need:
            //  4 bytes - "BPE " header
            //  4 bytes - something magical, assume 00 01 00 00, possibly version?
            //  4 bytes - compressed size
            //  4 bytes - uncompressed size
            //
            //  5 bytes - compression info, see below
            // Plus file, size + 21 total

            writer.Write(Encoding.ASCII.GetBytes("BPE "));
            writer.Write((byte)0);
            writer.Write((byte)1);
            writer.Write((byte)0);
            writer.Write((byte)0);
            writer.Write((Int32)(size + 5));
            writer.Write((Int32)(size));

            writer.Write((byte)255);
            writer.Write((byte)128);
            writer.Write((byte)254);
            writer.Write(size);
            
            writer.Write(file);

            writer.Close();
            return compressed.ToArray();
        }
    }
}
