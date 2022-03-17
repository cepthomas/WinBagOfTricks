using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Drawing.Printing;
using NBagOfTricks;




namespace ClipboardEx
{
    // c# - Printing content of RichTextBox to image in WYSIWYG mode
    // I have implemented a class to export the content of RichTextBox to
    // image in WYSISYG mode so that line breaks on the screen are the same as
    // exported.

    [StructLayout(LayoutKind.Sequential)]
    public struct STRUCT_RECT
    {
        public Int32 left;
        public Int32 top;
        public Int32 right;
        public Int32 bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct STRUCT_CHARRANGE
    {
        public Int32 cpMin;
        public Int32 cpMax;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct STRUCT_FORMATRANGE
    {
        public IntPtr hdc;
        public IntPtr hdcTarget;
        public STRUCT_RECT rc;
        public STRUCT_RECT rcPage;
        public STRUCT_CHARRANGE chrg;
    }

    public class MyTry
    {
        public void RenderRtf(RichTextBox rtb, ref Bitmap bmp, int min, int max)
        {
            // Chars to render.
            STRUCT_CHARRANGE cr;
            cr.cpMin = min;
            cr.cpMax = max;

            int HundredthInchToTwips(double n)
            {
                return (int)(n * 14.4);
            }

            // Specify the area inside page margins
            STRUCT_RECT rc;
            rc.top = HundredthInchToTwips(0);
            rc.bottom = HundredthInchToTwips(bmp.Height);
            rc.left = HundredthInchToTwips(0);
            rc.right = HundredthInchToTwips(bmp.Width);

            // Specify the page area
            STRUCT_RECT rcPage;
            rcPage.top = HundredthInchToTwips(0);
            rcPage.bottom = HundredthInchToTwips(bmp.Height);
            rcPage.left = HundredthInchToTwips(0);
            rcPage.right = HundredthInchToTwips(bmp.Width);

            int res;

            using (Graphics g = Graphics.FromImage(bmp))
            {
                IntPtr hdc = g.GetHdc();

                // Fill in the FORMATRANGE struct
                STRUCT_FORMATRANGE fr;
                fr.chrg = cr;
                fr.hdc = hdc;
                fr.hdcTarget = hdc;
                fr.rc = rc;
                fr.rcPage = rcPage;


                // Allocate memory for the FORMATRANGE struct and copy the contents of our struct to this memory.
                IntPtr lParam = Marshal.AllocCoTaskMem(Marshal.SizeOf(fr));
                Marshal.StructureToPtr(fr, lParam, false);

                // wParam of 0 means measure.
                //res = NativeMethods.SendMessage(rtb.Handle, EM_FORMATRANGE, (IntPtr)1, lParam);

                // Free allocated memory
                Marshal.FreeCoTaskMem(lParam);

                // and release the device context
                g.ReleaseHdc(hdc);

                //added:
                // Free cached data from rich edit control after printing
                //NativeMethods.SendMessage(rtb.Handle, EM_FORMATRANGE, (IntPtr)0, IntPtr.Zero);

                //Return last + 1 character printer
                //int n = res;

            }

            //// Get device context of output device
            ////Graphics g = Graphics.FromImage(b);
            //IntPtr hdc = g.GetHdc();

            //// Fill in the FORMATRANGE struct
            //STRUCT_FORMATRANGE fr;
            //fr.chrg = cr;
            //fr.hdc = hdc;
            //fr.hdcTarget = hdc;
            //fr.rc = rc;
            //fr.rcPage = rcPage;

            //// Non-Zero wParam means render, Zero means measure
            //int wParam = (measureOnly ? 0 : 1);

            //// Allocate memory for the FORMATRANGE struct and copy the contents of our struct to this memory.
            //IntPtr lParam = Marshal.AllocCoTaskMem(Marshal.SizeOf(fr));
            //Marshal.StructureToPtr(fr, lParam, false);

            //// Send the actual Win32 message
            //int res = NativeMethods.SendMessage(RTB.Handle, EM_FORMATRANGE, (IntPtr)wParam, lParam);

            //// Free allocated memory
            //Marshal.FreeCoTaskMem(lParam);

            //// and release the device context
            //g.ReleaseHdc(hdc);

            //g.Dispose();

           // return res;
        }

        public void Test()
        {
            // Init some controls.
            //rtbText.Text = "Just something to copy";
            //rtbText.LoadFile(@"C:\Dev\repos\WinBagOfTricks\ClipboardEx\ex.rtf");

            //As you may know, there's a quick way to render Rich Text from a RichEdit control to a DC by means of the EM_FORMATRANGE
            //message sent to the control via SendMessage. This approach is great and it's the perfect solution when all you need is
            //to render text on a static background.


            var rtf =
                "{\\rtf1\\ansi\\ansicpg1252\\deff0\\nouicompat\\deflang1033\\deflangfe1033" +
                "{\\fonttbl" +
                "{\\f0\\fswiss\\fprq2\\fcharset0 Calibri;}" +
                "{\\f1\\fdecor\\fprq2\\fcharset0 Bauhaus 93;}" +
                "}" +
                "\r\n" +
                "{\\colortbl ;\\red0\\green77\\blue187;\\red255\\green255\\blue0;\\red255\\green0\\blue0;}" +
                "\r\n" +
                "{\\*\\generator Riched20 10.0.19041}" +
                "{\\*\\mmathPr\\mdispDef1\\mwrapIndent1440 }" +
                "\\viewkind4\\uc1 " +
                "\r\n" +
                "\\pard\\widctlpar\\sa200\\sl276\\slmult1\\f0\\fs22 >>ABC \\cf1\\fs32 def\\cf0\\fs22  \\highlight2 ghi\\highlight0\\par" +
                "\r\n" +
                "\\cf3 >>123 \\cf0\\f1\\fs36 456\\f0\\fs22\\par" +
                "\r\n" +
                ">>oo\\b\\fs36 oooo\\b0\\fs22 ooo\\par" +
                "\r\n" +
                "}" +
                "\r\n";

            //Bitmap bmp = new(picText.ClientSize.Width, picText.ClientSize.Height);
            //int res = RenderRtf(rtbText, ref bmp, 0, 30);
            //picText.Image = bmp;
            //bmp.Save(@"C:\Dev\repos\WinBagOfTricks\ClipboardEx\out.png");

        }
    }

    /// <summary>
    /// Summary description for RtfToPicture2.
    /// </summary>
    public class RtfToPicture2
    {
        public RtfToPicture2()
        {
            //
            // TODO: Add constructor logic here
            //
        }

        [DllImport("user32.dll")]
        private static extern Int32 SendMessage(IntPtr hWnd, Int32 msg, Int32 wParam, IntPtr lParam);

        private const Int32 EM_FORMATRANGE = 0x0457;

        private int m_nFirstCharOnPage;

        /// <summary>
        /// Calculate or render the contents of our RichTextBox for printing
        /// </summary>
        /// <param name="measureOnly">If true, only the calculation is performed, otherwise the text is rendered as well</param>
        /// <param name="e">The PrintPageEventArgs object from the PrintPage event</param>
        /// <param name="charFrom">Index of first character to be printed</param>
        /// <param name="charTo">Index of last character to be printed</param>
        /// <returns>(Index of last character that fitted on the page) + 1</returns>
        //public int FormatRange(bool measureOnly, PrintPageEventArgs e, int charFrom, int charTo)
        public int FormatRange(bool measureOnly, ref RichTextBox RTB, PictureBox pb, int charFrom, int charTo)
        {
            // Specify which characters to print
            STRUCT_CHARRANGE cr;
            cr.cpMin = charFrom;
            cr.cpMax = charTo;

            // Specify the area inside page margins
            STRUCT_RECT rc;
            rc.top = HundredthInchToTwips(0);
            rc.bottom = HundredthInchToTwips(pb.Height);
            rc.left = HundredthInchToTwips(0);
            rc.right = HundredthInchToTwips(pb.Width);

            // Specify the page area
            STRUCT_RECT rcPage;
            rcPage.top = HundredthInchToTwips(0);
            rcPage.bottom = HundredthInchToTwips(pb.Height);
            rcPage.left = HundredthInchToTwips(0);
            rcPage.right = HundredthInchToTwips(pb.Width);

            // Get device context of output device
            Graphics g = pb.CreateGraphics();
            IntPtr hdc = g.GetHdc();

            // Fill in the FORMATRANGE struct
            STRUCT_FORMATRANGE fr;
            fr.chrg = cr;
            fr.hdc = hdc;
            fr.hdcTarget = hdc;
            fr.rc = rc;
            fr.rcPage = rcPage;

            // Non-Zero wParam means render, Zero means measure
            Int32 wParam = (measureOnly ? 0 : 1);

            // Allocate memory for the FORMATRANGE struct and
            // copy the contents of our struct to this memory
            IntPtr lParam = Marshal.AllocCoTaskMem(Marshal.SizeOf(fr));
            Marshal.StructureToPtr(fr, lParam, false);

            // Send the actual Win32 message
            int res = SendMessage(RTB.Handle, EM_FORMATRANGE, wParam, lParam);

            // Free allocated memory
            Marshal.FreeCoTaskMem(lParam);

            // and release the device context
            g.ReleaseHdc(hdc);

            g.Dispose();

            return res;
        }

        public int FormatRange(bool measureOnly, ref RichTextBox RTB, ref Bitmap bmp, int charFrom, int charTo)
        {
            // Specify which characters to print
            STRUCT_CHARRANGE cr;
            cr.cpMin = charFrom;
            cr.cpMax = charTo;

            // Specify the area inside page margins
            STRUCT_RECT rc;
            rc.top = HundredthInchToTwips(0);
            rc.bottom = HundredthInchToTwips(bmp.Height);
            rc.left = HundredthInchToTwips(0);
            rc.right = HundredthInchToTwips(bmp.Width);

            // Specify the page area
            STRUCT_RECT rcPage;
            rcPage.top = HundredthInchToTwips(0);
            rcPage.bottom = HundredthInchToTwips(bmp.Height);
            rcPage.left = HundredthInchToTwips(0);
            rcPage.right = HundredthInchToTwips(bmp.Width);

            // Get device context of output device
            Graphics g = Graphics.FromImage(bmp);
            IntPtr hdc = g.GetHdc();

            // Fill in the FORMATRANGE struct
            STRUCT_FORMATRANGE fr;
            fr.chrg = cr;
            fr.hdc = hdc;
            fr.hdcTarget = hdc;
            fr.rc = rc;
            fr.rcPage = rcPage;

            // Non-Zero wParam means render, Zero means measure
            Int32 wParam = (measureOnly ? 0 : 1);

            // Allocate memory for the FORMATRANGE struct and
            // copy the contents of our struct to this memory
            IntPtr lParam = Marshal.AllocCoTaskMem(Marshal.SizeOf(fr));
            Marshal.StructureToPtr(fr, lParam, false);

            // Send the actual Win32 message
            int res = SendMessage(RTB.Handle, EM_FORMATRANGE, wParam, lParam);

            // Free allocated memory
            Marshal.FreeCoTaskMem(lParam);

            // and release the device context
            g.ReleaseHdc(hdc);

            g.Dispose();

            return res;
        }


        /// <summary>
        /// Convert between 1/100 inch (unit used by the .NET framework)
        /// and twips (1/1440 inch, used by Win32 API calls)
        /// </summary>
        /// <param name="n">Value in 1/100 inch</param>
        /// <returns>Value in twips</returns>
        private Int32 HundredthInchToTwips(int n)
        {
            return (Int32)(n * 14.4);
        }

        // C#
        /// <summary>
        /// Free cached data from rich edit control after printing
        /// </summary>
        public void FormatRangeDone(ref RichTextBox RTB)
        {
            SendMessage(RTB.Handle, EM_FORMATRANGE, 0, IntPtr.Zero);
        }

        public void RTFtoPictureBox(ref RichTextBox RTB, ref PictureBox PB)
        {
           m_nFirstCharOnPage = 0;

           do
           {
               m_nFirstCharOnPage = FormatRange(false, ref RTB, PB, m_nFirstCharOnPage, RTB.Text.Length);
               if (m_nFirstCharOnPage != RTB.Text.Length)
               {
                   PB.Height += 1;
                   m_nFirstCharOnPage = 0;
               }
           }
           while (m_nFirstCharOnPage < RTB.Text.Length);

           FormatRangeDone(ref RTB);
        }

       public void RTFtoBitmap(ref RichTextBox RTB, ref Bitmap b)
       {
           m_nFirstCharOnPage = 0;

           do
           {
               m_nFirstCharOnPage = FormatRange(false, ref RTB, ref b, m_nFirstCharOnPage, RTB.Text.Length);
               if (m_nFirstCharOnPage != RTB.Text.Length)
               {
                   b = new Bitmap(b.Width, b.Height + 1);
                   m_nFirstCharOnPage = 0;
               }
           }
           while (m_nFirstCharOnPage < RTB.Text.Length);

           FormatRangeDone(ref RTB);
       }
    }

    public class RichTextBoxPrintCtrl : RichTextBox
    {
        //Convert the unit used by the .NET framework (1/100 inch) 
        //and the unit used by Win32 API calls (twips 1/1440 inch)
        private const double anInch = 14.4;

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct CHARRANGE
        {
            public int cpMin;         //First character of range (0 for start of doc)
            public int cpMax;           //Last character of range (-1 for end of doc)
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct FORMATRANGE
        {
            public IntPtr hdc;             //Actual DC to draw on
            public IntPtr hdcTarget;       //Target DC for determining text formatting
            public RECT rc;                //Region of the DC to draw to (in twips)
            public RECT rcPage;            //Region of the whole DC (page size) (in twips)
            public CHARRANGE chrg;         //Range of text to draw (see earlier declaration)
        }

        private const int WM_USER = 0x0400;
        private const int EM_FORMATRANGE = WM_USER + 57;

        [DllImport("USER32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wp, IntPtr lp);

        // Render the contents of the RichTextBox for printing
        //  Return the last character printed + 1 (printing start from this point for next page)
        public int Print(int charFrom, int charTo, PrintPageEventArgs e)
        {
            //Calculate the area to render and print
            RECT rectToPrint;
            rectToPrint.Top = (int)(e.MarginBounds.Top * anInch);
            rectToPrint.Bottom = (int)(e.MarginBounds.Bottom * anInch);
            rectToPrint.Left = (int)(e.MarginBounds.Left * anInch);
            rectToPrint.Right = (int)(e.MarginBounds.Right * anInch);

            //Calculate the size of the page
            RECT rectPage;
            rectPage.Top = (int)(e.PageBounds.Top * anInch);
            rectPage.Bottom = (int)(e.PageBounds.Bottom * anInch);
            rectPage.Left = (int)(e.PageBounds.Left * anInch);
            rectPage.Right = (int)(e.PageBounds.Right * anInch);

            IntPtr hdc = e.Graphics.GetHdc();

            FORMATRANGE fmtRange;
            fmtRange.chrg.cpMax = charTo;               //Indicate character from to character to 
            fmtRange.chrg.cpMin = charFrom;
            fmtRange.hdc = hdc;                    //Use the same DC for measuring and rendering
            fmtRange.hdcTarget = hdc;              //Point at printer hDC
            fmtRange.rc = rectToPrint;             //Indicate the area on page to print
            fmtRange.rcPage = rectPage;            //Indicate size of page


            //Get the pointer to the FORMATRANGE structure in memory
            IntPtr lparam = Marshal.AllocCoTaskMem(Marshal.SizeOf(fmtRange));
            Marshal.StructureToPtr(fmtRange, lparam, false);

            //Send the rendered data for printing 
            IntPtr wparam = new(1);
            IntPtr res = SendMessage(Handle, EM_FORMATRANGE, wparam, lparam);

            //Free the block of memory allocated
            Marshal.FreeCoTaskMem(lparam);

            //Release the device context handle obtained by a previous call
            e.Graphics.ReleaseHdc(hdc);

            //Return last + 1 character printer
            return res.ToInt32();
        }
    }
}
