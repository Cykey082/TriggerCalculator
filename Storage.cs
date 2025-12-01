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
    public ICardEffect? Effect { get; init; }
    public TargetType Target { get; init; } = TargetType.Self;

    public static readonly Card[] Lib =
    {
        new(),
    new(){Id=1,Name = "装填",Endurance = -1,RequirePoints = 1,Repeatable = true,Hope = 1, Effect = new Effect1(), Target = TargetType.Self},
    new(){Id=2,Name = "格挡",Endurance = -1,RequirePoints = 1,Repeatable = true,Hope = 2, Effect = new Effect2(), Target = TargetType.Self},
    new(){Id=3,Name = "搏击",Endurance = 2,RequirePoints = 1,Repeatable = true,RequireAmmo = 20,Damage = 20,Hope = -1, Effect = new Effect3(), Target = TargetType.Opponent},
    new(){Id=4,Name = "枪击",Endurance = 2,RequirePoints = 2,RequireAmmo = 80,Damage = 70,Hope = -2, Effect = new Effect4(), Target = TargetType.Opponent}
    };

    // 无需任何构造函数——record 自带值拷贝
    public static Card From(int index) => Lib[index] with { };
}

public enum TargetType
{
    Self,
    Opponent
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

    // 行为方法（面向对象封装）
    public void AddAmmo(int amount)
    {
        Ammo += amount;
        if (Ammo > MaxAmmo) Ammo = MaxAmmo;
    }

    public bool HasAmmo(int amount) => Ammo >= amount;

    public void ConsumeAmmo(int amount)
    {
        Ammo -= amount;
        if (Ammo < 0) Ammo = 0;
    }

    public void AddBlock(int amount)
    {
        Block += amount;
        MaxBlock += amount;
    }

    // 返回被 Block 吸收后的剩余伤害
    public int AbsorbBlock(int damage)
    {
        if (damage <= Block)
        {
            Block -= damage;
            return 0;
        }
        var rem = damage - Block;
        Block = 0;
        return rem;
    }

    public void ApplyDamage(int damage)
    {
        Health -= damage;
        if (Health <= Injury * 10)
        {
            IsAlive = false;
        }
    }

    public void ApplyInjury(int amount)
    {
        Injury += amount;
        InjuryCooldown = 3;
    }

    public void TickInjuryCooldown()
    {
        if (Injury <= 0) return;
        InjuryCooldown--;
        if (InjuryCooldown <= 0)
        {
            Injury--;
            if (Injury > 0)
                InjuryCooldown = 3;
            else
                InjuryCooldown = 0;
        }
    }

    // 行动点（每回合重置）
    public int ActionPoints { get; set; } = 2;

    public void ResetActionPoints()
    {
        ActionPoints = 2;
    }

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
    // 回合事件日志：CurrentRoundEvents 在执行时追加，EndRoundLog 后会移动到 LastRoundEvents
    public List<string> CurrentRoundEvents { get; private set; } = new List<string>();
    public List<string> LastRoundEvents { get; private set; } = new List<string>();

    public void StartRoundLog()
    {
        CurrentRoundEvents.Clear();
    }

    public void AddRoundEvent(string msg)
    {
        if (msg == null) return;
        CurrentRoundEvents.Add(msg);
    }

    public void EndRoundLog()
    {
        LastRoundEvents = new List<string>(CurrentRoundEvents);
        CurrentRoundEvents.Clear();
    }

    public Storage()
    {
        Players[0] = new Player(){Name = "P1"};
        Players[1] = new Player(){Name = "P2"};
    }
}