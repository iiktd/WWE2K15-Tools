using System;
using System.Collections.Generic;

using System.IO;

namespace PACTool
{
    class TextureParser
    {
        public TextureArchive[] ReadTextures(BinaryReader pacStream) 
        {
            int nfiles = pacStream.ReadInt32();
            pacStream.ReadBytes(12); //The rest of the header. Not important...

            var textures = new TextureArchive[nfiles];
            for (var i = 0; i < nfiles; i++)
            {
                var texture = new TextureArchive();
                texture.alignedstring = new string(pacStream.ReadChars(16));

                texture.extension = new string(pacStream.ReadChars(4));
                texture.size = pacStream.ReadInt32();
                texture.offset = pacStream.ReadInt32();
                pacStream.ReadBytes(4);

                var pos = pacStream.BaseStream.Position;
                pacStream.BaseStream.Position = texture.offset;
                texture.stream = pacStream.ReadBytes(texture.size);
                pacStream.BaseStream.Position = pos;
                textures[i] = texture;
            }

            return textures;
        }



    }
}
