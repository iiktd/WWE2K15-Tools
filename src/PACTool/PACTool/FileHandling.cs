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
        //FileIO
        public string id; //4 bytes epac, 8 bytes epk8
        byte[] unknown1; //3 bytes
        public int size;
        byte unknown2;

        //Useful Stuff
        public int offset;
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
            pacFile.dir = ReadDir().ToArray();
        }

        PacHeader ReadHeader()
        {
            var header = new PacHeader();
            header.id = new string(pacStream.ReadChars(4));
            header.listSize = (int)pacStream.ReadUInt32();
            header.dataSize = (int)pacStream.ReadUInt32();
            return header;
        }
        List<PacDir> ReadDir()
        {
            var iOffset = 16384; //Start of data chunk
            var dirList = new List<PacDir>();
            pacStream.BaseStream.Seek(2048, SeekOrigin.Begin);

            while (pacStream.BaseStream.Position < (2048+pacFile.header.listSize)) 
            {
                if (pacFile.header.id == "EPK8")
                {
                    var directory = new PacDir();
                    directory.id = new string(pacStream.ReadChars(4));
                    directory.nfiles = (int)pacStream.ReadUInt16() / 4;
                    pacStream.ReadBytes(6);
                    directory.file = new PacFile[directory.nfiles];
                    for (int i = 0; i < directory.nfiles; i++)
                    {
                        directory.file[i] = ReadFile();
                        directory.file[i].offset = iOffset;

                        //Need to 2048 byte align these chunks
                        var size = directory.file[i].size;
                        iOffset += size + ((2048 - (size % 2048)) % 2048);
                    }
                    dirList.Add(directory);
                }
                else 
                {
                    //EPAC, we're fudging this since we don't know how directories are laid out.
                    var directory = new PacDir();
                    directory.id = new string(pacStream.ReadChars(4));
                    directory.nfiles = (int)pacStream.ReadUInt16() / 3;
                    pacStream.ReadBytes(6);
                    directory.file = new PacFile[directory.nfiles];
                    directory.file = new PacFile[directory.nfiles];
                    for (int i = 0; i < directory.nfiles; i++)
                    {
                        directory.file[i] = ReadFile();
                        directory.file[i].offset = iOffset;

                        //Need to 2048 byte align these chunks
                        var size = directory.file[i].size;
                        iOffset += size + ((2048 - (size % 2048)) % 2048);
                    }
                    dirList.Add(directory);
                }
            }

            return dirList;
        }
        PacFile ReadFile()
        {
            var pacfile = new PacFile();

            if (pacFile.header.id == "EPK8") { pacfile.id = new string(pacStream.ReadChars(8)); }
            else { pacfile.id = new string(pacStream.ReadChars(4)); }
            
            pacStream.ReadBytes(3);
            pacfile.size = (int)pacStream.ReadUInt32();
            pacStream.ReadByte();

            return pacfile;
        }

        public void ExtractFile(ListViewItem item) 
        {
            string[] args = Environment.GetCommandLineArgs();
            int offset = Convert.ToInt32(item.SubItems[3].Text);
            int size = Convert.ToInt32(item.SubItems[2].Text);
            
            //Experienced coders will probably cringe at this...
            var newFile = new byte[size];
            pacStream.BaseStream.Seek(offset, SeekOrigin.Begin);
            newFile = pacStream.ReadBytes(size);

            //build path\\filename.ext
            string filename = Path.GetDirectoryName(args[1]) + "\\" + item.SubItems[0].Text;
            using (BinaryWriter b = new BinaryWriter(File.Open(filename, FileMode.Create)))
            {
                foreach (var i in newFile)
                {
                    b.Write(i);
                }
            }
        }
    }
}
