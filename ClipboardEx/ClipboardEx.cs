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

// The clipboard owner is the window associated with the information on the clipboard. A window becomes the clipboard owner when it
// places data on the clipboard, specifically, when it calls the EmptyClipboard function. The window remains the clipboard owner until
// it is closed or another window empties the clipboard.

// When the clipboard is emptied, the clipboard owner receives a WM_DESTROYCLIPBOARD message. Following are some reasons why a window
// might process this message:
//  - The window delayed rendering of one or more clipboard formats. In response to the WM_DESTROYCLIPBOARD message, the window might
//    free resources it had allocated in order to render data on request. For more information about the rendering of data, see Delayed Rendering.
//  - The window placed data on the clipboard in a private clipboard format.The data for private clipboard formats is not freed by the
//    system when the clipboard is emptied. Therefore, the clipboard owner should free the data upon receiving the WM_DESTROYCLIPBOARD message.
//    For more information about private clipboard formats, see Clipboard Formats.
//  - The window placed data on the clipboard using the CF_OWNERDISPLAY clipboard format.In response to the WM_DESTROYCLIPBOARD message,
//    the window might free resources it had used to display information in the clipboard viewer window. For more information about this
//    alternative format, see Owner Display Format.


namespace ClipboardEx
{
    public partial class ClipboardEx : Form
    {
        #region Fields
        IntPtr _nextCb;
        #endregion

        #region Windows Constants
        // Sent to the first window in the clipboard viewer chain when the content of the clipboard changes.
        const int WM_DRAWCLIPBOARD = 0x308;

        // Sent to the first window in the clipboard viewer chain when a window is being removed from the chain.
        const int WM_CHANGECBCHAIN = 0x030D;

        // Sent when the contents of the clipboard have changed.
        const int WM_CLIPBOARDUPDATE = 0x031D;

        const int WM_DESTROYCLIPBOARD = 0x0307;

        // Sent to the clipboard owner by a clipboard viewer window to request the name of a CF_OWNERDISPLAY clipboard format.
        const int WM_ASKCBFORMATNAME = 0x030C;

        const int WM_CLEAR = 0x0303;
        const int WM_COPY = 0x0301;
        const int WM_CUT = 0x0300;
        const int WM_PASTE = 0x0302;
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

        #region Lifecycle
        /// <summary>
        /// Constructor.
        /// </summary>
        public ClipboardEx()
        {
            //Visible = false;

            InitializeComponent();

            _nextCb = (IntPtr)NativeMethods.SetClipboardViewer((int)Handle);
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
        /// Handle window message.
        /// </summary>
        /// <param name="m"></param>
        protected override void WndProc(ref Message m)
        {
            switch (m.Msg)
            {
                case WM_CLEAR:
                    LogMessage("INF", $"WM_CLEAR");
                    break;

                case WM_COPY:
                    LogMessage("INF", $"WM_COPY");
                    break;

                case WM_CUT:
                    LogMessage("INF", $"WM_CUT");
                    break;

                case WM_PASTE:
                    LogMessage("INF", $"WM_PASTE");
                    break;


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
                    catch (Exception ex)
                    {
                        LogMessage("ERR", $"WM_DRAWCLIPBOARD2:{ex}");
                    }

                    // Pass along to the next in the chain.
                    int hres1 = NativeMethods.SendMessage(_nextCb, m.Msg, m.WParam, m.LParam);
                    if(hres1 > 0)
                    {
                        LogMessage("ERR", $"WM_DRAWCLIPBOARD3 hres:{hres1}");
                    }
                    break;

                case WM_CHANGECBCHAIN:
                    if (m.WParam == _nextCb)
                    {
                        _nextCb = m.LParam;
                    }
                    else
                    {
                        int hres2 = NativeMethods.SendMessage(_nextCb, m.Msg, m.WParam, m.LParam);
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
