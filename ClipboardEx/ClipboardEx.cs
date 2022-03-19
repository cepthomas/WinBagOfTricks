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


// Windows logo key 
// Open or close Start.
// Windows logo key  + A
// Open Action center.
// Windows logo key  + B
// Set focus in the notification area.
// Windows logo key  + C
// Open Cortana in listening mode.
// Notes
// This shortcut is turned off by default. To turn it on, select Start  > Settings  > Cortana, and turn on the toggle under Let Cortana listen for my commands when I press the Windows logo key + C.
// Cortana is available only in certain countries/regions, and some Cortana features might not be available everywhere. If Cortana isn't available or is turned off, you can still use search.
// Windows logo key  + Shift + C
// Open the charms menu.
// Windows logo key  + D
// Display and hide the desktop.
// Windows logo key  + Alt + D
// Display and hide the date and time on the desktop.
// Windows logo key  + E
// Open File Explorer.
// Windows logo key  + F
// Open Feedback Hub and take a screenshot.
// Windows logo key  + G
// Open Game bar when a game is open.
// Windows logo key  + Alt + B
// Turn HDR on or off.
// Note: Applies to the Xbox Game Bar app version 5.721.7292.0 or newer. To update your Xbox Game Bar, go to the Microsoft Store app and check for updates.
// Windows logo key  + H
// Start dictation.
// Windows logo key  + I
// Open Settings.
// Windows logo key  + J
// Set focus to a Windows tip when one is available.
// When a Windows tip appears, bring focus to the Tip.  Pressing the keyboard shortcuts again to bring focus to the element on the screen to which the Windows tip is anchored.
// Windows logo key  + K
// Open the Connect quick action.
// Windows logo key  + L
// Lock your PC or switch accounts.
// Windows logo key  + M
// Minimize all windows.
// Windows logo key  + O
// Lock device orientation.
// Windows logo key  + P
// Choose a presentation display mode.
// Windows logo key  + Ctrl + Q
// Open Quick Assist.
// Windows logo key  + R
// Open the Run dialog box.
// Windows logo key  + S
// Open search.
// Windows logo key  + Shift + S
// Take a screenshot of part of  your screen.
// Windows logo key  + T
// Cycle through apps on the taskbar.
// Windows logo key  + U
// Open Ease of Access Center.
// Windows logo key  + V
// Open the clipboard. 
// Note
// To activate this shortcut, select Start  > Settings  > System  > Clipboard, and turn on the toggle under Clipboard history.
// Windows logo key  + Shift + V
// Cycle through notifications.
// Windows logo key  + X
// Open the Quick Link menu.
// Windows logo key  + Y
// Switch input between Windows Mixed Reality and your desktop.
// Windows logo key  + Z
// Show the commands available in an app in full-screen mode.
// Windows logo key  + period (.) or semicolon (;)
// Open emoji panel.
// Windows logo key  + comma (,)
// Temporarily peek at the desktop.
// Windows logo key  + Pause
// Display the System Properties dialog box.
// Windows logo key  + Ctrl + F
// Search for PCs (if you're on a network).
// Windows logo key  + Shift + M
// Restore minimized windows on the desktop.
// Windows logo key  + number
// Open the desktop and start the app pinned to the taskbar in the position indicated by the number. If the app is already running, switch to that app.
// Windows logo key  + Shift + number
// Open the desktop and start a new instance of the app pinned to the taskbar in the position indicated by the number.
// Windows logo key  + Ctrl + number
// Open the desktop and switch to the last active window of the app pinned to the taskbar in the position indicated by the number.
// Windows logo key  + Alt + number
// Open the desktop and open the Jump List for the app pinned to the taskbar in the position indicated by the number.
// Windows logo key  + Ctrl + Shift + number
// Open the desktop and open a new instance of the app located at the given position on the taskbar as an administrator.
// Windows logo key  + Tab
// Open Task view.
// Windows logo key  + Up arrow
// Maximize the window.
// Windows logo key  + Down arrow
// Remove current app from screen or minimize the desktop window.
// Windows logo key  + Left arrow
// Maximize the app or desktop window to the left side of the screen.
// Windows logo key  + Right arrow
// Maximize the app or desktop window to the right side of the screen.
// Windows logo key  + Home
// Minimize all except the active desktop window (restores all windows on second stroke).
// Windows logo key  + Shift + Up arrow
// Stretch the desktop window to the top and bottom of the screen.
// Windows logo key  + Shift + Down arrow
// Restore/minimize active desktop windows vertically, maintaining width.
// Windows logo key  + Shift + Left arrow or Right arrow
// Move an app or window in the desktop from one monitor to another.
// Windows logo key  + Spacebar
// Switch input language and keyboard layout.
// Windows logo key  + Ctrl + Spacebar
// Change to a previously selected input.
// Windows logo key  + Ctrl + Enter
// Turn on Narrator.
// Windows logo key  + Plus (+)
// Open Magnifier.
// Windows logo key  + forward slash (/)
// Begin IME reconversion.
// Windows logo key  + Ctrl + V
// Open shoulder taps.
// Windows logo key  + Ctrl + Shift + B
// Wake PC from blank or black screen.


namespace ClipboardEx
{
    #region Types
    /// <summary>For internal management.</summary>
    public enum ClipType { PlainText, RichText, Image, FileList, Other };

    /// <summary>For key spec.</summary>
    public enum Modifiers { None = 0, Control = 0b0001, Shift = 0b0010, Alt = 0b0100, ControlShift = 0b0011, ControlAlt = 0b0101, ShiftAlt = 0b0110, ControlShiftAlt = 0b0111 };
    #endregion

    /// <summary>
    /// - Handles all interactions at the Clipboard.XXX() API level.
    /// - Hooks keyboard to intercept magic paste key.
    /// </summary>
    public partial class ClipboardEx : Form
    {
        #region Types
        /// <summary>One handled clipboard API message.</summary>
        record MsgSpec(string Name, Func<Message, uint> Handler, string Description);

        /// <summary>One entry in the collection.</summary>
        class Clip
        {
            public object? Data { get; set; } = null;
            public ClipType Ctype { get; set; } = ClipType.Other;
            public string Text { get; set; } = "";
            public Bitmap? Bitmap { get; set; } = null;
            public string OrigApp { get; set; } = "N/A";
            public string OrigTitle { get; set; } = "N/A";
            public override string ToString() => $"{Ctype}";
        }
        #endregion

        #region Fields
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

        /// <summary>Manage resources.</summary>
        bool _disposed;
        #endregion

        #region Debug Stuff
        /// <summary>Debug.</summary>
        int _ticks = 0;

        /// <summary></summary>
        bool _busy = false;

        /// <summary></summary>
        bool _debug = true;
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

            // Init clip displays.
            int x = 5;
            int y = 5;
            for(int i = 0; i < MAX_CLIPS; i++)
            {
                ClipDisplay cd = new() { Location = new Point(x, y), Id = i };
                //                cd.Hide();
                cd.Visible = _debug;
                _displays.Add(cd);
                Controls.Add(cd);
                cd.ClipEvent += Cd_ClipEvent;
                x = cd.Right + 5;
            }
            var borderWidth = (Width - ClientSize.Width) / 2;
            Width = x + borderWidth * 2;
            

            // Init controls.
            tvInfo.Colors.Add("ERR", Color.Pink);
            tvInfo.Colors.Add("DBG", Color.LightGreen);
            tvInfo.BackColor = Color.Cornsilk;

            btnClear.Click += (_, __) => tvInfo.Clear();
            lblLetter.Text = (UserSettings.TheSettings.KeyTrigger & Keys.KeyCode).ToString();

            _nextCb = NativeMethods.SetClipboardViewer(Handle);

            // HL messages of interest.
            _clipboardMessages = new()
            {
                { WM_DRAWCLIPBOARD, new("WM_DRAWCLIPBOARD", CbDraw, "Sent to the first window in the clipboard viewer chain when the content of the clipboard changes aka copy/cut.") },
                { WM_CHANGECBCHAIN, new("WM_CHANGECBCHAIN", CbChange, "Sent to the first window in the clipboard viewer chain when a window is being removed from the chain.") },
                { WM_CLIPBOARDUPDATE, new("WM_CLIPBOARDUPDATE", CbDefault, "Sent when the contents of the clipboard have changed. TODO like draw?") },
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
            timer1.Tick += (_, __) => { if (_ticks-- > 0) { Clipboard.SetText($"XXXXX{_ticks}"); TriggerPaste(); } };
            // timer1.Enabled = true;
        }

        // TODO cleaning up?
        ///// <summary>
        ///// Override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources.
        ///// </summary>
        //~ClipboardEx()
        //{
        //    // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method.
        //    Dispose(false);
        //}
        ///// <summary>
        ///// Boilerplate.
        ///// </summary>
        //public new void Dispose() // TODO why do I need new()?
        //{
        //    // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method.
        //    Dispose(true);
        //    GC.SuppressFinalize(this);
        //    base.Dispose();
        //}

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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Cd_ClipEvent(object? sender, ClipDisplay.ClipEventArgs e)
        {
            if (sender is not null)
            {
                // Paste this selection.

                var cd = (ClipDisplay)sender;

                switch(e.EventType)
                {
                    case ClipDisplay.ClipEventType.Click:
                        // TODO do paste.
                        LogMessage("DBG", "!!! Got a click");
                        break;

                }


                //TODO left, right, delete...
            }
        }

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

            for (int i = 0; i < MAX_CLIPS; i++)
            {
                var ds = _displays[i];
                ds.Show();

                if (i < _clips.Count)
                {
                    var clip = _clips.ElementAt(i);

                    switch (clip.Ctype)
                    {
                        case ClipType.Image:
                            ds.SetImage(clip.Bitmap);
                            break;

                        case ClipType.Other:
                            ds.SetOther("TODO");
                            break;

                        default:
                            ds.SetText(clip.Ctype.ToString(), clip.Text);
                            break;
                    }
                }
                else
                {
                    ds.SetText("N/A", "Hidden");
                    ds.Visible = _debug;
                }
            }
        }


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
                // Ignore, pass along.
                base.WndProc(ref m);
            }
        }

        /// <summary>
        /// Process the clipboard draw message becuase contents have changed.
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
                    int res = NativeMethods.GetWindowText(hwnd, title, 100);

                    if(res > 0)
                    {
                        LogMessage("INF", $"COPY appName:{appName} procName:{procName} title:{title}");

                        // Data type info.
                        var dtypes = dobj.GetFormats();
                        //var stypes = $"dtypes:{string.Join(",", dtypes)}";
                        //LogMessage("INF", stypes);

                        Clip clip = new()
                        {
                            Ctype = ClipType.Other,
                            Data = Clipboard.GetDataObject(),
                            OrigApp = appName ?? "N/A",
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

                        //LogMessage("INF", $"COPY {clip}");
                        _clips.AddFirst(clip);
                        UpdateClipDisplays();
                    }
                    else
                    {
                        LogMessage("ERR", $"res:{res}");
                    }
                }
                else
                {
                    LogMessage("ERR", $"GetDataObject() is null");
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
            uint tid = NativeMethods.GetWindowThreadProcessId(hwnd, out uint lpdwProcessId);
            var p = Process.GetProcessById((int)lpdwProcessId);
            LogMessage("DBG", $"FileName:{p.MainModule!.FileName} pid:{ lpdwProcessId} tid:{tid}");

            // This does work. Virtual keycodes from https://docs.microsoft.com/en-us/windows/win32/inputdev/virtual-key-codes
            byte vkey = 0x56; // 'v'
            byte ctrl = 0x11;
            int KEYEVENTF_KEYUP = 0x0002;
            NativeMethods.keybd_event(ctrl, 0, 0, 0);
            NativeMethods.keybd_event(vkey, 0, 0, 0);
            NativeMethods.keybd_event(ctrl, 0, KEYEVENTF_KEYUP, 0);
            NativeMethods.keybd_event(vkey, 0, KEYEVENTF_KEYUP, 0);

            // Note that this doesn't work:
            //NativeMethods.SendMessage(hwnd, 0x0302, IntPtr.Zero, IntPtr.Zero); // NativeMethods.WM_PASTE
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
                    bool myLetter = false;
                    bool controlPressed = false;
                    bool shiftPressed = false;
                    bool altPressed = false;


                    switch (key)
                    {
                        case Keys.LControlKey:
                        case Keys.RControlKey:
                            controlPressed = pressed;
                            break;

                        case Keys.LShiftKey:
                        case Keys.RShiftKey:
                            shiftPressed = pressed;
                            break;

                        case Keys.LMenu:
                        case Keys.RMenu:
                            altPressed = pressed;
                            break;

                        default:
                            if(key == UserSettings.TheSettings.KeyTrigger)
                            {
                                myLetter = pressed;
                            }
                            break;
                    }

                    // Is this the magic key?
                    bool match = myLetter;
                    if (match)
                    {
                        switch (UserSettings.TheSettings.ModTrigger)
                        {
                            case Modifiers.None:
                                match = !controlPressed && !shiftPressed && !altPressed;
                                break;

                            case Modifiers.Control:
                                match = controlPressed && !shiftPressed && !altPressed;
                                break;

                            case Modifiers.Shift:
                                match = !controlPressed && shiftPressed && !altPressed;
                                break;

                            case Modifiers.Alt:
                                match = !controlPressed && !shiftPressed && altPressed;
                                break;

                            case Modifiers.ControlShift:
                                match = controlPressed && shiftPressed && !altPressed;
                                break;

                            case Modifiers.ControlAlt:
                                match = controlPressed && !shiftPressed && altPressed;
                                break;

                            case Modifiers.ShiftAlt:
                                match = !controlPressed && shiftPressed && altPressed;
                                break;

                            case Modifiers.ControlShiftAlt:
                                match = controlPressed && shiftPressed && altPressed;
                                break;
                        }
                    }

                    // Diagnostics.
                    lblControl.BackColor = controlPressed ? UserSettings.TheSettings.ControlColor : Color.Transparent;
                    lblShift.BackColor = shiftPressed ? UserSettings.TheSettings.ControlColor : Color.Transparent;
                    lblAlt.BackColor = altPressed ? UserSettings.TheSettings.ControlColor : Color.Transparent;
                    lblLetter.BackColor = myLetter ? UserSettings.TheSettings.ControlColor : Color.Transparent;
                    lblMatch.BackColor = match ? UserSettings.TheSettings.ControlColor : Color.Transparent;

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
        private void Debug_Click(object sender, EventArgs e)
        {


            ////////////////////////////////////////////////////
            //Clipboard.SetText(tvInfo.Text);


            ////////////////////////////////////////////////////
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
            tvInfo.Add(s);
        }
    }
}