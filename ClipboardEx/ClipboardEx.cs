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


namespace ClipboardEx
{
    public partial class ClipboardEx : Form
    {
        #region Fields
        IntPtr _nextCb;

        record MsgSpec(string Name, Func<Message, uint> Handler, string Description);

        readonly Dictionary<int, MsgSpec> _messages;
        #endregion

        #region Interop
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
        #endregion

        #region Lifecycle
        /// <summary>
        /// Constructor.
        /// </summary>
        public ClipboardEx()
        {
            //Visible = false;

            InitializeComponent();

            _nextCb = (IntPtr)NativeMethods.SetClipboardViewer((int)Handle);

            _messages = new()
            {
                { 0x0308, new("WM_DRAWCLIPBOARD",    CbDraw,    "Sent to the first window in the clipboard viewer chain when the content of the clipboard changes.") },
                { 0x030D, new("WM_CHANGECBCHAIN",    CbChange,  "Sent to the first window in the clipboard viewer chain when a window is being removed from the chain.") },
                { 0x031D, new("WM_CLIPBOARDUPDATE",  CbDefault, "Sent when the contents of the clipboard have changed.") },
                { 0x0307, new("WM_DESTROYCLIPBOARD", CbDefault, "Sent to the clipboard owner when a call to the EmptyClipboard function empties the clipboard.") },
                { 0x030C, new("WM_ASKCBFORMATNAME",  CbDefault, "Sent to the clipboard owner by a clipboard viewer window to request the name of a CF_OWNERDISPLAY clipboard format.") },
                { 0x0303, new("WM_CLEAR",            CbDefault, "Clear") },
                { 0x0301, new("WM_COPY",             CbDefault, "Copy") },
                { 0x0300, new("WM_CUT",              CbDefault, "Cut") },
                { 0x0302, new("WM_PASTE",            CbDefault, "Paste") }
            };
        }

        /// <summary>
        /// Clean up.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void ClipboardEx_FormClosing(object sender, FormClosingEventArgs e)
        {
            NativeMethods.ChangeClipboardChain(Handle, _nextCb);
        }
        #endregion

        /// <summary>
        /// Handle window message. TODO play with some more.
        /// </summary>
        /// <param name="m"></param>
        protected override void WndProc(ref Message m)
        {
            uint ret = 0;

            if(_messages is not null && _messages.ContainsKey(m.Msg))
            {
                MsgSpec sp = _messages[m.Msg];

                ret = sp.Handler(m);

                if(ret > 0)
                {
                    LogMessage("ERR", $"{sp.Name} ret:0X{ret:X}");
                }
                else
                {
                    LogMessage("INF", $"{sp.Name}");
                }
            }
            else
            {
                // Ignore.
                base.WndProc(ref m);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="m"></param>
        /// <returns></returns>
        uint CbDraw(Message m)
        {
            uint ret;

            try
            {
                IDataObject? dobj = Clipboard.GetDataObject();

                // Do something...
                if (dobj is not null)
                {
                    LogMessage("INF", $"============= clip op =================");

                    ret = ProcessObject(dobj);

                    if (Clipboard.ContainsText())
                    {
                        var t = Clipboard.GetText();
                    }
                    else if (Clipboard.ContainsFileDropList())
                    {
                        var t = Clipboard.GetFileDropList();
                    }
                    else if (Clipboard.ContainsImage())
                    {
                        var t = Clipboard.GetImage();
                    }
                    else
                    {
                        // Don't care?
                        //bool ContainsAudio();
                        //Stream GetAudioStream();
                    }

                    //void SetFileDropList(StringCollection filePaths);
                    //void SetImage(Image image);
                    //void SetText(string text);
                }
            }
            catch (ExternalException ex)
            {
                // TODO retry: Data could not be retrieved from the Clipboard. This typically occurs when the Clipboard is being used by another process.
                LogMessage("ERR", $"WM_DRAWCLIPBOARD ExternalException:{ex}");
            }
            catch (Exception ex)
            {
                LogMessage("ERR", $"WM_DRAWCLIPBOARD Exception:{ex}");
            }

            // Pass along to the next in the chain.
            ret = (uint)NativeMethods.SendMessage(_nextCb, m.Msg, m.WParam, m.LParam);

            return ret;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="m"></param>
        /// <returns></returns>
        uint CbChange(Message m)
        {
            uint ret = 0;

            if (m.WParam == _nextCb)
            {
                // Fix our copy of the chain.
                _nextCb = m.LParam;
            }
            else
            {
                // Just pass along to the next in the chain.
                ret = (uint)NativeMethods.SendMessage(_nextCb, m.Msg, m.WParam, m.LParam);
            }

            return ret;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="m"></param>
        /// <returns></returns>
        uint CbDefault(Message m)
        {
            uint ret = 0;
            base.WndProc(ref m);
            return ret;
        }

        /// <summary>
        /// Do something with the clipboard contents. For now, just show it.
        /// </summary>
        /// <param name="dobj"></param>
        uint ProcessObject(IDataObject? dobj)
        {
            uint ret = 0;

            if (dobj is not null)
            {
                // Assemble info.
                var dtypes = dobj.GetFormats();

                IntPtr hwnd = NativeMethods.GetForegroundWindow();

                NativeMethods.GetWindowThreadProcessId((int)hwnd, out int processID);

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

                //LogMessage("INF", $"dtypes:{string.Join(",", dtypes)}");
                LogMessage("INF", $"appName:{appName} procName:{procName} winTitle:{winTitle}");
            }

            return ret;
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
