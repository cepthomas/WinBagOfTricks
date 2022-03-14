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
using System.Drawing.Design;
using System.Text.Json.Serialization;
using System.Text.Json;
using NBagOfTricks;
using NBagOfUis;


namespace ClipboardEx
{
    /// <summary>
    /// - Handles all interactions at the Clipboard.XXX() API level.
    /// - Hooks keyboard to intercept magic paste key.
    /// </summary>
    public partial class ClipboardEx : Form
    {
        #region Fields
        /// <summary>Next in line for clipboard  notification.</summary>
        IntPtr _nextCb = IntPtr.Zero;

        /// <summary>All handled clipboard API messages.</summary>
        record MsgSpec(string Name, Func<Message, uint> Handler, string Description);
        readonly Dictionary<int, MsgSpec> _clipboardMessages;

        /// <summary>Handle to the LL hook. Needed to unhook and call the next hook in the chain.</summary>
        readonly IntPtr _hhook = IntPtr.Zero;

        /// <summary>The magic key to paste. TODO Make configurable.</summary>
        readonly Keys _keyTrigger = Keys.Control | Keys.Shift | Keys.V;

        /// <summary>Current state.</summary>
        bool _controlPressed = false;

        /// <summary>Current state.</summary>
        bool _shiftPressed = false;

        /// <summary>Current state.</summary>
        bool _altPressed = false;

        /// <summary>Manage resources.</summary>
        bool _disposed;

        /// <summary>Debug.</summary>
        readonly Color _pressedColor = Color.LimeGreen;

        /// <summary>Debug.</summary>
        int _ticks = 0;

        /// <summary></summary>
        bool _busy = false;
        #endregion

        #region Constants
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
            //static extern IntPtr SetWindowsHookEx(HookType hookType, HookProc lpfn, IntPtr hMod, uint dwThreadId);

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
        //delegate IntPtr HookProc(int code, IntPtr wParam, IntPtr lParam);
        #endregion

        #region Lifecycle
        /// <summary>
        /// Constructor.
        /// </summary>
        public ClipboardEx()
        {
            //Visible = false;

            InitializeComponent();

            // Init some controls.
            rtbText.Text = "Just something to copy";
            btnClear.Click += (_, __) => rtbInfo.Clear();
            lblLetter.Text = (_keyTrigger & Keys.KeyCode).ToString();

            _nextCb = NativeMethods.SetClipboardViewer(Handle);

            // HL messages of interest.
            _clipboardMessages = new()
            {
                { WM_DRAWCLIPBOARD, new("WM_DRAWCLIPBOARD", CbDraw, "Sent to the first window in the clipboard viewer chain when the content of the clipboard changes.") },
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
            using (Process process = Process.GetCurrentProcess())
            using (ProcessModule? module = process.MainModule)
            {
                // hMod: Handle to the DLL containing the hook procedure pointed to by the lpfn parameter. The hMod parameter must be set
                //   to NULL if the dwThreadId parameter specifies a thread created by the current process and if the hook procedure is
                //   within the code associated with the current process.
                // dwThreadId: Specifies the identifier of the thread with which the hook procedure is to be associated.If this parameter is
                //   zero, the hook procedure is associated with all existing threads running in the same desktop as the calling thread.
                IntPtr hModule = NativeMethods.GetModuleHandle(module!.ModuleName!);
                _hhook = NativeMethods.SetWindowsHookEx(HookType.WH_KEYBOARD_LL, KeyboardHookProc, hModule, 0);
            }

            // Paste test.
            _ticks = 5;
            timer1.Tick += (_, __) => { if (_ticks-- > 0) { Clipboard.SetText($"XXXXX{_ticks}{Environment.NewLine}"); TriggerPaste(); } };
            timer1.Enabled = true;
        }

        /// <summary>
        /// Override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources.
        /// </summary>
        ~ClipboardEx()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method.
            Dispose(false);
        }

        /// <summary>
        /// Boilerplate.
        /// </summary>
        public new void Dispose() // TODO why do I need new()?
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method.
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
                    // Dispose managed state (managed objects).
                    components.Dispose();
                }

                // Free unmanaged resources (unmanaged objects) and override finalizer.
                // Set large fields to null.
                NativeMethods.ChangeClipboardChain(Handle, _nextCb);
                NativeMethods.UnhookWindowsHookEx(_hhook);
            }

            _disposed = true;
            base.Dispose(disposing);
        }
        #endregion

        #region Windows Message Processing
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
                LogMessage("DBG", $"message {sp.Name} HWnd:{m.HWnd} Msg:{m.Msg} WParam:{m.WParam} LParam:{m.LParam} ");
                // Call handler.
                ret = sp.Handler(m);

                if (ret > 0)
                {
                    LogMessage("ERR", $"handler {sp.Name} ret:0X{ret:X}");
                }
            }
            else
            {
                // Ignore.
                base.WndProc(ref m);
            }
        }

        /// <summary>
        /// Process the clipboard draw message.
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
                    // Info about the source window.
                    IntPtr hwnd = NativeMethods.GetForegroundWindow();
                    ret = NativeMethods.GetWindowThreadProcessId(hwnd, out uint processID);
                    var procName = Process.GetProcessById((int)processID).ProcessName;
                    var appPath = Process.GetProcessById((int)processID).MainModule!.FileName;
                    var appName = Path.GetFileName(appPath);
                    StringBuilder title = new(100);
                    int hres = NativeMethods.GetWindowText(hwnd, title, 100);
                    if(hres == 0)
                    {
                        LogMessage("INF", $"COPY appName:{appName} procName:{procName} title:{title}");

                        // Data type info.
                        //var dtypes = dobj.GetFormats();
                        //LogMessage("INF", $"dtypes:{string.Join(",", dtypes)}");

                        if (Clipboard.ContainsText())
                        {
                            var t = Clipboard.GetText();
                            LogMessage("INF", $"TEXT {t.Left(50)}");
                        }
                        else if (Clipboard.ContainsFileDropList())
                        {
                            var t = Clipboard.GetFileDropList();
                            foreach (var s in t)
                            {
                                LogMessage("INF", $"FILE {s}");
                            }
                        }
                        else if (Clipboard.ContainsImage())
                        {
                            var t = Clipboard.GetImage();
                            LogMessage("INF", $"IMAGE {t.Size}");
                        }
                        else
                        {
                            LogMessage("INF", $"OTHER");
                            // Don't care? Audio.
                        }
                    }
                    else
                    {

                    }
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
        /// Process the clipboard change message.
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
        /// Process all other messages. Doesn't do anything right now.
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

        #region Inject Paste Keyboard
        /// <summary>
        /// Send paste to focus window.
        /// </summary>
        public void TriggerPaste()
        {
            //Use GetWindowThreadProcessId to get the process ID, then Process.GetProcessById to retrieve the process information.
            //The resultant System.Diagnostics.Process Object's MainModule Property has the Filename Property, which is the
            //Information you are probably searching.
            IntPtr hwnd = NativeMethods.GetForegroundWindow();
            uint hres = NativeMethods.GetWindowThreadProcessId(hwnd, out uint lpdwProcessId);
            if(hres == 0)
            {
                var p = Process.GetProcessById((int)lpdwProcessId);
                LogMessage("DBG", $"FileName:{p.MainModule!.FileName}");

                // This does work. Virtual keycodes from https://docs.microsoft.com/en-us/windows/win32/inputdev/virtual-key-codes
                byte vkey = 0x56; // 'v'
                byte ctrl = 0x11;
                int KEYEVENTF_KEYUP = 0x0002;
                NativeMethods.keybd_event(ctrl, 0, 0, 0);
                NativeMethods.keybd_event(vkey, 0, 0, 0);
                NativeMethods.keybd_event(ctrl, 0, KEYEVENTF_KEYUP, 0);
                NativeMethods.keybd_event(vkey, 0, KEYEVENTF_KEYUP, 0);

                // TODO This doesn't work:
                //NativeMethods.SendMessage(hwnd, 0x0302, IntPtr.Zero, IntPtr.Zero); // NativeMethods.WM_PASTE
            }
            else
            {

            }
        }

        /// <summary>
        /// Low level hook function.
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

                LogMessage("DBG", $"KeyboardHookProc code:{code} wParam:{wParam} key:{key} scancode:{lParam.scanCode}");

                if (_busy) // TODO needed?
                {
                    return 0;
                }
                _busy = true;

                if (code >= 0)
                {
                    // Update statuses.
                    bool pressed = wParam == WM_KEYDOWN || wParam == WM_SYSKEYDOWN;
                    //bool up = wParam == WM_KEYUP || wParam == WM_SYSKEYUP;
                    var vk = (Keys)lParam.vkCode;
                    bool myLetter = false;

                    switch (vk)
                    {
                        case Keys.LControlKey:
                        case Keys.RControlKey:
                            _controlPressed = pressed;
                            break;

                        case Keys.LShiftKey:
                        case Keys.RShiftKey:
                            _shiftPressed = pressed;
                            break;

                        case Keys.LMenu:
                        case Keys.RMenu:
                            _altPressed = pressed;
                            break;

                        default:
                            if(vk == (_keyTrigger & Keys.KeyCode))
                            {
                                myLetter = pressed;
                            }
                            break;
                    }

                    // Is this the magic key?
                    bool match = false;
                    match &= _controlPressed && ((int)(_keyTrigger & Keys.Control) > 0);
                    match &= _shiftPressed && ((int)(_keyTrigger & Keys.Shift) > 0);
                    match &= _altPressed && ((int)(_keyTrigger & Keys.Alt) > 0);
                    match &= myLetter;

                    // Diagnostics.
                    lblControl.BackColor = _controlPressed ? _pressedColor : Color.Transparent;
                    lblShift.BackColor = _shiftPressed ? _pressedColor : Color.Transparent;
                    lblAlt.BackColor = _altPressed ? _pressedColor : Color.Transparent;
                    lblLetter.BackColor = myLetter ? _pressedColor : Color.Transparent;
                    lblMatch.BackColor = match ? _pressedColor : Color.Transparent;

                    if(match)
                    {
                        //TODO do paste;
                    }

                    _busy = false;
                }
            }

            return NativeMethods.CallNextHookEx(_hhook, code, wParam, ref lParam);
        }
        #endregion

        /// <summary>
        /// Debug stuff.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Paste_Click(object sender, EventArgs e)
        {
            Clipboard.SetText(rtbInfo.Text);


            //Clipboard.SetText("Paste_Click wants to TriggerPaste()");
            ////void SetFileDropList(StringCollection filePaths);
            ////void SetImage(Image image);
            ////void SetText(string text);
            //TriggerPaste();
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