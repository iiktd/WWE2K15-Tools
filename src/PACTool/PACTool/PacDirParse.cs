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
        private PACHParser[][] pachParsers = null;

        byte[] headerMisc = null;

        public PacDirParse(BinaryReader stream, Pac pac) 
        {
            pacStream = stream;
            pacFile = pac;
        }


        public List<PacDir> ReadDir()
        {
            var iOffset = 16384; //Start of data chunk
            var dirList = new List<PacDir>();

            pacStream.BaseStream.Position = 0;
            headerMisc = pacStream.ReadBytes(2048);

            pacStream.BaseStream.Seek(2048, SeekOrigin.Begin);

            List<PACHParser[]> parsers = new List<PACHParser[]>();

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
                directory.extraGarbage = pacStream.ReadBytes(6);
                var parserElement = new PACHParser[directory.nfiles];

                directory.PacFiles = new PacFile[directory.nfiles];
                for (int i = 0; i < directory.nfiles; i++)
                {
                    directory.PacFiles[i] = ReadPacFile();
                    directory.PacFiles[i].offset = iOffset;

                    //Get Pach
                    var pos = pacStream.BaseStream.Position;
                    pacStream.BaseStream.Position = directory.PacFiles[i].offset;

                    directory.PacFiles[i].stream = pacStream.ReadBytes(directory.PacFiles[i].size);

                    parserElement[i] = new PACHParser();
                    directory.PacFiles[i].PACHContainer = parserElement[i].ReadPACHContainer(directory.PacFiles[i].stream);

                    pacStream.BaseStream.Position = pos;

                    //Need to 2048 byte align these chunks
                    iOffset += directory.PacFiles[i].size + ((2048 - (directory.PacFiles[i].size % 2048)) % 2048);
                }
                parsers.Add(parserElement);
                dirList.Add(directory);
            }

            pachParsers = parsers.ToArray();
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

            pacfile.unknown1 = pacStream.ReadBytes(3);
            pacfile.size = (int)pacStream.ReadUInt32();
            pacfile.unknown2 = pacStream.ReadByte();
            return pacfile;
        }

        internal void WriteDir(BinaryWriter writer, PacDir[] pacDir)
        {
            var iOffset = 16384; //Start of data chunk
            writer.Write(headerMisc);
            writer.BaseStream.Position = 2048;

            for (int i = 0; i < pacDir.Length; i++)
            {
                writer.Write(Encoding.ASCII.GetBytes(pacDir[i].id));
                if (pacFile.header.id == "EPK8")
                {
                    writer.Write((Int16)(pacDir[i].nfiles * 4));
                }
                else
                {
                    writer.Write((Int16)(pacDir[i].nfiles * 3));
                }
                writer.Write(pacDir[i].extraGarbage);
                var writerPos = writer.BaseStream.Position;
                for (int j = 0; j < pacDir[i].nfiles; j++)
                {
                    if (pacDir[i].PacFiles[j].PACHContainer.nfiles > 0)
                    {
                        MemoryStream stream = new MemoryStream();
                        BinaryWriter streamWriter = new BinaryWriter(stream);
                        pachParsers[i][j].WritePACHContainer(streamWriter);
                        streamWriter.Close();
                        pacDir[i].PacFiles[j].stream = stream.ToArray();
                        pacDir[i].PacFiles[j].size = pacDir[i].PacFiles[j].stream.Length;
                    }

                    if (pacFile.header.id == "EPK8" || pacFile.header.id == "EPAC")
                    {
                        writer.Write(Encoding.ASCII.GetBytes(pacDir[i].PacFiles[j].id));
                    }
                    else
                    {
                        //PACH??
                    }
                    writer.Write(pacDir[i].PacFiles[j].unknown1);
                    writer.Write((Int32)(pacDir[i].PacFiles[j].size));
                    writer.Write(pacDir[i].PacFiles[j].unknown2);
                    var fileListPos = writer.BaseStream.Position;

                    var next_pos = iOffset + pacDir[i].PacFiles[j].size + ((2048 - (pacDir[i].PacFiles[j].size % 2048)) % 2048);
                    writer.BaseStream.Position = iOffset;
                    writer.Write(pacDir[i].PacFiles[j].stream);
                    var pos = writer.BaseStream.Position;
                    for (int k = 0; k < (next_pos - pos); k++ )
                    {
                        writer.Write((byte)(0));
                    }
                    iOffset = next_pos;

                    writer.BaseStream.Position = fileListPos;
                }
                writer.BaseStream.Position = writerPos;
            }
        }
    }
}
