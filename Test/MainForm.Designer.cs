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
            btn1 = new System.Windows.Forms.Button();
            btn2 = new System.Windows.Forms.Button();
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
            // btn1
            // 
            btn1.Location = new System.Drawing.Point(314, 7);
            btn1.Name = "btn1";
            btn1.Size = new System.Drawing.Size(100, 29);
            btn1.TabIndex = 62;
            btn1.Text = "btn1";
            btn1.UseVisualStyleBackColor = true;
            btn1.Click += Btn1_Click;
            // 
            // btn2
            // 
            btn2.Location = new System.Drawing.Point(414, 7);
            btn2.Name = "btn2";
            btn2.Size = new System.Drawing.Size(100, 29);
            btn2.TabIndex = 63;
            btn2.Text = "btn2";
            btn2.UseVisualStyleBackColor = true;
            btn2.Click += Btn2_Click;
            // 
            // MainForm
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(869, 400);
            Controls.Add(btn2);
            Controls.Add(btn1);
            Controls.Add(txtViewer);
            Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            Name = "MainForm";
            Text = "Test";
            ResumeLayout(false);
        }

        #endregion

        private Ephemera.NBagOfUis.TextViewer txtViewer;
        private System.Windows.Forms.Button btn1;
        private System.Windows.Forms.Button btn2;
    }
}

