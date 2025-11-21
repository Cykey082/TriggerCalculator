namespace TriggerCalculator;

public static class Visualizer
{
    public static void Print(this Storage storage)
    {
        Console.Write("TriggerTime! --");
        Console.WriteLine(storage.IsEnd?"游戏已结束":"游戏进行中");
        foreach (var player in storage.Players)
        {
            Console.Write("{0}: HP:{1}  Ammo:{2}\n    ", player.Name,player.Health,player.Ammo);
            foreach (var card in player.Hand)
            {
                if (card == null) continue;
                Console.Write(" [{0}]{1}", card.Name,card.Endurance);
            }
            Console.WriteLine();
        }

        if (!storage.IsEnd) return;
        if (storage.Winner == -1)
        {
            Console.WriteLine("Draw");
            return;
        }

        Console.WriteLine("Winner={0}", storage.Players[storage.Winner].Name);
    }
    public static void Print(this Operation operation)
    {
        if(operation.User!=operation.Target)
            Console.Write("{0}对{1}使用了{2}",operation.User.Name,operation.Target.Name,operation.Card.Name);
        else
            Console.Write("{0}使用了{1}",operation.User.Name,operation.Card.Name);
        if (operation.Repeat > 0)
        {
            Console.Write("*{0}",operation.Repeat);
        }
        Console.WriteLine();
    }
}