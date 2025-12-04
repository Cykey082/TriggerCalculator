using System.Net.Sockets;
using System.Threading;
using System;

namespace TriggerCalculator;

public class Client : IDisposable
{
    public string Name { get; }
    public string? PeerName => _peerName;
    public Socket socket { get; }
    public string? Operation => Interlocked.Exchange(ref _operation, null);
    private string? _operation;
    private string? _peerName;
    private readonly System.Diagnostics.Stopwatch _sw = new System.Diagnostics.Stopwatch();
    private void Send(string str)
    {
        lock (socket)
        {
            byte[] data = System.Text.Encoding.UTF8.GetBytes(str);
            socket.Send(data);
        }
    }
    public void Recv()
    {
        byte[] buffer = new byte[1024];
        try
        {
            int received = socket.Receive(buffer);
            if (received == 0)
            {
                throw new SocketException();
            }
            //1.b"HEARTBEAT." or 2.b"OPERATION:xxxx"
            string msg = System.Text.Encoding.UTF8.GetString(buffer, 0, received);
            if (msg.StartsWith("OPERATION:"))
            {
                // 原子写入
                Interlocked.Exchange(ref _operation, msg.Substring("OPERATION:".Length));
            }
            else if (msg.StartsWith("PEERNAME:"))
            {
                _peerName = msg.Substring("PEERNAME:".Length).Trim();
            }
        }
        catch (SocketException ex)
        {
            if (ex.SocketErrorCode == SocketError.TimedOut)
            {
                return;
            }
            throw;
        }
    }
    public void SendOperation(string operation)
    {
        Send($"OPERATION:{operation}");
    }
    public void SendFind(string opponentName)
    {
        Send($"FIND:{opponentName}");
    }
    public Client(string ip, int port, string name)
    {
        Name = name;
        socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        // set a receive timeout so Recv doesn't block forever
        socket.ReceiveTimeout = 2000; // 2 seconds
        socket.Connect(ip, port);
        // announce our name to server
        Send($"NAME:{name}");
    }

    public string WaitForPeerName(int timeoutMs = 10000)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        while (true)
        {
            if (_peerName != null) return _peerName;
            try { Recv(); } catch { }
            System.Threading.Thread.Sleep(50);
            if (sw.ElapsedMilliseconds > timeoutMs) throw new TimeoutException("Timeout waiting for peer name");
        }
    }

    public void Dispose()
    {
        try
        {
            socket.Shutdown(SocketShutdown.Both);
        }
        catch { }
        try { socket.Close(); } catch { }
    }
}