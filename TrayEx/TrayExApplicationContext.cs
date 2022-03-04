using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using NBagOfTricks;


namespace TrayEx
{
    /// <summary>Framework for running application as a tray app.</summary>
    public class TrayExApplicationContext : ApplicationContext
    {
        #region Fields
        readonly Icon _icon1 = Properties.Resources.Felipe;
        readonly Icon _icon2 = Properties.Resources.Regina;
        readonly Container _components = new();
        readonly NotifyIcon _notifyIcon;
        readonly Timer _timer = new();
        readonly UtilityDlg _util = new() { Visible = false };
        #endregion

        #region Lifecycle
        /// <summary>Start here.</summary>
        public TrayExApplicationContext()
        {
            ContextMenuStrip ctxm = new();

            ctxm.Items.Add("dialog", null, Menu_Click);
            ctxm.Items.Add("icon", null, Menu_Click);
            ctxm.Items.Add(new ToolStripSeparator());
            ctxm.Items.Add("close", null, Menu_Click);
            ctxm.Opening += ContextMenu_Opening;

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

            Application.Run();

            _notifyIcon.Visible = false; //TODO lingering icons?
        }

        /// <summary>Clean up.</summary>
        /// <param name="disposing"></param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && _components != null)
            {
                _components.Dispose();
            }
            base.Dispose(disposing);
        }

        /// <summary>
        /// If we are presently showing a form, clean it up.
        /// </summary>
        protected override void ExitThreadCore()
        {
            _util.LogMessage("INF", $"Shutting down 2");
            _notifyIcon.Visible = false; // should remove lingering tray icon
            base.ExitThreadCore();
        }
        #endregion

        #region Event handling
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void ContextMenu_Opening(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            // Could add more here.

            e.Cancel = false;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Nicon_MouseClick(object? sender, MouseEventArgs e)
        {
            _util.LogMessage("INF", $"You clicked icon:{e.Button}");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Nicon_Click(object? sender, EventArgs e)
        {
            var args = (MouseEventArgs)e;
            _util.LogMessage("INF", $"You clicked icon:{args.Button}");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Menu_Click(object? sender, EventArgs e)
        {
            var mi = (ToolStripMenuItem)sender!;
            _util.LogMessage("INF", $"You clicked menu:{mi.Text}");

            switch (mi.Text)
            {
                case "dialog":
                    if(!_util.IsDisposed)
                    {
                        _util.Visible = !_util.Visible;
                    }
                    break;

                case "icon":
                    _notifyIcon.Icon = _notifyIcon.Icon == _icon1 ? _icon2 : _icon1;
                    break;

                case "close":
                    // When the exit menu item is clicked, make a call to terminate the ApplicationContext.
                    _notifyIcon.Visible = false; // should remove lingering tray icon
                    ExitThread();
                    //Application.Exit();
                    break;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Nicon_MouseDoubleClick(object? sender, MouseEventArgs e)
        {
            _util.LogMessage("INF", $"You double clicked icon:{e}");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Timer_Tick(object? sender, EventArgs e)
        {
            // Do something interesting.
        }
        #endregion
    }
}
