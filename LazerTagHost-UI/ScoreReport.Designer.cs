namespace LazerTagHostUI
{
	partial class ScoreReport
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
			System.Windows.Forms.SplitContainer splitContainer;
			this.webBrowser = new System.Windows.Forms.WebBrowser();
			this.textBox = new System.Windows.Forms.TextBox();
			splitContainer = new System.Windows.Forms.SplitContainer();
			((System.ComponentModel.ISupportInitialize)(splitContainer)).BeginInit();
			splitContainer.Panel1.SuspendLayout();
			splitContainer.Panel2.SuspendLayout();
			splitContainer.SuspendLayout();
			this.SuspendLayout();
			// 
			// splitContainer
			// 
			splitContainer.Dock = System.Windows.Forms.DockStyle.Fill;
			splitContainer.Location = new System.Drawing.Point(0, 0);
			splitContainer.Name = "splitContainer";
			splitContainer.Orientation = System.Windows.Forms.Orientation.Horizontal;
			// 
			// splitContainer.Panel1
			// 
			splitContainer.Panel1.Controls.Add(this.webBrowser);
			// 
			// splitContainer.Panel2
			// 
			splitContainer.Panel2.Controls.Add(this.textBox);
			splitContainer.Panel2Collapsed = true;
			splitContainer.Size = new System.Drawing.Size(736, 501);
			splitContainer.SplitterDistance = 417;
			splitContainer.TabIndex = 1;
			// 
			// webBrowser
			// 
			this.webBrowser.AllowNavigation = false;
			this.webBrowser.Dock = System.Windows.Forms.DockStyle.Fill;
			this.webBrowser.IsWebBrowserContextMenuEnabled = false;
			this.webBrowser.Location = new System.Drawing.Point(0, 0);
			this.webBrowser.MinimumSize = new System.Drawing.Size(20, 20);
			this.webBrowser.Name = "webBrowser";
			this.webBrowser.ScriptErrorsSuppressed = true;
			this.webBrowser.Size = new System.Drawing.Size(736, 501);
			this.webBrowser.TabIndex = 0;
			this.webBrowser.TabStop = false;
			this.webBrowser.WebBrowserShortcutsEnabled = false;
			// 
			// textBox
			// 
			this.textBox.BackColor = System.Drawing.SystemColors.Window;
			this.textBox.Dock = System.Windows.Forms.DockStyle.Fill;
			this.textBox.Font = new System.Drawing.Font("Courier New", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.textBox.Location = new System.Drawing.Point(0, 0);
			this.textBox.Multiline = true;
			this.textBox.Name = "textBox";
			this.textBox.ReadOnly = true;
			this.textBox.ScrollBars = System.Windows.Forms.ScrollBars.Both;
			this.textBox.Size = new System.Drawing.Size(736, 80);
			this.textBox.TabIndex = 0;
			this.textBox.WordWrap = false;
			// 
			// ScoreReport
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(736, 501);
			this.Controls.Add(splitContainer);
			this.Name = "ScoreReport";
			this.Text = "Score Report";
			this.WindowState = System.Windows.Forms.FormWindowState.Maximized;
			this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.ScoreReport_FormClosed);
			this.Load += new System.EventHandler(this.ScoreReport_Load);
			splitContainer.Panel1.ResumeLayout(false);
			splitContainer.Panel2.ResumeLayout(false);
			splitContainer.Panel2.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(splitContainer)).EndInit();
			splitContainer.ResumeLayout(false);
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.WebBrowser webBrowser;
		private System.Windows.Forms.TextBox textBox;

	}
}