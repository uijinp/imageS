using System;
using System.Windows.Forms;
using System.Drawing;

namespace DrawClient
{
    public class LoginForm : Form
    {
        public string HostName { get; private set; }
        public int Port { get; private set; }
        public string RoomId { get; private set; }
        public string Nickname { get; private set; }

        private TextBox txtHost;
        private TextBox txtPort;
        private TextBox txtRoom;
        private TextBox txtNick;
        private Button btnConnect;

        public LoginForm()
        {
            this.Text = "로그인";
            this.Size = new Size(300, 250);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;

            int y = 20;
            
            // Host
            var lblHost = new Label { Text = "서버 주소:", Location = new Point(20, y), AutoSize = true };
            txtHost = new TextBox { Text = "127.0.0.1", Location = new Point(100, y), Width = 150 };
            this.Controls.Add(lblHost); this.Controls.Add(txtHost);
            y += 35;

            // Port
            var lblPort = new Label { Text = "포트:", Location = new Point(20, y), AutoSize = true };
            txtPort = new TextBox { Text = "9000", Location = new Point(100, y), Width = 150 };
            this.Controls.Add(lblPort); this.Controls.Add(txtPort);
            y += 35;

            // Room
            var lblRoom = new Label { Text = "방 번호:", Location = new Point(20, y), AutoSize = true };
            txtRoom = new TextBox { Text = "Room1", Location = new Point(100, y), Width = 150 };
            this.Controls.Add(lblRoom); this.Controls.Add(txtRoom);
            y += 35;

            // Nickname
            var lblNick = new Label { Text = "닉네임:", Location = new Point(20, y), AutoSize = true };
            txtNick = new TextBox { Text = "User" + new Random().Next(1000), Location = new Point(100, y), Width = 150 };
            this.Controls.Add(lblNick); this.Controls.Add(txtNick);
            y += 45;

            // Connect Button
            btnConnect = new Button { Text = "접속", Location = new Point(100, y), Width = 100, Height = 30 };
            btnConnect.Click += BtnConnect_Click;
            this.Controls.Add(btnConnect);
        }

        private void BtnConnect_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtHost.Text) || string.IsNullOrWhiteSpace(txtPort.Text) || 
                string.IsNullOrWhiteSpace(txtRoom.Text) || string.IsNullOrWhiteSpace(txtNick.Text))
            {
                MessageBox.Show("모든 필드를 입력해주세요.");
                return;
            }

            HostName = txtHost.Text;
            if (!int.TryParse(txtPort.Text, out int p))
            {
                MessageBox.Show("유효하지 않은 포트입니다.");
                return;
            }
            Port = p;
            RoomId = txtRoom.Text;
            Nickname = txtNick.Text;

            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}
