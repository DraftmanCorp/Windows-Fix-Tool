namespace FixTool
{
    partial class Form1
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.PictureBox pictureBox;
        private System.Windows.Forms.Label lblManutenzione;
        private System.Windows.Forms.Label lblFix;
        private System.Windows.Forms.Label lblGestioneDischi;
        private System.Windows.Forms.Label lblUtilita;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();

            this.SuspendLayout();

            // Form properties
            this.BackColor = System.Drawing.Color.FromArgb(38, 38, 38);
            this.Text = "Windows Fix Tool";
            this.Width = 940;
            this.Height = 670;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;

            // PictureBox
            this.pictureBox = new System.Windows.Forms.PictureBox();
            this.pictureBox.Left = 0;
            this.pictureBox.Top = 0;
            this.pictureBox.Width = 220;
            this.pictureBox.Height = this.ClientSize.Height;
            this.pictureBox.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left;
            this.pictureBox.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.Controls.Add(this.pictureBox);

            // lblManutenzione
            this.lblManutenzione = new System.Windows.Forms.Label();
            this.lblManutenzione.Text = "MANUTENZIONE DI SISTEMA";
            this.lblManutenzione.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold);
            this.lblManutenzione.Size = new System.Drawing.Size(320, 30);
            this.lblManutenzione.Location = new System.Drawing.Point(240, 20);
            this.lblManutenzione.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.lblManutenzione.ForeColor = System.Drawing.Color.FromArgb(0, 153, 255);
            this.lblManutenzione.AutoSize = false;
            this.Controls.Add(this.lblManutenzione);

            // lblFix
            this.lblFix = new System.Windows.Forms.Label();
            this.lblFix.Text = "FIX E CORREZIONI VARIE";
            this.lblFix.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold);
            this.lblFix.Size = new System.Drawing.Size(320, 30);
            this.lblFix.Location = new System.Drawing.Point(240, 70);  // spostato sotto lblManutenzione
            this.lblFix.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.lblFix.ForeColor = System.Drawing.Color.FromArgb(0, 153, 255);
            this.lblFix.AutoSize = false;
            this.Controls.Add(this.lblFix);

            // lblGestioneDischi
            this.lblGestioneDischi = new System.Windows.Forms.Label();
            this.lblGestioneDischi.AutoSize = true;
            this.lblGestioneDischi.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.lblGestioneDischi.ForeColor = System.Drawing.Color.FromArgb(0, 153, 255);
            this.lblGestioneDischi.Name = "lblGestioneDischi";
            this.lblGestioneDischi.Text = "GESTIONE DISCHI";
            this.Controls.Add(this.lblGestioneDischi);

            // lblUtilita
            this.lblUtilita = new System.Windows.Forms.Label();
            this.lblUtilita.AutoSize = true;
            this.lblUtilita.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.lblUtilita.ForeColor = System.Drawing.Color.FromArgb(0, 153, 255);
            this.lblUtilita.Name = "lblUtilita";
            this.lblUtilita.Text = "UTILITÀ";
            this.Controls.Add(this.lblUtilita);

            this.ResumeLayout(false);
            this.PerformLayout();
        }
    }
}
