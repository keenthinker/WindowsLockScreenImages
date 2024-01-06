using System.Diagnostics;

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

        this.assets = getAssets();
        if (!this.assets.Any())
        {
            this.Load += (s, e) => Application.Exit();
        }
        // status bar
        var statusStrip = new StatusStrip();
        // label to show file information (name, size)
        var statusStripLabel = new ToolStripStatusLabel("Click an image from the list to view");
        statusStrip.Items.Add(statusStripLabel);
        // button to open the directory with explorer
        var statusStripButton = new ToolStripStatusLabel("Click to open images folder")
        {
            Spring = true,
            TextAlign = ContentAlignment.MiddleRight
        };
        statusStripButton.Click += (s, e) =>
        {
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                Arguments = this.assetsUserDirectory,
                FileName = "explorer.exe"
            };
            Process.Start(startInfo);
        };
        statusStrip.Items.Add(statusStripButton);
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
        var listBox = new ListBox
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
                var fullFileName = metaItem.Info.FullName;

                pictureBox.Image = Image.FromFile(fullFileName);

                var item = (ToolStripStatusLabel)statusStrip.Items[0];
                item.Text = $"{metaItem.Info.Name} ({metaItem.HumanReadableSize()})";
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
