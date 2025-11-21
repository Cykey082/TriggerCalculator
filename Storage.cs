namespace TriggerCalculator;

public record Card
{
    public int Id { get; init; }
    public int Hope { get; init; }
    public string Name { get; init; } = "Card.Null";
    public int RequirePoints { get; init; }
    public int RequireAmmo { get; init; }
    public int Endurance { get; set; }
    public bool Repeatable { get; init; }
    public int Damage { get; init; }

    public static readonly Card[] Lib =
    {
        new(),
        new(){Id=1,Name = "装填",Endurance = -1,RequirePoints = 1,Repeatable = true,Hope = 1},
        new(){Id=2,Name = "格挡",Endurance = -1,RequirePoints = 1,Repeatable = true,Hope = 2},
        new(){Id=3,Name = "搏击",Endurance = 2,RequirePoints = 1,Repeatable = true,RequireAmmo = 20,Damage = 20,Hope = -1},
        new(){Id=4,Name = "枪击",Endurance = 2,RequirePoints = 2,RequireAmmo = 80,Damage = 70,Hope = -2}
    };

    // 无需任何构造函数——record 自带值拷贝
    public static Card From(int index) => Lib[index] with { };
}

public class Player
{
    public string Name { get; set; } = "Player.Null";
    public int Health { get; set; } = 100;
    public bool IsAlive { get; set; } = true;
    public int MaxHealth { get; set; } = 100;
    public int Ammo { get; set; } = 0;
    public int MaxAmmo { get; set; } = 160;
    public Card[] Body{ get; set; }
    public Card?[] Hand{ get; set; }
    //Todo:Remove them from Player itself
    public int Injury { get; set; } = 0;
    public int InjuryCooldown { get; set; } = 0;
    public int Block { get; set; } = 0;
    public int MaxBlock { get; set; } = 0;
    public int BlockLevel { get; set; } = 1;

    public Player()
    {
        Body=[Card.From(1),Card.From(2)];
        Hand=[Card.From(3),Card.From(3),Card.From(4),null];
    }
}
public class Storage
{
    public Player[] Players { get; set; } = new Player[2];
    public int Round { get; set; } = 0;
    public bool IsEnd { get; set; } = false;
    public int Winner { get; set; } = -1;

    public Storage()
    {
        Players[0] = new Player(){Name = "P1"};
        Players[1] = new Player(){Name = "P2"};
    }
}