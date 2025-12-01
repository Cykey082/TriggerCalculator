namespace TriggerCalculator;

using System.Collections.Generic;
using System.Linq;

public sealed class OperationRequest
{
    public Player User { get; }
    public int Code { get; }
    public int Repeat { get; }
    public OperationRequest(Player user, int code, int repeat)
    {
        User = user; Code = code; Repeat = repeat;
    }
}

public sealed class ValidationResult
{
    public bool IsValid => Errors == null || Errors.Count == 0;
    public List<string> Errors { get; }
    public string Message => Errors == null || Errors.Count == 0 ? string.Empty : string.Join("; ", Errors);

    public ValidationResult()
    {
        Errors = new List<string>();
    }

    public static ValidationResult Ok() => new ValidationResult();

    public static ValidationResult Fail(string msg)
    {
        var r = new ValidationResult();
        if (!string.IsNullOrEmpty(msg)) r.Errors.Add(msg);
        return r;
    }

    public static ValidationResult FromErrors(IEnumerable<string> errors)
    {
        var r = new ValidationResult();
        if (errors != null) r.Errors.AddRange(errors.Where(e => !string.IsNullOrEmpty(e)));
        return r;
    }

    public void AddError(string msg)
    {
        if (string.IsNullOrEmpty(msg)) return;
        Errors.Add(msg);
    }
}

public interface IRuleEngine
{
    ValidationResult ValidateRequests(IEnumerable<OperationRequest> requests, Storage storage);
}

public class RuleEngine : IRuleEngine
{
    public ValidationResult ValidateRequests(IEnumerable<OperationRequest> requests, Storage storage)
    {
        var result = new ValidationResult();
        if (requests == null) return result;
        var list = requests.ToList();

        // Group by user to check action points and ammo totals
        var byUser = list.GroupBy(r => r.User);
        foreach (var g in byUser)
        {
            var user = g.Key;
            int totalPoints = 0;
            int totalAmmoNeeded = 0;
            var usedHandIndices = new HashSet<int>();
            foreach (var req in g)
            {
                int code = req.Code;
                if (code is 1 or 2)
                {
                    var card = Card.From(code);
                    totalPoints += req.Repeat * card.RequirePoints;
                    // built-in have no ammo
                }
                else
                {
                    int handIndex = code - 11;
                    if (handIndex < 0 || handIndex >= user.Hand.Length)
                    {
                        result.AddError($"{user.Name} 手牌位 {handIndex} 不存在");
                        continue;
                    }
                    var card = user.Hand[handIndex];
                    if (card == null)
                    {
                        result.AddError($"{user.Name} 手牌位 {handIndex} 为空");
                        continue;
                    }
                    if (!card.Repeatable && req.Repeat != 1)
                    {
                        result.AddError($"{user.Name} 的牌【{card.Name}】不允许叠加使用");
                        continue;
                    }
                    if (card.Endurance >= 0 && req.Repeat > card.Endurance)
                    {
                        result.AddError($"{user.Name} 牌【{card.Name}】耐久不足");
                        continue;
                    }
                    totalPoints += req.Repeat * card.RequirePoints;
                    totalAmmoNeeded += req.Repeat * card.RequireAmmo;
                    if (!usedHandIndices.Add(handIndex))
                        result.AddError($"{user.Name} 重复使用同一手牌位{handIndex}");
                }
            }

            if (totalPoints > user.ActionPoints)
                result.AddError($"{user.Name} 行动点不足 (需 {totalPoints}，有 {user.ActionPoints})");
            if (totalAmmoNeeded > user.Ammo)
                result.AddError($"{user.Name} 火药不足 (需 {totalAmmoNeeded}，有 {user.Ammo})");
        }

        return result;
    }
}
