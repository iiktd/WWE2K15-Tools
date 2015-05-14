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

namespace PACTool
{
    public partial class Form1 : Form
    {

        public string[] args = Environment.GetCommandLineArgs();

        public Form1()
        {
            InitializeComponent();
            using (BinaryReader pacStream = new BinaryReader(File.Open(args[1], FileMode.Open))) 
            {
                PopulateTreeView(pacStream);
            }
            this.Text = Path.GetFileName(args[1]);
            this.treeView1.NodeMouseClick += new TreeNodeMouseClickEventHandler(this.treeView1_NodeMouseClick);
        }

        private void Form1_Load(object sender, EventArgs e) {  }


        //TreeView stuff
        private void PopulateTreeView(BinaryReader stream)
        {
            //read the file
            var openPacFile = new PacFileHandling(stream);

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

                GetPACFileDirectories(subDir.PacFiles, aNode);

                nodeToAddTo.Nodes.Add(aNode);
            }
        }

        private void GetPACFileDirectories(PacFile[] subDirs, TreeNode nodeToAddTo) 
        {
            TreeNode aNode;
            foreach (PacFile pacFile in subDirs)
            {
                if (pacFile.PACHContainer.id == "PACH") 
                {
                    aNode = new TreeNode(pacFile.id, 0, 0);
                    aNode.Tag = pacFile;
                    aNode.ImageKey = "Container";

                    if (pacFile.PACHContainer != null) 
                    {
                        GetPACHFileDirectories(pacFile.PACHContainer.PACHFiles, aNode);
                    }
                    
                    nodeToAddTo.Nodes.Add(aNode);
                }
            }
        }
        private void GetPACHFileDirectories(PACHFile[] subDirs, TreeNode nodeToAddTo)
        {
            TreeNode aNode;
            foreach (PACHFile subDir in subDirs) 
            {
                if (subDir.SubContainer != null) 
                {
                    aNode = new TreeNode(subDir.id, 0, 0);
                    aNode.Tag = subDir;
                    aNode.ImageKey = "Container";
                    
                    GetPACHFileDirectories(subDir.SubContainer.PACHFiles, aNode);

                    nodeToAddTo.Nodes.Add(aNode);
                }
            }  
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

                    if (nodeDirInfo.PacFiles != null) 
                    {
                        foreach (var file in nodeDirInfo.PacFiles)
                        {
                            item = new ListViewItem(file.id, 2);
                            item.Tag = file;
                            subItems = new ListViewItem.ListViewSubItem[]
                            {
                                 new ListViewItem.ListViewSubItem(item, file.PACHContainer.id.ToString()) //Header of the contained file
                                ,new ListViewItem.ListViewSubItem(item, file.size.ToString())
                            };
                            item.SubItems.AddRange(subItems);
                            listView1.Items.Add(item);
                        }
                    }
                }
                if (newSelected.Tag.GetType().Equals(typeof(PACHFile)))
                {
                    PACHFile nodeDirInfo = (PACHFile)newSelected.Tag;

                    if (nodeDirInfo.SubContainer != null) 
                    {
                        foreach (PACHFile subFile in nodeDirInfo.SubContainer.PACHFiles)
                        {
                            var subFileText = new byte[4];
                            Buffer.BlockCopy(subFile.stream, 0, subFileText, 0, 4);
                            item = new ListViewItem(subFile.id, 1);
                            item.Tag = subFile;
                            subItems = new ListViewItem.ListViewSubItem[]
                            {
                                 new ListViewItem.ListViewSubItem(item, System.Text.Encoding.UTF8.GetString(subFileText)) //Type
                                ,new ListViewItem.ListViewSubItem(item, subFile.size.ToString())
                            };
                            item.SubItems.AddRange(subItems);
                            listView1.Items.Add(item);
                        }
                    }
                }
                if (newSelected.Tag.GetType().Equals(typeof(PacFile)))
                {
                    PacFile nodeDirInfo = (PacFile)newSelected.Tag;

                    if (nodeDirInfo.PACHContainer.PACHFiles != null) 
                    {
                        foreach (var file in nodeDirInfo.PACHContainer.PACHFiles)
                        {
                            var subFileText = new byte[4];
                            Buffer.BlockCopy(file.stream, 0, subFileText, 0, 4);
                            item = new ListViewItem(file.id, 1);
                            item.Tag = file;
                            subItems = new ListViewItem.ListViewSubItem[]
                            {
                                 new ListViewItem.ListViewSubItem(item, System.Text.Encoding.UTF8.GetString(subFileText)) //Type
                                ,new ListViewItem.ListViewSubItem(item, file.size.ToString())
                            };
                            item.SubItems.AddRange(subItems);
                            listView1.Items.Add(item);
                        }
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
                        efile.DecompressBPE(file.stream, filename);
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
                    byte[] header = new byte[4]; Buffer.BlockCopy(file.PACHFiles[0].stream, 0, header, 0, 4);

                    if (System.Text.Encoding.UTF8.GetString(header) == "BPE ") 
                    {
                        efile.DecompressBPE(file.PACHFiles[0].stream, filename);
                    }
                    else 
                    {
                        //not compressed
                        efile.ExtractFile(file.PACHFiles[0].stream, filename);
                    }
                }
            }
        }
    }
}
