using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Drawing.Printing;
using NBagOfTricks;
using NBagOfUis;


namespace ClipboardEx
{
    /// <summary>
    /// One selectable clip item.
    /// </summary>
    public partial class ClipDisplay : UserControl
    {
        /// <summary>For owner use.</summary>
        public int Id { get; set; } = -1;

        #region Event Stuff
        public enum ClipEventType { Click, DoubleClick }

        /// <summary>Tell the boss.</summary>
        public event EventHandler<ClipEventArgs>? ClipEvent;
        public class ClipEventArgs : EventArgs
        {
            public ClipEventType EventType { get; private set; } = ClipEventType.Click;
            public ClipEventArgs(ClipEventType ce)
            {
                EventType = ce;
            }
        }
        #endregion

        /// <summary>
        /// Constructor.
        /// </summary>
        public ClipDisplay()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Init stuff.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ClipDisplay_Load(object sender, EventArgs e)
        {
            Size sz = new(Width, Height - lblInfo.Height);
            Point loc = new(0, lblInfo.Height);

            lblInfo.Left = 0;
            lblInfo.Width = Width;

            picImage.Size = sz;
            picImage.Location = loc;

            rtbText.Size = sz;
            rtbText.Location = loc;
            rtbText.ScrollBars = RichTextBoxScrollBars.Horizontal;
            rtbText.WordWrap = false;

            // Intercept UI.
            picImage.Click += Control_Click;
            rtbText.Click += Control_Click;
            lblInfo.Click += Control_Click;
        }

        /// <summary>
        /// User control event.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Control_Click(object? sender, EventArgs e)
        {
            ClipEvent?.Invoke(this, new ClipEventArgs(ClipEventType.Click));
        }

        /// <summary>
        /// Text specific setup.
        /// </summary>
        /// <param name="stype"></param>
        /// <param name="text"></param>
        public void SetText(string stype, string text)
        {
            const int PEEK_SIZE = 1000;
            const int NUM_LINES = 5; // size to fit control

            // Show just a part with leading ws removed.
            bool more = text.Length > PEEK_SIZE;
            var s = text.Left(PEEK_SIZE);
            var ls = s.SplitByTokens("\r\n");
            more |= ls.Count > NUM_LINES;
            StringBuilder sb = new();
            for (int i = 0; i < Math.Min(ls.Count, more ? NUM_LINES-1 : NUM_LINES); i++)
            {
                sb.AppendLine(ls[i]);
            }
            if (more)
            {
                sb.AppendLine("...");
            }

            picImage.Hide();
            rtbText.Show();
            rtbText.Text = sb.ToString();
            lblInfo.Text = stype;
        }

        /// <summary>
        /// Image specific setup.
        /// </summary>
        /// <param name="bmp"></param>
        public void SetImage(Bitmap bmp)
        {
            picImage.Show();
            rtbText.Hide();
            if (UserSettings.TheSettings.FitImage)
            {
                picImage.Image = GraphicsUtils.ResizeBitmap(bmp, Width, Height);
            }
            else
            {
                picImage.Image = bmp;
            }

            lblInfo.Text = "Image";
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="text"></param>
        public void SetOther(string text)
        {
            picImage.Show();
            rtbText.Hide();
            rtbText.Text = text;
            lblInfo.Text = "Other";
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="text"></param>
        public void SetEmpty()
        {
            picImage.Hide();
            rtbText.Show();
            rtbText.Text = "";
            lblInfo.Text = "Empty";
        }
    }
}
