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
            txtViewer.MatchText.Add("ERR", Color.LightPink);
            txtViewer.MatchText.Add("WRN", Color.Plum);
        }

        /// <summary>
        /// Clean up on shutdown. Dispose() will get the rest.
        /// </summary>
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);
        }
        #endregion

        private void Btn1_Click(object sender, EventArgs e)
        {

        }

        private void Btn2_Click(object sender, EventArgs e)
        {

        }
    }
}
