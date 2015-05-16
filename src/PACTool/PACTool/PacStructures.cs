using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PACTool
{
    public class PacHeader
    {
        public string id;
        public int listSize;
        public int dataSize;
        //int version;
    }
    public class PacDir
    {
        public string id;
        public int nfiles;
        //byte[] unknown;//6 bytes
        public PacFile[] PacFiles; //nfiles divided by 4

    }
    public class PacFile
    {
        //FileIO
        public string id; //4 bytes epac, 8 bytes epk8
        //byte unknown1; //3 bytes
        public int size;
        //byte unknown2;

        //Useful Stuff
        public int offset;
        public byte[] stream;
        public PACH PACHContainer;
    }

    public class PACH
    {
        public string id; //4 bytes
        public int nfiles;
        public PACHFile[] PACHFiles;
    }

    public class PACHFile
    {
        public string id;
        public int offset; //relative to start of data
        public int size;

        //byte array for stream
        public byte[] stream;

        //sigh...
        public PACH SubContainer;
    }

    public class TextureArchive
    {
        public string alignedstring;
        public string extension;
        public int size;
        public int offset;

        public byte[] stream;
    }


}
