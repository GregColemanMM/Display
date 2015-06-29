namespace DISPLAY
{
    partial class Display
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
            this.components = new System.ComponentModel.Container();
            this.openFileDialog1 = new System.Windows.Forms.OpenFileDialog();
            this.RedrawTimer = new System.Windows.Forms.Timer(this.components);
            this.RainbowTimer = new System.Windows.Forms.Timer(this.components);
            this.printDialog = new System.Windows.Forms.PrintDialog();
            this.drawingBar = new System.Windows.Forms.TrackBar();
            ((System.ComponentModel.ISupportInitialize)(this.drawingBar)).BeginInit();
            this.SuspendLayout();
            // 
            // RedrawTimer
            // 
            this.RedrawTimer.Interval = 300;
            this.RedrawTimer.Tick += new System.EventHandler(this.RedrawTimer_Tick);
            // 
            // RainbowTimer
            // 
            this.RainbowTimer.Interval = 40;
            this.RainbowTimer.Tick += new System.EventHandler(this.RainbowTimer_Tick);
            // 
            // printDialog
            // 
            this.printDialog.UseEXDialog = true;
            // 
            // drawingBar
            // 
            this.drawingBar.BackColor = System.Drawing.Color.Black;
            this.drawingBar.Location = new System.Drawing.Point(0, 0);
            this.drawingBar.Maximum = 1000;
            this.drawingBar.Minimum = 1;
            this.drawingBar.Name = "drawingBar";
            this.drawingBar.RightToLeft = System.Windows.Forms.RightToLeft.Yes;
            this.drawingBar.Size = new System.Drawing.Size(100, 45);
            this.drawingBar.TabIndex = 0;
            this.drawingBar.Value = 300;
            this.drawingBar.Scroll += new System.EventHandler(this.drawingBar_Scroll);
            this.drawingBar.KeyDown += new System.Windows.Forms.KeyEventHandler(this.drawingBar_KeyDown);
            // 
            // Display
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(344, 188);
            this.Controls.Add(this.drawingBar);
            this.Location = new System.Drawing.Point(3, 4);
            this.Name = "Display";
            this.Text = "Form1";
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.Display_FormClosed);
            this.Load += new System.EventHandler(this.Display_Load);
            this.Paint += new System.Windows.Forms.PaintEventHandler(this.Display_Paint);
            this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.Display_KeyDown);
            ((System.ComponentModel.ISupportInitialize)(this.drawingBar)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.OpenFileDialog openFileDialog1;
        private System.Windows.Forms.Timer RedrawTimer;
        private System.Windows.Forms.Timer RainbowTimer;
        private System.Windows.Forms.PrintDialog printDialog;
        private System.Windows.Forms.TrackBar drawingBar;
    }
}

