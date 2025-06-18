using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Ephemera.NBagOfTricks;
using Ephemera.NBagOfUis;
using W32 = Ephemera.Win32.Internals;
using WM = Ephemera.Win32.WindowManagement;


namespace TrayEx
{
    /// <summary>Framework for running application as a tray app.</summary>
    public class TrayExApplicationContext : ApplicationContext
    {
        #region Fields
        readonly Icon _icon1;
        readonly Icon _icon2;
        readonly Container _components = new();
        readonly NotifyIcon _notifyIcon;
        readonly Timer _timer = new();
        readonly List<string> _messages = [];
        #endregion

        #region Lifecycle
        /// <summary>Start here.</summary>
        public TrayExApplicationContext()
        {
            // Clean up resources.
            Application.ApplicationExit += ApplicationExit_Handler;

            // Or alternatively.
            ThreadExit += ThreadExit_Handler;

            ContextMenuStrip ctxm = new();
            ctxm.Items.Add("dialog", null, Menu_Click);
            ctxm.Items.Add("icon", null, Menu_Click);
            ctxm.Items.Add(new ToolStripSeparator());
            ctxm.Items.Add("close", null, Menu_Click);
            ctxm.Opening += ContextMenu_Opening;

            var sf = (Bitmap)Image.FromFile("glyphicons-22-snowflake.png"); // 26x26
            var img1 = sf.Colorize(Color.LightGreen);
            var img2 = sf.Colorize(Color.Red);
            _icon1 = IconFromImage(img1);
            _icon2 = IconFromImage(img2);

            _notifyIcon = new(_components)
            {
                Icon = _icon1,
                Text = "I am a tray application!",
                ContextMenuStrip = ctxm,
                Visible = true
            };

            _notifyIcon.MouseClick += Nicon_MouseClick;
            _notifyIcon.MouseDoubleClick += Nicon_MouseDoubleClick;

            // Timer ticks every second to check the directory and update status.
            _timer.Tick += new EventHandler(Timer_Tick);
            _timer.Interval = 1000;
            _timer.Enabled = true;
            _timer.Start();
        }

        /// <summary>
        /// Clean up resources.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ApplicationExit_Handler(object? sender, EventArgs e)
        {
            Tell($"ApplicationExit_Handler()");

            _notifyIcon.ContextMenuStrip?.Dispose();
            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();
            _icon1.Dispose();
            _icon2.Dispose();
            _timer.Dispose();
        }

        /// <summary>
        /// This may be useful too. Maybe not.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void ThreadExit_Handler(object? sender, EventArgs e)
        {
            Tell($"ThreadExit_Handler()");
        }

        /// <summary>
        /// This may be useful too. Maybe not.
        /// If we are presently showing a form, clean it up.
        /// </summary>
        protected override void ExitThreadCore()
        {
            Tell($"ExitThreadCore()");
            base.ExitThreadCore();
        }
        #endregion

        #region UI Event Handling
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void ContextMenu_Opening(object? sender, CancelEventArgs e)
        {
            // Could add more options here.

            e.Cancel = false;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Nicon_MouseClick(object? sender, MouseEventArgs e)
        {
            Tell($"You clicked icon:{e.Button}");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Nicon_MouseDoubleClick(object? sender, MouseEventArgs e)
        {
            Tell($"You double clicked icon:{e}");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Menu_Click(object? sender, EventArgs e)
        {
            var mi = (ToolStripMenuItem)sender!;
            Tell($"You clicked menu:{mi.Text}");

            switch (mi.Text)
            {
                case "dialog":
                    CreateDialog();
                    break;

                case "icon":
                    _notifyIcon.Icon = _notifyIcon.Icon == _icon1 ? _icon2 : _icon1;
                    break;

                case "close":
                    _notifyIcon.Visible = false; // should remove lingering tray icon
                    Tell($"call ExitThread()");
                    ExitThread();
                    break;
            }
        }

        /// <summary>
        /// Build a dialog dynamically.
        /// </summary>
        void CreateDialog()
        {
            using Form f = new()
            {
                Text = "Demo Viewer",
                Size = new Size(565, 450),
                BackColor = Color.Gold,
                StartPosition = FormStartPosition.Manual,
                Location = new Point(Cursor.Position.X - 565, Cursor.Position.Y - 450),
                FormBorderStyle = FormBorderStyle.SizableToolWindow,
                ShowIcon = false,
                ShowInTaskbar = false
            };

            RichTextBox rtbInfo = new()
            {
                BorderStyle = BorderStyle.FixedSingle,
                Location = new(0, 70),
                Size = new(565, 380)
            };
            _messages.ForEach(m => rtbInfo.AppendText(m));
            f.Controls.Add(rtbInfo);

            Button btnKickMe = new()
            {
                Location = new(44, 13),
                Size = new(456, 40),
                Text = "Kick Me!!",
                UseVisualStyleBackColor = true
            };
            btnKickMe.Click += (_, __) => rtbInfo.AppendText("Kick Me !!! ");
            f.Controls.Add(btnKickMe);

            f.ShowDialog();
        }

        /// <summary>
        /// Do something interesting.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Timer_Tick(object? sender, EventArgs e)
        {
        }
        #endregion

        #region Internal Functions
        /// <summary>
        /// Just for debugging.
        /// </summary>
        /// <param name="msg"></param>
        void Tell(string msg)
        {
            string s = $"{DateTime.Now:mm\\:ss\\.fff} {msg}{Environment.NewLine}";
            _messages.Add(s);
        }

        /// <summary>
        /// https://stackoverflow.com/a/21389253
        /// </summary>
        /// <param name="img"></param>
        /// <returns></returns>
        public Icon IconFromImage(Image img)
        {
            using var ms = new MemoryStream();
            using var bw = new BinaryWriter(ms);

            // Header
            bw.Write((short)0);   // 0 : reserved
            bw.Write((short)1);   // 2 : 1=ico, 2=cur
            bw.Write((short)1);   // 4 : number of images
            
            // Image directory
            bw.Write((byte)(img.Width > 256 ? 0 : img.Width));    // 0 : width of image
            bw.Write((byte)(img.Height > 256 ? 0 : img.Height));    // 1 : height of image
            bw.Write((byte)0);    // 2 : number of colors in palette
            bw.Write((byte)0);    // 3 : reserved
            bw.Write((short)0);   // 4 : number of color planes
            bw.Write((short)0);   // 6 : bits per pixel
            var sizeHere = ms.Position;
            bw.Write(0);     // 8 : image size
            var start = (int)ms.Position + 4;
            bw.Write(start);      // 12: offset of image data

            // Image data
            img.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
            var imageSize = (int)ms.Position - start;
            ms.Seek(sizeHere, SeekOrigin.Begin);
            bw.Write(imageSize);
            ms.Seek(0, SeekOrigin.Begin);

            return new Icon(ms);
        }
        #endregion
    }
}
