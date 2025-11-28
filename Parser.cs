namespace TriggerCalculator;

using System;
using System.Collections.Generic;
using System.Linq;

public static class Parser
{
    /// <summary>
    /// 入口函数  
    /// cmd 格式： "2*2,13;11,12*3"  
    /// 第一段给 P0，第二段给 P1……  
    /// 返回按书写顺序展开的所有 Operation（已展开 Repeat）。
    /// </summary>
    public static List<Operation> Parse(string cmd, Player[] players)
    {
        if (players == null) throw new ArgumentNullException(nameof(players));
        var segments = Split(cmd, ';');
        if (segments.Length > players.Length)
            throw new ParserException($"指令段数({segments.Length}) > 玩家数({players.Length})");

        var ops = new List<Operation>();
        for (int i = 0; i < segments.Length; i++)
        {
            var seg = segments[i];
            if (string.IsNullOrWhiteSpace(seg)) continue;
            ParseSegment(seg, players[i], players, ops);
        }
        return ops;
    }

    /*===================== 私有实现 =====================*/

    private static void ParseSegment(string seg, Player user, Player[] allPlayers, List<Operation> sink)
    {
        var tokens = Split(seg, ',');
        foreach (var tok in tokens)
        {
            var (code, repeat) = ParseToken(tok);        // 1*3 -> (1,3)
            var op = BuildOperation(code, repeat, user, allPlayers);
            if (op != null) sink.Add(op);                // 内置牌可能返回 null
        }
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

    // 根据 code 生成 Operation；检查资源、耐久等
    private static Operation? BuildOperation(int code, int repeat, Player user, Player[] allPlayers)
    {
        // 1,2 是内置牌
        if (code is 1 or 2)
        {
            var card = Card.From(code);
            if (repeat * card.RequirePoints > user.ActionPoints) throw new ParserException($"{user.Name} 行动点不足(code={code})");
            user.ActionPoints -= repeat * card.RequirePoints;
            return new Operation(user, user, card, repeat);
        }

        // 11,12,13 对应 Hand 0,1,2
        if (code is 11 or 12 or 13)
        {
            int handIndex = code - 11;
            if (handIndex >= user.Hand.Length || user.Hand[handIndex] is not { } card)
                throw new ParserException($"{user.Name} 手牌位{handIndex}为空");

            if (code == 13 && repeat != 1)
                throw new ParserException("13号位(枪击)不允许叠加");

            if (repeat > card.Endurance)
                throw new ParserException($"{user.Name} 牌【{card.Name}】耐久不足");

            if (repeat * card.RequirePoints > user.ActionPoints)
                throw new ParserException($"{user.Name} 行动点不足(code={code})");

            if (user.Ammo < user.Hand[handIndex]!.RequireAmmo * repeat)
                throw new ParserException($"{user.Name} 火药不足(code={code})");

            user.ActionPoints -= repeat * card.RequirePoints;
            // 13 打对方，其余打自己
            var target = code is 11 or 12 or 13
                ? allPlayers.First(p => p != user)
                : user;
            return new Operation(user, target, card, repeat);
        }

        throw new ParserException($"无效操作码：{code}");
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