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
    }
    public class PacDir
    {
        public string id;
        public int nfiles;
        byte[] unknown;//6 bytes
        public PacFile[] file; //nfiles divided by 4

    }
    public class PacFile
    {
        //FileIO
        public string id; //4 bytes epac, 8 bytes epk8
        byte[] unknown1; //3 bytes
        public int size;
        byte unknown2;

        //Useful Stuff
        public int offset;

        //byte array for stream
        public PACH subFile;
    }

    public class PACH 
    {
        public string id; //4 bytes
        public int nfiles;
        public PACHFile[] file;
    }

    public class PACHFile 
    {
        public int id;
        public int offset; //relative to start of data
        public int size;

        //byte array for stream
        public byte[] stream;
    }

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
            pacFile.dir = ReadDir().ToArray();
        }

        PacHeader ReadHeader()
        {
            var header = new PacHeader();
            header.id = new string(pacStream.ReadChars(4));

            if (header.id == "EPK8" || header.id == "EPAC") 
            {
                header.listSize = (int)pacStream.ReadUInt32();
                header.dataSize = (int)pacStream.ReadUInt32();
            }
            else 
            { 
                //PACH 
            }
            
            return header;
        }
        List<PacDir> ReadDir()
        {
            var iOffset = 16384; //Start of data chunk
            var dirList = new List<PacDir>();
            pacStream.BaseStream.Seek(2048, SeekOrigin.Begin);

            while (pacStream.BaseStream.Position < (2048+pacFile.header.listSize)) 
            {
                    var directory = new PacDir();
                    directory.id = new string(pacStream.ReadChars(4));
                    if (pacFile.header.id == "EPK8") { directory.nfiles = (int)pacStream.ReadUInt16() / 4; }
                    else { directory.nfiles = (int)pacStream.ReadUInt16() / 3; } //EPAC
                    pacStream.ReadBytes(6);
                    directory.file = new PacFile[directory.nfiles];
                    for (int i = 0; i < directory.nfiles; i++)
                    {
                        directory.file[i] = ReadFile();
                        directory.file[i].offset = iOffset;

                        //Need to 2048 byte align these chunks
                        var size = directory.file[i].size;
                        iOffset += size + ((2048 - (size % 2048)) % 2048);

                        //Check for PACH
                        var pos = pacStream.BaseStream.Position;
                        pacStream.BaseStream.Position = directory.file[i].offset;
                        directory.file[i].subFile = ReadSubFile(pacStream.ReadBytes(size));
                        pacStream.BaseStream.Position = pos;

                    }
                    dirList.Add(directory);
            }

            return dirList;
        }
        PacFile ReadFile()
        {
            var pacfile = new PacFile();

            if (pacFile.header.id == "EPK8") { pacfile.id = new string(pacStream.ReadChars(8)); }
            else if (pacFile.header.id == "EPAC") { pacfile.id = new string(pacStream.ReadChars(4)); }
            else { } //PACH
            
            pacStream.ReadBytes(3);
            pacfile.size = (int)pacStream.ReadUInt32();
            pacStream.ReadByte();
            return pacfile;
        }

        PACH ReadSubFile(byte[] file) 
        {
            MemoryStream pachmem = new MemoryStream(file);
            BinaryReader pachStream = new BinaryReader(pachmem);
            var subfile = new PACH();

            subfile.id = new string(pachStream.ReadChars(4)); //Obviously PACH
            subfile.nfiles = (int)pachStream.ReadUInt32();
            subfile.file = new PACHFile[subfile.nfiles];
            for(var i=0; i<subfile.nfiles; i++)
            {
                subfile.file[i] = ReadSubSubFile(pachStream);
            }

            pachStream.Close();
            pachmem.Close();
            return subfile;
        }
        PACHFile ReadSubSubFile(BinaryReader pachStream) 
        {
            var pachfile = new PACHFile();
            pachfile.id = (int)pachStream.ReadUInt32();
            pachfile.offset = (int)pachStream.ReadUInt32();
            pachfile.size = (int)pachStream.ReadUInt32();


            return pachfile;
        }

        
    }
}
