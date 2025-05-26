using System.Diagnostics;
using System.Drawing.Imaging;

namespace WindowsLockScreenImages;

public partial class WindowsLockScreenImagesForm : Form
{
    private FileInfo[] assets = Array.Empty<FileInfo>();

    private string assetsUserDirectory = string.Empty;
    private string assetsLockScreenDirectory = string.Empty;

    private FileInfo[] getAssets()
    {
        var userDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var wallpaper_directory = Path.Combine(userDirectory, @"AppData\Local\Packages\Microsoft.Windows.ContentDeliveryManager_cw5n1h2txyewy\LocalState\Assets");
        var lockscreen_directory = Path.Combine(userDirectory, @"AppData\Roaming\Microsoft\Windows\Themes");
        this.assetsUserDirectory = wallpaper_directory;
        this.assetsLockScreenDirectory = lockscreen_directory;
        List<FileInfo> files = new List<FileInfo>();
        if (Directory.Exists(wallpaper_directory))
        {
            foreach (var file in new DirectoryInfo(wallpaper_directory).GetFiles())
            {
                if (file.Length >= 50 * 1024)
                    files.Add(file);
            }
        }
        else
        {
            MessageBox.Show($"Directory '{wallpaper_directory}' does not exist!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        if (Directory.Exists(lockscreen_directory))
        {
            foreach (var file in new DirectoryInfo(lockscreen_directory).GetFiles())
            {
                if (file.Extension != ".ini")
                    files.Add(file);
            }
        }
        else
        {
            MessageBox.Show($"Directory '{lockscreen_directory}' does not exist!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        return files.ToArray();
    }
    private Bitmap createThumbnail(Image original, Size maxSize)
    {
        double ratioX = (double)maxSize.Width / original.Width;
        double ratioY = (double)maxSize.Height / original.Height;
        double ratio = Math.Min(ratioX, ratioY);

        int newWidth = (int)(original.Width * ratio);
        int newHeight = (int)(original.Height * ratio);

        var thumb = new Bitmap(newWidth, newHeight);
        using (var graphics = Graphics.FromImage(thumb))
        {
            graphics.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
            graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
            graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
            graphics.DrawImage(original, 0, 0, newWidth, newHeight);
        }
        return thumb;
    }

    public WindowsLockScreenImagesForm()
    {
        InitializeComponent();

        this.Icon = new Icon(AppDomain.CurrentDomain.BaseDirectory + @"\WindowsLockScreenImagesIcon.ico");

        this.assets = getAssets();

        ListBox listBox = new();

        if (!this.assets.Any())
        {
            this.Load += (s, e) => Application.Exit();
        }
        // status bar
        var statusStrip = new StatusStrip();
        statusStrip.ShowItemToolTips = true;
        // label to show file information (name, size)
        var statusStripLabel = new ToolStripStatusLabel("Click an image from the list to view");
        statusStrip.Items.Add(statusStripLabel);
        // buttons to save the selected file and to open the directory with explorer
        statusStrip.Items.Add(new ToolStripStatusLabel("") { Spring = true, TextAlign = ContentAlignment.MiddleRight });
        var statusStripSaveButton = new ToolStripSplitButton("Click to save selected image")
        {
            DropDownButtonWidth = 0
        };
        statusStrip.Items.Add(statusStripSaveButton);
        statusStrip.Items.Add(new ToolStripSeparator());
        var statusStripOpenButton = new ToolStripSplitButton("Click to open images folder")
        {
            DropDownButtonWidth = 0
        };
        // open explorer on the two directories
        statusStripOpenButton.Click += (s, e) =>
        {
            ProcessStartInfo startInfo1 = new ProcessStartInfo
            {
                Arguments = this.assetsUserDirectory,
                FileName = "explorer.exe"
            };

            ProcessStartInfo startInfo2 = new ProcessStartInfo
            {
                Arguments = this.assetsLockScreenDirectory,
                FileName = "explorer.exe"
            };
            Process.Start(startInfo1);
            Process.Start(startInfo2);
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
            SizeMode = PictureBoxSizeMode.Zoom,
        };
        var pictureContainer = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(5),
        };
        pictureContainer.Controls.Add(pictureBox);
        // create thumbnail viewer
        var thumbnailPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            WrapContents = true,
            FlowDirection = FlowDirection.LeftToRight
        };

        // map thumbnail PictureBox to FileInfo
        Dictionary<PictureBox, FileInfo> pictureMap = new();

        PictureBox? selectedThumbnail = null;

        foreach (var fileInfo in this.assets)
        {
            try
            {
                using var tempImage = Image.FromFile(fileInfo.FullName);
                var thumbnail = createThumbnail(tempImage, new Size(100, 60));

                var picBox = new PictureBox
                {
                    Image = thumbnail,
                    Width = 100,
                    Height = 60,
                    SizeMode = PictureBoxSizeMode.Zoom,
                    Cursor = Cursors.Hand,
                    Margin = new Padding(5)
                };

                picBox.Click += (s, e) =>
                {
                    if (File.Exists(fileInfo.FullName))
                    {
                        pictureBox.Image = Image.FromFile(fileInfo.FullName);

                        var filenameLength = fileInfo.Name.Length;
                        statusStripLabel.Text = $"{fileInfo.Name.Substring(0, 4)}...{fileInfo.Name.Substring(filenameLength - 4, 4)} ({new ListBoxMetaItem(fileInfo).HumanReadableSize()})";
                        statusStripLabel.ToolTipText = fileInfo.FullName;

                        selectedThumbnail = (PictureBox)s!;
                    }
                };

                pictureMap[picBox] = fileInfo;
                thumbnailPanel.Controls.Add(picBox);
            }
            catch
            {
                // discard invalid image files
            }
        }

        statusStripSaveButton.Click += (s, e) =>
        {
            FileInfo? selectedFile = null;

            if (listBox.Visible && listBox.SelectedIndex >= 0)
            {
                var metaItem = listBox.Items[listBox.SelectedIndex] as ListBoxMetaItem;
                selectedFile = metaItem?.Info;
            }
            else if (thumbnailPanel.Visible && selectedThumbnail != null && pictureMap.TryGetValue(selectedThumbnail, out var fileFromThumbnail))
            {
                selectedFile = fileFromThumbnail;
            }

            if (selectedFile != null && File.Exists(selectedFile.FullName))
            {
                var saveFileDialog = new SaveFileDialog
                {
                    FileName = $"{selectedFile.Name}.jpg",
                    Filter = "JPEG (*.jpg)|*.jpeg;*.jpg",
                    InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
                };

                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    using var img = Image.FromFile(selectedFile.FullName);
                    img.Save(saveFileDialog.FileName, ImageFormat.Jpeg);
                }
            }
            else
            {
                MessageBox.Show("Click on an image to select it and save it.", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        };

        listBox.Dock = DockStyle.Fill;

        foreach (var file in this.assets)
        {
            listBox.Items.Add(new ListBoxMetaItem(file));
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

        thumbnailPanel.Visible = false;
        listBox.Visible = true;

        var toggleViewButton = new ToolStripSplitButton("Click to show thumbnails")
        {
            DropDownButtonWidth = 0
        };
        // button to toggle between file list and thumbnails
        toggleViewButton.Click += (s, e) =>
        {
            bool showingThumbnails = thumbnailPanel.Visible;

            thumbnailPanel.Visible = !showingThumbnails;
            listBox.Visible = showingThumbnails;

            toggleViewButton.Text = showingThumbnails ? "Click to show file list" : "Click to show thumbnails";
        };
        statusStrip.Items.Add(new ToolStripSeparator());
        statusStrip.Items.Add(toggleViewButton);
        //
        splitContainer.Panel1.Controls.Add(thumbnailPanel);
        splitContainer.Panel1.Controls.Add(listBox);
        splitContainer.Panel2.Controls.Add(pictureContainer);
        //
        this.Controls.Add(splitContainer);
        this.Controls.Add(statusStrip);
    }
}
