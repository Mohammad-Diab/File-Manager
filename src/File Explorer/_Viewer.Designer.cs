namespace File_Manager
{
    partial class _Viewer
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(_Viewer));
            this.button1 = new System.Windows.Forms.Button();
            this.PdfDoc = new AxFOXITREADERLib.AxFoxitCtl();
            this.FileName = new System.Windows.Forms.TextBox();
            ((System.ComponentModel.ISupportInitialize)(this.PdfDoc)).BeginInit();
            this.SuspendLayout();
            // 
            // button1
            // 
            this.button1.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.button1.Location = new System.Drawing.Point(653, 344);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(69, 25);
            this.button1.TabIndex = 0;
            this.button1.Text = "button1";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // PdfDoc
            // 
            this.PdfDoc.Enabled = true;
            this.PdfDoc.Location = new System.Drawing.Point(0, 28);
            this.PdfDoc.Name = "PdfDoc";
            this.PdfDoc.OcxState = ((System.Windows.Forms.AxHost.State)(resources.GetObject("PdfDoc.OcxState")));
            this.PdfDoc.Size = new System.Drawing.Size(734, 355);
            this.PdfDoc.TabIndex = 1;
            // 
            // FileName
            // 
            this.FileName.BackColor = System.Drawing.Color.Black;
            this.FileName.Dock = System.Windows.Forms.DockStyle.Top;
            this.FileName.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.FileName.ForeColor = System.Drawing.Color.White;
            this.FileName.Location = new System.Drawing.Point(0, 0);
            this.FileName.Name = "FileName";
            this.FileName.ReadOnly = true;
            this.FileName.Size = new System.Drawing.Size(734, 29);
            this.FileName.TabIndex = 4;
            this.FileName.Text = "Path";
            this.FileName.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // _Viewer
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.Black;
            this.CancelButton = this.button1;
            this.ClientSize = new System.Drawing.Size(734, 381);
            this.Controls.Add(this.FileName);
            this.Controls.Add(this.PdfDoc);
            this.Controls.Add(this.button1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "_Viewer";
            this.Text = "_Viewer";
            this.Load += new System.EventHandler(this.Viewer_Load);
            ((System.ComponentModel.ISupportInitialize)(this.PdfDoc)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button button1;
        private AxFOXITREADERLib.AxFoxitCtl PdfDoc;
        private System.Windows.Forms.TextBox FileName;
    }
}