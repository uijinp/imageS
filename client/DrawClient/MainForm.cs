using System;
using System.Drawing;
using System.Windows.Forms;
using System.Text;
using System.Threading.Tasks;

namespace DrawClient
{
    public class MainForm : Form
    {
        private NetworkClient _client;
        private string _nickname;

        private PictureBox _canvas;
        private RichTextBox _chatLog;
        private TextBox _chatInput;

        private Button _btnSend;
        
        private Panel topPanel;
        private Button _btnClear;

        private Bitmap _buffer;
        private Graphics _g;
        private bool _isDrawing;
        private Panel btmPanel;
        private Panel inputPanel;
        private Point _lastPoint;
        private Point? _remoteLastPoint = null;

        public MainForm()
        {
            InitializeComponent();
            
            // Wire up events
            this._canvas.MouseDown += Canvas_MouseDown;
            this._canvas.MouseMove += Canvas_MouseMove;
            this._canvas.MouseUp += Canvas_MouseUp;
            this._canvas.MouseUp += Canvas_MouseUp;
            this._btnSend.Click += BtnSend_Click;
            this._chatInput.KeyDown += ChatInput_KeyDown;
            this._btnClear.Click += BtnClear_Click;

            this.Load += MainForm_Load;
            this.FormClosing += MainForm_FormClosing;
        }

        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            this._canvas = new System.Windows.Forms.PictureBox();
            this.btmPanel = new System.Windows.Forms.Panel();
            this._chatLog = new System.Windows.Forms.RichTextBox();
            this.inputPanel = new System.Windows.Forms.Panel();
            this._btnSend = new System.Windows.Forms.Button();
            this._chatInput = new System.Windows.Forms.TextBox();

            this.topPanel = new System.Windows.Forms.Panel();
            this._btnClear = new System.Windows.Forms.Button();
            this.topPanel.SuspendLayout();

            ((System.ComponentModel.ISupportInitialize)(this._canvas)).BeginInit();
            this.btmPanel.SuspendLayout();
            this.inputPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // _canvas
            // 
            this._canvas.BackColor = System.Drawing.Color.White;
            this._canvas.Dock = System.Windows.Forms.DockStyle.Fill;
            this._canvas.Image = ((System.Drawing.Image)(resources.GetObject("_canvas.Image")));
            this._canvas.Location = new System.Drawing.Point(0, 0);
            this._canvas.Name = "_canvas";
            this._canvas.Size = new System.Drawing.Size(984, 561);
            this._canvas.TabIndex = 0;
            this._canvas.TabStop = false;
            // 
            // btmPanel
            // 
            this.btmPanel.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.btmPanel.Controls.Add(this._chatLog);
            this.btmPanel.Controls.Add(this.inputPanel);
            this.btmPanel.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.btmPanel.Location = new System.Drawing.Point(0, 561);
            this.btmPanel.Name = "btmPanel";
            this.btmPanel.Size = new System.Drawing.Size(984, 200);
            this.btmPanel.TabIndex = 1;
            // 
            // _chatLog
            // 
            this._chatLog.Dock = System.Windows.Forms.DockStyle.Top;
            this._chatLog.Location = new System.Drawing.Point(0, 0);
            this._chatLog.Name = "_chatLog";
            this._chatLog.ReadOnly = true;
            this._chatLog.Size = new System.Drawing.Size(982, 160);
            this._chatLog.TabIndex = 0;
            this._chatLog.Text = "";
            // 
            // inputPanel
            // 
            this.inputPanel.Controls.Add(this._btnSend);
            this.inputPanel.Controls.Add(this._chatInput);
            this.inputPanel.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.inputPanel.Location = new System.Drawing.Point(0, 158);
            this.inputPanel.Name = "inputPanel";
            this.inputPanel.Size = new System.Drawing.Size(982, 40);
            this.inputPanel.TabIndex = 1;
            // 
            // _btnSend
            // 
            this._btnSend.Dock = System.Windows.Forms.DockStyle.Right;
            this._btnSend.Location = new System.Drawing.Point(902, 0);
            this._btnSend.Name = "_btnSend";
            this._btnSend.Size = new System.Drawing.Size(80, 40);
            this._btnSend.TabIndex = 0;
            this._btnSend.Text = "전송";
            // 
            // _chatInput
            // 
            this._chatInput.Dock = System.Windows.Forms.DockStyle.Fill;
            this._chatInput.Location = new System.Drawing.Point(0, 0);
            this._chatInput.Name = "_chatInput";
            this._chatInput.Size = new System.Drawing.Size(982, 23);
            this._chatInput.TabIndex = 1;
            // 
            // topPanel
            // 
            this.topPanel.Controls.Add(this._btnClear);
            this.topPanel.Dock = System.Windows.Forms.DockStyle.Top;
            this.topPanel.Location = new System.Drawing.Point(0, 0);
            this.topPanel.Name = "topPanel";
            this.topPanel.Size = new System.Drawing.Size(984, 40);
            this.topPanel.TabIndex = 2;
            // 
            // _btnClear
            // 
            this._btnClear.Dock = System.Windows.Forms.DockStyle.Left;
            this._btnClear.Location = new System.Drawing.Point(0, 0);
            this._btnClear.Name = "_btnClear";
            this._btnClear.Size = new System.Drawing.Size(100, 40);
            this._btnClear.TabIndex = 0;
            this._btnClear.Text = "지우기";
            this._btnClear.UseVisualStyleBackColor = true;
            // 
            // MainForm
            // 
            this.ClientSize = new System.Drawing.Size(984, 761);
            this.ClientSize = new System.Drawing.Size(984, 761);
            this.Controls.Add(this._canvas); // Fill first, but panels might overlap if not careful with Dock order. 
            // Docker order matters. Bottom first, then Top, then Fill.
            this.Controls.Add(this.btmPanel);
            this.Controls.Add(this.topPanel);
            
            // Re-add canvas to be last (Fill takes remaining space)
            this.Controls.Remove(this._canvas);
            this.Controls.Add(this._canvas);
            this.Name = "MainForm";
            this.Text = "그림판 클라이언트";
            ((System.ComponentModel.ISupportInitialize)(this._canvas)).EndInit();
            this.topPanel.ResumeLayout(false);
            this.btmPanel.ResumeLayout(false);
            this.inputPanel.ResumeLayout(false);
            this.inputPanel.PerformLayout();
            this.ResumeLayout(false);

        }

        private async void MainForm_Load(object sender, EventArgs e)
        {
            // Init Graphics (Restored)
            _buffer = new Bitmap(1920, 1080);
            _g = Graphics.FromImage(_buffer);
            _g.Clear(Color.White);
            _canvas.Image = _buffer;

            // Show Login Dialog
            var login = new LoginForm();
            if (login.ShowDialog() != DialogResult.OK)
            {
                this.Close();
                return;
            }

            _nickname = login.Nickname;
            this.Text = $"그림판 클라이언트 - 방: {login.RoomId} - 사용자: {_nickname}";

            _client = new NetworkClient(login.HostName, login.Port);
            _client.OnDrawReceived += Client_OnDrawReceived;
            _client.OnChatReceived += Client_OnChatReceived;
            _client.OnClearReceived += Client_OnClearReceived;
            _client.OnReconnecting += Client_OnReconnecting;
            _client.OnReconnected += Client_OnReconnected;

            try
            {
                await _client.ConnectAsync(login.RoomId);
                AppendChat("시스템", "방 접속 성공: " + login.RoomId);
            }
            catch (Exception ex)
            {
                MessageBox.Show("연결 실패: " + ex.Message);
                this.Close();
            }
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            _client?.Disconnect();
        }

        // --- Drawing Logic ---

        private void Canvas_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                _isDrawing = true;
                _lastPoint = e.Location;
            }
        }

        private async void Canvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isDrawing)
            {
                DrawLine(_lastPoint, e.Location);
                
                // Send normalized coordinate (End point only for simplicity, or implement interpolation)
                // Sending just Points (0-1)
                
                var norm = CoordinateMapper.Normalize(e.X, e.Y);
                byte[] data = CoordinateMapper.ToBytes(norm);
                await _client.SendPacketAsync(PacketType.Draw, data);

                _lastPoint = e.Location;
            }
        }

        private void Canvas_MouseUp(object sender, MouseEventArgs e)
        {
            _isDrawing = false;
        }

        private void DrawLine(Point p1, Point p2)
        {
            // Draw on local buffer
            using (var pen = new Pen(Color.Black, 2))
            {
                _g.DrawLine(pen, p1, p2);
            }
            _canvas.Invalidate();
        }

        private void Client_OnDrawReceived(byte[] data)
        {
            // Received normalized point
            var pF = CoordinateMapper.FromBytes(data);
            var tuple = CoordinateMapper.Denormalize(pF);
            var pScreen = new Point(tuple.X, tuple.Y);
            
            this.Invoke(new Action(() => 
            {
                if (_remoteLastPoint.HasValue)
                {
                    // Distance check to prevent connecting lines between separate strokes
                    // (Simple heuristic: if jump is > 50 pixels, treat as new stroke)
                    // But for fast mouse movement, 50 might be small.
                    // Let's just connect everything for now, or use a threshold.
                    // Since we don't send "MouseUp", we can't know for sure.
                    // Improving "Dotted Line" issue is the priority.
                    
                    using (var pen = new Pen(Color.Red, 2))
                    {
                        _g.DrawLine(pen, _remoteLastPoint.Value, pScreen);
                    }
                }
                
                // Always draw a dot as well to fill gaps or for single clicks
                using (var brush = new SolidBrush(Color.Red))
                {
                    _g.FillEllipse(brush, pScreen.X - 1, pScreen.Y - 1, 2, 2);
                }

                _remoteLastPoint = pScreen;
                _canvas.Invalidate();
            }));

            // Reset remote point after a short timeout? No, that requires timer.
            // For now, simple connecting.
            // For now, simple connecting.
        }

        private async void BtnClear_Click(object sender, EventArgs e)
        {
            await _client.SendPacketAsync(PacketType.Clear, new byte[0]);
        }

        private void Client_OnClearReceived()
        {
            this.Invoke(new Action(() =>
            {
                _g.Clear(Color.White);
                _canvas.Invalidate();
                _remoteLastPoint = null; // Reset remote point state
                AppendChat("시스템", "캔버스가 초기화되었습니다.");
            }));
        }

        // --- Chat Logic ---

        private async void BtnSend_Click(object sender, EventArgs e)
        {
            var text = _chatInput.Text;
            if (string.IsNullOrWhiteSpace(text)) return;

            var fullMsg = $"{_nickname}: {text}";
            await _client.SendPacketAsync(PacketType.Chat, Encoding.UTF8.GetBytes(fullMsg));
            
            AppendChat("나", text);
            _chatInput.Text = "";
        }

        private void Client_OnChatReceived(string msg)
        {
            this.Invoke(new Action(() => 
            {
                _chatLog.AppendText(msg + "\n");
                _chatLog.ScrollToCaret();
            }));
        }

        private void AppendChat(string user, string msg)
        {
            _chatLog.AppendText($"[{user}] {msg}\n");
            _chatLog.ScrollToCaret();
        }
        private void ChatInput_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                BtnSend_Click(sender, e);
                e.Handled = true;
                e.SuppressKeyPress = true; // Prevent ding sound
            }
        }

        private void Client_OnReconnecting()
        {
            this.Invoke(new Action(() =>
            {
                this.Text = "그림판 클라이언트 - [재연결 시도 중...]";
                AppendChat("시스템", "서버와의 연결이 끊어졌습니다. 재연결을 시도합니다...");
            }));
        }

        private void Client_OnReconnected()
        {
            this.Invoke(new Action(() =>
            {
                this.Text = $"그림판 클라이언트 - 방: {_client.GetLastRoomId()} - 사용자: {_nickname}";
                AppendChat("시스템", "서버에 다시 연결되었습니다!");
            }));
        }
    }
}
