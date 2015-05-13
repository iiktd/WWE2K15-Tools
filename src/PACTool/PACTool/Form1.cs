using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using System.IO;

// this is needed to use DLLImport
using System.Runtime.InteropServices;

namespace PACTool
{
    public partial class Form1 : Form
    {

        public string[] args = Environment.GetCommandLineArgs();
        public BinaryReader pacStream;

        public Form1()
        {
            InitializeComponent();
            pacStream = new BinaryReader(File.Open(args[1], FileMode.Open));
            PopulateTreeView();
            this.Text = Path.GetFileName(args[1]);
            this.treeView1.NodeMouseClick += new TreeNodeMouseClickEventHandler(this.treeView1_NodeMouseClick);
        }

        private void Form1_Load(object sender, EventArgs e) {  }


        //TreeView stuff
        private void PopulateTreeView()
        {
            //read the file
                var openPacFile = new PacFileHandling(pacStream);

                //create the tree
                var rootNode = new TreeNode(openPacFile.pacFile.header.id.ToString());
                rootNode.Tag = null;

                //This populates sub directories under the root
                GetDirectories(openPacFile.pacFile.dir, rootNode);

                //Add the root to the tree
                treeView1.Nodes.Add(rootNode);
        }

        private void GetDirectories(PacDir[] subDirs, TreeNode nodeToAddTo)
        {
            TreeNode aNode;
            foreach (PacDir subDir in subDirs)
            {
                aNode = new TreeNode(subDir.id, 0, 0);
                aNode.Tag = subDir;
                aNode.ImageKey = "folder";

                GetPACFileDirectories(subDir.file, aNode);

                nodeToAddTo.Nodes.Add(aNode);
            }
        }

        private void GetPACFileDirectories(PacFile[] subDirs, TreeNode nodeToAddTo) 
        {
            TreeNode aNode;
            foreach (PacFile subDir in subDirs)
            {
                if (subDir.file.id == "PACH") 
                {
                    aNode = new TreeNode(subDir.id, 0, 0);
                    aNode.Tag = subDir;
                    aNode.ImageKey = "Container";

                    if (subDir.id == "PACH")
                    {
                        foreach (PACHFile subFile in subDir.file.file)
                        {
                            if (subFile.file.id == "PACH")
                            {
                                GetPACHFileDirectories(subFile, aNode);
                            }
                        }
                    }

                    nodeToAddTo.Nodes.Add(aNode);
                }
            }
        }
        private void GetPACHFileDirectories(PACHFile subDir, TreeNode nodeToAddTo)
        {
            TreeNode aNode;
            aNode = new TreeNode(subDir.id, 0, 0);
            aNode.Tag = subDir;
            aNode.ImageKey = "Container";
            nodeToAddTo.Nodes.Add(aNode);
        }



        //Mouse click stuff
        void treeView1_NodeMouseClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            if (e.Node.Tag != null)
            {
                TreeNode newSelected = e.Node;
                listView1.Items.Clear();
                ListViewItem item = null;
                ListViewItem.ListViewSubItem[] subItems;


                //There's probably a better way of doing this...
                if (newSelected.Tag.GetType().Equals(typeof(PacDir))) 
                {
                    PacDir nodeDirInfo = (PacDir)newSelected.Tag;
                    foreach (var file in nodeDirInfo.file)
                    {
                        item = new ListViewItem(file.id, 2);
                        item.Tag = file;
                        subItems = new ListViewItem.ListViewSubItem[]
                        {
                            new ListViewItem.ListViewSubItem(item, file.file.id.ToString()) //Type
                            ,new ListViewItem.ListViewSubItem(item, file.size.ToString())
                            ,new ListViewItem.ListViewSubItem(item, file.offset.ToString())
                        };
                        item.SubItems.AddRange(subItems);
                        listView1.Items.Add(item);
                    }
                }
                if (newSelected.Tag.GetType().Equals(typeof(PacFile)))
                {
                    PacFile nodeDirInfo = (PacFile)newSelected.Tag;

                    if (nodeDirInfo.file.file != null) 
                    {
                        foreach (PACHFile subFile in nodeDirInfo.file.file)
                        {
                            var subFileText = new byte[4];
                            Buffer.BlockCopy(subFile.stream, 0, subFileText, 0, 4);
                            item = new ListViewItem(subFile.id, 1);
                            item.Tag = subFile;
                            subItems = new ListViewItem.ListViewSubItem[]
                            {
                                 new ListViewItem.ListViewSubItem(item, System.Text.Encoding.UTF8.GetString(subFileText)) //Type
                                ,new ListViewItem.ListViewSubItem(item, subFile.size.ToString())
                                ,new ListViewItem.ListViewSubItem(item, subFile.offset.ToString())
                            };
                            item.SubItems.AddRange(subItems);
                            listView1.Items.Add(item);
                        }
                    }
                }

                if (newSelected.Tag.GetType().Equals(typeof(PACHFile))) 
                {
                    PACHFile nodeDirInfo = (PACHFile)newSelected.Tag;

                    foreach (PACHFile subFile in nodeDirInfo.file.file)
                    {
                        var subFileText = new byte[4];
                        Buffer.BlockCopy(subFile.stream, 0, subFileText, 0, 4);
                        item = new ListViewItem(subFile.id, 1);
                        item.Tag = subFile;
                        subItems = new ListViewItem.ListViewSubItem[]
                        {
                             new ListViewItem.ListViewSubItem(item, System.Text.Encoding.UTF8.GetString(subFileText)) //Type
                            ,new ListViewItem.ListViewSubItem(item, subFile.size.ToString())
                            ,new ListViewItem.ListViewSubItem(item, subFile.offset.ToString())
                        };
                        item.SubItems.AddRange(subItems);
                        listView1.Items.Add(item);
                    }

                }
            }
            listView1.AutoResizeColumns(ColumnHeaderAutoResizeStyle.HeaderSize);
        }

        private void listView1_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                if (listView1.FocusedItem.Bounds.Contains(e.Location) == true)
                {
                    contextMenuStrip1.Show(Cursor.Position);
                }
            }
        }

        private void contextMenuStrip1_Opening(object sender, CancelEventArgs e) 
        {
            // If there are no items selected, cancel viewing the context menu
            if (listView1.SelectedItems.Count <= 0)
            {
                e.Cancel = true;
            }
        }

        private void option1ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var efile = new FileIO();
            foreach (ListViewItem item in listView1.SelectedItems)
            {
                string filename = Path.GetDirectoryName(args[1]) + "\\" + item.SubItems[0].Text;


                if (item.Tag.GetType().Equals(typeof(PacFile)))
                {
                    PacFile file = (PacFile)item.Tag;
                    efile.ExtractFile(file.stream, filename);
                }
                if (item.Tag.GetType().Equals(typeof(PACHFile)))
                {
                    PACHFile file = (PACHFile)item.Tag;

                    //We're going sloppily do this here for now
                    byte[] header = new byte[4]; 
                    Buffer.BlockCopy(file.stream, 0, header, 0, 4);
                    var compressionCheck = System.Text.Encoding.UTF8.GetString(header);

                    if (compressionCheck == "BPE ")
                    {
                        byte[] align = new byte[4]; Buffer.BlockCopy(file.stream, 4, align, 0, 4); //used for byte alignment. But it's being fudged by the dll.
                        byte[] compressed = new byte[4]; Buffer.BlockCopy(file.stream, 8, compressed, 0, 4);
                        byte[] uncompressed = new byte[4]; Buffer.BlockCopy(file.stream, 12, uncompressed, 0, 4);
                        byte[] input = new byte[BitConverter.ToInt32(compressed, 0)]; Buffer.BlockCopy(file.stream, 16, input, 0, file.stream.Length - 16);
                        byte[] output = new byte[BitConverter.ToInt32(uncompressed, 0)];
                        int padding = BitConverter.ToInt32(uncompressed, 0) - BitConverter.ToInt32(compressed, 0);

                        if (yukes_bpe(input, BitConverter.ToInt32(compressed, 0), output, BitConverter.ToInt32(uncompressed, 0), padding) == BitConverter.ToInt32(uncompressed, 0))
                        {
                            efile.ExtractFile(output, filename);
                        }
                    }
                    else
                    {
                        //not compressed
                        efile.ExtractFile(file.stream, filename);
                    }

                }
                if (item.Tag.GetType().Equals(typeof(PACH)))
                {
                    PACH file = (PACH)item.Tag;

                    //We're going sloppily do this here for now
                    byte[] header = new byte[4]; Buffer.BlockCopy(file.file[0].stream, 0, header, 0, 4);

                    if (System.Text.Encoding.UTF8.GetString(header) == "BPE ") 
                    {
                        byte[] align = new byte[4]; Buffer.BlockCopy(file.file[0].stream, 4, align, 0, 4); //used for byte alignment. But it's being fudged by the dll.
                        byte[] compressed = new byte[4]; Buffer.BlockCopy(file.file[0].stream, 8, compressed, 0, 4);
                        byte[] uncompressed = new byte[4]; Buffer.BlockCopy(file.file[0].stream, 12, uncompressed, 0, 4);
                        byte[] input = new byte[BitConverter.ToInt32(compressed, 0)]; Buffer.BlockCopy(file.file[0].stream, 16, input, 0, file.file[0].stream.Length - 16);
                        byte[] output = new byte[BitConverter.ToInt32(uncompressed, 0)];
                        int padding = BitConverter.ToInt32(uncompressed, 0) - BitConverter.ToInt32(compressed, 0);

                        if (yukes_bpe(input, BitConverter.ToInt32(compressed, 0), output, BitConverter.ToInt32(uncompressed, 0), padding) == BitConverter.ToInt32(uncompressed, 0))
                        {
                            efile.ExtractFile(output, filename);
                        }
                    }
                    else 
                    {
                        //not compressed
                        efile.ExtractFile(file.file[0].stream, filename);
                    }
                   

                }
            }
        }

        // this is where we load the functions from our unmanaged dll
        [DllImport("YukesBPE.dll")]
        public static extern int yukes_bpe(byte[] input, int insz, byte[] ouput, int outsz, int fill_outsz);
    }
}
