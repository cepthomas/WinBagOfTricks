using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Reflection;
using Microsoft.WindowsAPICodePack.Taskbar;
using Microsoft.WindowsAPICodePack.Shell;
using NBagOfTricks;
using NBagOfUis;


namespace JumpListEx
{
    public partial class JumpListEx : Form
    {
        /// <summary>The jumplist.</summary>
        JumpList _jl;

        /// <summary>Filter recents.</summary>
        readonly string _filters = "bat cmd config css csv json log md txt xml";
        // bat cmd c cpp h cc config cs csproj css csv cxx dot js json log lua md map neb np py settings txt xaml xml

        /// <summary>
        /// Constructor.
        /// </summary>
        public JumpListEx()
        {
            InitializeComponent();

            Load += JumpListEx_Load;
            Shown += JumpListEx_Shown;
        }

        /// <summary>
        /// Show stuff.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void JumpListEx_Load(object? sender, EventArgs e)
        {
            if (TaskbarManager.IsPlatformSupported)
            {
                var args = Environment.GetCommandLineArgs().ToList();
                LogMessage("INF", $"args:{args}");

                StartPosition = FormStartPosition.Manual;
                Location = new Point(200, 200);

                // Optionally show minimized.
                WindowState = FormWindowState.Minimized;
            }
            else
            {
                LogMessage("ERR", "Platform not supported");
                MessageBox.Show("Your OS is no good!");
                Environment.Exit(1);
            }
        }

        /// <summary>
        /// Apparently you need to create the jumplist afte the window is shown.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void JumpListEx_Shown(object? sender, EventArgs e)
        {
            BuildMyList();
        }

        /// <summary>
        /// Build the actual list.
        /// </summary>
        private void BuildMyList()
        {
            _jl = JumpList.CreateJumpList();
            _jl.ClearAllUserTasks();
            _jl.KnownCategoryToDisplay = JumpListKnownCategoryType.Recent;

            // Get filters.
            var filters = _filters.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToList();

            ///// Add recent files.
            DirectoryInfo diRecent = new(Environment.GetFolderPath(Environment.SpecialFolder.Recent));
            // Key is target, value is shortcut.
            Dictionary<FileInfo, FileInfo> finfos = new();

            // Get the links.
            foreach (var f in filters)
            {
                // Get the links.
                foreach (var fs in diRecent.GetFiles($"*.{f}.lnk").ToList())
                {
                    var sl = ShellObject.FromParsingName(fs.FullName);
                    var ft = ((ShellLink)sl).TargetLocation;
                    var fi1 = new FileInfo(ft);
                    finfos.Add(fi1, fs);
                }
            }

            // Most recent first.
            List<JumpListLink> recentItems = new();
            foreach (KeyValuePair<FileInfo, FileInfo> scut in finfos.OrderBy(key => key.Key.LastAccessTime).Reverse())
            {
                JumpListLink jlink = new(scut.Value.FullName, scut.Key.Name);
                recentItems.Add(jlink);
            }

            JumpListCustomCategory catRecent = new("Recent");
            catRecent.AddJumpListItems(recentItems.ToArray());
            _jl.AddCustomCategories(catRecent);

            ///// Add user tasks.
            var stPath = @"C:\Program Files\Sublime Text\sublime_text.exe";
            _jl.AddUserTasks(new JumpListLink(stPath, "Open ST")
            {
                IconReference = new IconReference(stPath, 0) // 0 is default icon
            });
            _jl.AddUserTasks(new JumpListSeparator());

            ///// Add call to myself. Note this actually goes to MainForm. TODO
            var assy = Assembly.GetEntryAssembly();
            var loc = assy!.Location.Replace(".dll", ".exe");

            _jl.AddUserTasks(new JumpListLink(loc, "Configure")
            {
                IconReference = new IconReference(loc, 0),
                Arguments = "config_taskbar"
            });
            _jl.AddUserTasks(new JumpListSeparator());

            _jl.Refresh();
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
