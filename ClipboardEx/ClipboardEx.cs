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
    /// <summary>
    /// Handles all interactions at the Clipboard.XXX() API level.
    /// </summary>
    public partial class ClipboardEx : Form
    {
        #region Fields
        /// <summary>Next in line for clipboard  notification.</summary>
        IntPtr _nextCb = new(0);

        /// <summary>All handled clipboard messages.</summary>
        record MsgSpec(string Name, Func<Message, uint> Handler, string Description);
        readonly Dictionary<int, MsgSpec> _messages;
        #endregion

        #region Interop
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
        }
        #endregion

        globalKeyboardHook _ghook;


        #region Lifecycle
        /// <summary>
        /// Constructor.
        /// </summary>
        public ClipboardEx()
        {
            //Visible = false;

            InitializeComponent();

            rtbText.Text = "Just something to copy";
            btnClear.Click += (_, __) => rtbInfo.Clear();

            _nextCb = NativeMethods.SetClipboardViewer(Handle);

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


            //// initialize our delegate
            //_myCallbackDelegate = new HookProc(MyCallbackFunction);

            //// setup a keyboard hook
            //_hhook = NativeMethods.SetWindowsHookEx(2 /*HookType.WH_KEYBOARD*/, _myCallbackDelegate, IntPtr.Zero, AppDomain.GetCurrentThreadId());


            /////// the other way
            _ghook = new();
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
        /// Handle window messages.
        /// </summary>
        /// <param name="m"></param>
        protected override void WndProc(ref Message m)
        {
            uint ret = 0;

            if (_messages is not null && _messages.ContainsKey(m.Msg))
            {
                //LogMessage("DBG", $"m.HWnd:{m.HWnd} me:{Handle}");

                MsgSpec sp = _messages[m.Msg];
                LogMessage("INF", $"message {sp.Name}");

                ret = sp.Handler(m);

                if(ret > 0)
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
                    NativeMethods.GetWindowText(hwnd, title, 100);
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
                        // Don't care?
                        //bool ContainsAudio();
                        //Stream GetAudioStream();
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Paste_Click(object sender, EventArgs e)
        {
            Clipboard.SetText("A paste experiment");
            //void SetFileDropList(StringCollection filePaths);
            //void SetImage(Image image);
            //void SetText(string text);
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


    ////////////////////////// LL keyboard hook //////////////////////////////////////

    /// <summary>
    /// A class that manages a global low level keyboard hook
    /// </summary>
    class globalKeyboardHook
    {
        /// <summary>The collections of keys to watch for</summary>
        List<Keys> _hookedKeys = new();

        /// <summary>defines the callback type for the hook</summary>
        public delegate int keyboardHookProc(int code, int wParam, ref KBDLLHOOKSTRUCT lParam);
        //delegate IntPtr HookProc(int code, IntPtr wParam, IntPtr lParam);

        /// <summary>Occurs when one of the hooked keys is pressed</summary>
        public event KeyEventHandler KeyDown;

        /// <summary>Occurs when one of the hooked keys is released</summary>
        public event KeyEventHandler KeyUp;


        keyboardHookProc _hookProc;

        /// <summary>Handle to the hook, need this to unhook and call the next hook</summary>
        IntPtr _hhook = IntPtr.Zero;

        #region Interop
        [DllImport("user32.dll")]
        static extern int CallNextHookEx(IntPtr idHook, int nCode, int wParam, ref KBDLLHOOKSTRUCT lParam);

        [DllImport("kernel32.dll")]
        static extern IntPtr LoadLibrary(string lpFileName);

        [DllImport("user32.dll")]
        static extern IntPtr SetWindowsHookEx(HookType hookType, keyboardHookProc lpfn, IntPtr hMod, uint dwThreadId);
        //static extern IntPtr SetWindowsHookEx(HookType hookType, HookProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll")]
        static extern bool UnhookWindowsHookEx(IntPtr hInstance);

        [DllImport("kernel32.dll", CharSet=CharSet.Unicode, SetLastError=true)]
        static extern IntPtr GetModuleHandle(string lpModuleName);
        //static extern IntPtr GetModuleHandle([MarshalAs(UnmanagedType.LPWStr)] in string lpModuleName);

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
            public uint vkCode;
            public uint scanCode;
            public KBDLLHOOKSTRUCTFlags flags;
            public uint time;
            public UIntPtr dwExtraInfo;
        }

        /// <summary>
        /// Enumerates the valid hook types passed as the idHook parameter into a call to SetWindowsHookEx.
        /// </summary>
        public enum HookType : int
        {
            /// Installs a hook procedure that monitors messages generated as a result of an input event in a dialog box,
            /// message box, menu, or scroll bar. For more information, see the MessageProc hook procedure.
            WH_MSGFILTER = -1,
            /// Installs a hook procedure that records input messages posted to the system message queue. This hook is
            /// useful for recording macros. For more information, see the JournalRecordProc hook procedure.
            WH_JOURNALRECORD = 0,
            /// Installs a hook procedure that posts messages previously recorded by a WH_JOURNALRECORD hook procedure.
            /// For more information, see the JournalPlaybackProc hook procedure.
            WH_JOURNALPLAYBACK = 1,
            /// Installs a hook procedure that monitors keystroke messages. For more information, see the KeyboardProc
            /// hook procedure.
            WH_KEYBOARD = 2,
            /// Installs a hook procedure that monitors messages posted to a message queue. For more information, see the
            /// GetMsgProc hook procedure.
            WH_GETMESSAGE = 3,
            /// Installs a hook procedure that monitors messages before the system sends them to the destination window
            /// procedure. For more information, see the CallWndProc hook procedure.
            WH_CALLWNDPROC = 4,
            /// Installs a hook procedure that receives notifications useful to a CBT application. For more information,
            /// see the CBTProc hook procedure.
            WH_CBT = 5,
            /// Installs a hook procedure that monitors messages generated as a result of an input event in a dialog box,
            /// message box, menu, or scroll bar. The hook procedure monitors these messages for all applications in the
            /// same desktop as the calling thread. For more information, see the SysMsgProc hook procedure.
            WH_SYSMSGFILTER = 6,
            /// Installs a hook procedure that monitors mouse messages. For more information, see the MouseProc hook
            /// procedure.
            WH_MOUSE = 7,
            ///
            WH_HARDWARE = 8,
            /// Installs a hook procedure useful for debugging other hook procedures. For more information, see the
            /// DebugProc hook procedure.
            WH_DEBUG = 9,
            /// Installs a hook procedure that receives notifications useful to shell applications. For more information,
            /// see the ShellProc hook procedure.
            WH_SHELL = 10,
            /// Installs a hook procedure that will be called when the application's foreground thread is about to become
            /// idle. This hook is useful for performing low priority tasks during idle time. For more information, see the
            /// ForegroundIdleProc hook procedure.
            WH_FOREGROUNDIDLE = 11,
            /// Installs a hook procedure that monitors messages after they have been processed by the destination window
            /// procedure. For more information, see the CallWndRetProc hook procedure.
            WH_CALLWNDPROCRET = 12,
            /// Installs a hook procedure that monitors low-level keyboard input events. For more information, see the
            /// LowLevelKeyboardProc hook procedure.
            WH_KEYBOARD_LL = 13,
            /// Installs a hook procedure that monitors low-level mouse input events. For more information, see the
            /// LowLevelMouseProc hook procedure.
            WH_MOUSE_LL = 14
        }

        //const int WH_KEYBOARD_LL = 13;
        const int WM_KEYDOWN = 0x100;
        const int WM_KEYUP = 0x101;
        const int WM_SYSKEYDOWN = 0x104;
        const int WM_SYSKEYUP = 0x105;
        #endregion


        bool busy = false;

        public int HP(int code, int wParam, ref KBDLLHOOKSTRUCT lParam)
        {
            if (code >= 0)
            {
                Keys key = (Keys)lParam.vkCode;

                Debug.WriteLine($"globalKeyboardHook_o code:{code} wParam:{wParam} key:{key} scancode:{lParam.scanCode}");

                if (_hookedKeys.Contains(key))
                {
                    KeyEventArgs kea = new(key);
                    if ((wParam == WM_KEYDOWN || wParam == WM_SYSKEYDOWN) && (KeyDown != null))
                    {
                        KeyDown(this, kea);
                    }
                    else if ((wParam == WM_KEYUP || wParam == WM_SYSKEYUP) && (KeyUp != null))
                    {
                        KeyUp(this, kea);
                    }

                    if (kea.Handled)
                    {
                        return 1;
                    }

                    return CallNextHookEx(_hhook, code, wParam, ref lParam);

                    ////////////////////////////////////////////////////////////////
                    bool ctrl_pressed = false;
                    bool v_pressed = false;
                    bool shft_pressed = false;
                    bool sth_else = false;
                    const Keys MY_LCTRL = Keys.LControlKey;
                    const Keys MY_RCTRL = Keys.RControlKey;
                    const Keys MY_LSHIFT = Keys.LShiftKey;
                    const Keys MY_RSHIFT = Keys.RShiftKey;
                    const Keys MY_V = Keys.V;

                    if (busy)
                        return 0;
                    else
                        busy = true;

                    if (code >= 0)
                    {
                        Keys key2 = (Keys)lParam.vkCode;

                        if (wParam == WM_KEYDOWN || wParam == WM_SYSKEYDOWN)
                        {
                            switch (key2)
                            {
                                case MY_LCTRL:
                                case MY_RCTRL:
                                    ctrl_pressed = true;
                                    break;
                                case MY_LSHIFT:
                                case MY_RSHIFT:
                                    shft_pressed = true;
                                    break;
                                case MY_V:
                                    v_pressed = true;
                                    break;
                                default:
                                    sth_else = true;
                                    break;
                            }
                        }
                        if (wParam == WM_KEYUP || wParam == WM_SYSKEYDOWN)
                        {
                            switch (key2)
                            {
                                case MY_LCTRL:
                                case MY_RCTRL:
                                    if (ctrl_pressed)
                                        ctrl_pressed = false;
                                    break;
                                case MY_LSHIFT:
                                case MY_RSHIFT:
                                    if (shft_pressed)
                                        shft_pressed = false;
                                    break;
                                case MY_V:
                                    if (v_pressed)
                                        v_pressed = false;
                                    break;
                                default:
                                    sth_else = true;
                                    break;
                            }
                        }
                        if (sth_else)
                        {
                            ctrl_pressed = false;
                            v_pressed = false;
                            shft_pressed = false;
                            sth_else = false;
                        }

                        if (ctrl_pressed && v_pressed && shft_pressed)
                        {
                            ctrl_pressed = false;
                            v_pressed = false;
                            shft_pressed = false;

                            //ShowWindow();
                            busy = false;
                            return 0;
                        }
                    }

                    busy = false;
                }
            }
        }


        /// <summary>Initializes a new instance of the class and installs the keyboard hook.</summary>
        public globalKeyboardHook()
        {
            _hookProc = HP;

            //_hookProc = delegate (int code, int wParam, ref KBDLLHOOKSTRUCT lParam)
            //{
            //    if (code >= 0)
            //    {
            //        Keys key = (Keys)lParam.vkCode;

            //        Debug.WriteLine($"globalKeyboardHook_o code:{code} wParam:{wParam} key:{key} scancode:{lParam.scanCode}");

            //        if (_hookedKeys.Contains(key))
            //        {
            //            KeyEventArgs kea = new(key);
            //            if ((wParam == WM_KEYDOWN || wParam == WM_SYSKEYDOWN) && (KeyDown != null))
            //            {
            //                KeyDown(this, kea);
            //            }
            //            else if ((wParam == WM_KEYUP || wParam == WM_SYSKEYUP) && (KeyUp != null))
            //            {
            //                KeyUp(this, kea);
            //            }
            //            if (kea.Handled)
            //                return 1;
            //        }
            //    }
            //    return CallNextHookEx(_hhook, code, wParam, ref lParam);
            //};

            //hook();
            //IntPtr hInstance = LoadLibrary("User32");
            IntPtr hInstance = GetModuleHandle("User32.dll");
            _hhook = SetWindowsHookEx(HookType.WH_KEYBOARD_LL, _hookProc, hInstance, 0);
        }






        /// <summary>Releases unmanaged resources. TODO</summary>
        ~globalKeyboardHook()
        {
            //unhook();
            UnhookWindowsHookEx(_hhook);
        }


    }



#if MY_BAD
    /// <summary>
    /// A class that manages a global low level keyboard hook
    /// </summary>
    class globalKeyboardHook
    {
    #region Interop
        internal class NativeMethods
        {
            [StructLayout(LayoutKind.Sequential)]
            public class KBDLLHOOKSTRUCT
            {
                public uint vkCode;
                public uint scanCode;
                public KBDLLHOOKSTRUCTFlags flags;
                public uint time;
                public UIntPtr dwExtraInfo;
            }

            [Flags]
            public enum KBDLLHOOKSTRUCTFlags : uint
            {
                LLKHF_EXTENDED = 0x01,
                LLKHF_INJECTED = 0x10,
                LLKHF_ALTDOWN = 0x20,
                LLKHF_UP = 0x80,
            }

            [DllImport("user32.dll")]
            static internal extern int CallNextHookEx(IntPtr idHook, int nCode, int wParam, ref KBDLLHOOKSTRUCT lParam);

            [DllImport("kernel32.dll")]
            static internal extern IntPtr LoadLibrary(string lpFileName);

            [DllImport("user32.dll")]
            static internal extern IntPtr SetWindowsHookEx(int idHook, HookProc callback, IntPtr hInstance, uint threadId);

            [DllImport("user32.dll")]
            static internal extern bool UnhookWindowsHookEx(IntPtr hInstance);

            /// <summary>defines the callback type for the hook</summary>
            public delegate int HookProc(int code, int wParam, ref NativeMethods.KBDLLHOOKSTRUCT lParam);

        }
        #endregion


        /// <summary>Occurs when one of the hooked keys is pressed</summary>
        public event KeyEventHandler KeyDown;

        /// <summary>Occurs when one of the hooked keys is released</summary>
        public event KeyEventHandler KeyUp;


        /// <summary>The collections of keys to watch for</summary>
        List<Keys> _hookedKeys = new();
        NativeMethods.HookProc _hookProc;

        const int WH_KEYBOARD_LL = 13;
        const int WM_KEYDOWN = 0x100;
        const int WM_KEYUP = 0x101;
        const int WM_SYSKEYDOWN = 0x104;
        const int WM_SYSKEYUP = 0x105;

        /// <summary>Handle to the hook, need this to unhook and call the next hook</summary>
        IntPtr _hhook = IntPtr.Zero;


        /// <summary>Initializes a new instance of the class and installs the keyboard hook.</summary>
        public globalKeyboardHook()
        {
            _hookProc = delegate (int code, int wParam, ref NativeMethods.KBDLLHOOKSTRUCT lParam)
            {
                Debug.WriteLine($"globalKeyboardHook entry");

                if (code >= 0)
                {
                    Keys key = (Keys)lParam.vkCode;

                    Debug.WriteLine($"globalKeyboardHook code:{code} wParam:{wParam} key:{key} scancode:{lParam.scanCode}");

                    if (_hookedKeys.Contains(key))
                    {
                        KeyEventArgs kea = new(key);
                        if ((wParam == WM_KEYDOWN || wParam == WM_SYSKEYDOWN) && (KeyDown != null))
                        {
                            KeyDown(this, kea);
                        }
                        else if ((wParam == WM_KEYUP || wParam == WM_SYSKEYUP) && (KeyUp != null))
                        {
                            KeyUp(this, kea);
                        }

                        if (kea.Handled)
                        {
                            return 1;
                        }
                    }
                }

                return NativeMethods.CallNextHookEx(_hhook, code, wParam, ref lParam);
            };

            //hook(); TODO why this?
            IntPtr hInstance = NativeMethods.LoadLibrary("User32");
            _hhook = NativeMethods.SetWindowsHookEx(WH_KEYBOARD_LL, _hookProc, hInstance, 0); // 2 blows up real bad
        }

        /// <summary>Releases unmanaged resources.</summary>
        ~globalKeyboardHook()
        {
            //unhook();
            NativeMethods.UnhookWindowsHookEx(_hhook);
        }


        ///// <summary>defines the callback type for the hook</summary>
        //public delegate int keyboardHookProc(int code, int wParam, ref keyboardHookStruct lParam);

        ///// <summary>Occurs when one of the hooked keys is pressed</summary>
        //public event KeyEventHandler KeyDown;

        ///// <summary>Occurs when one of the hooked keys is released</summary>
        //public event KeyEventHandler KeyUp;
    }
#endif

}
