using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Drawing;
using Ephemera.NBagOfTricks;


#pragma warning disable SYSLIB1054, CA1401, CA2101, CS1591, CA1416, IDE0058, CA1806


namespace Ephemera.Win32
{
    public class WindowManagement
    {
        #region Types
        /// <summary>Useful info about a window.</summary>
        public class AppWindowInfo
        {
            /// <summary>Native window handle.</summary>
            public IntPtr Handle { get; init; }

            /// <summary>Owner process.</summary>
            public int Pid { get; init; }

            /// <summary>Running on this thread.</summary>
            public IntPtr ThreadId { get; init; }

            /// <summary>Who's your daddy?</summary>
            public IntPtr Parent { get; init; }

            /// <summary>The coordinates of the window.</summary>
            public Rectangle DisplayRectangle { get; init; }

            /// <summary>The coordinates of the client area.</summary>
            public Rectangle ClientRectangle { get; init; }

            /// <summary>Window Text.</summary>
            public string Title { get; init; } = "";

            /// <summary>This is not trustworthy as it is true for some unseen windows.</summary>
            public bool IsVisible { get; set; }

            /// <summary>For humans.</summary>
            public override string ToString()
            {
                var g = $"X:{DisplayRectangle.Left} Y:{DisplayRectangle.Top} W:{DisplayRectangle.Width} H:{DisplayRectangle.Height}";
                var s = $"Title[{Title}] Geometry[{g}] IsVisible[{IsVisible}] Handle[{Handle}] Pid[{Pid}]";
                return s;
            }
        }
        #endregion

        #region API
        /// <summary>
        /// Get or set the fg.
        /// </summary>
        public static IntPtr ForegroundWindow
        {
            get { return GetForegroundWindow(); }
            set { SetForegroundWindow(value); }
        }

        /// <summary>
        /// Get the shell window.
        /// </summary>
        public static IntPtr ShellWindow
        {
            get { return GetShellWindow(); }
        }

        /// <summary>
        /// Get all pertinent/visible windows for the application. Ignores non-visible or non-titled (internal).
        /// Note that new explorers may be in the same process or separate ones. Depends on explorer user options.
        /// </summary>
        /// <param name="appName">The application name to filter on.</param>
        /// <param name="includeAnonymous">Include those without titles or base "Program Manager".</param>
        /// <returns>List of window infos.</returns>
        public static List<AppWindowInfo> GetAppWindows(string appName, bool includeAnonymous = false)
        {
            List<AppWindowInfo> winfos = [];
            List<IntPtr> procids = [];

            // Get all processes.
            Process[] procs = Process.GetProcessesByName(appName);
            procs.ForEach(p => procids.Add(p.Id));

            // Enumerate all windows. https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-enumwindows
            List<IntPtr> visHandles = [];
            bool addWindowHandle(IntPtr hWnd, IntPtr param) // callback
            {
                if (IsWindowVisible(hWnd))
                {
                    visHandles.Add(hWnd);
                }
                return true;
            }
            IntPtr param = IntPtr.Zero;
            EnumWindows(addWindowHandle, param);

            foreach (var vh in visHandles)
            {
                var wi = GetAppWindowInfo(vh);

                var realWin = wi.Title != "" && wi.Title != "Program Manager";
                if (procids.Contains(wi.Pid) && (includeAnonymous || realWin))
                {
                    winfos.Add(wi);
                }
            }

            return winfos;
        }

        /// <summary>
        /// Get main window(s) for the application. Could be multiple if more than one process.
        /// </summary>
        /// <param name="appName">The application name to filter on.</param>
        /// <returns>List of window handles.</returns>
        public static List<IntPtr> GetAppMainWindows(string appName)
        {
            List<IntPtr> handles = [];

            // Get all processes. There is one entry per separate process.
            Process[] procs = Process.GetProcessesByName(appName);
            // Get each main window.
            procs.ForEach(p => handles.Add(p.MainWindowHandle));
            return handles;
        }

        /// <summary>
        /// Get everything you need to know about a window.
        /// </summary>
        /// <param name="handle"></param>
        /// <returns>The info object.</returns>
        public static AppWindowInfo GetAppWindowInfo(IntPtr handle)
        {
            IntPtr threadId = GetWindowThreadProcessId(handle, out IntPtr pid);
            //GetWindowRect(handle, out Rect rect);

            StringBuilder sb = new(1024);
            GetWindowText(handle, sb, sb.Capacity);

            WindowInfo wininfo = new();
            GetWindowInfo(handle, ref wininfo);

            // Helper.
            static Rectangle Convert(Rect rect)
            {
                return new()
                {
                    X = rect.Left,
                    Y = rect.Top,
                    Width = rect.Right - rect.Left,
                    Height = rect.Bottom - rect.Top
                };
            }

            AppWindowInfo wi = new()
            {
                Handle = handle,
                ThreadId = threadId,
                Pid = pid.ToInt32(),
                Parent = GetParent(handle),
                Title = sb.ToString(),
                IsVisible = IsWindowVisible(handle),
                DisplayRectangle = Convert(wininfo.rcWindow),
                ClientRectangle = Convert(wininfo.rcClient)
            };

            return wi;
        }

        /// <summary>
        /// Show the window.
        /// </summary>
        /// <param name="handle"></param>
        /// <returns></returns>        
        public static bool ShowWindow(IntPtr handle)
        {
            return ShowWindow(handle, 1);
            //SW_HIDE = 0, TODO these are defined in internals.
            //SW_SHOWNORMAL = 1,
            //SW_SHOWMINIMIZED = 2,
            //SW_SHOWMAXIMIZED = 3,
        }

        /// <summary>
        /// Move a window.
        /// </summary>
        /// <param name="handle"></param>
        /// <param name="loc">Where to.</param>
        /// <returns></returns>
        public static bool MoveWindow(IntPtr handle, Point loc)
        {
            bool ok = GetWindowRect(handle, out Rect rect);
            if (ok)
            {
                ok = MoveWindow(handle, loc.X, loc.Y, rect.Right - rect.Left, rect.Bottom - rect.Top, true);
            }
            return ok;
        }

        /// <summary>
        /// Resize a window.
        /// </summary>
        /// <param name="handle"></param>
        /// <param name="size">How big is it.</param>
        /// <returns></returns>
        public static bool ResizeWindow(IntPtr handle, Size size)
        {
            bool ok = GetWindowRect(handle, out Rect rect);
            if (ok)
            {
                ok = MoveWindow(handle, rect.Left, rect.Top, size.Width, size.Height, true);
            }
            return ok;
        }
        #endregion

        #region Native Methods

        #region Definitions
        [StructLayout(LayoutKind.Sequential)]
        struct Rect
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        /// <summary>Contains information about a window.</summary>
        [StructLayout(LayoutKind.Sequential)]
        struct WindowInfo
        {
            // The size of the structure, in bytes.The caller must set this member to sizeof(WindowInfo).
            public uint cbSize;
            // The coordinates of the window.
            public Rect rcWindow;
            // The coordinates of the client area.
            public Rect rcClient;
            // The window styles.For a table of window styles, see Window Styles.
            public uint dwStyle;
            // The extended window styles. For a table of extended window styles, see Extended Window Styles.
            public uint dwExStyle;
            // The window status.If this member is WS_ACTIVECAPTION (0x0001), the window is active.Otherwise, this member is zero.
            public uint dwWindowStatus;
            // The width of the window border, in pixels.
            public uint cxWindowBorders;
            // The height of the window border, in pixels.
            public uint cyWindowBorders;
            // The window class atom (see RegisterClass).
            public ushort atomWindowType;
            // The Windows version of the application that created the window.
            public ushort wCreatorVersion;
        }
        #endregion

        [DllImport("user32.dll")]
        static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        static extern IntPtr GetTopWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        static extern bool IsIconic(IntPtr hWnd);

        [DllImport("user32.dll")]
        static extern bool IsZoomed(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true)]
        extern static bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, int uFlags);

        [DllImport("user32.dll")]
        static extern bool BringWindowToTop(IntPtr hWnd);

        [DllImport("user32.dll")]
        static extern bool IsWindowVisible(IntPtr hWnd);

        /// <summary>Retrieves a handle to the Shell's desktop window.</summary>
        [DllImport("user32.dll")]
        static extern IntPtr GetShellWindow();

        [DllImport("user32.dll")]
        static extern IntPtr GetParent(IntPtr hWnd);

        [DllImport("user32.dll")]
        static extern bool GetWindowInfo(IntPtr hWnd, ref WindowInfo winfo);

        /// <summary>Retrieves the thread and process ids that created the window.</summary>
        [DllImport("user32.dll")]
        static extern IntPtr GetWindowThreadProcessId(IntPtr hWnd, out IntPtr ProcessId);

        [DllImport("user32.dll", SetLastError = true)]
        static extern bool MoveWindow(IntPtr hWnd, int x, int y, int width, int height, bool repaint);

        [DllImport("user32.dll")]
        static extern bool ShowWindowAsync(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll", CharSet = CharSet.Ansi, SetLastError = true)]
        static extern bool EnumWindows(EnumWindowsCallback callback, IntPtr extraData);
        delegate bool EnumWindowsCallback(IntPtr hWnd, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true)]
        static extern bool GetWindowRect(IntPtr hWnd, out Rect lpRect);

        /// <summary>Copies the text of the specified window's title bar (if it has one) into a buffer.</summary>
        /// <param name="hwnd">handle to the window</param>
        /// <param name="lpString">StringBuilder to receive the result</param>
        /// <param name="cch">Max number of characters to copy to the buffer, including the null character. If the text exceeds this limit, it is truncated</param>
        [DllImport("user32.dll", EntryPoint = "GetWindowTextA", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true)]
        static extern int GetWindowText(IntPtr hwnd, StringBuilder lpString, int cch);

        [DllImport("user32.dll", EntryPoint = "GetWindowTextLengthA", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true)]
        static extern int GetWindowTextLength(IntPtr hwnd);
        #endregion
    }
}
