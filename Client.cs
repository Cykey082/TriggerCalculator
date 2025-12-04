using System.Net.Sockets;
using System.Threading;
using System;

namespace TriggerCalculator;

class Client : IDisposable
{
    public string Name { get; }
    public Socket socket { get; }
    public string? Operation => Interlocked.Exchange(ref _operation, null);
    private string? _operation;
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
    public Client(string ip, int port, string name)
    {
        Name = name;
        socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        // set a receive timeout so Recv doesn't block forever
        socket.ReceiveTimeout = 2000; // 2 seconds
        socket.Connect(ip, port);
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