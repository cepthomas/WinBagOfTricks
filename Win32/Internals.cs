using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;


namespace Ephemera.Win32
{
    public static class Internals
    {
        #region Definitions
        ///// Windows messages.
        public const int WM_KEYDOWN = 0x100;
        public const int WM_KEYUP = 0x101;
        public const int WM_SYSKEYDOWN = 0x104; // when the user presses the F10 key (menu bar) or holds down the ALT key and then presses another key
        public const int WM_SYSKEYUP = 0x105; // when the user releases a key that was pressed while the ALT key was held down
        public const int WM_DRAWCLIPBOARD = 0x0308;
        public const int WM_CHANGECBCHAIN = 0x030D;
        public const int WM_CLIPBOARDUPDATE = 0x031D;
        public const int WM_DESTROYCLIPBOARD = 0x0307;
        public const int WM_ASKCBFORMATNAME = 0x030C;
        public const int WM_CLEAR = 0x0303;
        public const int WM_COPY = 0x0301;
        public const int WM_CUT = 0x0300;
        public const int WM_PASTE = 0x0302;
        public const int WM_HOTKEY_MESSAGE_ID = 0x0312;
        public const int WM_GETTEXT = 0x000D;

        ///// Show Commands
        public const int SW_HIDE = 0;
        public const int SW_SHOWNORMAL = 1;
        public const int SW_NORMAL = SW_SHOWNORMAL;
        public const int SW_SHOWMINIMIZED = 2;
        public const int SW_SHOWMAXIMIZED = 3;
        public const int SW_MAXIMIZE = SW_SHOWMAXIMIZED;
        public const int SW_SHOWNOACTIVATE = 4;
        public const int SW_SHOW = 5;
        public const int SW_MINIMIZE = 6;
        public const int SW_SHOWMINNOACTIVE = 7;
        public const int SW_SHOWNA = 8;
        public const int SW_RESTORE = 9;
        public const int SW_SHOWDEFAULT = 10;
        public const int SW_FORCEMINIMIZE = 11;
        public const int SW_MAX = SW_FORCEMINIMIZE;

        ///// Message Box Flags
        public const uint MB_OK = 0x00000000;
        public const uint MB_OKCANCEL = 0x00000001;
        public const uint MB_ABORTRETRYIGNORE = 0x00000002;
        public const uint MB_YESNOCANCEL = 0x00000003;
        public const uint MB_YESNO = 0x00000004;
        public const uint MB_RETRYCANCEL = 0x00000005;
        public const uint MB_CANCELTRYCONTINUE = 0x00000006;
        public const uint MB_ICONHAND = 0x00000010;
        public const uint MB_ICONQUESTION = 0x00000020;
        public const uint MB_ICONEXCLAMATION = 0x00000030;
        public const uint MB_ICONASTERISK = 0x00000040;
        public const uint MB_USERICON = 0x00000080;
        public const uint MB_ICONWARNING = MB_ICONEXCLAMATION;
        public const uint MB_ICONERROR = MB_ICONHAND;
        public const uint MB_ICONINFORMATION = MB_ICONASTERISK;
        public const uint MB_ICONSTOP = MB_ICONHAND;

        ///// Virtual keys - from https://docs.microsoft.com/en-us/windows/win32/inputdev/virtual-key-codes
        // Populate as needed.
        public const byte VK_CONTROL = 0x11;

        ///// Key Modifiers
        public const int MOD_ALT = 0x0001;
        public const int MOD_CTRL = 0x0002;
        public const int MOD_SHIFT = 0x0004;
        public const int MOD_WIN = 0x0008;

        ///// Shell Events
        public const int HSHELL_WINDOWCREATED = 1;
        public const int HSHELL_WINDOWDESTROYED = 2;
        public const int HSHELL_ACTIVATESHELLWINDOW = 3; // not used
        public const int HSHELL_WINDOWACTIVATED = 4;
        public const int HSHELL_GETMINRECT = 5;
        public const int HSHELL_REDRAW = 6;
        public const int HSHELL_TASKMAN = 7;
        public const int HSHELL_LANGUAGE = 8;
        public const int HSHELL_ACCESSIBILITYSTATE = 11;
        public const int HSHELL_APPCOMMAND = 12;

        ///// Shell Execute Mask Flags
        public const uint SEE_MASK_DEFAULT = 0x00000000;
        public const uint SEE_MASK_CLASSNAME = 0x00000001;
        public const uint SEE_MASK_CLASSKEY = 0x00000003;
        public const uint SEE_MASK_IDLIST = 0x00000004;
        public const uint SEE_MASK_INVOKEIDLIST = 0x0000000c;   // Note SEE_MASK_INVOKEIDLIST(0xC) implies SEE_MASK_IDLIST(0x04)
        public const uint SEE_MASK_HOTKEY = 0x00000020;
        public const uint SEE_MASK_NOCLOSEPROCESS = 0x00000040;
        public const uint SEE_MASK_CONNECTNETDRV = 0x00000080;
        public const uint SEE_MASK_NOASYNC = 0x00000100;
        public const uint SEE_MASK_FLAG_DDEWAIT = SEE_MASK_NOASYNC;
        public const uint SEE_MASK_DOENVSUBST = 0x00000200;
        public const uint SEE_MASK_FLAG_NO_UI = 0x00000400;
        public const uint SEE_MASK_UNICODE = 0x00004000;
        public const uint SEE_MASK_NO_CONSOLE = 0x00008000;
        public const uint SEE_MASK_ASYNCOK = 0x00100000;
        public const uint SEE_MASK_HMONITOR = 0x00200000;
        public const uint SEE_MASK_NOZONECHECKS = 0x00800000;
        public const uint SEE_MASK_NOQUERYCLASSSTORE = 0x01000000;
        public const uint SEE_MASK_WAITFORINPUTIDLE = 0x02000000;
        public const uint SEE_MASK_FLAG_LOG_USAGE = 0x04000000;

        ///// Some internal definitions.
        // public const int MOD_ALT = (int)KeyModifiers.MOD_ALT;
        // public const int MOD_CTRL = (int)KeyModifiers.MOD_CTRL;
        // public const int MOD_SHIFT = (int)KeyModifiers.MOD_SHIFT;
        // public const int MOD_WIN = (int)KeyModifiers.MOD_WIN;
        // public const int HSHELL_WINDOWCREATED = (int)ShellEvents.HSHELL_WINDOWCREATED;
        // public const int HSHELL_WINDOWDESTROYED = (int)ShellEvents.HSHELL_WINDOWDESTROYED;
        // public const int HSHELL_WINDOWACTIVATED = (int)ShellEvents.HSHELL_WINDOWACTIVATED;
        #endregion

        #region Fields
        static readonly List<int> _hotKeyIds = [];
        #endregion

        #region API
        /// <summary>
        /// Generic message sender.
        /// </summary>
        /// <param name="handle"></param>
        /// <param name="msg"></param>
        /// <param name="wParam"></param>
        /// <param name="lParam"></param>
        /// <returns></returns>
        public static int SendMessage(IntPtr handle, int msg, IntPtr wParam, IntPtr lParam)
        {
            return SendMessageInternal(handle, msg, wParam, lParam);
        }

        /// <summary> 
        /// Inject a keystroke.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="up"></param>
        public static void InjectKey(byte key, bool up = false)
        {
            // TODO Should use SendInput() instead.
            keybd_event(key, 0, up ? 2 : 0, 0); // KEYEVENTF_KEYUP = 0x0002
        }

        /// <summary> 
        /// Inject a character.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="up"></param>
        public static void InjectKey(char key, bool up = false)
        {
            InjectKey((byte)key, up);
        }

        /// <summary>
        /// Register shell hook.
        /// </summary>
        /// <param name="handle"></param>
        /// <returns></returns>
        public static int RegisterShellHook(IntPtr handle)
        {
            int msg = RegisterWindowMessage("SHELLHOOK"); // test for 0?
            _ = RegisterShellHookWindow(handle);
            return msg;
        }

        /// <summary>
        /// Deregister shell hook.
        /// </summary>
        /// <param name="handle"></param>
        public static void DeregisterShellHook(IntPtr handle)
        {
            _ = DeregisterShellHookWindow(handle);
        }

        /// <summary>
        /// Rudimentary management of hotkeys. Only supports one (global) handle.
        /// </summary>
        /// <param name="handle"></param>
        /// <param name="key"></param>
        /// <param name="mod"></param>
        /// <returns></returns>
        public static int RegisterHotKey(IntPtr handle, int key, int mod)
        {
            int id = mod ^ key ^ (int)handle;
            _ = RegisterHotKey(handle, id, mod, key);
            _hotKeyIds.Add(id);
            return id;
        }

        /// <summary>
        /// Rudimentary management of hotkeys. Only supports one (global) handle.
        /// </summary>
        /// <param name="handle"></param>
        public static void UnregisterHotKeys(IntPtr handle)
        {
            _hotKeyIds.ForEach(id => UnregisterHotKey(handle, id));
        }

        /// <summary>
        /// Generic limited modal message box. Just enough for a typical console or hidden application.
        /// </summary>
        /// <param name="message">Body.</param>
        /// <param name="caption">Caption.</param>
        /// <param name="error">Use error icon otherwise info.</param>
        /// <param name="ask">Use OK/cancel buttons.</param>
        /// <returns>True if OK </returns>
        public static bool MessageBox(string message, string caption, bool error = false, bool ask = false)
        {
            uint flags = error ? MB_ICONERROR : MB_ICONINFORMATION;

            if (ask)
            {
                flags |= MB_OKCANCEL;
                int res = MessageBox(IntPtr.Zero, message, caption, flags);
                return res == 1; //IDOK
            }
            else
            {
                _ = MessageBox(IntPtr.Zero, message, caption, flags);
            }

            return true;
        }

        /// <summary>
        /// Thunk.
        /// </summary>
        public static void DisableDpiScaling()
        {
            SetProcessDPIAware();
        }

        /// <summary>
        /// Streamlined version of the real function.
        /// </summary>
        /// <param name="verb">Standard verb</param>
        /// <param name="path">Where</param>
        /// <param name="hide">Hide the new window.</param>
        /// <returns>Standard error code</returns>
        public static int ShellExecute(string verb, string path, bool hide = false)
        {
            var ss = new List<string> { "edit", "explore", "find", "open", "print", "properties", "runas" };
            if (!ss.Contains(verb)) { throw new ArgumentException($"Invalid verb:{verb}"); }

            // If ShellExecute() succeeds, it returns a value greater than 32,
            //   else it returns an error value that indicates the cause of the failure.
            int res = (int)ShellExecute(IntPtr.Zero, verb, path, IntPtr.Zero, IntPtr.Zero,
                hide ? SW_HIDE : SW_NORMAL);

            return res > 32 ? 0 : res;
        }
        #endregion

        #region Native Methods

        #region Types
        /// <summary>For ShellExecuteEx().</summary>
        /// https://learn.microsoft.com/en-us/windows/win32/api/shellapi/ns-shellapi-shellexecuteinfoa
        /// ? Be careful with the string structure fields: UnmanagedType.LPTStr will be marshalled as unicode string so only
        /// the first character will be recognized by the function. Use UnmanagedType.LPStr instead.
        [StructLayout(LayoutKind.Sequential)]
        struct ShellExecuteInfo
        {
            // The size of this structure, in bytes.
            public int cbSize;
            // A combination of one or more ShellExecuteMaskFlags.
            public uint fMask;
            // Optional handle to the owner window.
            public IntPtr hwnd;
            // Specific operation.
            [MarshalAs(UnmanagedType.LPTStr)]
            public string lpVerb;
            // null-terminated string that specifies the name of the file or object on which ShellExecuteEx will perform the action specified by the lpVerb parameter.
            [MarshalAs(UnmanagedType.LPTStr)]
            public string lpFile;
            // Optional null-terminated string that contains the application parameters separated by spaces.
            [MarshalAs(UnmanagedType.LPTStr)]
            public string lpParameters;
            // Optional null-terminated string that specifies the name of the working directory.
            [MarshalAs(UnmanagedType.LPTStr)]
            public string lpDirectory;
            // Flags that specify how an application is to be shown when it is opened. See ShowCommands.
            public int nShow;
            // The rest are ?????
            public IntPtr hInstApp;
            public IntPtr lpIDList;
            [MarshalAs(UnmanagedType.LPTStr)]
            public string lpClass;
            public IntPtr hkeyClass;
            public uint dwHotKey;
            public IntPtr hIcon;
            public IntPtr hProcess;
        }
        #endregion

        #region shell32.dll
        /// <summary>Performs an operation on a specified file.
        /// Args: https://learn.microsoft.com/en-us/windows/win32/api/shellapi/ns-shellapi-shellexecuteinfoa.
        /// </summary>
        [DllImport("shell32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        static extern IntPtr ShellExecute(IntPtr hwnd, string lpVerb, string lpFile, string lpParameters, string lpDirectory, int nShow);

        /// <summary>Overload of above for nullable args.</summary>
        [DllImport("shell32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        static extern IntPtr ShellExecute(IntPtr hwnd, string lpVerb, string lpFile, IntPtr lpParameters, IntPtr lpDirectory, int nShow);

        /// <summary>Finer control version of above.</summary>
        [DllImport("shell32.dll", SetLastError = true)]
        static extern bool ShellExecuteEx(ref ShellExecuteInfo lpExecInfo);
        #endregion

        #region user32.dll
        /// <summary>Rudimentary UI notification for use in a console application.</summary>
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        static extern int MessageBox(IntPtr hWnd, string msg, string caption, uint type);

        [DllImport("user32.dll")]
        static extern bool SetProcessDPIAware();

        [DllImport("user32.dll", EntryPoint = "RegisterWindowMessageA", SetLastError = true, ExactSpelling = true, CharSet = CharSet.Unicode)]
        static extern int RegisterWindowMessage(string lpString);

        [DllImport("user32.dll", SetLastError = true, ExactSpelling = true)]
        static extern int RegisterShellHookWindow(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true, ExactSpelling = true)]
        static extern int DeregisterShellHookWindow(IntPtr hWnd);

        // Keyboard hooks.
        [DllImport("user32.dll", SetLastError = true, ExactSpelling = true)]
        static extern bool RegisterHotKey(IntPtr hWnd, int id, int fsModifiers, int vk);

        [DllImport("user32.dll", SetLastError = true, ExactSpelling = true)]
        static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        [DllImport("user32.dll", EntryPoint = "SendMessage")]
        static extern int SendMessageInternal(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        static extern void keybd_event(byte bVk, byte bScan, int dwFlags, int dwExtraInfo);
        #endregion

        #endregion
    }
}
