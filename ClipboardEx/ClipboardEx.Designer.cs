
namespace ClipboardEx
{
    partial class ClipboardEx
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        ///// <summary>
        ///// Clean up any resources being used.
        ///// </summary>
        ///// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        //protected override void Dispose(bool disposing)
        //{
        //    if (disposing && (components != null))
        //    {
        //        components.Dispose();
        //    }
        //    base.Dispose(disposing);
        //}

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.rtbInfo = new System.Windows.Forms.RichTextBox();
            this.rtbText = new System.Windows.Forms.RichTextBox();
            this.btnPaste = new System.Windows.Forms.Button();
            this.btnClear = new System.Windows.Forms.Button();
            this.timer1 = new System.Windows.Forms.Timer(this.components);
            this.lblControl = new System.Windows.Forms.Label();
            this.lblShift = new System.Windows.Forms.Label();
            this.lblAlt = new System.Windows.Forms.Label();
            this.lblLetter = new System.Windows.Forms.Label();
            this.lblMatch = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // rtbInfo
            // 
            this.rtbInfo.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.rtbInfo.Location = new System.Drawing.Point(233, 12);
            this.rtbInfo.Name = "rtbInfo";
            this.rtbInfo.Size = new System.Drawing.Size(836, 466);
            this.rtbInfo.TabIndex = 0;
            this.rtbInfo.Text = "";
            // 
            // rtbText
            // 
            this.rtbText.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.rtbText.Location = new System.Drawing.Point(12, 12);
            this.rtbText.Name = "rtbText";
            this.rtbText.Size = new System.Drawing.Size(205, 120);
            this.rtbText.TabIndex = 1;
            this.rtbText.Text = "";
            // 
            // btnPaste
            // 
            this.btnPaste.Location = new System.Drawing.Point(13, 175);
            this.btnPaste.Name = "btnPaste";
            this.btnPaste.Size = new System.Drawing.Size(94, 29);
            this.btnPaste.TabIndex = 2;
            this.btnPaste.Text = "Paste op";
            this.btnPaste.UseVisualStyleBackColor = true;
            this.btnPaste.Click += new System.EventHandler(this.Paste_Click);
            // 
            // btnClear
            // 
            this.btnClear.Location = new System.Drawing.Point(130, 175);
            this.btnClear.Name = "btnClear";
            this.btnClear.Size = new System.Drawing.Size(94, 29);
            this.btnClear.TabIndex = 3;
            this.btnClear.Text = "Clear";
            this.btnClear.UseVisualStyleBackColor = true;
            // 
            // timer1
            // 
            this.timer1.Interval = 1000;
            // 
            // lblControl
            // 
            this.lblControl.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblControl.Location = new System.Drawing.Point(12, 227);
            this.lblControl.Name = "lblControl";
            this.lblControl.Size = new System.Drawing.Size(25, 25);
            this.lblControl.TabIndex = 4;
            this.lblControl.Text = "C";
            // 
            // lblShift
            // 
            this.lblShift.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblShift.Location = new System.Drawing.Point(43, 227);
            this.lblShift.Name = "lblShift";
            this.lblShift.Size = new System.Drawing.Size(25, 25);
            this.lblShift.TabIndex = 5;
            this.lblShift.Text = "S";
            // 
            // lblAlt
            // 
            this.lblAlt.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblAlt.Location = new System.Drawing.Point(74, 227);
            this.lblAlt.Name = "lblAlt";
            this.lblAlt.Size = new System.Drawing.Size(25, 25);
            this.lblAlt.TabIndex = 6;
            this.lblAlt.Text = "A";
            // 
            // lblLetter
            // 
            this.lblLetter.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblLetter.Location = new System.Drawing.Point(105, 227);
            this.lblLetter.Name = "lblLetter";
            this.lblLetter.Size = new System.Drawing.Size(25, 25);
            this.lblLetter.TabIndex = 7;
            this.lblLetter.Text = "L";
            // 
            // lblMatch
            // 
            this.lblMatch.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblMatch.Location = new System.Drawing.Point(136, 227);
            this.lblMatch.Name = "lblMatch";
            this.lblMatch.Size = new System.Drawing.Size(25, 25);
            this.lblMatch.TabIndex = 8;
            this.lblMatch.Text = "!!";
            // 
            // ClipboardEx
            // 
            this.ClientSize = new System.Drawing.Size(1081, 490);
            this.Controls.Add(this.lblMatch);
            this.Controls.Add(this.lblLetter);
            this.Controls.Add(this.lblAlt);
            this.Controls.Add(this.lblShift);
            this.Controls.Add(this.lblControl);
            this.Controls.Add(this.btnClear);
            this.Controls.Add(this.btnPaste);
            this.Controls.Add(this.rtbText);
            this.Controls.Add(this.rtbInfo);
            this.Name = "ClipboardEx";
            this.Text = "Hoo Haa";
            this.ResumeLayout(false);

        }
        #endregion

        private System.Windows.Forms.RichTextBox rtbInfo;
        private System.Windows.Forms.RichTextBox rtbText;
        private System.Windows.Forms.Button btnPaste;
        private System.Windows.Forms.Button btnClear;
        private System.Windows.Forms.Timer timer1;
        private System.Windows.Forms.Label lblControl;
        private System.Windows.Forms.Label lblShift;
        private System.Windows.Forms.Label lblAlt;
        private System.Windows.Forms.Label lblLetter;
        private System.Windows.Forms.Label lblMatch;
    }
}