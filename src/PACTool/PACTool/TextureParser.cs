using System;
using System.Collections.Generic;

using System.IO;

using System.Text;

namespace PACTool
{
    class TextureParser
    {
        private byte[] extraGarbage;
        private int nfiles;
        private TextureArchive[] ownTextures;
        private byte[,] textureGarbage;

        public TextureArchive[] ReadTextures(BinaryReader pacStream) 
        {
            nfiles = pacStream.ReadInt32();
            extraGarbage = pacStream.ReadBytes(12); //The rest of the header. Not important...

            var textures = new TextureArchive[nfiles];
            ownTextures = textures;
            textureGarbage = new byte[nfiles,4];
            for (var i = 0; i < nfiles; i++)
            {
                var texture = new TextureArchive();
                texture.alignedstring = new string(pacStream.ReadChars(16));

                texture.extension = new string(pacStream.ReadChars(4));
                texture.size = pacStream.ReadInt32();
                texture.offset = pacStream.ReadInt32();
                textureGarbage[i, 0] = pacStream.ReadByte();
                textureGarbage[i, 1] = pacStream.ReadByte();
                textureGarbage[i, 2] = pacStream.ReadByte();
                textureGarbage[i, 3] = pacStream.ReadByte();

                var pos = pacStream.BaseStream.Position;
                pacStream.BaseStream.Position = texture.offset;
                texture.stream = pacStream.ReadBytes(texture.size);
                pacStream.BaseStream.Position = pos;
                textures[i] = texture;
            }

            return textures;
        }




        internal void WriteTextures(BinaryWriter writer)
        {
            writer.Seek(0, SeekOrigin.Begin);
            writer.Write((Int32)(nfiles));
            writer.Write(extraGarbage);
            for (var i = 0; i < nfiles; i++)
            {
                writer.Write(Encoding.ASCII.GetBytes(ownTextures[i].alignedstring));
                writer.Write(Encoding.ASCII.GetBytes(ownTextures[i].extension));
                writer.Write((Int32)(ownTextures[i].size));
                writer.Write((Int32)(ownTextures[i].offset));
                writer.Write(textureGarbage[i,0]);
                writer.Write(textureGarbage[i,1]);
                writer.Write(textureGarbage[i,2]);
                writer.Write(textureGarbage[i,3]);

                var pos = writer.BaseStream.Position;
                writer.Seek(ownTextures[i].offset, SeekOrigin.Begin);
                writer.Write(ownTextures[i].stream);
                writer.BaseStream.Position = pos;
            }
        }
    }
}
