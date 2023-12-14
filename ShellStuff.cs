using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using Ephemera.NBagOfTricks.Slog;
using Ephemera.NBagOfTricks;
using System.Drawing;


// https://www.codeproject.com/Articles/15059/C-File-Browser

// HKEY_CLASSES_ROOT\Directory\Background\shell

/*

** tool:  https://github.com/ikas-mc/ContextMenuForWindows11
a reg:  https://www.groovypost.com/howto/add-any-program-windows-context-menu/
remove: https://www.makeuseof.com/remove-new-context-menu-items-windows-10/
add:  https://www.makeuseof.com/windows-11-add-shortcut-desktop-context-menu


https://www.pinvoke.net/default.aspx/advapi32.RegGetValue

https://learn.microsoft.com/en-us/dotnet/api/microsoft.win32.registry?view=net-8.0

https://learn.microsoft.com/en-us/windows/win32/api/shobjidl_core/nn-shobjidl_core-icontextmenu


advapi32
1: RegGetValue
        [DllImport("Advapi32.dll", EntryPoint = "RegGetValueW", CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern LONG RegGetValue(
        [DllImport("Advapi32.dll", EntryPoint = "RegGetValueW", CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern Int32 RegGetValue(
Declare Function RegGetValue Lib "advapi32.dll" (TODO) As TODO
    /// https://docs.microsoft.com/en-us/windows/desktop/api/Winreg/nf-winreg-reggetvaluea
        Api.advapi32.RegGetValue(
        Api.advapi32.RegGetValue(
Documentation
[RegGetValue] on MSDN

Summary
Retrieves the type and data for the specified registry value
C# Signature:
        /* Retrieves the type and data for the specified registry value. - Original documented types 
        [DllImport("Advapi32.dll", EntryPoint = "RegGetValueW", CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern LONG RegGetValue(
        SafeRegistryHandle hkey,
        string lpSubKey,
        string lpValue,
        EnumLib.RFlags dwFlags,
        out EnumLib.RType pdwType,
        IntPtr pvData,
        ref DWORD pcbData);

        /* Retrieves the type and data for the specified registry value. - C# Compliant types
        [DllImport("Advapi32.dll", EntryPoint = "RegGetValueW", CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern Int32 RegGetValue(
        EnumLib.HKEY hkey,
        string lpSubKey,
        string lpValue,
        RFlags dwFlags,
        out RType pdwType,
        IntPtr pvData,
        ref UInt32 pcbData);

Sample Code:
        uint pcbData = 0;
        EnumLib.RType type;
        var pvData = IntPtr.Zero;

        Api.advapi32.RegGetValue(
        EnumLib.HKEY.HKEY_CURRENT_USER,
        @"Software\LG Electronics\LG PC Suite IV\1.0", @"DS_URL",
        EnumLib.RFlags.Any,
        out type, pvData, ref pcbData);

        pvData = pvData.Reallocate(pcbData);
        Api.advapi32.RegGetValue(
        EnumLib.HKEY.HKEY_CURRENT_USER,
        @"Software\LG Electronics\LG PC Suite IV\1.0", @"DS_URL",
        type.ToFlag(),
        out type, pvData, ref pcbData);

        if (type == EnumLib.RType.RegSz)
        Console.WriteLine(pvData.ToUnicodeStr());

Alternative Managed API:
Microsoft.Win32.Registry.GetValue()
*/



// Shlwapi dll contains a collection of functions that provide support for various shell operations, such as 
//file and folder manipulation, user interface elements, and internet-related tasks.



#region shlwapi.dll
/*
AssocCreate
AssocGetPerceivedType
AssocQueryString

ColorHLSToRGB
ColorRGBToHLS  HashData  IPreviewHandler  IsOS  PathAddBackslash  PathAppend  PathBuildRoot  PathCanonicalize  PathCombine  
PathCommonPrefix  PathCompactPath  PathCompactPathEx  PathCreateFromUrl  PathFileExists  PathFindNextComponent  PathFindOnPath  
PathGetArgs  PathIsDirectory  PathIsFileSpec  PathIsHTMLFile  PathIsNetworkPath  PathIsRelative  PathIsRoot  PathIsSameRoot  
PathIsUNC  PathIsUNCServer  PathIsUNCServerShare  PathIsURL  PathMatchSpec  PathQuoteSpaces  PathRelativePathTo  PathRemoveArgs  
PathRemoveBackslash  PathRemoveBlanks  PathRemoveExtension  PathRemoveFileSpec  PathRenameExtension  PathStripPath  
PathStripToRoot  PathUndecorate  PathUnExpandEnvStrings  PathUnQuoteSpaces  SHAutoComplete  SHCreateStreamOnFile  
SHCreateStreamOnFileEx  SHLoadIndirectString  SHMessageBoxCheck  StrCmpLogicalW  StrFormatByteSize  StrFormatByteSizeA  
StrFromTimeInterval  UrlCreateFromPath
*/
#endregion

#region shell.dll
/*
DllGetVersion
DLLGETVERSIONINFO
ExtendedFileInfo
ExtractAssociatedIcon
ExtractIcon
ExtractIconEx
FileIconInit
EnumFontFamExProc
FindExecutable
IShellIcon
IsNetDrive
ITaskbarList
ITaskbarList2
ITaskbarList3
ITaskbarList4
KNOWNFOLDERID
SHAddToRecentDocs
SHAppBarMessage
SHBrowseForFolder
ShellAbout
ShellExecute
ShellExecuteEx
ShellExecuteExW
Shell_GetImageLists
Shell_NotifyIcon
Shell_NotifyIconGetRect
SHGetDesktopFolder
SHGetFolderLocation
SHGetFolderPath
SHGetKnownFolderPath
SHGetImageList
SHGetSpecialFolderLocation
SHGetSpecialFolderPath
SHGetSpecialFolderPathA
SHGetStockIconInfo
SHSetKnownFolderPath

api   APPBARDATA   APPBARDATA   BatchExec   CharSet   CommandLineToArgvW   CSIDL   CSIDL   dll ILCLONEFULL   DoEnvironmentSubst   
DragAcceptFiles   DragFinish   DragQueryFile   DragQueryPoint   DuplicateIcon   ERazMA   FileSystemWatcher   FZ79pQ   
GetFinalPathNameByHandle   HChangeNotifyEventID   HChangeNotifyFlags   ILClone   ILCombine   ILCreateFromPath   ILFindLastID   
ILFree   ILIsEqual   ILIsParent   ILRemoveLastID   IsUserAnAdmin   ljlsjsf   PathCleanupSpec   PathIsExe   PathMakeUniqueName   
PathYetAnotherMakeUniqueName   PickIconDlg   Run   SetCurrentProcessExplicitAppUserModelID   SHBindToParent   SHChangeNotify   
SHChangeNotifyRegister   SHChangeNotifyUnregister   SHCNRF   SHCreateDirectoryEx   SHCreateItemFromIDList   SHCreateItemFromParsingName
SHCreateItemWithParent   SHCreateProcessAsUserW   SHEmptyRecycleBin   SHFileOperation   SHFormatDrive   SHFreeNameMappings   
SHGetDataFromIDList   SHGetDiskFreeSpace   SHGetFileInfo   SHGetFileInfoA   SHGetIconOverlayIndex   SHGetIDListFromObject   
SHGetInstanceExplorer   SHGetMalloc   SHGetNameFromIDList   SHGetNewLinkInfo   SHGetPathFromIDList   
SHGetPropertyStoreFromParsingNamehtml   SHGetRealIDL   SHGetSetSettings   SHGetSettings   SHInvokePrinterCommand   
SHIsFileAvailableOffline   SHLoadInProc   SHLoadNonloadedIconOverlayIdentifiers   SHObjectProperties   SHOpenFolderAndSelectItems   
SHOpenWithDialog   SHParseDisplayName   SHParseDisplayName   SHPathPrepareForWrite   SHQueryRecycleBin
SHQueryUserNotificationState   SHRunFileDialog   SHSetUnreadMailCount   StartInfo   THUMBBUTTON   ultimate   virt girl hd
*/
#endregion


namespace Ephemera.Xplr
{
    public class ShellStuff
    {
        public List<string> DoFileAssociation()
        {
            List<string> vals = new();

            foreach (var ext in new[] { ".doc", ".txt" })
            {
                //vals.AddRange(GetFileAssociationInfo(ext));

                vals.Add(GetFileAssociationInfo(NativeMethods.AssocStr.Command, ext)); // this one
                vals.Add(GetFileAssociationInfo(NativeMethods.AssocStr.Executable, ext));
                vals.Add(GetFileAssociationInfo(NativeMethods.AssocStr.FriendlyAppName, ext));
                vals.Add(GetFileAssociationInfo(NativeMethods.AssocStr.FriendlyDocName, ext));
            }

            return vals;
        }

        public string GetFileAssociationInfo(NativeMethods.AssocStr assocStr, string ext, string verb = null)
        {
            uint pcchOut = 0;
            NativeMethods.AssocQueryString(NativeMethods.AssocF.Verify, assocStr, ext, verb, null, ref pcchOut);
            StringBuilder pszOut = new((int)pcchOut);
            NativeMethods.AssocQueryString(NativeMethods.AssocF.Verify, assocStr, ext, verb, pszOut, ref pcchOut);
            return pszOut.ToString();
        }

        // Fancier one.
        public string GetFileAssociationInfo2(string ext, string verb = null)
        {
            if (ext[0] != '.')
            {
                ext = "." + ext;
            }

            string executablePath = GetFileAssociationInfo(NativeMethods.AssocStr.Executable, ext, verb); // Will only work for 'open' verb

            if (string.IsNullOrEmpty(executablePath))
            {
                executablePath = GetFileAssociationInfo(NativeMethods.AssocStr.Command, ext, verb); // required to find command of any other verb than 'open'

                // Extract only the path
                if (!string.IsNullOrEmpty(executablePath) && executablePath.Length > 1)
                {
                    if (executablePath[0] == '"')
                    {
                        executablePath = executablePath.Split('\"')[1];
                    }
                    else if (executablePath[0] == '\'')
                    {
                        executablePath = executablePath.Split('\'')[1];
                    }
                }
            }

            // Ensure to not return the default OpenWith.exe associated executable in Windows 8 or higher
            if (!string.IsNullOrEmpty(executablePath) && File.Exists(executablePath) && !executablePath.ToLower().EndsWith(".dll"))
            {
                if (executablePath.ToLower().EndsWith("openwith.exe"))
                {
                    return null; // 'OpenWith.exe' is th windows 8 or higher default for unknown extensions. I don't want to have it as associted file
                }
                return executablePath;
            }
            return executablePath;
        }

        public string ExecInNewProcess()
        {
            string ret = "Nada";

            // https://stackoverflow.com/questions/1469764/run-command-prompt-commands

            // One:
            Process process = new();
            ProcessStartInfo startInfo = new();
            startInfo.WindowStyle = ProcessWindowStyle.Hidden;
            startInfo.FileName = "cmd.exe";
            startInfo.Arguments = "/C copy /b Image1.jpg + Archive.rar Image2.jpg";
            process.StartInfo = startInfo;
            process.Start();

            // Another:
            Process cmd = new();
            cmd.StartInfo.FileName = "cmd.exe";
            cmd.StartInfo.RedirectStandardInput = true;
            cmd.StartInfo.RedirectStandardOutput = true;
            cmd.StartInfo.CreateNoWindow = true;
            cmd.StartInfo.UseShellExecute = false;
            cmd.Start();
            cmd.StandardInput.WriteLine("echo Oscar");
            cmd.StandardInput.Flush();
            cmd.StandardInput.Close();
            cmd.WaitForExit(); // wait for the process to complete before continuing and process.ExitCode
            ret = cmd.StandardOutput.ReadToEnd();

            return ret;
        }

        public Icon GetSmallFolderIcon()
        {
            return GetIcon("folder", NativeMethods.SHGFI.SmallIcon | NativeMethods.SHGFI.UseFileAttributes, true);
        }

        public Icon GetSmallIcon(string fileName)
        {
            return GetIcon(fileName, NativeMethods.SHGFI.SmallIcon);
        }

        public Icon GetSmallIconFromExtension(string extension)
        {
            return GetIcon(extension, NativeMethods.SHGFI.SmallIcon | NativeMethods.SHGFI.UseFileAttributes);
        }

        private Icon GetIcon(string fileName, NativeMethods.SHGFI flags, bool isFolder = false)
        {
            NativeMethods.SHFILEINFO shinfo = new();
            NativeMethods.SHGetFileInfo(fileName, isFolder ? NativeMethods.FILE_ATTRIBUTE_DIRECTORY : NativeMethods.FILE_ATTRIBUTE_NORMAL, ref shinfo, (uint)Marshal.SizeOf(shinfo), (uint)(NativeMethods.SHGFI.Icon | flags));
            Icon icon = (Icon)Icon.FromHandle(shinfo.hIcon).Clone();
            NativeMethods.DestroyIcon(shinfo.hIcon);
            return icon;
        }
    }

    // The interop signatures.
    public static class NativeMethods
    {
        [Flags]
        public enum AssocF
        {
            Init_NoRemapCLSID = 0x1,
            Init_ByExeName = 0x2,
            Open_ByExeName = 0x2,
            Init_DefaultToStar = 0x4,
            Init_DefaultToFolder = 0x8,
            NoUserSettings = 0x10,
            NoTruncate = 0x20,
            Verify = 0x40,
            RemapRunDll = 0x80,
            NoFixUps = 0x100,
            IgnoreBaseClass = 0x200
        }

        public enum AssocStr
        {
            Command = 1,
            Executable,
            FriendlyDocName,
            FriendlyAppName,
            NoOpen,
            ShellNewValue,
            DDECommand,
            DDEIfExec,
            DDEApplication,
            DDETopic
        }

        [DllImport("Shlwapi.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern uint AssocQueryString(AssocF flags, AssocStr str, string pszAssoc, string pszExtra,
            [Out] StringBuilder pszOut, [In][Out] ref uint pcchOut);


        #region Interop constants
        public const uint FILE_ATTRIBUTE_NORMAL = 0x80;
        public const uint FILE_ATTRIBUTE_DIRECTORY = 0x10;
        #endregion

        #region Interop data types
        [StructLayout(LayoutKind.Sequential)]
        public struct SHFILEINFO
        {
            public IntPtr hIcon;
            public IntPtr iIcon;
            public uint dwAttributes;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string szDisplayName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
            public string szTypeName;
        }

        [Flags]
        public enum SHGFI : int
        {
            /// <summary>get icon</summary>
            Icon = 0x000000100,
            /// <summary>get display name</summary>
            DisplayName = 0x000000200,
            /// <summary>get type name</summary>
            TypeName = 0x000000400,
            /// <summary>get attributes</summary>
            Attributes = 0x000000800,
            /// <summary>get icon location</summary>
            IconLocation = 0x000001000,
            /// <summary>return exe type</summary>
            ExeType = 0x000002000,
            /// <summary>get system icon index</summary>
            SysIconIndex = 0x000004000,
            /// <summary>put a link overlay on icon</summary>
            LinkOverlay = 0x000008000,
            /// <summary>show icon in selected state</summary>
            Selected = 0x000010000,
            /// <summary>get only specified attributes</summary>
            Attr_Specified = 0x000020000,
            /// <summary>get large icon</summary>
            LargeIcon = 0x000000000,
            /// <summary>get small icon</summary>
            SmallIcon = 0x000000001,
            /// <summary>get open icon</summary>
            OpenIcon = 0x000000002,
            /// <summary>get shell size icon</summary>
            ShellIconSize = 0x000000004,
            /// <summary>pszPath is a pidl</summary>
            PIDL = 0x000000008,
            /// <summary>use passed dwFileAttribute</summary>
            UseFileAttributes = 0x000000010,
            /// <summary>apply the appropriate overlays</summary>
            AddOverlays = 0x000000020,
            /// <summary>Get the index of the overlay in the upper 8 bits of the iIcon</summary>
            OverlayIndex = 0x000000040,
        }

        [DllImport("shell32.dll")]
        public static extern IntPtr SHGetFileInfo(string pszPath, uint dwFileAttributes, ref SHFILEINFO psfi, uint cbSizeFileInfo, uint uFlags);

        [DllImport("User32.dll")]
        public static extern int DestroyIcon(IntPtr hIcon);
        #endregion
    }
}
