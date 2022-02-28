using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using NBagOfTricks;



namespace NOrfima
{
    public partial class ClipboardMonitor : Form
    {
        #region Fields
        IntPtr _nextClipboardViewer;

        readonly RichTextBox rtbInfo = new();

        #endregion

        internal class NativeMethods
        {
            [DllImport("User32.dll")]
            internal static extern int SetClipboardViewer(int hWndNewViewer);

            [DllImport("User32.dll", CharSet = CharSet.Auto)]
            internal static extern bool ChangeClipboardChain(IntPtr hWndRemove, IntPtr hWndNewNext);

            [DllImport("user32.dll", CharSet = CharSet.Auto)]
            internal static extern int SendMessage(IntPtr hwnd, int wMsg, IntPtr wParam, IntPtr lParam);

            [DllImport("user32.dll")]
            internal static extern IntPtr GetForegroundWindow();

            [DllImport("user32.dll")]
            internal static extern IntPtr GetForegroundWindowPtr();

            [DllImport("user32.dll", CharSet = CharSet.Unicode)]
            internal static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);

            [DllImport("user32")]
            internal static extern UInt32 GetWindowThreadProcessId(Int32 hWnd, out Int32 lpdwProcessId);
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        public ClipboardMonitor()
        {
            ClientSize = new(800, 500);
            rtbInfo.Dock = DockStyle.Fill;
            rtbInfo.WordWrap = false;
            Controls.Add(rtbInfo);
            //Visible = false;

            _nextClipboardViewer = (IntPtr)NativeMethods.SetClipboardViewer((int)Handle);
        }

        /// <summary>
        /// Clean up.
        /// </summary>
        /// <param name="disposing"></param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                rtbInfo.Dispose();
                NativeMethods.ChangeClipboardChain(Handle, _nextClipboardViewer);
            }

            base.Dispose(disposing);
        }

        /// <summary>
        /// Handle window message.
        /// </summary>
        /// <param name="m"></param>
        protected override void WndProc(ref Message m)
        {
            // Sent to the first window in the clipboard viewer chain when the content of the clipboard changes.
            const int WM_DRAWCLIPBOARD = 0x308;

            // Sent to the first window in the clipboard viewer chain when a window is being removed from the chain.
            const int WM_CHANGECBCHAIN = 0x030D;

            // Sent when the contents of the clipboard have changed.
            //const int WM_CLIPBOARDUPDATE = 0x031D;

            int hres = 0;

            switch (m.Msg)
            {
                case WM_DRAWCLIPBOARD:
                    IDataObject? dobj = null;

                    try
                    {
                        dobj = Clipboard.GetDataObject();
                    }
                    catch (Exception ex) // TODO ExternalException?
                    {
                        LogMessage("ERR", ex.ToString());
                    }

                    try
                    {
                        // Do something...
                        if (dobj is not null)
                        {
                            LogMessage("INF", $"============= clip op =================");
                            ProcessObject(dobj);

                            if (Clipboard.ContainsText())
                            {
                            }
                            // bool ContainsAudio();
                            // bool ContainsData(string format);
                            // bool ContainsFileDropList();
                            // bool ContainsImage();
                            // bool ContainsText(TextDataFormat format);
                            // bool ContainsText();
                        }
                    }
                    catch (Exception ex) // TODO?
                    {
                        LogMessage("ERR", ex.ToString());
                    }


                    //ClipboardChanged?.Invoke(this, new ClipboardChangedEventArgs(dataObj));

                    // Pass along to the next in the chain.
                    hres = NativeMethods.SendMessage(_nextClipboardViewer, m.Msg, m.WParam, m.LParam);

                    break;


                case WM_CHANGECBCHAIN:
                    if (m.WParam == _nextClipboardViewer)
                    {
                        _nextClipboardViewer = m.LParam;
                    }
                    else
                    {
                        hres = NativeMethods.SendMessage(_nextClipboardViewer, m.Msg, m.WParam, m.LParam);
                    }
                    break;

                default:
                    base.WndProc(ref m);
                    break;

            }

            if(hres > 0)
            {
                LogMessage("ERR", $"hres:{hres}");
            }
        }

        /// <summary>
        /// Do something with the clipboard contents. For now, just whow it.
        /// </summary>
        /// <param name="dobj"></param>
        void ProcessObject(IDataObject? dobj)
        {
            if (dobj is not null)
            {
                // Assemble info.
                var dtypes = dobj.GetFormats();

               // var appName = GetApplicationName();
                IntPtr hwnd = NativeMethods.GetForegroundWindow();

                uint hres = NativeMethods.GetWindowThreadProcessId((int)hwnd, out int processID);

                var procName = Process.GetProcessById(processID).ProcessName;
                var appPath = Process.GetProcessById(processID).MainModule!.FileName;
                var appName = Path.GetFileName(appPath);

                var winTitle = "Nothing here";
                const int capacity = 256;
                StringBuilder content = new(capacity);
                IntPtr handle = NativeMethods.GetForegroundWindow();

                if (NativeMethods.GetWindowText(handle, content, capacity) > 0)
                {
                    winTitle = content.ToString();
                }

                LogMessage("INF", $"dtypes:{string.Join(",", dtypes)}");
                LogMessage("INF", $"appName:{appName} procName:{procName} winTitle:{winTitle}");
            }
        }

        /// <summary>
        /// Just for debugging.
        /// </summary>
        /// <param name="cat"></param>
        /// <param name="msg"></param>
        void LogMessage(string cat, string msg)
        {
            int catSize = 3;
            cat = cat.Length >= catSize ? cat.Left(catSize) : cat.PadRight(catSize);
            string s = $"{DateTime.Now:mm\\:ss\\.fff} {cat} {msg}{Environment.NewLine}";
            rtbInfo.AppendText(s);
        }
    }
}
