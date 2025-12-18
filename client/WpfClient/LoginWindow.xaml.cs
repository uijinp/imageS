using System;
using System.Windows;

namespace WpfClient
{
    public partial class LoginWindow : Window
    {
        public string HostName { get; private set; }
        public int Port { get; private set; }
        public string RoomId { get; private set; }
        public string Nickname { get; private set; }

        public LoginWindow()
        {
            InitializeComponent();
            txtNick.Text = "User" + new Random().Next(1000);
        }

        private void BtnConnect_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtHost.Text) || 
                string.IsNullOrWhiteSpace(txtPort.Text) || 
                string.IsNullOrWhiteSpace(txtRoom.Text) || 
                string.IsNullOrWhiteSpace(txtNick.Text))
            {
                MessageBox.Show("Please fill in all fields.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!int.TryParse(txtPort.Text, out int p))
            {
                MessageBox.Show("Invalid Port number.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            HostName = txtHost.Text;
            Port = p;
            RoomId = txtRoom.Text;
            Nickname = txtNick.Text;

            // Open MainWindow
            var mainWindow = new MainWindow(HostName, Port, RoomId, Nickname);
            mainWindow.Show();
            this.Close();
        }
    }
}
