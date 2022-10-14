using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Drawing.Design;
using System.Text.Json.Serialization;
using System.Text.Json;
using Ephemera.NBagOfTricks;
using Ephemera.NBagOfUis;

// TODO persist clip data.


namespace Ephemera.ClipboardEx
{
    #region Types
    /// <summary>For internal management.</summary>
    public enum ClipType { Empty, PlainText, RichText, FileList, Image, Other };
    #endregion

    /// <summary>
    /// - Handles all interactions at the Clipboard.XXX() API level.
    /// - Hooks keyboard to intercept magic paste key.
    /// </summary>
    public sealed partial class ClipboardEx : Form
    {
        #region Types
        /// <summary>One handled clipboard API message.</summary>
        record MsgSpec(string Name, Func<Message, uint> Handler, string Description);

        /// <summary>One entry in the collection.</summary>
        class Clip
        {
            public object? Data { get; set; } = null;
            public ClipType Ctype { get; set; } = ClipType.Empty;
            public string Text { get; set; } = "";
            public Bitmap? Bitmap { get; set; } = null;
            public string OrigApp { get; set; } = "Unknown";
            public string OrigTitle { get; set; } = "Unknown";
            public override string ToString() => $"Ctype:{Ctype}";
        }
        #endregion

        #region Fields
        /// <summary>The settings.</summary>
        readonly UserSettings _settings;

        /// <summary>Next in line for clipboard  notification.</summary>
        IntPtr _nextCb = IntPtr.Zero;

        /// <summary>Handle to the LL hook. Needed to unhook and call the next hook in the chain.</summary>
        readonly IntPtr _hhook = IntPtr.Zero;

        /// <summary>All handled clipboard API messages.</summary>
        readonly Dictionary<int, MsgSpec> _clipboardMessages = new();

        /// <summary>All clips in the collection.</summary>
        readonly LinkedList<Clip> _clips = new();

        /// <summary>All clip displays.</summary>
        readonly List<ClipDisplay> _displays = new();

        /// <summary>Key status.</summary>
        bool _letterPressed = false;

        /// <summary>Key status.</summary>
        bool _winPressed = false;

        /// <summary>Manage resources.</summary>
        bool _disposed;
        #endregion

        #region Constants
        const int MAX_CLIPS = 10;
        // Windows messages.
        const int WM_KEYDOWN = 0x100;
        //const int WM_KEYUP = 0x101;
        const int WM_SYSKEYDOWN = 0x104; // when the user presses the F10 key (menu bar) or holds down the ALT key and then presses another key
        //const int WM_SYSKEYUP = 0x105; // when the user releases a key that was pressed while the ALT key was held down
        const int WM_DRAWCLIPBOARD = 0x0308;
        const int WM_CHANGECBCHAIN = 0x030D;
        const int WM_CLIPBOARDUPDATE = 0x031D;
        const int WM_DESTROYCLIPBOARD = 0x0307;
        const int WM_ASKCBFORMATNAME = 0x030C;
        const int WM_CLEAR = 0x0303;
        const int WM_COPY = 0x0301;
        const int WM_CUT = 0x0300;
        const int WM_PASTE = 0x0302;
        #endregion

        #region Interop Methods
        internal class NativeMethods
        {
            [DllImport("User32.dll")]
            internal static extern IntPtr SetClipboardViewer(IntPtr hWndNewViewer);

            [DllImport("User32.dll", CharSet = CharSet.Auto)]
            internal static extern bool ChangeClipboardChain(IntPtr hWndRemove, IntPtr hWndNewNext);

            [DllImport("user32.dll", CharSet = CharSet.Auto)]
            internal static extern int SendMessage(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);

            [DllImport("user32.dll")]
            internal static extern IntPtr GetForegroundWindow();

            [DllImport("user32.dll", CharSet = CharSet.Unicode)]
            internal static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

            [DllImport("user32.dll")]
            internal static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

            [DllImport("user32.dll")]
            internal static extern void keybd_event(byte bVk, byte bScan, int dwFlags, int dwExtraInfo);

            [DllImport("user32.dll")]
            internal static extern int CallNextHookEx(IntPtr idHook, int nCode, int wParam, ref KBDLLHOOKSTRUCT lParam);

            [DllImport("user32.dll")]
            internal static extern IntPtr SetWindowsHookEx(HookType hookType, HookProc lpfn, IntPtr hMod, uint dwThreadId);

            [DllImport("user32.dll")]
            internal static extern bool UnhookWindowsHookEx(IntPtr hInstance);

            [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
            internal static extern IntPtr GetModuleHandle(string lpModuleName);
        }
        #endregion

        #region Interop Definitions
        [Flags]
        public enum KBDLLHOOKSTRUCTFlags : uint
        {
            LLKHF_EXTENDED = 0x01,
            LLKHF_INJECTED = 0x10,
            LLKHF_ALTDOWN = 0x20,
            LLKHF_UP = 0x80,
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct KBDLLHOOKSTRUCT
        {
            public uint vkCode;    // A virtual-key code in the range 1 to 254.
            public uint scanCode;  // A hardware scan code for the key.
            public KBDLLHOOKSTRUCTFlags flags;
            public uint time;
            public UIntPtr dwExtraInfo;
        }

        /// <summary> https://www.pinvoke.net/default.aspx/Enums/HookType.html </summary>
        // Global hooks are not supported in the.NET Framework except for WH_KEYBOARD_LL and WH_MOUSE_LL.
        public enum HookType : int
        {
            WH_KEYBOARD_LL = 13,
            WH_MOUSE_LL = 14
        }

        /// <summaryDefines the callback for the hook. Apparently you can have multiple typed overloads.</summary>
        internal delegate int HookProc(int code, int wParam, ref KBDLLHOOKSTRUCT lParam);
        #endregion

        #region Lifecycle
        /// <summary>
        /// Constructor.
        /// </summary>
        public ClipboardEx()
        {
            InitializeComponent();

            string appDir = MiscUtils.GetAppDataDir("ClipboardEx", "Ephemera");
            _settings = (UserSettings)SettingsCore.Load(appDir, typeof(UserSettings));

            Visible = _settings.Debug;

            // Init clip displays.
            int x = 5;
            int y = 5;
            for (int i = 0; i < MAX_CLIPS; i++)
            {
                ClipDisplay cd = new() { Location = new Point(x, y), Id = i };
                _displays.Add(cd);
                Controls.Add(cd);
                cd.ClipEvent += Cd_ClipEvent;
                //x = cd.Right + 5;
                y = cd.Bottom + 5;
            }

            UpdateClipDisplays();

            // Clean me up.
            var borderWidth = (Width - ClientSize.Width) / 2;
            //Width = x + borderWidth * 2;
            Height = y + borderWidth * 2 + 55;

            if (!_settings.Debug)
            {
                //Height = h + 55;
                Width = _displays[0].Right + borderWidth * 2 + 5;
            }

            // Init controls.
            tvInfo.MatchColors.Add("ERR", Color.Pink);
            tvInfo.MatchColors.Add("DBG", Color.LightGreen);
            tvInfo.BackColor = Color.Cornsilk;

            if(_settings.Debug)
            {
                rtbText.LoadFile(@"..\..\\ex.rtf");
            }

            btnClear.Click += (_, __) => tvInfo.Clear();
            lblLetter.Text = _settings.KeyTrigger.ToString();

            _nextCb = NativeMethods.SetClipboardViewer(Handle);

            // HL messages of interest.
            _clipboardMessages = new()
            {
                { WM_DRAWCLIPBOARD, new("WM_DRAWCLIPBOARD", CbDraw, "Sent to the first window in the clipboard viewer chain when the content of the clipboard changes aka copy/cut.") },
                { WM_CHANGECBCHAIN, new("WM_CHANGECBCHAIN", CbChange, "Sent to the first window in the clipboard viewer chain when a window is being removed from the chain.") },
                { WM_CLIPBOARDUPDATE, new("WM_CLIPBOARDUPDATE", CbDefault, "Sent when the contents of the clipboard have changed.") },
                { WM_DESTROYCLIPBOARD, new("WM_DESTROYCLIPBOARD", CbDefault, "Sent to the clipboard owner when a call to the EmptyClipboard function empties the clipboard.") },
                { WM_ASKCBFORMATNAME, new("WM_ASKCBFORMATNAME", CbDefault, "Sent to the clipboard owner by a clipboard viewer window to request the name of a CF_OWNERDISPLAY clipboard format.") },
                { WM_CLEAR, new("WM_CLEAR", CbDefault, "Clear") },
                { WM_COPY, new("WM_COPY", CbDefault, "Copy") },
                { WM_CUT, new("WM_CUT", CbDefault, "Cut") },
                { WM_PASTE, new("WM_PASTE", CbDefault, "Paste") }
            };

            // Init LL keyboard hook.
            using Process process = Process.GetCurrentProcess();
            using ProcessModule? module = process.MainModule;
            // hMod: Handle to the DLL containing the hook procedure pointed to by the lpfn parameter. The hMod parameter must be set
            //   to NULL if the dwThreadId parameter specifies a thread created by the current process and if the hook procedure is
            //   within the code associated with the current process.
            // dwThreadId: Specifies the identifier of the thread with which the hook procedure is to be associated.If this parameter is
            //   zero, the hook procedure is associated with all existing threads running in the same desktop as the calling thread.
            IntPtr hModule = NativeMethods.GetModuleHandle(module!.ModuleName!);
            _hhook = NativeMethods.SetWindowsHookEx(HookType.WH_KEYBOARD_LL, KeyboardHookProc, hModule, 0);

            // Paste test.
            //_ticks = 5;
            //timer1.Tick += (_, __) => { if (_ticks-- > 0) { Clipboard.SetText($"XXXXX{_ticks}"); DoPaste(); } };
            //timer1.Enabled = true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="e"></param>
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            _settings.Save();
            base.OnFormClosing(e);
        }

        /// <summary>
        /// Override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources.
        /// </summary>
        ~ClipboardEx()
        {
            Dispose(false);
        }

        /// <summary>
        /// Boilerplate.
        /// </summary>
        public new void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
            base.Dispose();
        }

        /// <summary>
        /// Boilerplate.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // called via myClass.Dispose(). 
                    // OK to use any private object references
                    // Dispose managed state (managed objects).
                    components.Dispose();
                }

                // Release unmanaged resources.
                // Set large fields to null.
                NativeMethods.ChangeClipboardChain(Handle, _nextCb);
                NativeMethods.UnhookWindowsHookEx(_hhook);

                _disposed = true;
            }

            base.Dispose(disposing);
        }
        #endregion

        #region Clip Management
        /// <summary>
        /// 
        /// </summary>
        void UpdateClipDisplays()
        {
            // Remove tail.
            while (_clips.Count > MAX_CLIPS)
            {
                _clips.RemoveLast();
            }

            // Fill UI with what we have.
            for (int i = 0; i < MAX_CLIPS; i++)
            {
                var ds = _displays[i];
                ds.Show();

                if (i < _clips.Count)
                {
                    var clip = _clips.ElementAt(i);

                    switch (clip.Ctype)
                    {
                        case ClipType.Empty:
                            ds.SetEmpty();
                            //ds.Visible = _settings.Debug;
                            break;

                        case ClipType.Image:
                            ds.SetImage(clip.Bitmap!, _settings.FitImage);
                            break;

                        case ClipType.Other:
                            ds.SetOther(clip.Data!.ToString()!);
                            break;

                        case ClipType.PlainText:
                        case ClipType.RichText:
                        case ClipType.FileList:
                            ds.SetText(clip.Ctype, clip.Text);
                            break;
                    }
                }
                else
                {
                    ds.SetEmpty();
                    //ds.Visible = _settings.Debug;
                }
            }
        }
        #endregion

        #region Windows Message Processing - Cut/Copy/Paste
        /// <summary>
        /// Handle window messages.
        /// </summary>
        /// <param name="m"></param>
        protected override void WndProc(ref Message m)
        {
            uint ret = 0;

            if (_clipboardMessages is not null && _clipboardMessages.ContainsKey(m.Msg))
            {
                MsgSpec sp = _clipboardMessages[m.Msg];
                Tell($"WndProc message {sp.Name} HWnd:{m.HWnd} Msg:{m.Msg} WParam:{m.WParam} LParam:{m.LParam} ");
                
                // Call handler.
                ret = sp.Handler(m);
                if (ret > 0)
                {
                    Tell($"ERR WndProc handler {sp.Name} ret:0X{ret:X}");
                }
            }
            else
            {
                // Ignore, pass along.
                base.WndProc(ref m);
            }
        }

        /// <summary>
        /// Process the clipboard draw message because contents have changed.
        /// </summary>
        /// <param name="m"></param>
        /// <returns></returns>
        uint CbDraw(Message m)
        {
            uint ret;

            int retries = 5;

            while(retries > 0)
            {
                try
                {
                    IDataObject? dobj = Clipboard.GetDataObject();

                    // Do something...
                    if (dobj is not null)
                    {
                        // Info about the source window.
                        IntPtr hwnd = NativeMethods.GetForegroundWindow();
                        ret = NativeMethods.GetWindowThreadProcessId(hwnd, out uint processID);
                        var procName = Process.GetProcessById((int)processID).ProcessName;
                        var appPath = Process.GetProcessById((int)processID).MainModule!.FileName;
                        var appName = Path.GetFileName(appPath);
                        StringBuilder title = new(100);
                        int res = NativeMethods.GetWindowText(hwnd, title, 100);

                        if(res > 0)
                        {
                            Tell($"CbDraw COPY appName:{appName} procName:{procName} title:{title}");

                            // Data type info.
                            var dtypes = dobj.GetFormats();
                            //var stypes = $"CbDraw dtypes:{string.Join(",", dtypes)}";
                            //Tell("INF", stypes);

                            Clip clip = new()
                            {
                                Ctype = ClipType.Other,
                                Data = Clipboard.GetDataObject(),
                                OrigApp = appName ?? "Unknown",
                                OrigTitle = title.ToString()
                            };

                            if (Clipboard.ContainsText())
                            {
                                // Is it rich text? dtypes: Rich Text Format, Rich Text Format Without Objects, RTF As Text.
                                var ctype = ClipType.PlainText;
                                foreach(var dt in dtypes)
                                {
                                    if(dt.Contains("Rich Text Format") || dt.Contains("RTF"))
                                    {
                                        ctype = ClipType.RichText;
                                        break;
                                    }
                                }

                                clip.Ctype = ctype;
                                clip.Text = Clipboard.GetText();
                            }
                            else if (Clipboard.ContainsFileDropList())
                            {
                                clip.Ctype = ClipType.FileList;
                                clip.Text = string.Join(Environment.NewLine, Clipboard.GetFileDropList());
                            }
                            else if (Clipboard.ContainsImage())
                            {
                                clip.Ctype = ClipType.Image;
                                clip.Bitmap = (Bitmap)Clipboard.GetImage();
                            }
                            else
                            {
                                // Something else, don't try to show it.
                            }

                            _clips.AddFirst(clip);
                            UpdateClipDisplays();
                        }
                        else
                        {
                            Tell($"ERR CbDraw res:{res}");
                        }
                    }
                    else
                    {
                        Tell($"ERR CbDraw GetDataObject() is null");
                    }

                    retries = 0;
                }
                catch (ExternalException ex)
                {
                    // TODO retry: Data could not be retrieved from the Clipboard.
                    // This typically occurs when the Clipboard is being used by another process.
                    Tell($"ERR CbDraw WM_DRAWCLIPBOARD ExternalException:{ex}");
                    retries--;
                    Thread.Sleep(50);
                }
                catch (Exception ex)
                {
                    Tell($"ERR CbDraw WM_DRAWCLIPBOARD Exception:{ex}");
                    retries = 0;
                }
            }

            // Pass along to the next in the chain.
            ret = (uint)NativeMethods.SendMessage(_nextCb, m.Msg, m.WParam, m.LParam);

            return ret;
        }

        /// <summary>
        /// Process the clipboard change message. Update our link.
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
        /// Process all other messages. Doesn't do anything right now other than call base.
        /// </summary>
        /// <param name="m"></param>
        /// <returns></returns>
        uint CbDefault(Message m)
        {
            uint ret = 0;
            base.WndProc(ref m);
            return ret;
        }
        #endregion

        #region New Paste Functions
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Cd_ClipEvent(object? sender, ClipDisplay.ClipEventArgs e)
        {
            if (sender is not null)
            {
                // Paste this selection and move it to the front.

                var cd = (ClipDisplay)sender;

                switch (e.EventType)
                {
                    case ClipDisplay.ClipEventType.Click:
                        Tell("!!! Got a click");
                        int i = cd.Id;
                        if (i >= 0 && i < _clips.Count)
                        {
                            Clip clip = _clips.ElementAt(i);
                            Clipboard.SetDataObject(clip.Data);
                            DoPaste();
                            // Push to head of class.
                            _clips.Remove(clip);
                            _clips.AddFirst(clip);
                            UpdateClipDisplays();
                            Visible = _settings.Debug;
                        }
                        else
                        {
                            // error?
                        }
                        break;
                }

                //  Also could do left, right, delete, etc.
            }
        }

        /// <summary>
        /// Send paste to focus window.
        /// </summary>
        public void DoPaste()
        {
            // TODO but the focus is me now!
            
            IntPtr hwnd = NativeMethods.GetForegroundWindow();
            uint tid = NativeMethods.GetWindowThreadProcessId(hwnd, out uint lpdwProcessId);
            var p = Process.GetProcessById((int)lpdwProcessId);
            Tell($"FileName:{p.MainModule!.FileName} pid:{ lpdwProcessId} tid:{tid}");

            // This does work. Virtual keycodes from https://docs.microsoft.com/en-us/windows/win32/inputdev/virtual-key-codes
            byte vkey = 0x56; // 'v'
            byte ctrl = 0x11;
            int KEYEVENTF_KEYUP = 0x0002;
            NativeMethods.keybd_event(ctrl, 0, 0, 0);
            NativeMethods.keybd_event(vkey, 0, 0, 0);
            NativeMethods.keybd_event(ctrl, 0, KEYEVENTF_KEYUP, 0);
            NativeMethods.keybd_event(vkey, 0, KEYEVENTF_KEYUP, 0);

            // Note that this doesn't work, which makes sense.
            //NativeMethods.SendMessage(hwnd, 0x0302, IntPtr.Zero, IntPtr.Zero); // NativeMethods.WM_PASTE
        }

        /// <summary>
        /// Low level hook function. Sniffs for magic key.
        /// </summary>
        /// <param name="code">If less than zero, pass the message to the CallNextHookEx function without further processing.</param>
        /// <param name="wParam">One of the following messages: WM_KEYDOWN, WM_KEYUP, WM_SYSKEYDOWN, or WM_SYSKEYUP.</param>
        /// <param name="lParam">Pointer to a KBDLLHOOKSTRUCT structure.</param>
        /// <returns></returns>
        public int KeyboardHookProc(int code, int wParam, ref KBDLLHOOKSTRUCT lParam)
        {
            if (code >= 0)
            {
                Keys key = (Keys)lParam.vkCode;

                Tell($"KeyboardHookProc code:{code} wParam:{wParam} key:{key} scancode:{lParam.scanCode}");

                if (code >= 0)
                {
                    // Update statuses.
                    bool pressed = wParam == WM_KEYDOWN || wParam == WM_SYSKEYDOWN;
                    //bool up = wParam == WM_KEYUP || wParam == WM_SYSKEYUP;

                    if (key == Keys.LWin || key == Keys.RWin)
                    {
                        _winPressed = pressed;
                    }

                    if (key == _settings.KeyTrigger)
                    {
                        _letterPressed = pressed;
                    }

                    bool match = _winPressed && _letterPressed;

                    // Diagnostics.
                    lblWin.BackColor = _winPressed ? _settings.ControlColor : Color.Transparent;
                    lblLetter.BackColor = _letterPressed ? _settings.ControlColor : Color.Transparent;
                    lblMatch.BackColor = match ? _settings.ControlColor : Color.Transparent;

                    if (match)
                    {
                        // show UI;
                        Visible = true;
                    }
                }
            }

            return NativeMethods.CallNextHookEx(_hhook, code, wParam, ref lParam);
        }
        #endregion

        #region Debug
        /// <summary>
        /// Debug stuff.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Debug_Click(object sender, EventArgs e)
        {

        }

        /// <summary>
        /// Just for debugging.
        /// </summary>
        /// <param name="msg"></param>
        void Tell(string msg)
        {
            string s = $"{DateTime.Now:mm\\:ss\\.fff} {msg}";
            tvInfo.AppendLine(s);
        }
        #endregion
    }

    [Serializable]
    public sealed class UserSettings : SettingsCore
    {
        #region Persisted editable properties
        [DisplayName("Control Color")]
        [Description("Pick whatever you like.")]
        [Browsable(true)]
        [JsonConverter(typeof(JsonColorConverter))]
        public Color ControlColor { get; set; } = Color.LimeGreen;

        [DisplayName("Fit Image")]
        [Description("Fit to space or clip.")]
        [Browsable(true)]
        public bool FitImage { get; set; } = true;

        [DisplayName("Key Trigger")]
        [Description("Windows key + this opens clip viewer")]
        [Browsable(true)]
        public Keys KeyTrigger { get; set; } = Keys.Z;

        [DisplayName("Debug Mode")]
        [Description("Shows more stuff.")]
        [Browsable(true)]
        public bool Debug { get; set; } = false;
        #endregion
    }
}