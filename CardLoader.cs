namespace TriggerCalculator;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public sealed class CardDefinitionAttribute : Attribute
{
    public int Id { get; }
    public CardDefinitionAttribute(int id) => Id = id;
}

public interface ICardBuilder
{
    Card Build();
}

public static class CardLoader
{
    public static Card[] LoadLibrary()
    {
        var asm = Assembly.GetExecutingAssembly();
        var types = asm.GetTypes()
            .Where(t => typeof(ICardBuilder).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract)
            .ToList();

        var cards = new List<Card>();
        foreach (var t in types)
        {
            try
            {
                var inst = Activator.CreateInstance(t) as ICardBuilder;
                if (inst == null) continue;
                var card = inst.Build();
                if (card == null) continue;
                cards.Add(card);
            }
            catch
            {
                // ignore faulty builders
            }
        }

        if (!cards.Any())
        {
            // fallback minimal library
            return new[] { new Card() };
        }

        int maxId = cards.Max(c => c.Id);
        var arr = new Card[maxId + 1];
        arr[0] = new Card();
        foreach (var c in cards)
        {
            if (c.Id >= 0 && c.Id < arr.Length) arr[c.Id] = c;
        }

        // Fill missing indices with default Card instances
        for (int i = 0; i < arr.Length; i++)
        {
            if (arr[i] == null) arr[i] = new Card();
        }

        return arr;
    }
}
