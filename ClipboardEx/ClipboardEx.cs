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



// TODO multi slot clipboard.
//  - ST:  { "keys": ["ctrl+k", "ctrl+v"], "command": "paste_from_history" },
//  - Win: To get to your clipboard history at any time, press Windows logo key + V
//
// Clipboard methods: https://docs.microsoft.com/en-us/dotnet/api/system.windows.clipboard?view=windowsdesktop-6.0
// DataFormats: https://docs.microsoft.com/en-us/dotnet/api/system.windows.dataformats?view=windowsdesktop-6.0
// IDataObject: https://docs.microsoft.com/en-us/dotnet/api/system.windows.idataobject?view=windowsdesktop-6.0
//
// A window should use the clipboard when cutting, copying, or pasting data. A window places data on the clipboard for
//  cut and copy operations and retrieves data from the clipboard for paste operations.
// 
// To place information on the clipboard, a window first clears any previous clipboard content by using the EmptyClipboard function.
// This function sends the WM_DESTROYCLIPBOARD message to the previous clipboard owner, frees resources associated with data on
// the clipboard, and assigns clipboard ownership to the window that has the clipboard open.To find out which window owns the
// clipboard, call the GetClipboardOwner function.
// 
// After emptying the clipboard, the window places data on the clipboard in as many clipboard formats as possible, ordered from
// the most descriptive clipboard format to the least descriptive. For each format, the window calls the SetClipboardData function,
// specifying the format identifier and a global memory handle. The memory handle can be NULL, indicating that the window renders
// the data on request. For more information, see Delayed Rendering.
// 
// To examine the formats of data on the system Clipboard, call GetFormats on the data object returned by this method.
// To retrieve data from the system Clipboard, call GetData and specify the desired data format.


namespace ClipboardEx
{
    public partial class ClipboardEx : Form
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
        public ClipboardEx()
        {
            Text = "ClipboardEx";
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

            switch (m.Msg)
            {
                case WM_DRAWCLIPBOARD:
                    IDataObject? dobj = null;

                    try
                    {
                        dobj = Clipboard.GetDataObject();
                    }
                    catch (Exception ex)
                    {
                        LogMessage("ERR", $"WM_DRAWCLIPBOARD1:{ex}");
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
                    catch (Exception ex)
                    {
                        LogMessage("ERR", $"WM_DRAWCLIPBOARD2:{ex}");
                    }

                    // Pass along to the next in the chain.
                    int hres1 = NativeMethods.SendMessage(_nextClipboardViewer, m.Msg, m.WParam, m.LParam);
                    if(hres1 > 0)
                    {
                        LogMessage("ERR", $"WM_DRAWCLIPBOARD3 hres:{hres1}");
                    }
                    break;

                case WM_CHANGECBCHAIN:
                    if (m.WParam == _nextClipboardViewer)
                    {
                        _nextClipboardViewer = m.LParam;
                    }
                    else
                    {
                        int hres2 = NativeMethods.SendMessage(_nextClipboardViewer, m.Msg, m.WParam, m.LParam);
                        if(hres2 > 0)
                        {
                            LogMessage("ERR", $"WM_CHANGECBCHAIN1 hres:{hres2}");
                        }
                    }
                    break;

                default:
                    base.WndProc(ref m);
                    break;
            }
        }

        /// <summary>
        /// Do something with the clipboard contents. For now, just show it.
        /// </summary>
        /// <param name="dobj"></param>
        void ProcessObject(IDataObject? dobj)
        {
            if (dobj is not null)
            {
                // Assemble info.
                var dtypes = dobj.GetFormats();

                IntPtr hwnd = NativeMethods.GetForegroundWindow();

                uint hres = NativeMethods.GetWindowThreadProcessId((int)hwnd, out int processID);
                if (hres > 0)
                {
                    LogMessage("ERR", $"ProcessObject hres:{hres}");
                }

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
