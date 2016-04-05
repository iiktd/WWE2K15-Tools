using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PACTool
{
    public class PacHeader
    {
        public string id {get; set;}
        public int listSize { get; set; }
        public int dataSize { get; set; }
        //int version;
    }
    public class PacDir
    {
        public string id { get; set; }
        public int nfiles { get; set; }
        //byte[] unknown;//6 bytes
        public byte[] extraGarbage { get; set; }

        public PacFile[] PacFiles { get; set; } //nfiles divided by 4

    }
    public class PacFile
    {
        //FileIO
        public string id { get; set; } //4 bytes epac, 8 bytes epk8
        //byte unknown1; //3 bytes
        public byte[] unknown1 { get; set; }
        public int size { get; set; }
        //byte unknown2;
        public byte unknown2 { get; set; }

        //Useful Stuff
        public int offset { get; set; }
        public byte[] stream { get; set; }
        public PACH PACHContainer { get; set; }
    }

    public class PACH
    {
        public string id { get; set; } //4 bytes
        public int nfiles { get; set; }
        public PACHFile[] PACHFiles { get; set; }
    }

    public class PACHFile
    {
        public string id { get; set; }
        public int offset { get; set; } //relative to start of data
        public int size { get; set; }

        public string compression; //used for writing

        //byte array for stream
        public byte[] stream { get; set; }

        //sigh...
        public PACH SubContainer { get; set; }
        public TextureArchive[] TexContainer { get; set; }
    }

    public class TextureArchive
    {
        public string alignedstring { get; set; }
        public string extension { get; set; }
        public int size { get; set; }
        public int offset { get; set; }

        public byte[] stream { get; set; }
    }


}
