namespace WebMConverter
{
    partial class CropForm
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
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.buttonReset = new System.Windows.Forms.Button();
            this.buttonConfirm = new System.Windows.Forms.Button();
            this.pictureBoxVideo = new System.Windows.Forms.PictureBox();
            this.boxX = new System.Windows.Forms.TextBox();
            this.boxY = new System.Windows.Forms.TextBox();
            this.boxW = new System.Windows.Forms.TextBox();
            this.boxH = new System.Windows.Forms.TextBox();
            this.tableLayoutPanel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxVideo)).BeginInit();
            this.SuspendLayout();
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.ColumnCount = 4;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.tableLayoutPanel1.Controls.Add(this.boxH, 3, 1);
            this.tableLayoutPanel1.Controls.Add(this.boxW, 2, 1);
            this.tableLayoutPanel1.Controls.Add(this.boxY, 1, 1);
            this.tableLayoutPanel1.Controls.Add(this.buttonConfirm, 2, 2);
            this.tableLayoutPanel1.Controls.Add(this.pictureBoxVideo, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.buttonReset, 0, 2);
            this.tableLayoutPanel1.Controls.Add(this.boxX, 0, 1);
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel1.Location = new System.Drawing.Point(3, 3);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 3;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 27F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 31F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(298, 345);
            this.tableLayoutPanel1.TabIndex = 0;
            // 
            // buttonReset
            // 
            this.tableLayoutPanel1.SetColumnSpan(this.buttonReset, 2);
            this.buttonReset.Dock = System.Windows.Forms.DockStyle.Fill;
            this.buttonReset.Location = new System.Drawing.Point(3, 317);
            this.buttonReset.Name = "buttonReset";
            this.buttonReset.Size = new System.Drawing.Size(142, 25);
            this.buttonReset.TabIndex = 1;
            this.buttonReset.Text = "Reset";
            this.buttonReset.UseVisualStyleBackColor = true;
            // 
            // buttonConfirm
            // 
            this.tableLayoutPanel1.SetColumnSpan(this.buttonConfirm, 2);
            this.buttonConfirm.Dock = System.Windows.Forms.DockStyle.Fill;
            this.buttonConfirm.Location = new System.Drawing.Point(151, 317);
            this.buttonConfirm.Name = "buttonConfirm";
            this.buttonConfirm.Size = new System.Drawing.Size(144, 25);
            this.buttonConfirm.TabIndex = 2;
            this.buttonConfirm.Text = "Confirm";
            this.buttonConfirm.UseVisualStyleBackColor = true;
            // 
            // pictureBoxVideo
            // 
            this.tableLayoutPanel1.SetColumnSpan(this.pictureBoxVideo, 4);
            this.pictureBoxVideo.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pictureBoxVideo.Location = new System.Drawing.Point(3, 3);
            this.pictureBoxVideo.Name = "pictureBoxVideo";
            this.pictureBoxVideo.Size = new System.Drawing.Size(292, 281);
            this.pictureBoxVideo.TabIndex = 0;
            this.pictureBoxVideo.TabStop = false;
            // 
            // boxX
            // 
            this.boxX.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.boxX.Location = new System.Drawing.Point(3, 290);
            this.boxX.Name = "boxX";
            this.boxX.Size = new System.Drawing.Size(68, 20);
            this.boxX.TabIndex = 3;
            // 
            // boxY
            // 
            this.boxY.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.boxY.Location = new System.Drawing.Point(77, 290);
            this.boxY.Name = "boxY";
            this.boxY.Size = new System.Drawing.Size(68, 20);
            this.boxY.TabIndex = 4;
            // 
            // boxW
            // 
            this.boxW.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.boxW.Location = new System.Drawing.Point(151, 290);
            this.boxW.Name = "boxW";
            this.boxW.Size = new System.Drawing.Size(68, 20);
            this.boxW.TabIndex = 5;
            // 
            // boxH
            // 
            this.boxH.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.boxH.Location = new System.Drawing.Point(225, 290);
            this.boxH.Name = "boxH";
            this.boxH.Size = new System.Drawing.Size(70, 20);
            this.boxH.TabIndex = 6;
            // 
            // CropForm
            // 
            this.AcceptButton = this.buttonConfirm;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(304, 351);
            this.Controls.Add(this.tableLayoutPanel1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.SizableToolWindow;
            this.Name = "CropForm";
            this.Padding = new System.Windows.Forms.Padding(3);
            this.Text = "TODO: Implement this";
            this.tableLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxVideo)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.Button buttonConfirm;
        private System.Windows.Forms.PictureBox pictureBoxVideo;
        private System.Windows.Forms.Button buttonReset;
        private System.Windows.Forms.TextBox boxX;
        private System.Windows.Forms.TextBox boxH;
        private System.Windows.Forms.TextBox boxW;
        private System.Windows.Forms.TextBox boxY;
    }
}