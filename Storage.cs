namespace TriggerCalculator;

public record Card
{
    public int Id { get; init; }
    public int Hope { get; init; }
    public string Name { get; init; } = "Card.Null";
        public string Description { get; init; } = string.Empty;
    public int RequirePoints { get; init; }
    public int RequireAmmo { get; init; }
    public int Endurance { get; set; }
    public bool Repeatable { get; init; }
    public int Damage { get; init; }
    public System.Action<Storage, Operation>? ExecuteAction { get; init; }
    public System.Action<Storage, Operation>? PostExecuteAction { get; init; }
    public TargetType Target { get; init; } = TargetType.Self;

        // 卡牌库通过反射动态加载（查找实现了 ICardBuilder 的类型）
        public static readonly Card[] Lib = CardLoader.LoadLibrary();

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
    private int AbsorbBlock(int damage)
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

    public int ApplyDamage(int damage)
    {
        damage = AbsorbBlock(damage);
        if(damage<=0)return 0;
        Health -= damage;
        if (Health <= Injury * 10)
        {
            IsAlive = false;
        }
        return damage;
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
    public Storage(string[] names)
    {
        if(names.Length!=2)throw new ArgumentException("names length must be 2");
        Players[0] = new Player(){Name = names[0]};
        Players[1] = new Player(){Name = names[1]};
    }
}