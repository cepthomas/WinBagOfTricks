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
    /// <summary>
    /// Actually more of a test host.
    /// </summary>
    public partial class MainForm : Form
    {
        #region Fields

        #endregion

        #region Lifecycle
        /// <summary>
        /// Constructor.
        /// </summary>
        public MainForm()
        {
            InitializeComponent();

            // Toolbar configs.
            btnLoop.Checked = false;

            // The text output.
            txtViewer.Font = new("Lucida Console", 9);
            txtViewer.WordWrap = true;
            txtViewer.BackColor = Color.Cornsilk;
            txtViewer.Colors.Add("ERR", Color.LightPink);
            txtViewer.Colors.Add("WRN", Color.Plum);

            InitNavigator();

            Text = $"WinBagOfTricks {MiscUtils.GetVersionString()} - No file loaded";
        }

        /// <summary>
        /// Clean up on shutdown. Dispose() will get the rest.
        /// </summary>
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            //SaveSettings();
            base.OnFormClosing(e);
        }
        #endregion

        #region Info
        /// <summary>
        /// All about me.
        /// </summary>
        void About_Click(object? sender, EventArgs e)
        {
            MiscUtils.ShowReadme("WinBagOfTricks");
        }
        #endregion

        /// <summary>
        /// Initialize tree from user settings.
        /// </summary>
        void InitNavigator()
        {
            ftreeLeft.FilterExts.Clear();
            ftreeLeft.FilterExts.Add(".txt .cs");

            ftreeLeft.RootDirs.Clear();
            ftreeLeft.RootDirs.Add(@"C:\Dev");
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Debug_Click(object sender, EventArgs e)
        {
        }
    }
}
