namespace Test
{
    partial class MainForm
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
            txtViewer = new Ephemera.NBagOfUis.TextViewer();
            btnTrayEx = new System.Windows.Forms.Button();
            btnJumpListEx = new System.Windows.Forms.Button();
            btnClipboardEx = new System.Windows.Forms.Button();
            button3 = new System.Windows.Forms.Button();
            button4 = new System.Windows.Forms.Button();
            SuspendLayout();
            // 
            // txtViewer
            // 
            txtViewer.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            txtViewer.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            txtViewer.Location = new System.Drawing.Point(12, 43);
            txtViewer.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            txtViewer.MaxText = 5000;
            txtViewer.Name = "txtViewer";
            txtViewer.Prompt = "";
            txtViewer.Size = new System.Drawing.Size(844, 344);
            txtViewer.TabIndex = 58;
            txtViewer.WordWrap = true;
            // 
            // btnTrayEx
            // 
            btnTrayEx.Location = new System.Drawing.Point(14, 7);
            btnTrayEx.Name = "btnTrayEx";
            btnTrayEx.Size = new System.Drawing.Size(100, 29);
            btnTrayEx.TabIndex = 59;
            btnTrayEx.Text = "TrayEx";
            btnTrayEx.UseVisualStyleBackColor = true;
            btnTrayEx.Click += TrayEx_Click;
            // 
            // btnJumpListEx
            // 
            btnJumpListEx.Location = new System.Drawing.Point(114, 7);
            btnJumpListEx.Name = "btnJumpListEx";
            btnJumpListEx.Size = new System.Drawing.Size(100, 29);
            btnJumpListEx.TabIndex = 60;
            btnJumpListEx.Text = "JumpListEx";
            btnJumpListEx.UseVisualStyleBackColor = true;
            btnJumpListEx.Click += JumpListEx_Click;
            // 
            // btnClipboardEx
            // 
            btnClipboardEx.Location = new System.Drawing.Point(214, 7);
            btnClipboardEx.Name = "btnClipboardEx";
            btnClipboardEx.Size = new System.Drawing.Size(100, 29);
            btnClipboardEx.TabIndex = 61;
            btnClipboardEx.Text = "ClipboardEx";
            btnClipboardEx.UseVisualStyleBackColor = true;
            btnClipboardEx.Click += ClipboardEx_Click;
            // 
            // button3
            // 
            button3.Location = new System.Drawing.Point(314, 7);
            button3.Name = "button3";
            button3.Size = new System.Drawing.Size(100, 29);
            button3.TabIndex = 62;
            button3.Text = "button3";
            button3.UseVisualStyleBackColor = true;
            button3.Click += button3_Click;
            // 
            // button4
            // 
            button4.Location = new System.Drawing.Point(414, 7);
            button4.Name = "button4";
            button4.Size = new System.Drawing.Size(100, 29);
            button4.TabIndex = 63;
            button4.Text = "button4";
            button4.UseVisualStyleBackColor = true;
            button4.Click += button4_Click;
            // 
            // MainForm
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(869, 400);
            Controls.Add(button4);
            Controls.Add(button3);
            Controls.Add(btnClipboardEx);
            Controls.Add(btnJumpListEx);
            Controls.Add(btnTrayEx);
            Controls.Add(txtViewer);
            Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            Name = "MainForm";
            Text = "Test";
            ResumeLayout(false);
        }

        #endregion

        private Ephemera.NBagOfUis.TextViewer txtViewer;
        private System.Windows.Forms.Button btnTrayEx;
        private System.Windows.Forms.Button btnJumpListEx;
        private System.Windows.Forms.Button btnClipboardEx;
        private System.Windows.Forms.Button button3;
        private System.Windows.Forms.Button button4;
    }
}

