using System;
using System.Collections.Generic;

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

            PacDirParse dirParse = new PacDirParse(pacStream, pacFile);
            PACHParser pachParse = new PACHParser();
            TextureParser textureParse = new TextureParser();

            if (pacFile.header.id == "PACH")
            {
                //You opened a free floating PACH file~!
                pacStream.BaseStream.Position = 0;
                pacFile.container = pachParse.ReadPACHContainer(pacStream.ReadBytes((int)pacStream.BaseStream.Length));
            }
            else if (pacFile.header.id == "EPK8" || pacFile.header.id == "EPAC")  
            {
                pacFile.dir = dirParse.ReadDir().ToArray(); 
            }
            else 
            { 
                //probably a texture archive...
                //pacFile.header.id = "Texture Archive";
                pacStream.BaseStream.Position = 0;
                pacFile.textures = textureParse.ReadTextures(pacStream);
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
            else if (header.id == "PACH"){}
            else
            { 
                //other
                //Texture??
                pacStream.ReadBytes(28);
                header.id = new string(pacStream.ReadChars(4));
            }
            return header;
        }
    }
}
