using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

using System.IO;

namespace PACTool
{

    

    public class Pac
    {
        public PacHeader header;
        public PacDir[] dir;
        public PACH container; //There are some free floating pachs.
        public TextureArchive[] textures;
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

            if (pacFile.header.id == "PACH")
            {
                //You opened a free floating PACH file~!
                pacStream.BaseStream.Position = 0;
                pacFile.container = ReadPACHContainer(pacStream.ReadBytes((int)pacStream.BaseStream.Length));
            }
            else if (pacFile.header.id == "EPK8" || pacFile.header.id == "EPAC")  
            { 
                pacFile.dir = ReadDir().ToArray(); 
            }
            else 
            { 
                //probably a texture archive...
                pacFile.header.id = "Texture Archive";
                pacStream.BaseStream.Position = 0;
                int nfiles = pacStream.ReadInt32();
                pacStream.ReadBytes(12); //The rest of the header. Not important...

                pacFile.textures = new TextureArchive[nfiles];
                for (var i = 0; i < nfiles; i++) 
                {
                    var texture = new TextureArchive();
                    char[] texturefilename = new char[0];

                    //sigh, apparantly these aren't just padded strings...
                    var temp = pacStream.ReadBytes(16);
                    for (var j = 0; j < temp.Length; j++)
                    {
                        if (temp[j] == 0) 
                        {
                            texturefilename = new char[j];
                            Array.Copy(temp, texturefilename, j);
                            break;
                        }
                    }

                    texture.alignedstring = new string(texturefilename);
                    texture.extension = new string(pacStream.ReadChars(4));
                    texture.size = pacStream.ReadInt32();
                    texture.offset = pacStream.ReadInt32();
                    pacStream.ReadBytes(4);

                    var pos = pacStream.BaseStream.Position;
                    pacStream.BaseStream.Position = texture.offset;
                    texture.stream = pacStream.ReadBytes(texture.size);
                    pacStream.BaseStream.Position = pos;
                    pacFile.textures[i] = texture;
                }
            }
        }

        PacHeader ReadHeader()
        {
            var header = new PacHeader();
            header.id = new string(pacStream.ReadChars(4));

            if (header.id == "EPK8" || header.id == "EPAC") 
            {
                header.listSize = (int)pacStream.ReadUInt32();
                header.dataSize = (int)pacStream.ReadUInt32();
                pacStream.ReadUInt32(); //We don't need the version right now...
            }
            else
            { 
                //other
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
                    if (pacFile.header.id == "EPK8") 
                    { 
                        directory.nfiles = (int)pacStream.ReadUInt16() / 4; 
                    }
                    else 
                    { 
                        directory.nfiles = (int)pacStream.ReadUInt16() / 3; 
                    }
                    pacStream.ReadBytes(6);

                    directory.PacFiles = new PacFile[directory.nfiles];
                    for (int i = 0; i < directory.nfiles; i++)
                    {
                        directory.PacFiles[i] = ReadPacFile();
                        directory.PacFiles[i].offset = iOffset;

                        //Get Pach
                        var pos = pacStream.BaseStream.Position;
                        pacStream.BaseStream.Position = directory.PacFiles[i].offset;

                        directory.PacFiles[i].stream = pacStream.ReadBytes(directory.PacFiles[i].size);

                        directory.PacFiles[i].PACHContainer = ReadPACHContainer(directory.PacFiles[i].stream);

                        pacStream.BaseStream.Position = pos;



                        //Need to 2048 byte align these chunks
                        iOffset += directory.PacFiles[i].size + ((2048 - (directory.PacFiles[i].size % 2048)) % 2048);
                    }
                    dirList.Add(directory);
            }

            return dirList;
        }
        PacFile ReadPacFile()
        {
            var pacfile = new PacFile();

            if (pacFile.header.id == "EPK8") 
            { 
                pacfile.id = new string(pacStream.ReadChars(8)); 
            }
            else if (pacFile.header.id == "EPAC") 
            { 
                pacfile.id = new string(pacStream.ReadChars(4)); 
            }
            else 
            {
                //PACH??
            } 
            
            pacStream.ReadBytes(3);
            pacfile.size = (int)pacStream.ReadUInt32();
            pacStream.ReadByte();
            return pacfile;
        }

        PACH ReadPACHContainer(byte[] file) 
        {

            using (MemoryStream pachStream = new MemoryStream(file)) 
            {
                using (BinaryReader pachReader = new BinaryReader(pachStream)) 
                {
                    var Container = new PACH();
                    Container.id = new string(pachReader.ReadChars(4));

                    if (Container.id != "PACH")
                    {
                        return Container; //Thanks a lot createmode.pac....
                    }

                    Container.nfiles = (int)pachReader.ReadUInt32();
                    Container.PACHFiles = new PACHFile[Container.nfiles];
                    for (var i = 0; i < Container.nfiles; i++)
                    {
                        Container.PACHFiles[i] = ReadSubSubFile(pachReader);
                    }
                    var pos = pachReader.BaseStream.Position; //offsets are based on this.
                    for (var j = 0; j < Container.nfiles; j++)
                    {
                        //var pos = pachReader.BaseStream.Position;
                        pachReader.BaseStream.Position = Container.PACHFiles[j].offset + pos;

                        Container.PACHFiles[j].stream = pachReader.ReadBytes(Container.PACHFiles[j].size);

                        byte[] header = new byte[4]; Buffer.BlockCopy(Container.PACHFiles[j].stream, 0, header, 0, 4);
                        var recursionCheck = System.Text.Encoding.UTF8.GetString(header);
                        if (recursionCheck == "PACH")
                        {
                            Container.PACHFiles[j].SubContainer = ReadPACHContainer(Container.PACHFiles[j].stream);
                        }

                        //pachReader.BaseStream.Position = pos;
                    }

                    return Container;
                }
            }

           
        }
        PACHFile ReadSubSubFile(BinaryReader pachStream) 
        {
            var pachfile = new PACHFile();
            pachfile.id = pachStream.ReadInt32().ToString();
            pachfile.offset = (int)pachStream.ReadUInt32();
            pachfile.size = (int)pachStream.ReadUInt32();
            return pachfile;
        }

        
    }
}
