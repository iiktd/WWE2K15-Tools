using System;
using System.Collections.Generic;
using System.Text;

using System.IO;


namespace PACTool
{
    class PACHParser
    {

        FileIO decompressor = new FileIO();
        TextureParser textureParse = new TextureParser();

        public PACH ReadPACHContainer(byte[] file)
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


                    //Read the file meta data
                    Container.PACHFiles = new PACHFile[Container.nfiles];
                    for (var i = 0; i < Container.nfiles; i++)
                    {
                        Container.PACHFiles[i] = ReadPACHheader(pachReader);
                    }


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
                            Container.PACHFiles[j].SubContainer = ReadPACHContainer(Container.PACHFiles[j].stream);
                        }
                        //Let's go ahead and decompress here.
                        if (header == "ZLIB" || header == "BPE ") 
                        {
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
                                if (Container.PACHFiles[j].stream.Length >= 32) 
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
                                                Container.PACHFiles[j].TexContainer = textureParse.ReadTextures(texReader);
                                            }
                                        }
                                    }
                                }
                                
                            }
                        }
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
    }
}
