using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

using System.IO;

namespace PACTool
{

    public class PacHeader
    {
        public string id;
        public int listSize;
        public int dataSize;
        int version;
    };
    public class PacDir
    {
        public string id;
        public int nfiles;
        byte[] unknown;//6 bytes
        public PacFile[] file; //nfiles divided by 4

    };
    public class PacFile
    {
        public string id; //4 bytes epac, 8 bytes epk8
        byte[] unknown1; //3 bytes
        public int size;
        byte unknown2;
    };
    public class Pac
    {
        public PacHeader header;
        public PacDir[] dir;
    }
    

    public class PacFileHandling 
    {
        public BinaryReader pacStream;
        public Pac pacFile;

        public PacFileHandling(BinaryReader b)
        {
            pacFile = new Pac();
            pacStream = b;
            pacFile.header = ReadHeader();

            pacFile.dir = new PacDir[1];
            pacFile.dir[0] = ReadDir();
        }

        public PacHeader ReadHeader()
        {
            var header = new PacHeader();
            header.id = new string(pacStream.ReadChars(4));
            header.listSize = (int)pacStream.ReadUInt32();
            header.dataSize = (int)pacStream.ReadUInt32();
            return header;
        }
        public PacDir ReadDir()
        {
            var directory = new PacDir();
            pacStream.BaseStream.Seek(2048, SeekOrigin.Begin);
            directory.id = new string(pacStream.ReadChars(4));
            directory.nfiles = (int)pacStream.ReadUInt16() / 4;
            pacStream.ReadBytes(6);
            directory.file = new PacFile[directory.nfiles];
            for (int i = 0; i < directory.nfiles; i++)
            {
                directory.file[i] = ReadFile();
            }



                return directory;
        }
        public PacFile ReadFile()
        {
            var pacfile = new PacFile();

            pacfile.id = new string(pacStream.ReadChars(8));
            pacStream.ReadBytes(3);
            pacfile.size = (int)pacStream.ReadUInt32();
            pacStream.ReadByte();

            return pacfile;
        }
    }
}
