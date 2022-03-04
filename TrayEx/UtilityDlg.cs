using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using NBagOfTricks;


namespace TrayEx
{
    public partial class UtilityDlg : Form
    {
        /// <summary>
        /// 
        /// </summary>
        public UtilityDlg()
        {
            InitializeComponent();

            //StartPosition = FormStartPosition.Manual,
            //Location = new Point(200, 200),
            ShowIcon = false;
            ShowInTaskbar = false;
        }

        /// <summary>
        /// Just for debugging.
        /// </summary>
        /// <param name="cat"></param>
        /// <param name="msg"></param>
        public void LogMessage(string cat, string msg)
        {
            int catSize = 3;
            cat = cat.Length >= catSize ? cat.Left(catSize) : cat.PadRight(catSize);
            string s = $"{DateTime.Now:mm\\:ss\\.fff} {cat} {msg}{Environment.NewLine}";
            rtbInfo.AppendText(s);
        }
    }
}
