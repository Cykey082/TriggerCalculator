using TriggerCalculator;

Storage storage;
Console.WriteLine("是否启用多人模式？(y/N)：");
var key = Console.ReadKey(true);
bool multi = key.Key == ConsoleKey.Y;
string host = "127.0.0.1";
int port = 12345;
string name = "Player";
if (multi)
{
    Console.WriteLine();
    Console.WriteLine($"使用默认网络配置: host={host} port={port}");
    Console.Write($"按 Enter 接受默认，或输入 host[:port]（例如 {host}:{port}）：");
    var input = Console.ReadLine();
    if (!string.IsNullOrWhiteSpace(input))
    {
        var hp = input.Split(':');
        host = hp[0];
        if (hp.Length > 1 && int.TryParse(hp[1], out var p)) port = p;
    }
    Console.Write($"请输入玩家名（默认 {name}）：");
    var n = Console.ReadLine();
    if (!string.IsNullOrWhiteSpace(n)) name = n.Trim();
}

if (multi)
{
    // create client, exchange names before creating Storage
    Client client = new Client(host, port, name);
    string peerName;
    try
    {
        // first, give server a short window to match us if someone already FINDed our name
        peerName = client.WaitForPeerName(1000);
    }
    catch (TimeoutException)
    {
        // not matched immediately; ask user for opponent name and send FIND
        Console.Write("请输入要匹配的对手名字（区分大小写）: ");
        string opponentName = Console.ReadLine() ?? string.Empty;
        while (string.IsNullOrWhiteSpace(opponentName))
        {
            Console.Write("对手名字不能为空，请重新输入: ");
            opponentName = Console.ReadLine() ?? string.Empty;
        }
        client.SendFind(opponentName.Trim());
        try
        {
            peerName = client.WaitForPeerName(10000);
        }
        catch (Exception ex)
        {
            Console.WriteLine();
            Console.WriteLine($"等待对方名称超时: {ex.Message}");
            // fallback to local-only names
            storage = new Storage([name, "[Offline]"]);
            Interactive.Run(storage, false);
            return;
        }
    }
    storage = new Storage(new string[] { name, peerName });
    Interactive.Run(storage, true, host, port, name, client);
}
else
{
    storage = new Storage();
    Interactive.Run(storage, false);
}