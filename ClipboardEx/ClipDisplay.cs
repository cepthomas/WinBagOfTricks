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
    public partial class ClipDisplay : UserControl
    {
        //StringCollection GetFileDropList();
        //Image GetImage();
        //string GetText();


        public ClipDisplay()
        {
            InitializeComponent();
        }

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
            rtbText.ScrollBars = RichTextBoxScrollBars.Both;
            rtbText.WordWrap = false;
        }

        public void SetText(string stype, string text)
        {
            picImage.Hide();
            rtbText.Show();
            // Show just a part.
            rtbText.Text = text.Left(300);
            lblInfo.Text = stype;
        }

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

        public void SetOther(string text)
        {
            picImage.Show();
            rtbText.Hide();
            rtbText.Text = text;
            lblInfo.Text = "Other";
        }
    }
}
