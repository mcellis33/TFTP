namespace Tftp
{
    partial class TftpClientForm
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
            this.UploadButton = new System.Windows.Forms.Button();
            this.FilePathTextBox = new System.Windows.Forms.TextBox();
            this.RemoteNameTextBox = new System.Windows.Forms.TextBox();
            this.DownloadButton = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.ServerTextBox = new System.Windows.Forms.TextBox();
            this.MessageList = new System.Windows.Forms.ListView();
            this.SuspendLayout();
            // 
            // UploadButton
            // 
            this.UploadButton.Location = new System.Drawing.Point(170, 88);
            this.UploadButton.Name = "UploadButton";
            this.UploadButton.Size = new System.Drawing.Size(75, 23);
            this.UploadButton.TabIndex = 0;
            this.UploadButton.Text = "Upload";
            this.UploadButton.UseVisualStyleBackColor = true;
            this.UploadButton.Click += new System.EventHandler(this.UploadButton_Click);
            // 
            // FilePathTextBox
            // 
            this.FilePathTextBox.Location = new System.Drawing.Point(93, 12);
            this.FilePathTextBox.Name = "FilePathTextBox";
            this.FilePathTextBox.Size = new System.Drawing.Size(392, 20);
            this.FilePathTextBox.TabIndex = 1;
            this.FilePathTextBox.Text = "C:\\Users\\mellis\\Dropbox\\interview\\tftp\\Tftp\\out.txt";
            // 
            // RemoteNameTextBox
            // 
            this.RemoteNameTextBox.Location = new System.Drawing.Point(93, 62);
            this.RemoteNameTextBox.Name = "RemoteNameTextBox";
            this.RemoteNameTextBox.Size = new System.Drawing.Size(392, 20);
            this.RemoteNameTextBox.TabIndex = 2;
            this.RemoteNameTextBox.Text = "file0";
            // 
            // DownloadButton
            // 
            this.DownloadButton.Location = new System.Drawing.Point(273, 88);
            this.DownloadButton.Name = "DownloadButton";
            this.DownloadButton.Size = new System.Drawing.Size(75, 23);
            this.DownloadButton.TabIndex = 3;
            this.DownloadButton.Text = "Download";
            this.DownloadButton.UseVisualStyleBackColor = true;
            this.DownloadButton.Click += new System.EventHandler(this.DownloadButton_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(41, 15);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(48, 13);
            this.label1.TabIndex = 4;
            this.label1.Text = "File Path";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(14, 65);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(75, 13);
            this.label2.TabIndex = 5;
            this.label2.Text = "Remote Name";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(51, 39);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(38, 13);
            this.label3.TabIndex = 6;
            this.label3.Text = "Server";
            // 
            // ServerTextBox
            // 
            this.ServerTextBox.Location = new System.Drawing.Point(93, 36);
            this.ServerTextBox.Name = "ServerTextBox";
            this.ServerTextBox.Size = new System.Drawing.Size(392, 20);
            this.ServerTextBox.TabIndex = 7;
            this.ServerTextBox.Text = "127.0.0.1";
            // 
            // MessageList
            // 
            this.MessageList.Location = new System.Drawing.Point(12, 117);
            this.MessageList.Name = "MessageList";
            this.MessageList.Size = new System.Drawing.Size(473, 350);
            this.MessageList.TabIndex = 9;
            this.MessageList.UseCompatibleStateImageBehavior = false;
            // 
            // TftpClientForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(497, 479);
            this.Controls.Add(this.MessageList);
            this.Controls.Add(this.ServerTextBox);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.DownloadButton);
            this.Controls.Add(this.RemoteNameTextBox);
            this.Controls.Add(this.FilePathTextBox);
            this.Controls.Add(this.UploadButton);
            this.Name = "TftpClientForm";
            this.Text = "TFTP Client";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button UploadButton;
        private System.Windows.Forms.TextBox FilePathTextBox;
        private System.Windows.Forms.TextBox RemoteNameTextBox;
        private System.Windows.Forms.Button DownloadButton;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox ServerTextBox;
        private System.Windows.Forms.ListView MessageList;
    }
}

