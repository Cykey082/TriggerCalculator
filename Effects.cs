namespace TriggerCalculator;

public interface ICardEffect
{
    void Execute(Storage storage, Operation operation);
    void PostExecute(Storage storage, Operation operation);
}

public class Effect1 : ICardEffect
{
    public void Execute(Storage storage, Operation operation)
    {
        var amount = operation.Repeat * 20;
        operation.Target.AddAmmo(amount);
        storage.AddRoundEvent($"{operation.User.Name} 装填了 {amount} 弹药 (当前弹药: {operation.Target.Ammo})");
    }

    public void PostExecute(Storage storage, Operation operation) { }
}

public class Effect2 : ICardEffect
{
    public void Execute(Storage storage, Operation operation)
    {
        var block = operation.Repeat * (operation.User.BlockLevel + 1) * 10;
        operation.Target.AddBlock(block);
        storage.AddRoundEvent($"{operation.User.Name} 获得了 {block} 点格挡 (等级:{operation.User.BlockLevel})");
    }

    public void PostExecute(Storage storage, Operation operation)
    {
        if (operation.Target.Block != operation.Target.MaxBlock)
            operation.Target.BlockLevel = 1;
        else if (operation.Target.BlockLevel < 3)
            operation.Target.BlockLevel++;
        operation.Target.Block = 0;
        operation.Target.MaxBlock = 0;
    }
}

public class Effect3 : ICardEffect
{
    public void Execute(Storage storage, Operation operation)
    {
        var ammoCost = operation.Card.RequireAmmo * operation.Repeat;
        operation.User.ConsumeAmmo(ammoCost);
        var damage = operation.Card.Damage * operation.Repeat;
        var rem = operation.Target.AbsorbBlock(damage);
        if (rem > 0)
            operation.Target.ApplyDamage(rem);
        storage.AddRoundEvent($"{operation.User.Name} 使用 {operation.Card.Name} 消耗 {ammoCost} 弹药，造成 {damage} 点伤害 (穿透后:{rem})。目标HP={operation.Target.Health}");
    }

    public void PostExecute(Storage storage, Operation operation) { }
}

public class Effect4 : ICardEffect
{
    public void Execute(Storage storage, Operation operation)
    {
        var ammoCost = operation.Card.RequireAmmo * operation.Repeat;
        operation.User.ConsumeAmmo(ammoCost);
        var damage = operation.Card.Damage * operation.Repeat;
        var rem = operation.Target.AbsorbBlock(damage);
        if (rem > 0)
            operation.Target.ApplyDamage(rem);
        operation.Target.ApplyInjury(2 * operation.Repeat);
        storage.AddRoundEvent($"{operation.User.Name} 使用 {operation.Card.Name} 消耗 {ammoCost} 弹药，造成 {damage} 点伤害，附带重伤 {2 * operation.Repeat}。目标HP={operation.Target.Health}");
    }

    public void PostExecute(Storage storage, Operation operation) { }
}
