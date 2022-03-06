
namespace ClipboardEx
{
    partial class ClipboardEx
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.rtbInfo = new System.Windows.Forms.RichTextBox();
            this.rtbText = new System.Windows.Forms.RichTextBox();
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
            this.rtbText.Location = new System.Drawing.Point(10, 358);
            this.rtbText.Name = "rtbText";
            this.rtbText.Size = new System.Drawing.Size(205, 120);
            this.rtbText.TabIndex = 1;
            this.rtbText.Text = "";
            // 
            // ClipboardEx
            // 
            this.ClientSize = new System.Drawing.Size(1081, 490);
            this.Controls.Add(this.rtbText);
            this.Controls.Add(this.rtbInfo);
            this.Name = "ClipboardEx";
            this.Text = "Hoo Haa";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.ClipboardEx_FormClosing);
            this.ResumeLayout(false);

        }
        #endregion

        private System.Windows.Forms.RichTextBox rtbInfo;
        private System.Windows.Forms.RichTextBox rtbText;
    }
}