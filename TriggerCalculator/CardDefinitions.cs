namespace TriggerCalculator;

public class Card1Builder : ICardBuilder
{
    public Card Build() => new Card
    {
        Id = 1,
        Name = "装填",
        Description = "装填：消耗1点行动点。结算：己方装填20弹药（最多160）。",
        Endurance = -1,
        RequirePoints = 1,
        Repeatable = true,
        Hope = 1,
        ExecuteAction = (storage, operation) =>
        {
            var amount = operation.Repeat * 20;
            operation.Target.AddAmmo(amount);
            storage.AddRoundEvent($"{operation.User.Name} 装填了 {amount} 弹药 (当前弹药: {operation.Target.Ammo})");
        },
        Target = TargetType.Self
    };
}

public class Card2Builder : ICardBuilder
{
    public Card Build() => new Card
    {
        Id = 2,
        Name = "格挡",
        Description = "格挡：消耗1点行动点。结算：基于格挡等级获得（20/30/40）格挡值。若格挡未被使用则提升格挡等级。",
        Endurance = -1,
        RequirePoints = 1,
        Repeatable = true,
        Hope = 2,
        ExecuteAction = (storage, operation) =>
        {
            var block = operation.Repeat * (operation.User.BlockLevel + 1) * 10;
            operation.Target.AddBlock(block);
            storage.AddRoundEvent($"{operation.User.Name} 获得了 {block} 格挡值 (等级:{operation.User.BlockLevel})");
        },
        PostExecuteAction = (storage, operation) =>
        {
            if (operation.Target.Block != operation.Target.MaxBlock)
                operation.Target.BlockLevel = 1;
            else if (operation.Target.BlockLevel < 3)
                operation.Target.BlockLevel++;
            operation.Target.Block = 0;
            operation.Target.MaxBlock = 0;
        },
        Target = TargetType.Self
    };
}

public class Card3Builder : ICardBuilder
{
    public Card Build() => new Card
    {
        Id = 3,
        Name = "搏击",
        Description = "搏击：2耐久，消耗1点行动点与20弹药。结算：造成20点伤害。",
        Endurance = 2,
        RequirePoints = 1,
        Repeatable = true,
        RequireAmmo = 20,
        Damage = 20,
        Hope = -1,
        ExecuteAction = (storage, operation) =>
        {
            var ammoCost = operation.Card.RequireAmmo * operation.Repeat;
            operation.User.ConsumeAmmo(ammoCost);
            var damage = operation.Card.Damage * operation.Repeat;
            var rem = operation.Target.ApplyDamage(damage);
            storage.AddRoundEvent($"{operation.User.Name} 使用 {operation.Card.Name} ，造成 {rem} 点伤害。目标HP={operation.Target.Health}");
        },
        Target = TargetType.Opponent
    };
}

public class Card4Builder : ICardBuilder
{
    public Card Build() => new Card
    {
        Id = 4,
        Name = "枪击",
        Description = "枪击：2耐久，消耗2点行动点与80弹药。结算：造成70点伤害，并附带2（重伤）。",
        Endurance = 2,
        RequirePoints = 2,
        Repeatable = true,
        RequireAmmo = 80,
        Damage = 70,
        Hope = -2,
        ExecuteAction = (storage, operation) =>
        {
            var ammoCost = operation.Card.RequireAmmo * operation.Repeat;
            operation.User.ConsumeAmmo(ammoCost);
            var damage = operation.Card.Damage * operation.Repeat;
            var rem = operation.Target.ApplyDamage(damage);
            if (rem > 0)
            {
                operation.Target.ApplyInjury(2 * operation.Repeat);
                storage.AddRoundEvent($"{operation.User.Name} 使用 {operation.Card.Name} ，造成 {rem} 点伤害，附带重伤 {2 * operation.Repeat}。目标HP={operation.Target.Health}");
            }
            else
            {
                storage.AddRoundEvent($"{operation.User.Name} 使用 {operation.Card.Name} ，造成 {rem} 点伤害。目标HP={operation.Target.Health}");
            }
        },
        Target = TargetType.Opponent
    };
}


