namespace TriggerCalculator;

public static class Executor
{
    public static void Execute(this Storage storage, string command)
    {
        try
        {
            var results = Parser.Parse(command, storage.Players).HopeOfLive();
            foreach (var result in results)
            {
                result.Print();
                storage.Executes(result);
            }
            foreach (var result in results)
            {
                storage.PostExecutes(result);
            }
            storage.PostExecute();
        }
        catch (ParserException e)
        {
            Console.WriteLine(e.Message);
        }
    }

    public static void Executes(this Storage storage, Operation operation)
    {
        operation.Card.Endurance -= operation.Repeat;
        switch (operation.Card.Id)
        {
            case 1:
                storage.Execute1(operation);
                break;
            case 2:
                storage.Execute2(operation);
                break;
            case 3:
                storage.Execute3(operation);
                break;
            case 4:
                storage.Execute4(operation);
                break;
        }
    }
    public static void Execute1(this Storage storage, Operation operation)
    {
        operation.Target.Ammo += operation.Repeat * 20;
        if(operation.Target.Ammo>operation.Target.MaxAmmo)
            operation.Target.Ammo = operation.Target.MaxAmmo;
    }

    public static void Execute2(this Storage storage, Operation operation)
    {
        var block = operation.Repeat * (operation.User.BlockLevel + 1) * 10;
        operation.Target.Block += block;
        operation.Target.MaxBlock += block;
    }

    public static void Execute3(this Storage storage, Operation operation)
    {
        operation.User.Ammo -= operation.Card.RequireAmmo * operation.Repeat;
        var damage = operation.Card.Damage * operation.Repeat;
        if (damage < operation.Target.Block)
        {
            operation.Target.Block -= damage;
            return;
        }
        damage-=operation.Target.Block;
        operation.Target.Block = 0;
        operation.Target.Health -= damage;
        if (operation.Target.Health <= operation.Target.Injury * 10)
        {
            operation.Target.IsAlive = false;
        }
    }

    public static void Execute4(this Storage storage, Operation operation)
    {
        operation.User.Ammo -= operation.Card.RequireAmmo;
        var damage = operation.Card.Damage;
        if (damage < operation.Target.Block)
        {
            operation.Target.Block -= damage;
            return;
        }
        damage-=operation.Target.Block;
        operation.Target.Block = 0;
        operation.Target.Health -= damage;
        if (operation.Target.Health <= operation.Target.Injury * 10)
        {
            operation.Target.IsAlive = false;
        }
        operation.Target.Injury += 2;
        operation.Target.InjuryCooldown = 3;
    }
    public static void PostExecutes(this Storage storage, Operation operation)
    {
        switch (operation.Card.Id)
        {
            case 2:
                storage.PostExecute2(operation);
                break;
        }
    }

    public static void PostExecute2(this Storage storage, Operation operation)
    {
        if (operation.Target.Block != operation.Target.MaxBlock)
            operation.Target.BlockLevel = 1;
        else if (operation.Target.BlockLevel < 3)
            operation.Target.BlockLevel++;
        operation.Target.Block = 0;
        operation.Target.MaxBlock = 0;
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