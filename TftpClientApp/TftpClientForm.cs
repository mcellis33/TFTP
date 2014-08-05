using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net;
using System.Threading;

namespace Tftp
{
    public partial class TftpClientForm : Form
    {
        private TftpClient _tftpClient;
        private CancellationTokenSource _cts;

        public TftpClientForm()
        {
            _tftpClient = new TftpClient();
            _cts = new CancellationTokenSource();
            InitializeComponent();
        }

        private void UploadButton_Click(object sender, EventArgs e)
        {

        }

        private void DownloadButton_Click(object sender, EventArgs e)
        {
            try
            {
                IPEndPoint remoteEP = new IPEndPoint(IPAddress.Parse(ServerTextBox.Text), 69);
                string fileName = RemoteNameTextBox.Text;
                string outputPath = FilePathTextBox.Text;
                _tftpClient.Read(remoteEP, fileName, outputPath, _cts.Token);
            }
            catch (Exception ex)
            {
                MessageList.Items.Add(ex.Message);
            }
        }
    }
}
