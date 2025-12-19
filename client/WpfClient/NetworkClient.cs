using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WpfClient
{
    public class NetworkClient
    {
        private TcpClient _client;
        private NetworkStream _stream;
        private CancellationTokenSource _cts;
        private readonly string _host;
        private readonly int _port;
        
        // State for Reconnection
        private string _lastRoomId;
        private bool _isExplicitDisconnect;

        // Configuration
        public bool AutoReconnect { get; set; } = true;

        // Events
        public event Action<byte[]> OnDrawReceived;
        public event Action<string> OnChatReceived;
        public event Action OnClearReceived;
        public event Action OnEndStrokeReceived;
        public event Action OnReconnecting;
        public event Action OnReconnected;

        public NetworkClient(string host, int port)
        {
            _host = host;
            _port = port;
        }

        public async Task ConnectAsync(string roomId)
        {
            _lastRoomId = roomId;
            _isExplicitDisconnect = false;

            await ConnectInternalAsync();

            // Start reading loop
            _cts = new CancellationTokenSource();
            _ = ReceiveLoopAsync(_cts.Token);
        }

        private async Task ConnectInternalAsync()
        {
            _client = new TcpClient();
            _client.NoDelay = true; // Reduce latency
            await _client.ConnectAsync(_host, _port).ConfigureAwait(false);
            _stream = _client.GetStream();

            Console.WriteLine($"Connected to {_host}:{_port}");

            // 1. Send Handshake
            if (!string.IsNullOrEmpty(_lastRoomId))
            {
                byte[] roomBytes = Encoding.UTF8.GetBytes(_lastRoomId);
                await SendRawAsync(Protocol.CreatePacket(PacketType.Handshake, roomBytes)).ConfigureAwait(false);
            }
        }

        public async Task SendPacketAsync(PacketType type, byte[] data)
        {
            if (_client == null || !_client.Connected) return;
            await SendRawAsync(Protocol.CreatePacket(type, data));
        }

        private async Task SendRawAsync(byte[] packet)
        {
            // Propagate exception so callers know if send failed (critical for Handshake)
            if (_stream != null)
            {
                await _stream.WriteAsync(packet, 0, packet.Length).ConfigureAwait(false);
            }
        }

        private async Task ReceiveLoopAsync(CancellationToken token)
        {
            byte[] headerBuffer = new byte[4];
            try
            {
                while (!token.IsCancellationRequested)
                {
                    // 1. Read Length (4 bytes)
                    if (!await ReadFullAsync(headerBuffer, 4, token)) break; // Disconnected
                    int length = BitConverter.ToInt32(headerBuffer, 0);

                    // 2. Read Body (Length bytes)
                    byte[] bodyBuffer = new byte[length];
                    if (!await ReadFullAsync(bodyBuffer, length, token)) break;

                    // 3. Parse Type
                    PacketType type = (PacketType)bodyBuffer[0];
                    byte[] payload = new byte[length - 1];
                    Array.Copy(bodyBuffer, 1, payload, 0, length - 1);

                    // 4. Dispatch
                    switch (type)
                    {
                        case PacketType.Draw:
                            OnDrawReceived?.Invoke(payload);
                            break;
                        case PacketType.Chat:
                            string msg = Encoding.UTF8.GetString(payload);
                            OnChatReceived?.Invoke(msg);
                            break;
                        case PacketType.Clear:
                            OnClearReceived?.Invoke();
                            break;
                        case PacketType.EndStroke:
                            OnEndStrokeReceived?.Invoke();
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Receive error: {ex.Message}");
            }
            finally
            {
                // Cleanup current connection
                CleanupConnection();

                // Trigger Reconnect if not explicit disconnect
                if (!_isExplicitDisconnect && AutoReconnect)
                {
                    // Prevent tight loop thrashing
                    await Task.Delay(2000); 
                    await ReconnectLoopAsync();
                }
            }
        }

        private async Task ReconnectLoopAsync()
        {
            OnReconnecting?.Invoke();
            
            while (!_isExplicitDisconnect && AutoReconnect)
            {
                try
                {
                    Console.WriteLine("Attempting to reconnect...");
                    await ConnectInternalAsync();
                    
                    // If success
                    Console.WriteLine("Reconnected!");
                    OnReconnected?.Invoke();
                    
                    // Restart Receive Loop
                    _cts = new CancellationTokenSource();
                    _ = ReceiveLoopAsync(_cts.Token);
                    return;
                }
                catch (Exception)
                {
                    // Wait before retry
                    await Task.Delay(3000);
                }
            }
        }

        private void CleanupConnection()
        {
            try { _stream?.Close(); } catch { }
            try { _client?.Close(); } catch { }
        }

        private async Task<bool> ReadFullAsync(byte[] buffer, int count, CancellationToken token)
        {
            int offset = 0;
            while (offset < count)
            {
                if (_stream == null) return false;
                int read = await _stream.ReadAsync(buffer, offset, count - offset, token).ConfigureAwait(false);
                if (read == 0) return false;
                offset += read;
            }
            return true;
        }

        public void Disconnect()
        {
            _isExplicitDisconnect = true;
            _cts?.Cancel();
            CleanupConnection();
            Console.WriteLine("Disconnected.");
        }

        public string GetLastRoomId() => _lastRoomId;
    }
}
