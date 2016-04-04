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

using S16.Drawing;

namespace PACTool
{
    public partial class Form1 : Form
    {

        public string[] args = Environment.GetCommandLineArgs();
        public string file;
        public string filename;
        public PacFileHandling openPacFile;
        public FileIO efile = new FileIO();

        public Form1()
        {
            InitializeComponent();
            if (args.Length > 1)
            {
                file = args[1];
                filename = Path.GetFileName(file);
                using (BinaryReader pacStream = new BinaryReader(File.Open(filename, FileMode.Open)))
                {
                    PopulateTreeView(pacStream);
                }
            }
            this.treeView1.NodeMouseClick += new TreeNodeMouseClickEventHandler(this.treeView1_NodeMouseClick);
            treeView1.NodeMouseClick += (sender, e) => treeView1.SelectedNode = e.Node;
        }

        private void Form1_Load(object sender, EventArgs e) {  }

        private PacFileHandling currentlyOpenFile = null;

        //TreeView stuff
        private void PopulateTreeView(BinaryReader stream)
        {
            //create the tree
            var rootNode = new TreeNode(filename);

            //read the file
            openPacFile = new PacFileHandling(stream);
            currentlyOpenFile = openPacFile;

            //What kind of file are we opening? Populate subdirecteries based on that
            if (openPacFile.pacFile.header.id == "EPK8" || openPacFile.pacFile.header.id == "EPAC") 
            {
                GetDirectories(openPacFile.pacFile.dir, rootNode);
            }
            else if (openPacFile.pacFile.header.id == "PACH")
            {
                rootNode.Tag = openPacFile.pacFile.container; //Setting the root tag causes the list view to populate
                GetPACHFileDirectories(openPacFile.pacFile.container.PACHFiles, rootNode);
            }
            else if (openPacFile.pacFile.header.id == "dds\0")
            {
                rootNode.Tag = openPacFile.pacFile.textures; //Setting the root tag causes the list view to populate
            }
            //Add the root to the tree
            treeView1.Nodes.Add(rootNode);
        }

        private void RewriteFile( string filename )
        {
            if ( currentlyOpenFile == null )
            {
                return;
            }

            FileStream stream = File.Open( filename, FileMode.Create);
            BinaryWriter writer = new BinaryWriter( stream );
            currentlyOpenFile.Write( writer );
            stream.Close();
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
                if (subDir.SubContainer != null) //More pachs...
                {
                    aNode = new TreeNode(subDir.id, 0, 0);
                    aNode.Tag = subDir;
                    aNode.ImageKey = "Container";
                    GetPACHFileDirectories(subDir.SubContainer.PACHFiles, aNode);
                    nodeToAddTo.Nodes.Add(aNode);
                }
                else if (subDir.TexContainer != null) //Texture archive
                {
                    aNode = new TreeNode(subDir.id, 0, 0);
                    aNode.Tag = subDir;
                    aNode.ImageKey = "Container";
                    nodeToAddTo.Nodes.Add(aNode);
                }
            }  
        }


        //Check the node type and populate the listview based on it.
        void treeView1_NodeMouseClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            TreeNode newSelected = e.Node;
            listView1.Items.Clear();
            ListViewItem item = null;
            ListViewItem.ListViewSubItem[] subItems;


            //right click = extract all menu.
            if (e.Button == MouseButtons.Right)
            {
                if (newSelected.Bounds.Contains(e.Location) == true) 
                {
                    contextMenuStrip2.Show(Cursor.Position);
                }
            }

            if (newSelected.Tag != null)
            {

                if (newSelected.Tag.GetType().Equals(typeof(TextureArchive[]))) 
                { 
                    //TextureArchive textures
                    TextureArchive[] textures = (TextureArchive[])newSelected.Tag;
                    foreach (var texture in textures) 
                    {

                        var file = texture.alignedstring.Replace("\0", string.Empty).Trim() + "." + texture.extension.Replace("\0", string.Empty).Trim();

                        item = new ListViewItem(file, 2);
                        item.Tag = texture;
                        subItems = new ListViewItem.ListViewSubItem[]
                        {
                            new ListViewItem.ListViewSubItem(item, texture.extension)
                           ,new ListViewItem.ListViewSubItem(item, texture.size.ToString())
                        };
                        item.SubItems.AddRange(subItems);
                        listView1.Items.Add(item);
                    }
                }
                else if (newSelected.Tag.GetType().Equals(typeof(PacDir))) 
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
                else if (newSelected.Tag.GetType().Equals(typeof(PACH))) 
                {
                    PACH nodeDirInfo = (PACH)newSelected.Tag;

                    if (nodeDirInfo.PACHFiles != null)
                    {
                        foreach (var file in nodeDirInfo.PACHFiles)
                        {
                            var subFileText = new byte[4];
                            Array.Copy(file.stream, 0, subFileText, 0, 4);
                            var name = System.Text.Encoding.UTF8.GetString(subFileText);

                            item = new ListViewItem(file.id, 1);
                            item.Tag = file;
                            subItems = new ListViewItem.ListViewSubItem[]
                            {
                                new ListViewItem.ListViewSubItem(item, name) //Type
                               ,new ListViewItem.ListViewSubItem(item, file.size.ToString())
                            };
                            item.SubItems.AddRange(subItems);
                            listView1.Items.Add(item);
                        }
                    }
                
                }
                else if (newSelected.Tag.GetType().Equals(typeof(PACHFile)))
                {
                    PACHFile nodeDirInfo = (PACHFile)newSelected.Tag;
                    if (nodeDirInfo.SubContainer != null) //more pach files...
                    {
                        foreach (PACHFile subFile in nodeDirInfo.SubContainer.PACHFiles)
                        {
                            var subFileText = new byte[4];
                            Array.Copy(subFile.stream, 0, subFileText, 0, 4);
                            var name = System.Text.Encoding.UTF8.GetString(subFileText);

                            item = new ListViewItem(subFile.id, 1);
                            item.Tag = subFile;
                            subItems = new ListViewItem.ListViewSubItem[]
                            {
                                 new ListViewItem.ListViewSubItem(item, name) //Type
                                ,new ListViewItem.ListViewSubItem(item, subFile.size.ToString())
                            };
                            item.SubItems.AddRange(subItems);
                            listView1.Items.Add(item);
                        }
                    }
                    else if (nodeDirInfo.TexContainer != null) // Texture files
                    {
                        foreach (TextureArchive subFile in nodeDirInfo.TexContainer)
                        {
                            var file = subFile.alignedstring.Replace("\0", string.Empty).Trim() + "." + subFile.extension.Replace("\0", string.Empty).Trim();

                            item = new ListViewItem(file, 2);
                            item.Tag = subFile;
                            subItems = new ListViewItem.ListViewSubItem[]
                            {
                                 new ListViewItem.ListViewSubItem(item, subFile.extension) //Header of the contained file
                                ,new ListViewItem.ListViewSubItem(item, subFile.size.ToString())
                            };
                            item.SubItems.AddRange(subItems);
                            listView1.Items.Add(item);
                        }
                    }
                    else //Everything else
                    {
                        var subFileText = new byte[4];
                        Array.Copy(nodeDirInfo.stream, 0, subFileText, 0, 4);
                        var name = System.Text.Encoding.UTF8.GetString(subFileText);

                        item = new ListViewItem(nodeDirInfo.id, 1);
                        item.Tag = nodeDirInfo;
                        subItems = new ListViewItem.ListViewSubItem[]
                        {
                            new ListViewItem.ListViewSubItem(item, name) //Type
                           ,new ListViewItem.ListViewSubItem(item, nodeDirInfo.size.ToString())
                        };
                        item.SubItems.AddRange(subItems);
                        listView1.Items.Add(item);
                    }
                }
                else if (newSelected.Tag.GetType().Equals(typeof(PacFile)))
                {
                    PacFile nodeDirInfo = (PacFile)newSelected.Tag;

                    if (nodeDirInfo.PACHContainer.PACHFiles != null) 
                    {
                        foreach (var file in nodeDirInfo.PACHContainer.PACHFiles)
                        {
                            var subFileText = new byte[4];
                            Array.Copy(file.stream, 0, subFileText, 0, 4);
                            var name = System.Text.Encoding.UTF8.GetString(subFileText);

                            item = new ListViewItem(file.id, 1);
                            item.Tag = file;
                            subItems = new ListViewItem.ListViewSubItem[]
                            {
                                new ListViewItem.ListViewSubItem(item, name) //Type
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
            if (listView1.SelectedItems.Count <= 0) { e.Cancel = true; }
        }
        private void contextMenuStrip2_Opening(object sender, CancelEventArgs e)
        {
            // If there are no items selected, cancel viewing the context menu
            if (treeView1.SelectedNode == null) { e.Cancel = true; }
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog1 = new OpenFileDialog();
            openFileDialog1.Filter = "Pac Files (.pac)|*.pac|All Files (*.*)|*.*";
            openFileDialog1.FilterIndex = 1;

            // Call the ShowDialog method to show the dialog box.
            DialogResult userClickedOK = openFileDialog1.ShowDialog();

            if (userClickedOK == DialogResult.OK) // Test result.
            {
                file = @openFileDialog1.FileName;
                filename = Path.GetFileName(file);
                try
                {
                    using (BinaryReader pacStream = new BinaryReader(File.Open(file, FileMode.Open)))
                    {
                        openPacFile = null;
                        treeView1.Nodes.Clear();
                        listView1.Items.Clear();
                        PopulateTreeView(pacStream);
                    }
                }
                catch (IOException)
                {
                }
            }
        }

        private void option1ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (ListViewItem item in listView1.SelectedItems)
            {
                string name = Path.GetDirectoryName(file) + "\\" + item.SubItems[0].Text;

                if (item.Tag.GetType().Equals(typeof(TextureArchive))) 
                {
                    TextureArchive texfile = (TextureArchive)item.Tag;
                    efile.ExtractFile(texfile.stream, name);
                }
                if (item.Tag.GetType().Equals(typeof(PacFile)))
                {
                    PacFile pacfile = (PacFile)item.Tag;
                    efile.ExtractFile(pacfile.stream, name);
                }
                if (item.Tag.GetType().Equals(typeof(PACHFile)))
                {
                    PACHFile pachfile = (PACHFile)item.Tag;
                    efile.ExtractFile(pachfile.stream, name);

                }
                if (item.Tag.GetType().Equals(typeof(PACH)))
                {
                    PACH pachcontainer = (PACH)item.Tag;
                    efile.ExtractFile(pachcontainer.PACHFiles[0].stream, name);
                }
            }
        }

        private void extractAllToolStripMenuItem_Click(object sender, EventArgs e)
        {

            TreeNode selectedNode = treeView1.SelectedNode;
            if (selectedNode.Tag != null) 
            {
                extractAll(selectedNode, selectedNode.Text); //extract everything here and then go for the children
            }

            var nNodes = selectedNode.Nodes.Count;
            for (var i = 0; i < nNodes; i++)
            {
                var name = "";
                if (selectedNode.Tag == null) 
                {
                    name = selectedNode.Nodes[i].Text;
                }
                else 
                {
                    name = selectedNode.Text + "\\" + selectedNode.Nodes[i].Text;
                } 
                var tagcheck = selectedNode.Nodes[i].Tag;
                extractAll(selectedNode.Nodes[i], name);
                getFilePath(selectedNode.Nodes[i], name);
            }
        }
        private void getFilePath(TreeNode treeNode, string name) 
        {
            var nNodes = treeNode.Nodes.Count;
            for (var i = 0; i < nNodes; i++)
            {
                var subname = name + "\\" + treeNode.Nodes[i].Text;
                var tagcheck = treeNode.Nodes[i].Tag;
                extractAll(treeNode.Nodes[i], subname);
                getFilePath(treeNode.Nodes[i], subname);
            }
        }

        private void extractAll(TreeNode treeNode, string path) 
        {
            if (treeNode.Tag.GetType().Equals(typeof(PACHFile))) 
            {
                PACHFile pfile = (PACHFile)treeNode.Tag;
                if (pfile.SubContainer != null) 
                {
                    foreach (PACHFile sfile in pfile.SubContainer.PACHFiles) 
                    {
                        if (sfile.SubContainer == null && sfile.TexContainer == null) 
                        {
                            var filepath = Path.GetDirectoryName(file) + "\\" + path + "\\" + sfile.id;
                            efile.ExtractFile(sfile.stream, filepath);
                        }
                    }
                }
                if (pfile.TexContainer != null)
                {
                    foreach (TextureArchive sfile in pfile.TexContainer)
                    {
                        var name = sfile.alignedstring.Replace("\0", string.Empty).Trim() + "." + sfile.extension.Replace("\0", string.Empty).Trim();
                        var filepath = Path.GetDirectoryName(file) + "\\" + path + "\\" + name;
                        efile.ExtractFile(sfile.stream, filepath);
                    }
                }
 
            }
            if (treeNode.Tag.GetType().Equals(typeof(PacFile)))
            {
                PacFile pfile = (PacFile)treeNode.Tag;
                if (pfile.PACHContainer != null)
                {
                    foreach (PACHFile sfile in pfile.PACHContainer.PACHFiles)
                    {
                        if (sfile.SubContainer == null && sfile.TexContainer == null)
                        {
                            var filepath = Path.GetDirectoryName(file) + "\\" + path + "\\" + sfile.id;
                            efile.ExtractFile(sfile.stream, filepath);
                        }
                    }
                }
            }
        }

        private void replaceToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (ListViewItem item in listView1.SelectedItems)
            {
                if (item.Tag.GetType().Equals(typeof(TextureArchive)))
                {
                    TextureArchive texfile = (TextureArchive)item.Tag;

                    OpenFileDialog openFileDialog1 = new OpenFileDialog();
                    openFileDialog1.Filter = "Textures (*.dds)|*.dds";
                    openFileDialog1.FilterIndex = 1;

                    DialogResult userClickedOK = openFileDialog1.ShowDialog();

                    if (userClickedOK == DialogResult.OK) // Test result.
                    {
                        file = @openFileDialog1.FileName;
                        filename = Path.GetFileName(file);
                        try
                        {
                            using (Stream fileStream = File.Open(file, FileMode.Open))
                            {
                                MemoryStream full_texture = new MemoryStream();
                                fileStream.CopyTo(full_texture);

                                if (full_texture.Length != texfile.stream.Length)
                                {
                                    MessageBox.Show("Cannot replace texture, sizes don't match!");
                                }
                                else
                                {
                                    full_texture.ToArray().CopyTo(texfile.stream, 0);
                                }
                            }
                        }
                        catch (IOException)
                        {
                        }
                    }
                }
            }
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveFileDialog saveFileDialog1 = new SaveFileDialog();

            DialogResult userClickedOK = saveFileDialog1.ShowDialog();

            if (userClickedOK == DialogResult.OK) // Test result.
            {
                RewriteFile( @saveFileDialog1.FileName );
            }
        }

        private void previewTextureToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (ListViewItem item in listView1.SelectedItems)
            {
                if (item.Tag.GetType().Equals(typeof(TextureArchive)))
                {
                    using (Form previewForm = new Form())
                    {
                        TextureArchive texfile = (TextureArchive)item.Tag;
                        MemoryStream previewFile = new MemoryStream(texfile.stream, 0, texfile.stream.Length);
                        var dds = new DDSImage(previewFile);
                        Bitmap preview = dds.BitmapImage;

                        previewForm.StartPosition = FormStartPosition.CenterScreen;
                        previewForm.Size = preview.Size;

                        PictureBox previewBox = new PictureBox();
                        previewBox.Dock = DockStyle.Fill;
                        previewBox.Image = preview;

                        previewForm.Controls.Add(previewBox);
                        previewForm.ShowDialog();
                    }
                }
            }
        }
    }
}
