using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Diagnostics;
using System.Drawing.Design;
using System.Text.Json.Serialization;
using System.Text.Json;
using NBagOfTricks;
using NBagOfUis;



namespace WinBagOfTricks
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
            string appDir = MiscUtils.GetAppDataDir("WinBagOfTricks", "Ephemera");
            DirectoryInfo di = new(appDir);
            di.Create();
            _settings = UserSettings.Load(appDir);

            // Make it colorful.
            toolStrip1.Renderer = new NBagOfUis.CheckBoxRenderer() { SelectedColor = _settings.SelectedColor };

            fileDropDownButton.Image = GraphicsUtils.ColorizeBitmap((Bitmap)fileDropDownButton.Image, _settings.ControlColor);
            btnSettings.Image = GraphicsUtils.ColorizeBitmap((Bitmap)btnSettings.Image, _settings.ControlColor);
            btnAbout.Image = GraphicsUtils.ColorizeBitmap((Bitmap)btnAbout.Image, _settings.ControlColor);
            btnLoop.Image = GraphicsUtils.ColorizeBitmap((Bitmap)btnLoop.Image, _settings.ControlColor);
            btnDebug.Image = GraphicsUtils.ColorizeBitmap((Bitmap)btnDebug.Image, _settings.ControlColor);

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

            Text = $"WinBagOfTricks {MiscUtils.GetVersionString()} - No file loaded";
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
            _settings.FormGeometry = new Rectangle(Location.X, Location.Y, Size.Width, Size.Height);
            _settings.Save();
        }

        /// <summary>
        /// Edit the common options in a property grid.
        /// </summary>
        void Settings_Click(object? sender, EventArgs e)
        {
            var res = _settings.Edit();

            // Figure out what changed - each handled differently.
            if (res.restart)
            {
                MessageBox.Show("Restart required for device changes to take effect");
            }

            if (res.navChange)
            {
                InitNavigator();
            }

            _settings.Save();

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
            ftreeLeft.FilterExts.Add(".txt .cs");

            if (_settings.RootDirs.Count == 0)
            {
                _settings.RootDirs.Add(@"C:\");
            }

            ftreeLeft.RootDirs = _settings.RootDirs;
            ftreeLeft.SingleClickSelect = true;// or?

            ftreeLeft.Init();
        }

        /// <summary>
        /// Tree has seleccted a file.
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
}
