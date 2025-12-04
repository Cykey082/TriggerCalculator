namespace TriggerCalculator;

using System;
using System.Collections.Generic;
using System.Linq;

public static class Parser
{
    /// <summary>
    /// 解析为意图请求（不做状态检测），返回 OperationRequest 列表用于后续验证/执行。
    /// </summary>
    public static List<OperationRequest> ParseRequests(string cmd, Player[] players)
    {
        if (players == null) throw new ArgumentNullException(nameof(players));
        var segments = Split(cmd, ';');
        if (segments.Length > players.Length)
            throw new ParserException($"指令段数({segments.Length}) > 玩家数({players.Length})");

        var reqs = new List<OperationRequest>();
        for (int i = 0; i < segments.Length; i++)
        {
            var seg = segments[i];
            if (string.IsNullOrWhiteSpace(seg)) continue;
            ParseSegmentRequest(seg, players[i], players, reqs);
        }
        return reqs;
    }

    /*===================== 私有实现 =====================*/

    // 解析为请求（不校验状态）
    private static void ParseSegmentRequest(string seg, Player user, Player[] allPlayers, List<OperationRequest> sink)
    {
        var tokens = Split(seg, ',');
        foreach (var tok in tokens)
        {
            var (code, repeat) = ParseToken(tok);
            var req = BuildRequest(code, repeat, user, allPlayers);
            if (req != null) sink.Add(req);
        }
    }

    private static OperationRequest? BuildRequest(int code, int repeat, Player user, Player[] allPlayers)
    {
        // 支持的 code 与 Parse 中一致，但不做资源/耐久校验（仅语法/编码层面）
        if (code is 1 or 2)
        {
            return new OperationRequest(user, code, repeat);
        }

        // 手牌槽编码从 11 开始，具体槽位由玩家的 Hand 长度决定
        if (code >= 11)
        {
            var handIndex = code - 11;
            // 语法层面仍允许指定任意手牌位，后续 RuleEngine 会校验存在性与属性
            return new OperationRequest(user, code, repeat);
        }

        throw new ParserException($"无效操作码：{code}");
    }

    // 把 "11*3" 拆成 (11,3) ； "13" 拆成 (13,1)
    private static (int code, int repeat) ParseToken(string token)
    {
        var sp = Split(token, '*');
        if (!int.TryParse(sp[0], out int code))
            throw new ParserException($"无法解析数字：{token}");
        int repeat = sp.Length == 2 && int.TryParse(sp[1], out int r) ? r : 1;
        if (repeat <= 0)
            throw new ParserException($"重复次数必须>0：{token}");
        return (code, repeat);
    }

    // 统一 split 去空
    private static string[] Split(string s, char c)
        => s.Split(c, StringSplitOptions.TrimEntries);
    
    //按照生的希望排序
    public static List<Operation> HopeOfLive(this IEnumerable<Operation> ops)
    {
        return ops.OrderByDescending(op => op.Card.Hope).ToList();
    }
    public static bool PatchDupCards(List<Operation> ops)
    {
        if (ops == null || ops.Count == 0) return true;
        HashSet<Card> set = new HashSet<Card>();
        Player owner = ops.First().User;
        foreach (var op in ops)
        {
            if (owner != op.User)
            {
                set.Clear();
                owner = op.User;
            }

            if (!set.Add(op.Card))
            {
                return false;
            }
        }

        return true;
    }
}

/*===================== 辅助类型 =====================*/

public sealed class Operation
{
    public Player User   { get; }
    public Player Target { get; }
    public Card   Card   { get; }
    public int    Repeat { get; }
    public Operation(Player user, Player target, Card card, int repeat)
    {
        User = user; Target = target; Card = card; Repeat = repeat;
    }
    public override string ToString()
    {
        if (User != Target)
            return $"{User.Name} 对 {Target.Name} 使用了 {Card.Name}{(Repeat>0?"*"+Repeat.ToString():string.Empty)}";
        return $"{User.Name} 使用了 {Card.Name}{(Repeat>0?"*"+Repeat.ToString():string.Empty)}";
    }
}

public sealed class ParserException : Exception
{
    public ParserException(string msg) : base(msg) { }
}