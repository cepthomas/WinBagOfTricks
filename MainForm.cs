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
using System.Diagnostics;
using System.Drawing.Design;
using System.Text.Json.Serialization;
using System.Text.Json;
using NBagOfTricks;
using NBagOfUis;


namespace NOrfima
{
    public partial class MainForm : Form
    {
        #region Fields
        /// <summary>Current global user settings.</summary>
        UserSettings _settings = new();
        #endregion

        #region Lifecycle
        /// <summary>
        /// Constructor.
        /// </summary>
        public MainForm()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Initialize form controls.
        /// </summary>
        void MainForm_Load(object? sender, EventArgs e)
        {
            // Get the settings.
            string appDir = MiscUtils.GetAppDataDir("NOrfima", "Ephemera");
            DirectoryInfo di = new(appDir);
            di.Create();
            _settings = UserSettings.Load(appDir);

            toolStrip1.Renderer = new NBagOfUis.CheckBoxRenderer() { SelectedColor = _settings.ControlColor };

            // Toolbar configs.
            btnLoop.Checked = false;

            // The text output.
            txtViewer.WordWrap = true;
            txtViewer.BackColor = Color.Cornsilk;
            txtViewer.Colors.Add("ERR", Color.LightPink);
            txtViewer.Colors.Add("WRN", Color.Plum);
            txtViewer.Font = new("Lucida Console", 9);

            // Check for "config_taskbar" then do something...
            LogMessage("INF", $"args: {string.Join(" ", Environment.GetCommandLineArgs())}");

            // Init UI from settings
            Location = new Point(_settings.FormGeometry.X, _settings.FormGeometry.Y);
            Size = new Size(_settings.FormGeometry.Width, _settings.FormGeometry.Height);
            WindowState = FormWindowState.Normal;
            //KeyPreview = true; // for routing kbd strokes through MainForm_KeyDown

            InitNavigator();

            Text = $"NOrfima {MiscUtils.GetVersionString()} - No file loaded";

            new ClipboardMonitor().Show();
        }

        /// <summary>
        /// Jumplist population is after a valid window.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MainForm_Shown(object? sender, EventArgs e)
        {
            // Jumplist population is after a valid window shown.
            new TaskBar().Show();
        }

        /// <summary>
        /// Clean up on shutdown. Dispose() will get the rest.
        /// </summary>
        void MainForm_FormClosing(object? sender, FormClosingEventArgs e)
        {
            SaveSettings();
        }
        #endregion

        #region User settings
        /// <summary>
        /// Collect and save user settings.
        /// </summary>
        void SaveSettings()
        {
            //_settings.Autoplay = btnAutoplay.Checked;
            //_settings.Loop = btnLoop.Checked;
            //_settings.Volume = sldVolume.Value;
            _settings.FormGeometry = new Rectangle(Location.X, Location.Y, Size.Width, Size.Height);

            _settings.Save();
        }

        /// <summary>
        /// Edit the common options in a property grid.
        /// </summary>
        void Settings_Click(object? sender, EventArgs e)
        {
            using Form f = new()
            {
                Text = "User Settings",
                Size = new Size(450, 450),
                StartPosition = FormStartPosition.Manual,
                Location = new Point(200, 200),
                FormBorderStyle = FormBorderStyle.FixedToolWindow,
                ShowIcon = false,
                ShowInTaskbar = false
            };

            PropertyGridEx pg = new()
            {
                Dock = DockStyle.Fill,
                PropertySort = PropertySort.Categorized,
                SelectedObject = _settings
            };

            // Detect changes of interest.
            bool midiChange = false;
            bool audioChange = false;
            bool navChange = false;
            bool restart = false;

            pg.PropertyValueChanged += (sdr, args) =>
            {
                restart |= args.ChangedItem.PropertyDescriptor.Name.EndsWith("Device");
                midiChange |= args.ChangedItem.PropertyDescriptor.Category == "Midi";
                audioChange |= args.ChangedItem.PropertyDescriptor.Category == "Audio";
                navChange |= args.ChangedItem.PropertyDescriptor.Category == "Navigator";
            };

            f.Controls.Add(pg);
            f.ShowDialog();

            // Figure out what changed - each handled differently.
            if (restart)
            {
                MessageBox.Show("Restart required for device changes to take effect");
            }

            if (navChange)
            {
                InitNavigator();
            }

            SaveSettings();
        }
        #endregion

        #region Info
        /// <summary>
        /// All about me.
        /// </summary>
        void About_Click(object? sender, EventArgs e)
        {
            Tools.MarkdownToHtml(File.ReadAllLines(@".\README.md").ToList(), "lightcyan", "helvetica", true);
        }

        /// <summary>
        /// Something you should know.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="ea"></param>
        void LogMessage(string cat, string msg)
        {
            int catSize = 3;
            cat = cat.Length >= catSize ? cat.Left(catSize) : cat.PadRight(catSize);

            // May come from a different thread.
            this.InvokeIfRequired(_ =>
            {
                string s = $"{DateTime.Now:mm\\:ss\\.fff} {cat} {msg}";
                txtViewer.AddLine(s);
            });
        }
        #endregion

        /// <summary>
        /// Initialize tree from user settings.
        /// </summary>
        void InitNavigator()
        {
            ftreeLeft.FilterExts.Clear();
            ftreeLeft.FilterExts.Add(".txt");

            if (_settings.RootDirs.Count == 0)
            {
                _settings.RootDirs.Add(@"C:\Dev");
            }

            ftreeLeft.RootDirs = _settings.RootDirs;
            ftreeLeft.SingleClickSelect = true;// or?

            ftreeLeft.Init();
        }

        /// <summary>
        /// Tree has seleccted a file to play.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="fn"></param>
        void FtreeLeft_FileSelectedEvent(object? sender, string fn)
        {
            //OpenFile(fn);
            //_fn = fn;
        }

        void Debug_Click(object sender, EventArgs e)
        {
        }
    }


    [Serializable]
    public class UserSettings
    {
        #region Persisted editable properties
        [DisplayName("Root Directories")]
        [Description("Where to look in order as they appear.")]
        [Category("Navigator")]
        [Browsable(true)]
        [Editor(typeof(StringListEditor), typeof(UITypeEditor))] // Should be a proper folder picker.
        public List<string> RootDirs { get; set; } = new List<string>();

        [DisplayName("Control Color")]
        [Description("Pick what you like.")]
        [Category("Cosmetics")]
        [Browsable(true)]
        [JsonConverter(typeof(JsonColorConverter))]
        public Color ControlColor { get; set; } = Color.MediumOrchid;
        #endregion

        #region Persisted Non-editable Properties
        [Browsable(false)]
        [JsonConverter(typeof(JsonRectangleConverter))]
        public Rectangle FormGeometry { get; set; } = new Rectangle(50, 50, 800, 800);

        [Browsable(false)]
        public List<string> RecentFiles { get; set; } = new List<string>();
        #endregion

        #region Fields
        /// <summary>The file name.</summary>
        string _fn = "???";
        #endregion

        #region Persistence
        /// <summary>Save object to file.</summary>
        public void Save()
        {
            JsonSerializerOptions opts = new() { WriteIndented = true };
            string json = JsonSerializer.Serialize(this, opts);
            File.WriteAllText(_fn, json);
        }

        /// <summary>Create object from file.</summary>
        public static UserSettings Load(string appDir)
        {
            UserSettings us;

            string fn = Path.Combine(appDir, "settings.json");

            if (File.Exists(fn))
            {
                string json = File.ReadAllText(fn);
                UserSettings? set = JsonSerializer.Deserialize<UserSettings>(json);
                us = set ?? new();
                us._fn = fn;

                // Clean up any bad file names.
                us.RecentFiles.RemoveAll(f => !File.Exists(f));
            }
            else
            {
                // Doesn't exist, create a new one.
                us = new UserSettings
                {
                    _fn = fn
                };
            }

            return us;
        }
        #endregion
    }
}
