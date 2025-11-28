namespace TriggerCalculator;

public static class Executor
{
    public static void Execute(this Storage storage, string command)
    {
        try
        {
            // 开始新的回合日志收集
            storage.StartRoundLog();
            var results = Parser.Parse(command, storage.Players).HopeOfLive();
            if (!Parser.PatchDupCards(results))
            {
                Console.WriteLine("非法：同一玩家重复使用同一张手牌。");
                return;
            }
            foreach (var result in results)
            {
                // 打印并记录操作描述
                result.Print();
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

    public static void Executes(this Storage storage, Operation operation)
    {
        if (operation.Card.Endurance > 0)
            operation.Card.Endurance -= operation.Repeat;
        operation.Card.Effect?.Execute(storage, operation);
    }
    public static void PostExecutes(this Storage storage, Operation operation)
    {
        operation.Card.Effect?.PostExecute(storage, operation);
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