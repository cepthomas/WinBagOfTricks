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
using System.Collections;

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
            //_ghook = null;
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


            ClientStuff cl = new();
            cl.ForwardDataToClipboard();


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

        /// <summary> https://www.pinvoke.net/default.aspx/Enums/HookType.html </summary>
        public enum HookType : int
        {
            WH_MSGFILTER = -1,
            WH_JOURNALRECORD = 0,
            WH_JOURNALPLAYBACK = 1,
            WH_KEYBOARD = 2,
            WH_GETMESSAGE = 3,
            WH_CALLWNDPROC = 4,
            WH_CBT = 5,
            WH_SYSMSGFILTER = 6,
            WH_MOUSE = 7,
            WH_HARDWARE = 8,
            WH_DEBUG = 9,
            WH_SHELL = 10,
            WH_FOREGROUNDIDLE = 11,
            WH_CALLWNDPROCRET = 12,
            WH_KEYBOARD_LL = 13,
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


                    ////////////////////////////////////////////////////////////////
#if STUFF
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
#endif

                    busy = false;


                }
            }
            return CallNextHookEx(_hhook, code, wParam, ref lParam);
        }


        /// <summary>Initializes a new instance of the class and installs the keyboard hook.</summary>
        public globalKeyboardHook()
        {
            _hookProc = HP;
            IntPtr hInstance = GetModuleHandle("User32.dll");
            _hhook = SetWindowsHookEx(HookType.WH_KEYBOARD_LL, _hookProc, hInstance, 0);


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
            //_hhook = SetWindowsHookEx(HookType.WH_KEYBOARD_LL, _hookProc, hInstance, 0);
        }






        /// <summary>Releases unmanaged resources. TODO</summary>
        ~globalKeyboardHook()
        {
            //unhook();
            UnhookWindowsHookEx(_hhook);
        }
    }


    ////////////////////////// Other client stuff //////////////////////////////////////


    public class ClientStuff //: Form
    {
        [DllImport("user32.dll")]
        internal static extern void keybd_event(byte bVk, byte bScan, int dwFlags, int dwExtraInfo);

        [DllImport("user32", EntryPoint = "VkKeyScan")]
        internal static extern short VkKeyScan(byte cChar_Renamed);

        [DllImport("user32.dll")]
        internal static extern bool SetForegroundWindow(int hWnd);

        [DllImport("user32.dll")]
        internal static extern int SetFocus(int hWnd);

        [DllImport("user32.dll")]
        internal static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();



        public const int WM_PASTE = 0x0302;

        public const byte VK_CONTROL = 0x11;
        public const int KEYEVENTF_KEYUP = 0x0002;

        public const int WM_KEYDOWN = 0x0100;
        public const int WM_KEYUP = 0x0101;






        public void ForwardDataToClipboard()
        {
            // This before called -->
            // //put the chosen item as last added - so it would be on top of the list
            // cl.list.Remove(text);
            // cl.list.Add(text);
            // cl.prevclipboardcontents = text;
            // ClipboardListener.SetClipboardText(text);
            // frm.ForwardDataToClipboard();


            //// was:
            //GetOneDownWindow wnd = new GetOneDownWindow();
            //int outsideApphwnd = (int)wnd.GetThatWindow(); //wnd.wndArray[1];
            //SetForegroundWindow(outsideApphwnd);
            //SetFocus(outsideApphwnd);
            //keybd_event(VK_CONTROL, 0, 0, 0);
            //keybd_event((byte)VkKeyScan((byte)'v'), 0, 0, 0);
            //keybd_event(VK_CONTROL, 0, KEYEVENTF_KEYUP, 0);
            //keybd_event((byte)VkKeyScan((byte)'v'), 0, KEYEVENTF_KEYUP, 0);


            // new:
            //Use GetWindowThreadProcessId to get the process ID, then Process.GetProcessById to retrieve the process information.
            //The resultant System.Diagnostics.Process Object's MainModule Property has the Filename Property, which is the
            //Information you are probably searching.
            IntPtr hwnd = GetForegroundWindow();
            uint ret = GetWindowThreadProcessId(hwnd, out uint lpdwProcessId);
            var p = Process.GetProcessById((int)lpdwProcessId);

            Debug.WriteLine($"FileName:{p.MainModule.FileName}");

            //globalKeyboardHook_o code:0 wParam: 256 key: LControlKey scancode:0
            //globalKeyboardHook_o code:0 wParam: 256 key: V scancode:0
            //globalKeyboardHook_o code:0 wParam: 257 key: LControlKey scancode:0
            //globalKeyboardHook_o code:0 wParam: 257 key: V scancode:0

            keybd_event(VK_CONTROL, 0, 0, 0);
            keybd_event((byte)VkKeyScan((byte)'v'), 0, 0, 0);
            keybd_event(VK_CONTROL, 0, KEYEVENTF_KEYUP, 0);
            keybd_event((byte)VkKeyScan((byte)'v'), 0, KEYEVENTF_KEYUP, 0);
        }
    }

    public class GetOneDownWindow
    {
        [DllImport("user32.dll")]
        private static extern int EnumWindows(EnumWindowsProc ewp, int lParam);

        // [DllImport("user32.dll")]
        // private static extern int GetWindowText(int hWnd, StringBuilder title, int size);

        [DllImport("user32.dll")]
        private static extern int GetWindowModuleFileName(int hWnd, StringBuilder title, int size);

        [DllImport("user32.dll")]
        private static extern bool IsWindowVisible(int hWnd);

        [DllImport("user32.dll")]
        internal static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);


        public delegate bool EnumWindowsProc(int hWnd, int lParam);

        public ArrayList wndArray = new(); //array of windows

        public IntPtr GetThatWindow()
        {
            EnumWindowsProc ewp = EnumWindow;
            EnumWindows(ewp, 0);
            return (IntPtr)wndArray[1];
        }

        private bool EnumWindow(int hWnd, int lParam)
        {
            if (!IsWindowVisible(hWnd))
                return (true);



            StringBuilder module = new(256);
            GetWindowModuleFileName(hWnd, module, 256);
            //string test = module.ToString();
            wndArray.Add((IntPtr)hWnd);



            return (true);
        }
    }



#if MY_OLD_BAD
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
