namespace TriggerCalculator;

public static class Executor
{
    public static void Execute(this Storage storage, string command)
    {
        try
        {
            // 开始新的回合日志收集
            storage.StartRoundLog();
            // 解析为意图请求 -> 验证 -> 构建 Operation -> 执行
            var requests = Parser.ParseRequests(command, storage.Players);
            var validator = new RuleEngine();
            var vr = validator.ValidateRequests(requests, storage);
            if (!vr.IsValid)
            {
                if (vr.Errors != null && vr.Errors.Count > 0)
                {
                    foreach (var e in vr.Errors) Console.WriteLine(e);
                }
                else
                {
                    Console.WriteLine(vr.Message);
                }
                return;
            }

            var ops = new List<Operation>();
            foreach (var req in requests)
            {
                var user = req.User;
                var code = req.Code;
                var repeat = req.Repeat;
                if (code is 1 or 2)
                {
                    var card = Card.FromID(code);
                    ops.Add(new Operation(user, user, card, repeat));
                }
                else if (code >= 11)
                {
                    int handIndex = code - 11;
                    if (handIndex < 0 || handIndex >= user.Hand.Length) continue;
                    var card = user.Hand[handIndex]!;
                    var target = card.Target == TargetType.Opponent ? storage.Players.First(p => p != user) : user;
                    ops.Add(new Operation(user, target, card, repeat));
                }
            }

            var results = ops.HopeOfLive();
            if (!Parser.PatchDupCards(results))
            {
                Console.WriteLine("非法：同一玩家重复使用同一张手牌。");
                return;
            }

            foreach (var result in results)
            {
                // 记录操作描述
                storage.AddRoundEvent(result.ToString());
                storage.Executes(result);
            }

            foreach (var result in results)
            {
                storage.PostExecutes(result);
            }

            storage.PostExecute();
            // 回合执行完毕，把当前回合日志保留供下回合显示
            storage.EndRoundLog();
        }
        catch (ParserException e)
        {
            Console.WriteLine(e.Message);
        }
    }

    // 直接执行一组已经构造好的 Operation（绕过 Parser）
    public static void ExecuteOperations(this Storage storage, IEnumerable<Operation> operations)
    {
        if (operations == null) return;
        storage.StartRoundLog();
        var results = operations.ToList().HopeOfLive();
        if (!Parser.PatchDupCards(results))
        {
            Console.WriteLine("非法：同一玩家重复使用同一张手牌（ExecuteOperations）。");
            return;
        }

        foreach (var op in results)
        {
            storage.AddRoundEvent(op.ToString());
            storage.Executes(op);
        }

        foreach (var op in results)
        {
            storage.PostExecutes(op);
        }

        storage.PostExecute();
        storage.EndRoundLog();
    }

    public static void Executes(this Storage storage, Operation operation)
    {
        if (operation.Card.Endurance > 0)
            operation.Card.Endurance -= operation.Repeat;
        operation.Card.ExecuteAction?.Invoke(storage, operation);
    }
    public static void PostExecutes(this Storage storage, Operation operation)
    {
        operation.Card.PostExecuteAction?.Invoke(storage, operation);
    }
    public static void PostExecute(this Storage storage)
    {
        var alives = 0;
        int aliver = -1;
        for(var i=0;i<storage.Players.Length;i++)
        {
            var player = storage.Players[i];
            if(!player.IsAlive)continue;
            alives++;
            aliver = i;
            if (player.Injury > 0)
            {
                player.InjuryCooldown--;
                if (player.InjuryCooldown == 0)
                {
                    player.Injury--;
                    if (player.Injury != 0)
                    {
                        player.InjuryCooldown = 3;
                    }
                }
            }
        }

        if (alives <= 1)
        {
            storage.IsEnd = true;
            storage.Winner=aliver;
        }

        storage.Round++;
    }
}