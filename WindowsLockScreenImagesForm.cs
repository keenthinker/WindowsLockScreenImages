using System.Diagnostics;
using System.Drawing.Imaging;

namespace WindowsLockScreenImages;

public partial class WindowsLockScreenImagesForm : Form
{
    private FileInfo[] assets = Array.Empty<FileInfo>();

    private string assetsUserDirectory = string.Empty;

    private FileInfo[] getAssets()
    {
        var userDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        //const string directory = @"C:\Users\USERNAME\AppData\Local\Packages\Microsoft.Windows.ContentDeliveryManager_cw5n1h2txyewy\LocalState\Assets";
        var directory = Path.Combine(userDirectory, @"AppData\Local\Packages\Microsoft.Windows.ContentDeliveryManager_cw5n1h2txyewy\LocalState\Assets");
        this.assetsUserDirectory = directory;
        if (!Directory.Exists(directory))
        {
            MessageBox.Show($"Directory '{directory}' does not exist!", "Something went wrong", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return this.assets;
        }
        return new DirectoryInfo(directory).GetFiles();
    }

    public WindowsLockScreenImagesForm()
    {
        InitializeComponent();

        this.Icon = new Icon(AppDomain.CurrentDomain.BaseDirectory +  @"\WindowsLockScreenImagesIcon.ico");

        this.assets = getAssets();

        ListBox listBox = new();

        if (!this.assets.Any())
        {
            this.Load += (s, e) => Application.Exit();
        }
        // status bar
        var statusStrip = new StatusStrip();
        // label to show file information (name, size)
        var statusStripLabel = new ToolStripStatusLabel("Click an image from the list to view");
        statusStrip.Items.Add(statusStripLabel);
        // buttons to save the selected file and to open the directory with explorer
        statusStrip.Items.Add(new ToolStripStatusLabel("") { Spring = true, TextAlign = ContentAlignment.MiddleRight });
       var statusStripSaveButton = new ToolStripSplitButton("Click to save selected image")
        {
            DropDownButtonWidth = 0
        };
        statusStripSaveButton.Click += (s, e) =>
        {
            var selectedIndex = listBox.SelectedIndex;
            if (selectedIndex >= 0)
            {
                var metaItem = listBox.Items[selectedIndex] as ListBoxMetaItem;
                if (metaItem is not null)
                {
                    var fullFileName = metaItem.Info?.FullName;

                    var saveFileDialog = new SaveFileDialog
                    {
                        FileName = $"{metaItem.Info?.Name}.jpg",
                        Filter = "JPEG (*.jpg)|*.jpeg,*.jpg",
                        InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
                    };
                    if (saveFileDialog.ShowDialog() == DialogResult.OK)
                    {
                        if (File.Exists(fullFileName))
                        {
                            Image.FromFile(fullFileName).Save(saveFileDialog.FileName, ImageFormat.Jpeg);
                        }
                    }
                }
            }
            else
            {
                MessageBox.Show("Click on an image on the list to select it and save it.", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        };
        statusStrip.Items.Add(statusStripSaveButton);
        statusStrip.Items.Add(new ToolStripSeparator());
        var statusStripOpenButton = new ToolStripSplitButton("Click to open images folder")
        {
            DropDownButtonWidth = 0
        };
        statusStripOpenButton.Click += (s, e) =>
        {
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                Arguments = this.assetsUserDirectory,
                FileName = "explorer.exe"
            };
            Process.Start(startInfo);
        };
        statusStrip.Items.Add(statusStripOpenButton);
        // split panel
        var splitContainer = new SplitContainer
        {
            Dock = DockStyle.Fill,
            SplitterWidth = 10
        };
        // right = image preview
        var pictureBox = new PictureBox
        {
            Dock = DockStyle.Fill,
            SizeMode = PictureBoxSizeMode.Zoom
        };
        // left = list of files
        listBox = new ListBox
        {
            Dock = DockStyle.Fill
        };
        foreach (var fileInfo in this.assets)
        {
            listBox.Items.Add(new ListBoxMetaItem(fileInfo));
        }
        listBox.Click += (s, e) =>
        {
            var selectedIndex = listBox.SelectedIndex;
            if (selectedIndex >= 0)
            {
                var metaItem = listBox.Items[selectedIndex] as ListBoxMetaItem;
                if (metaItem is not null)
                {
                    var fullFileName = metaItem.Info?.FullName;
                    if (File.Exists(fullFileName))
                    {
                        pictureBox.Image = Image.FromFile(fullFileName);
                    }
                    var item = (ToolStripStatusLabel)statusStrip.Items[0];
                    var filenameLength = (metaItem.Info is not null) ? metaItem.Info.Name.Length : 0;
                    item.Text = $"{metaItem.Info?.Name.Substring(0, 4)}...{metaItem.Info?.Name.Substring(filenameLength - 4, 4)} ({metaItem.HumanReadableSize()})";
                }
            }
        };
        //
        splitContainer.Panel1.Controls.Add(listBox);
        splitContainer.Panel2.Controls.Add(pictureBox);
        //
        this.Controls.Add(splitContainer);
        this.Controls.Add(statusStrip);
    }
}
