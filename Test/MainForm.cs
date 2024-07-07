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
using Ephemera.NBagOfTricks;
using TrayEx;
using JumpListEx;
using ClipboardEx;
using Ephemera.Win32;


namespace Test
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

            // The text output.
            txtViewer.Font = new("Lucida Console", 9);
            txtViewer.WordWrap = true;
            txtViewer.BackColor = Color.Cornsilk;
            txtViewer.MatchColors.Add("ERR", Color.LightPink);
            txtViewer.MatchColors.Add("WRN", Color.Plum);

            //Text = $"WinBagOfTricks {MiscUtils.GetVersionString()} - No file loaded";
        }

        /// <summary>
        /// Clean up on shutdown. Dispose() will get the rest.
        /// </summary>
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);
        }
        #endregion

        private void TrayEx_Click(object sender, EventArgs e)
        {
            using var app = new TrayEx.TrayExApplicationContext();
        }

        private void JumpListEx_Click(object sender, EventArgs e)
        {
            using var app = new JumpListEx.JumpListEx();
        }

        private void ClipboardEx_Click(object sender, EventArgs e)
        {
            using var app = new ClipboardEx.ClipboardEx();
        }

        private void button3_Click(object sender, EventArgs e)
        {

        }

        private void button4_Click(object sender, EventArgs e)
        {

        }
    }
}
