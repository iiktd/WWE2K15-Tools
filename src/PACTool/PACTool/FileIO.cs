using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;

// this is needed to use DLLImport
using System.Runtime.InteropServices;

namespace PACTool
{
    class FileIO
    {
        public void ExtractFile(byte[] newFile, string filename)
        {
            using (BinaryWriter b = new BinaryWriter(File.Open(filename, FileMode.Create)))
            {
                foreach (var i in newFile) { b.Write(i); }
            }
        }
    }


}
