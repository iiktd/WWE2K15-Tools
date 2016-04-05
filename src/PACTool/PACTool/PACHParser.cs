using System;
using System.Collections.Generic;
using System.Text;

using System.IO;


namespace PACTool
{
    class PACHParser
    {

        FileIO decompressor = new FileIO();
        TextureParser[] textureParse = null;
        PACHParser[] pachParse = null;
        PACH Container = null;

        public PACH ReadPACHContainer(byte[] file)
        {
            using (MemoryStream pachStream = new MemoryStream(file))
            {
                using (BinaryReader pachReader = new BinaryReader(pachStream))
                {
                    Container = new PACH();
                    Container.id = new string(pachReader.ReadChars(4));

                    if (Container.id != "PACH")
                    {
                        return Container; //Thanks a lot createmode.pac....
                    }

                    Container.nfiles = (int)pachReader.ReadUInt32();
                    pachParse = new PACHParser[Container.nfiles];

                    //Read the file meta data
                    Container.PACHFiles = new PACHFile[Container.nfiles];
                    for (var i = 0; i < Container.nfiles; i++)
                    {
                        Container.PACHFiles[i] = ReadPACHheader(pachReader);
                    }

                    textureParse = new TextureParser[Container.nfiles];

                    bool isPach = false;

                    //Start of data. Meta data offsets start here
                    var pos = pachReader.BaseStream.Position; //offsets are based on this.
                    for (var j = 0; j < Container.nfiles; j++)
                    {
                        pachReader.BaseStream.Position = Container.PACHFiles[j].offset + pos;

                        Container.PACHFiles[j].stream = pachReader.ReadBytes(Container.PACHFiles[j].size);

                        byte[] headerByteArray = new byte[4]; Array.Copy(Container.PACHFiles[j].stream, 0, headerByteArray, 0, 4);
                        var header = System.Text.Encoding.UTF8.GetString(headerByteArray);
                        if (header == "PACH")
                        {
                            isPach = true;
                            pachParse[j] = new PACHParser();
                            Container.PACHFiles[j].SubContainer = pachParse[j].ReadPACHContainer(Container.PACHFiles[j].stream);
                        }
                        //Let's go ahead and decompress here.
                        if (header == "ZLIB" || header == "BPE ") 
                        {
                            Container.PACHFiles[j].compression = header;
                            Container.PACHFiles[j].stream = decompressor.Decompress(Container.PACHFiles[j].stream, header);
                            Container.PACHFiles[j].size = Container.PACHFiles[j].stream.Length;

                            //Recheck it.
                            headerByteArray = new byte[4]; 
                            Array.Copy(Container.PACHFiles[j].stream, 0, headerByteArray, 0, 4);
                            header = System.Text.Encoding.UTF8.GetString(headerByteArray);
                            if (header == "PACH")
                            {
                                Container.PACHFiles[j].SubContainer = ReadPACHContainer(Container.PACHFiles[j].stream);
                            }
                            else
                            {
                                //Check for textures, the first dds extension starts at 32.
                                if (Container.PACHFiles[j].stream.Length >= 36) 
                                {
                                    headerByteArray = new byte[4];
                                    Array.Copy(Container.PACHFiles[j].stream, 32, headerByteArray, 0, 4);
                                    header = System.Text.Encoding.UTF8.GetString(headerByteArray);
                                    if (header == "dds\0")
                                    {
                                        using (MemoryStream texStream = new MemoryStream(Container.PACHFiles[j].stream))
                                        {
                                            using (BinaryReader texReader = new BinaryReader(texStream))
                                            {
                                                textureParse[j] = new TextureParser();
                                                Container.PACHFiles[j].TexContainer = textureParse[j].ReadTextures(texReader);
                                            }
                                        }
                                    }
                                }
                                
                            }
                        }
                        /*else
                        {
                            //Check for textures, the first dds extension starts at 32.
                            if ((!isPach) && (Container.PACHFiles[j] != null) && (Container.PACHFiles[j].stream.Length >= 36))
                            {
                                headerByteArray = new byte[4];
                                Array.Copy(Container.PACHFiles[j].stream, 32, headerByteArray, 0, 4);
                                header = System.Text.Encoding.UTF8.GetString(headerByteArray);
                                if (header == "dds\0")
                                {
                                    using (MemoryStream texStream = new MemoryStream(Container.PACHFiles[j].stream))
                                    {
                                        using (BinaryReader texReader = new BinaryReader(texStream))
                                        {
                                            textureParse[j] = new TextureParser();
                                            Container.PACHFiles[j].TexContainer = textureParse[j].ReadTextures(texReader);
                                        }
                                    }
                                }
                            }
                        }*/
                    }

                    return Container;
                }
            }
        }
        PACHFile ReadPACHheader(BinaryReader pachStream)
        {
            var pachfile = new PACHFile();
            pachfile.id = pachStream.ReadInt32().ToString();
            pachfile.offset = (int)pachStream.ReadUInt32();
            pachfile.size = (int)pachStream.ReadUInt32();
            return pachfile;
        }

        internal void WritePACHContainer(BinaryWriter writer, PACH writeContainer = null)
        {
            if ( writeContainer == null )
            {
                writeContainer = Container;
            }
            writer.Write(Encoding.ASCII.GetBytes(writeContainer.id));

            writer.Write((Int32)(writeContainer.nfiles));

            //files metadata
            var sizePosition = new long[writeContainer.nfiles]; //We have to save positions for size to update it if compression is used later
            for (var i = 0; i < writeContainer.nfiles; i++)
            {
                writer.Write(Int32.Parse(writeContainer.PACHFiles[i].id));
                writer.Write((Int32)writeContainer.PACHFiles[i].offset);
                sizePosition[i] = writer.BaseStream.Position;
                writer.Write((Int32)writeContainer.PACHFiles[i].size);
            }

            var pos = writer.BaseStream.Position; //offsets are based on this

            for (var j = 0; j < writeContainer.nfiles; j++)
            {
                var offPos = writer.BaseStream.Position;
                var new_offset = offPos - pos;

                byte[] headerByteArray = new byte[4]; Array.Copy(writeContainer.PACHFiles[j].stream, 0, headerByteArray, 0, 4);
                var header = System.Text.Encoding.UTF8.GetString(headerByteArray);
                if (header == "PACH")
                {
                    MemoryStream pach_stream = new MemoryStream();
                    BinaryWriter pach_writer = new BinaryWriter(pach_stream);
                    pachParse[j].WritePACHContainer( pach_writer, writeContainer.PACHFiles[j].SubContainer );
                    pach_writer.Close();
                    writeContainer.PACHFiles[j].stream = pach_stream.ToArray();
                }
                else
                {
                    MemoryStream texturearchive_stream = new MemoryStream();
                    BinaryWriter texturearchive_writer = new BinaryWriter(texturearchive_stream);
                    textureParse[j].WriteTextures(texturearchive_writer);
                    texturearchive_writer.Close();
                    writeContainer.PACHFiles[j].stream = texturearchive_stream.ToArray();
                }

                writer.BaseStream.Position = sizePosition[j] - 4;
                writer.Write((Int32)(new_offset));
                writer.Write((Int32)(writeContainer.PACHFiles[j].stream.Length));
                writer.BaseStream.Position = offPos;

                //writer.Write(writeContainer.PACHFiles[j].stream);

                if (writeContainer.PACHFiles[j].compression == "ZLIB" || writeContainer.PACHFiles[j].compression == "BPE ")
                {
                    byte[] compressedStream = decompressor.Compress(writeContainer.PACHFiles[j].stream, writeContainer.PACHFiles[j].compression);
                    var tmpPos = writer.BaseStream.Position;
                    writer.BaseStream.Position = sizePosition[j];
                    writer.Write(compressedStream.Length);
                    writer.BaseStream.Position = tmpPos;
                    writer.Write(compressedStream);
                }
                else
                {
                    writer.Write(writeContainer.PACHFiles[j].stream);
                }
            }
        }
    }
}
