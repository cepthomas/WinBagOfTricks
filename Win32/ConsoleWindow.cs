using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Windows.Forms;


namespace Ephemera.Win32
{
    // Experiment for manipulating consoles in a WinForms world. Needs more debugging.
    // Borrowed from https://developercommunity.visualstudio.com/t/console-output-is-gone-in-vs2017-works-fine-when-d/12166

    public class ConsoleWindow
    {
        #region Native Methods
        //private const int STD_OUTPUT_HANDLE = -11;
        //private const int STD_ERROR_HANDLE = -12;
        //private const int MY_CODE_PAGE = 437;

        public struct COORD
        {
            public short X;
            public short Y;
        };

        [Flags]
        enum DesiredAccess : uint
        {
            GenericRead = 0x80000000,
            GenericWrite = 0x40000000,
            GenericExecute = 0x20000000,
            GenericAll = 0x10000000
        }

        private enum StdHandle : int
        {
            Input = -10,
            Output = -11,
            Error = -12
        }

        private static readonly IntPtr InvalidHandleValue = new(-1);


        [DllImport("kernel32.dll", EntryPoint = "GetStdHandle", SetLastError = true, CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        private static extern IntPtr GetStdHandle(int nStdHandle);

        [DllImport("kernel32.dll", EntryPoint = "AllocConsole", SetLastError = true, CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        private static extern int AllocConsole();

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.StdCall, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        protected static extern bool FreeConsole();

        [DllImport("kernel32.dll")]
        public static extern IntPtr GetConsoleWindow();

        [DllImport("kernel32.dll")]
        public static extern bool SetConsoleScreenBufferSize(IntPtr hConsoleOutput, COORD size);

        [DllImport("user32.dll")]
        public static extern bool ShowWindow(nint hWnd, int cmdShow);

        [DllImport("user32.dll")]
        public static extern bool MoveWindow( IntPtr hWnd, int  X, int  Y, int  nWidth, int  nHeight, bool bRepaint);

        [DllImport("kernel32.dll")]
        private static extern bool SetStdHandle(StdHandle nStdHandle, IntPtr hHandle);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern IntPtr CreateFile(string lpFileName, [MarshalAs(UnmanagedType.U4)] DesiredAccess dwDesiredAccess, [MarshalAs(UnmanagedType.U4)] FileShare dwShareMode, IntPtr lpSecurityAttributes, [MarshalAs(UnmanagedType.U4)] FileMode dwCreationDisposition, [MarshalAs(UnmanagedType.U4)] FileAttributes dwFlagsAndAttributes, IntPtr hTemplateFile);
        #endregion

        public static void Hide()
        {
            FreeConsole();
        }

        public static void Show(int bufferWidth = -1, bool breakRedirection = true, int bufferHeight = 1600, int screenNum = -1 /*-1 = Any but primary*/)
        {
            AllocConsole();
            IntPtr stdOut = InvalidHandleValue, stdIn, stdErr;
            if (breakRedirection)
            {
                UnredirectConsole(out stdOut, out stdIn, out stdErr);
            }

            var outStream = Console.OpenStandardOutput();
            var errStream = Console.OpenStandardError();

            //Encoding encoding = Encoding.GetEncoding(MY_CODE_PAGE);
            Encoding encoding = Encoding.GetEncoding(0);


            StreamWriter standardOutput = new(outStream, encoding), standardError = new(errStream, encoding);
            Screen? screen = null;

            try
            {
                screen = screenNum < 0 ?
                    Screen.AllScreens.Where(s => !s.Primary).FirstOrDefault() :
                    Screen.AllScreens[Math.Min(screenNum, Screen.AllScreens.Count() - 1)];
            }
            catch (Exception e)
            {
                //???
            }

            if (bufferWidth == -1)
            {
                if (screen == null)
                {
                    bufferWidth = 180;
                }
                else
                {
                    var bwid = screen.WorkingArea.Width / 10;
                    bufferWidth = bwid > 15 ? bwid - 5 : bwid + 10;
                }
            }

            try
            {
                standardOutput.AutoFlush = true;
                standardError.AutoFlush = true;
                Console.SetOut(standardOutput);
                Console.SetError(standardError);

                if (breakRedirection)
                {
                    var coord = new COORD
                    {
                        X = (short)bufferWidth,
                        Y = (short)bufferHeight
                    };
                    SetConsoleScreenBufferSize(stdOut, coord);
                }
                else
                {
                    Console.SetBufferSize(bufferWidth, bufferHeight);
                }
            }
            catch (Exception e) // Could be redirected
            {
                Debug.WriteLine(e.ToString());
            }

            try
            {
                if (screen != null)
                {
                    var workingArea = screen.WorkingArea;
                    IntPtr hConsole = GetConsoleWindow();
                    MoveWindow(hConsole, workingArea.Left, workingArea.Top, workingArea.Width, workingArea.Height, true);
                }
            }
            catch (Exception e) // Could be redirected
            {
                Debug.WriteLine(e.ToString());
            }
        }

        public static void Maximize()
        {
            Process p = Process.GetCurrentProcess();
            ShowWindow(p.MainWindowHandle, 3); // SW_MAXIMIZE = 3
        }

        public static void UnredirectConsole(out IntPtr stdOut, out IntPtr stdIn, out IntPtr stdErr)
        {
            SetStdHandle(StdHandle.Output, stdOut = GetConsoleStandardOutput());
            SetStdHandle(StdHandle.Input, stdIn = GetConsoleStandardInput());
            SetStdHandle(StdHandle.Error, stdErr = GetConsoleStandardError());
        }

        private static IntPtr GetConsoleStandardInput()
        {
            var handle = CreateFile("CONIN$", DesiredAccess.GenericRead | DesiredAccess.GenericWrite,
                FileShare.ReadWrite, IntPtr.Zero, FileMode.Open, FileAttributes.Normal, IntPtr.Zero);
            return handle; // InvalidHandleValue
        }

        private static IntPtr GetConsoleStandardOutput()
        {
            var handle = CreateFile("CONOUT$", DesiredAccess.GenericWrite | DesiredAccess.GenericWrite,
                FileShare.ReadWrite, IntPtr.Zero, FileMode.Open, FileAttributes.Normal, IntPtr.Zero);
            return handle; // InvalidHandleValue
        }

        private static IntPtr GetConsoleStandardError()
        {
            var handle = CreateFile("CONERR$", DesiredAccess.GenericWrite | DesiredAccess.GenericWrite,
                FileShare.ReadWrite, IntPtr.Zero, FileMode.Open, FileAttributes.Normal, IntPtr.Zero);
            return handle; // InvalidHandleValue
        }
    }
}