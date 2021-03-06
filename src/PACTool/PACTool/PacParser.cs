﻿using System;
using System.Collections.Generic;
using System.Text;

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
        private byte[] extraGarbage;

        private TextureParser texParser = null;
        private PACHParser pachParser = null;
        private PacDirParse dirParser = null;
        private uint pacVersion = 0;

        public PacFileHandling(BinaryReader b)
        {
            pacFile = new Pac();
            pacStream = b;
            pacFile.header = ReadHeader();

            PacDirParse dirParse = new PacDirParse(pacStream, pacFile);
            dirParser = dirParse;
            PACHParser pachParse = new PACHParser();
            pachParser = pachParse;
            TextureParser textureParse = new TextureParser();
            texParser = textureParse;

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
                pacVersion = pacStream.ReadUInt32();
            }
            else if (header.id == "PACH"){}
            else
            { 
                //other
                //Texture??
                List<byte> bytes = new List<byte>();
                bytes.AddRange(Encoding.ASCII.GetBytes(header.id));
                bytes.AddRange(pacStream.ReadBytes(28));
                extraGarbage = bytes.ToArray();
                header.id = new string(pacStream.ReadChars(4));
            }
            return header;
        }

        internal void Write(BinaryWriter writer)
        {
            if ( pacFile.header.id == "EPK8" || pacFile.header.id == "EPAC" )
            {
                dirParser.WriteDir(writer, pacFile.dir);
                var endPos = writer.BaseStream.Position;
                writer.BaseStream.Position = 8; //header.dataSize
                int newDataSize = 0;
                foreach (var dir in pacFile.dir)
                {
                    foreach (var file in dir.PacFiles)
                    {
                        newDataSize += file.size;
                    }
                }
                writer.Write((Int32)(newDataSize));
                writer.BaseStream.Position = endPos;

                return;
            }
            if ( pacFile.header.id == "PACH" )
            {
                writer.Write(Encoding.ASCII.GetBytes(pacFile.header.id));
                writer.BaseStream.Position = 0;
                pachParser.WritePACHContainer(writer);

                return;
            }

            //Texture
            writer.Write(extraGarbage);
            writer.Write(Encoding.ASCII.GetBytes(pacFile.header.id));

            TextureParser textureParse = new TextureParser();

            texParser.WriteTextures(writer);

        }
    }
}
