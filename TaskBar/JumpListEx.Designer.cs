
namespace JumpListEx
{
    partial class JumpListEx
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
        /// <summary>
        /// 
        /// </summary>
        private void InitializeComponent()
        {
            this.rtbInfo = new System.Windows.Forms.RichTextBox();
            this.SuspendLayout();
            // 
            // rtbInfo
            // 
            this.rtbInfo.Location = new System.Drawing.Point(72, 32);
            this.rtbInfo.Name = "rtbInfo";
            this.rtbInfo.Size = new System.Drawing.Size(407, 218);
            this.rtbInfo.TabIndex = 0;
            this.rtbInfo.Text = "";
            // 
            // JumpListEx
            // 
            this.ClientSize = new System.Drawing.Size(596, 320);
            this.Controls.Add(this.rtbInfo);
            this.Name = "JumpListEx";
            this.ResumeLayout(false);

        }
        #endregion

        private System.Windows.Forms.RichTextBox rtbInfo;
    }
}