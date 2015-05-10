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
            PopulateTreeView();
            this.Text = Path.GetFileName(args[1]);
            this.treeView1.NodeMouseClick += new TreeNodeMouseClickEventHandler(this.treeView1_NodeMouseClick);
        }

        private void Form1_Load(object sender, EventArgs e) {  }


        //TreeView stuff
        private void PopulateTreeView()
        {
            //This populates the tree/list and then closes the file. Keeping it in memory is for chumps.
            using (BinaryReader b = new BinaryReader(File.Open(args[1], FileMode.Open)))
            {
                //read the file
                var openPacFile = new PacFileHandling(b);

                //create the tree
                var rootNode = new TreeNode(openPacFile.pacFile.header.id.ToString());
                rootNode.Tag = null;

                //This populates sub directories under the root
                GetDirectories(openPacFile.pacFile.dir, rootNode);

                //Add the root to the tree
                treeView1.Nodes.Add(rootNode);
            }
        }

        private void GetDirectories(PacDir[] subDirs, TreeNode nodeToAddTo)
        {
            TreeNode aNode;
            foreach (PacDir subDir in subDirs)
            {
                aNode = new TreeNode(subDir.id, 0, 0);
                aNode.Tag = subDir;
                aNode.ImageKey = "folder";
                nodeToAddTo.Nodes.Add(aNode);
            }
        }

        //Mouse click stuff
        void treeView1_NodeMouseClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            TreeNode newSelected = e.Node;
            listView1.Items.Clear();
            PacDir nodeDirInfo = (PacDir)newSelected.Tag;
            ListViewItem.ListViewSubItem[] subItems;
            ListViewItem item = null;

            if (e.Node.Tag != null)
            {
                foreach (PacFile file in nodeDirInfo.file)
                {
                    item = new ListViewItem(file.id, 1);
                    subItems = new ListViewItem.ListViewSubItem[]
                    {
                        new ListViewItem.ListViewSubItem(item, "File")
                        ,new ListViewItem.ListViewSubItem(item, file.size.ToString())
                        ,new ListViewItem.ListViewSubItem(item, file.offset.ToString())
                    };
                    item.SubItems.AddRange(subItems);
                    listView1.Items.Add(item);
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
            //Open the file to extract
            using (BinaryReader b = new BinaryReader(File.Open(args[1], FileMode.Open)))
            {
                var openPacFile = new PacFileHandling(b);
                foreach (ListViewItem item in listView1.SelectedItems)
                {
                    openPacFile.ExtractFile(item);
                }

            }
        }
    }
}
