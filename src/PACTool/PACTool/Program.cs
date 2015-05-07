using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

using System.IO;

namespace PACTool
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {

            args = new[] { "ch100.pac" }; //temporary

            using (BinaryReader b = new BinaryReader(File.Open(args[0], FileMode.Open)))
            {

                //read the file
                var openPacFile = new PacFileHandling(b);

                //show the file
                //Application.EnableVisualStyles();
                //Application.SetCompatibleTextRenderingDefault(false);
                //Application.Run(new Form1());

            }
        }
    }
}
