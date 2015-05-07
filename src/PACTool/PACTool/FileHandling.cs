using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

using System.IO;

namespace PACTool
{

    class PacHeader
    {
        public char[] id;
        public int listSize;
        public int dataSize;
        int version;
    };
    class PacDir
    {
        public char[] id;
        public int nfiles;
        byte[] unknown;//6 bytes
        public PacFile[] file; //nfiles divided by 4

    };
    class PacFile
    {
        public char[] id; //4 bytes epac, 8 bytes epk8
        byte[] unknown1; //3 bytes
        public int size;
        byte unknown2;
    };

    class PacFileHandling 
    {
        BinaryReader pac;

        public PacFileHandling(BinaryReader b)
        {
            pac = b;
            ReadHeader();
            ReadDir();
        }

        PacHeader ReadHeader()
        {
            var header = new PacHeader();
            header.id = pac.ReadChars(4);
            header.listSize = (int)pac.ReadUInt32();
            header.dataSize = (int)pac.ReadUInt32();
            return header;
        }
        PacDir ReadDir()
        {
            var directory = new PacDir();
            pac.BaseStream.Seek(2048, SeekOrigin.Begin);
            directory.id = pac.ReadChars(4);
            directory.nfiles = (int)pac.ReadUInt16()/4;
            pac.ReadBytes(6);
            directory.file = new PacFile[directory.nfiles];
            for (int i = 0; i < directory.nfiles; i++)
            {
                directory.file[i] = ReadFile();
            }



                return directory;
        }
        PacFile ReadFile()
        {
            var pacfile = new PacFile();

            pacfile.id = pac.ReadChars(8);
            pac.ReadBytes(3);
            pacfile.size = (int)pac.ReadUInt32();
            pac.ReadByte();

            return pacfile;
        }
    }
}
