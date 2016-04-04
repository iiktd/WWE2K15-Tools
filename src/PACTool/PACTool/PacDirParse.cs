using System;
using System.Collections.Generic;
using System.Text;

using System.IO;

namespace PACTool
{
    class PacDirParse
    {
        BinaryReader pacStream { get; set; }
        Pac pacFile { get; set; }

        private PACHParser[] pachParsers;

        public PacDirParse(BinaryReader stream, Pac pac) 
        {
            pacStream = stream;
            pacFile = pac;
        }


        public List<PacDir> ReadDir()
        {
            var iOffset = 16384; //Start of data chunk
            var dirList = new List<PacDir>();
            var pachParse = new PACHParser();
            pacStream.BaseStream.Seek(2048, SeekOrigin.Begin);

            List<PACHParser> parsers = new List<PACHParser>();

            while (pacStream.BaseStream.Position < (2048 + pacFile.header.listSize))
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

                    directory.PacFiles[i].PACHContainer = pachParse.ReadPACHContainer(directory.PacFiles[i].stream);

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
    }
}
