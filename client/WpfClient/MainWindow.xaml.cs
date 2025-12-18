using System;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace WpfClient
{
    public partial class MainWindow : Window
    {
        private NetworkClient _client;
        private string _nickname;
        private string _roomId;

        private bool _isDrawing;
        private Point _lastPoint;
        private Point? _remoteLastPoint = null; // We might need a dict for multiple users if protocol supported it, but protocol is simple.

        public MainWindow(string host, int port, string roomId, string nickname)
        {
            InitializeComponent();
            
            _nickname = nickname;
            _roomId = roomId;

            txtTitle.Text = $"Room: {_roomId}";
            txtUser.Text = $"User: {_nickname}";

            _client = new NetworkClient(host, port);
            _client.OnDrawReceived += Client_OnDrawReceived;
            _client.OnChatReceived += Client_OnChatReceived;
            _client.OnClearReceived += Client_OnClearReceived;
            _client.OnEndStrokeReceived += Client_OnEndStrokeReceived;
            _client.OnReconnecting += Client_OnReconnecting;
            _client.OnReconnected += Client_OnReconnected;

            this.Loaded += MainWindow_Loaded;
            this.Closing += MainWindow_Closing;
            this.SizeChanged += MainWindow_SizeChanged;
        }

        private void MainWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            // Update CoordinateMapper with current canvas size
            CoordinateMapper.CanvasWidth = drawCanvas.ActualWidth;
            CoordinateMapper.CanvasHeight = drawCanvas.ActualHeight;
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                await _client.ConnectAsync(_roomId);
                AddChatMessage("System", $"Connected to {_roomId}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Connection failed: {ex.Message}");
                this.Close();
            }
        }

        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            _client?.Disconnect();
        }

        // --- Drawing ---

        private void DrawCanvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                _isDrawing = true;
                _lastPoint = e.GetPosition(drawCanvas);
                drawCanvas.CaptureMouse();
            }
        }

        private async void DrawCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isDrawing)
            {
                Point currentPoint = e.GetPosition(drawCanvas);
                
                // Draw local
                DrawLine(_lastPoint, currentPoint, Brushes.Black);

                // Send to server
                // Normalize using current canvas size
                CoordinateMapper.CanvasWidth = drawCanvas.ActualWidth;
                CoordinateMapper.CanvasHeight = drawCanvas.ActualHeight;
                
                var norm = CoordinateMapper.Normalize(currentPoint.X, currentPoint.Y);
                byte[] data = CoordinateMapper.ToBytes(norm);

                await _client.SendPacketAsync(PacketType.Draw, data);

                _lastPoint = currentPoint;
            }
        }

        private async void DrawCanvas_MouseUp(object sender, MouseButtonEventArgs e)
        {
            _isDrawing = false;
            drawCanvas.ReleaseMouseCapture();
            await _client.SendPacketAsync(PacketType.EndStroke, new byte[0]);
        }

        private void DrawLine(Point p1, Point p2, Brush brush)
        {
            Line line = new Line
            {
                X1 = p1.X,
                Y1 = p1.Y,
                X2 = p2.X,
                Y2 = p2.Y,
                Stroke = brush,
                StrokeThickness = 2
            };
            drawCanvas.Children.Add(line);
        }

        private void Client_OnDrawReceived(byte[] data)
        {
            var pF = CoordinateMapper.FromBytes(data);
            
            Dispatcher.Invoke(() =>
            {
                 // Update Mapper context just in case
                CoordinateMapper.CanvasWidth = drawCanvas.ActualWidth;
                CoordinateMapper.CanvasHeight = drawCanvas.ActualHeight;

                var tuple = CoordinateMapper.Denormalize(pF);
                Point pScreen = new Point(tuple.X, tuple.Y);

                if (_remoteLastPoint.HasValue)
                {
                    DrawLine(_remoteLastPoint.Value, pScreen, Brushes.Red);
                }
                
                // Draw dot
                Ellipse dot = new Ellipse
                {
                    Width = 2, Height = 2, Fill = Brushes.Red
                };
                Canvas.SetLeft(dot, pScreen.X - 1);
                Canvas.SetTop(dot, pScreen.Y - 1);
                drawCanvas.Children.Add(dot);

                _remoteLastPoint = pScreen;
            });
        }

        private void Client_OnClearReceived()
        {
            Dispatcher.Invoke(() =>
            {
                drawCanvas.Children.Clear();
                _remoteLastPoint = null;
                AddChatMessage("System", "Canvas Cleared.");
            });
        }

        private void Client_OnEndStrokeReceived()
        {
            Dispatcher.Invoke(() =>
            {
                _remoteLastPoint = null;
            });
        }

        private async void BtnClear_Click(object sender, RoutedEventArgs e)
        {
            await _client.SendPacketAsync(PacketType.Clear, new byte[0]);
            Client_OnClearReceived(); // Clear locally immediately
        }


        // --- Chat ---

        private async void BtnSend_Click(object sender, RoutedEventArgs e)
        {
            var text = txtChatInput.Text;
            if (string.IsNullOrWhiteSpace(text)) return;

            var fullMsg = $"{_nickname}: {text}";
            await _client.SendPacketAsync(PacketType.Chat, Encoding.UTF8.GetBytes(fullMsg));

            AddChatMessage("Me", text);
            txtChatInput.Text = "";
        }

        private void TxtChatInput_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                BtnSend_Click(sender, e);
            }
        }

        private void Client_OnChatReceived(string msg)
        {
            Dispatcher.Invoke(() =>
            {
                // Parse? Or just show. Protocol sends "Nick: Msg"
                TextBlock tb = new TextBlock { Text = msg, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0,2,0,2) };
                chatPanel.Children.Add(tb);
                scrollChat.ScrollToBottom();
            });
        }
        
        private void AddChatMessage(string user, string msg)
        {
            TextBlock tb = new TextBlock();
            tb.Text = $"{user}: {msg}";
            tb.TextWrapping = TextWrapping.Wrap;
            tb.Margin = new Thickness(0, 2, 0, 2);
            tb.Foreground = user == "System" ? Brushes.Gray : Brushes.Black;
            if (user == "Me") tb.FontWeight = FontWeights.Bold;

            chatPanel.Children.Add(tb);
            scrollChat.ScrollToBottom();
        }

        private void Client_OnReconnecting()
        {
            Dispatcher.Invoke(() =>
            {
                 this.Title = "Modern Draw Client - Reconnecting...";
                 AddChatMessage("System", "Connection lost. Reconnecting...");
            });
        }

        private void Client_OnReconnected()
        {
            Dispatcher.Invoke(() =>
            {
                 this.Title = $"Modern Draw Client - {_roomId}";
                 AddChatMessage("System", "Reconnected!");
            });
        }
    }
}
