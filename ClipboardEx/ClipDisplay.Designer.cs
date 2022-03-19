
namespace ClipboardEx
{
    partial class ClipDisplay
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

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.rtbText = new System.Windows.Forms.RichTextBox();
            this.picImage = new System.Windows.Forms.PictureBox();
            this.lblInfo = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.picImage)).BeginInit();
            this.SuspendLayout();
            // 
            // rtbText
            // 
            this.rtbText.BackColor = System.Drawing.SystemColors.Control;
            this.rtbText.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.rtbText.Location = new System.Drawing.Point(15, 55);
            this.rtbText.Name = "rtbText";
            this.rtbText.Size = new System.Drawing.Size(125, 30);
            this.rtbText.TabIndex = 0;
            this.rtbText.Text = "";
            // 
            // picImage
            // 
            this.picImage.Location = new System.Drawing.Point(15, 91);
            this.picImage.Name = "picImage";
            this.picImage.Size = new System.Drawing.Size(125, 31);
            this.picImage.TabIndex = 1;
            this.picImage.TabStop = false;
            // 
            // lblInfo
            // 
            this.lblInfo.BackColor = System.Drawing.Color.PaleTurquoise;
            this.lblInfo.Location = new System.Drawing.Point(-1, -1);
            this.lblInfo.Name = "lblInfo";
            this.lblInfo.Size = new System.Drawing.Size(146, 25);
            this.lblInfo.TabIndex = 2;
            this.lblInfo.Text = "Empty";
            // 
            // ClipDisplay
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.Controls.Add(this.lblInfo);
            this.Controls.Add(this.picImage);
            this.Controls.Add(this.rtbText);
            this.Name = "ClipDisplay";
            this.Size = new System.Drawing.Size(148, 148);
            this.Load += new System.EventHandler(this.ClipDisplay_Load);
            ((System.ComponentModel.ISupportInitialize)(this.picImage)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.RichTextBox rtbText;
        private System.Windows.Forms.PictureBox picImage;
        private System.Windows.Forms.Label lblInfo;
    }
}
