using System;
using System.Collections.Generic;
using System;
using System.Collections.Generic;

namespace TriggerCalculator;

public static class Interactive
{
    // 更接近 GUI 的 TUI：左右并排显示双方面板，使用方向键移动选择并按 Enter 加入动作
    public static void Run(Storage storage)
    {
        while (!storage.IsEnd)
        {
            Console.CursorVisible = false;
            int players = storage.Players.Length;
            // 每位玩家的选项计数：索引 0->装填(1),1->格挡(2), 2->hand0(code=11), ...
            var counts = new List<int[]>();
            var mps = new int[players];
            for (int i = 0; i < players; i++)
            {
                counts.Add(new int[2 + storage.Players[i].Hand.Length]);
                // reset player's action points at start of round
                storage.Players[i].ResetActionPoints();
                mps[i] = storage.Players[i].ActionPoints;
            }

            int activePid = 0;
            int selectedIdx = 0; // index within options for active player

            while (true)
            {
                DrawBothPanelsRealtime(storage, activePid, selectedIdx, counts, mps);
                var key = Console.ReadKey(true);
                if (key.Key == ConsoleKey.Tab)
                {
                    // switch player
                    activePid = (activePid + 1) % players;
                    selectedIdx = Math.Min(selectedIdx, counts[activePid].Length - 1);
                }
                else if (key.Key == ConsoleKey.UpArrow)
                {
                    selectedIdx = Math.Max(0, selectedIdx - 1);
                }
                else if (key.Key == ConsoleKey.DownArrow)
                {
                    selectedIdx = Math.Min(counts[activePid].Length - 1, selectedIdx + 1);
                }
                else if (key.Key == ConsoleKey.RightArrow)
                {
                    // 尝试增加当前选项
                    var player = storage.Players[activePid];
                    var arr = counts[activePid];
                    int opt = selectedIdx;
                    int code = opt < 2 ? opt + 1 : 11 + (opt - 2);
                    var card = code <= 4 ? Card.From(code) : player.Hand[code - 11];
                    if (card == null)
                    {
                        FlashMessage("该手牌位为空。", 400);
                        continue;
                    }
                    int cur = arr[opt];
                    int maxRepeat = card.Repeatable ? card.Endurance==-1?int.MaxValue : card.Endurance:1;                    if (!card.Repeatable && cur >= card.Endurance)
                    {
                        FlashMessage("耐久不足。", 400);
                        continue;
                    }
                    else if (cur >= maxRepeat)
                    {
                        FlashMessage("该牌不可叠加使用。", 400);
                        continue;
                    }
                    // 计算 prospective cost and ammo
                    arr[opt] = cur + 1;
                    int cost = 0;
                    int ammoNeeded = 0;
                    for (int j = 0; j < arr.Length; j++)
                    {
                        int ccode = j < 2 ? j + 1 : 11 + (j - 2);
                        var cc = ccode <= 4 ? Card.From(ccode) : player.Hand[ccode - 11];
                        if (cc == null) continue;
                        cost += arr[j] * cc.RequirePoints;
                        ammoNeeded += arr[j] * cc.RequireAmmo;
                    }
                    if (cost > player.ActionPoints)
                    {
                        arr[opt] = cur; // rollback
                        FlashMessage("行动点不足。", 400);
                        continue;
                    }
                    if (ammoNeeded > player.Ammo)
                    {
                        arr[opt] = cur; // rollback
                        FlashMessage("火药不足。", 400);
                        continue;
                    }
                    // update remaining mp
                    mps[activePid] = player.ActionPoints - cost;
                }
                else if (key.Key == ConsoleKey.LeftArrow)
                {
                    // decrease current option
                    var arr = counts[activePid];
                    int opt = selectedIdx;
                    if (arr[opt] > 0)
                    {
                        arr[opt]--;
                        // recompute mp
                        var player = storage.Players[activePid];
                        int cost = 0;
                        for (int j = 0; j < arr.Length; j++)
                        {
                            int ccode = j < 2 ? j + 1 : 11 + (j - 2);
                            var cc = ccode <= 4 ? Card.From(ccode) : player.Hand[ccode - 11];
                            if (cc == null) continue;
                            cost += arr[j] * cc.RequirePoints;
                        }
                        mps[activePid] = player.ActionPoints - cost;
                    }
                }
                else if (key.Key == ConsoleKey.Enter)
                {
                    // finalize round, build command and execute
                    var segments = new string[players];
                    for (int p = 0; p < players; p++)
                    {
                        var pArr = counts[p];
                        var list = new List<string>();
                        for (int j = 0; j < pArr.Length; j++)
                        {
                            if (pArr[j] <= 0) continue;
                            int ccode = j < 2 ? j + 1 : 11 + (j - 2);
                            list.Add(pArr[j] == 1 ? $"{ccode}" : $"{ccode}*{pArr[j]}");
                        }
                        segments[p] = string.Join(',', list);
                    }
                    var cmd = string.Join(';', segments);
                    Console.Clear();
                    Console.WriteLine("执行命令：" + cmd);
                    storage.Execute(cmd);
                    break;
                }
                else if (key.Key == ConsoleKey.Escape)
                {
                    // cancel entire round planning
                    break;
                }
            }
        }

        Console.CursorVisible = true;
        Console.Clear();
        // 在结束时使用 TUI 界面完整展示双方面板与上回合日志
        int finalPlayers = storage.Players.Length;
        var finalCounts = new List<int[]>();
        var finalMps = new int[finalPlayers];
        for (int i = 0; i < finalPlayers; i++)
        {
            finalCounts.Add(new int[2 + storage.Players[i].Hand.Length]);
            finalMps[i] = storage.Players[i].ActionPoints;
        }
        DrawBothPanelsRealtime(storage, 0, 0, finalCounts, finalMps);

        // 显示胜负信息在界面底部
        Console.SetCursorPosition(0, Console.WindowHeight - 2);
        if (storage.IsEnd)
        {
            if (storage.Winner == -1)
                Console.WriteLine("游戏结束 — 平局。 按任意键退出。");
            else
                Console.WriteLine($"游戏结束 — 胜利者: {storage.Players[storage.Winner].Name}。 按任意键退出。");
        }
        else
        {
            Console.WriteLine("按任意键退出。");
        }
        Console.ReadKey(true);
    }

    private static (int code, int repeat) ParseTokenForRefund(string token)
    {
        var sp = token.Split('*');
        if (!int.TryParse(sp[0], out int code)) return (0, 1);
        int repeat = 1;
        if (sp.Length == 2 && int.TryParse(sp[1], out int r)) repeat = r;
        return (code, repeat);
    }

    // 实时绘制左右面板与选择信息。counts: 每位玩家每个选项的已选次数；mps: 每位剩余行动点
    private static void DrawBothPanelsRealtime(Storage storage, int activePid, int selectedIdx, List<int[]> counts, int[] mps)
    {
        Console.Clear();
        int width = Math.Max(40, Console.WindowWidth)-1;
        int half = width / 2;
        for (int i = 0; i < storage.Players.Length; i++)
        {
            var p = storage.Players[i];
            int left = i * half;
            DrawPlayerPanelRealtime(p, left, half, counts[i], selectedIdx, i == activePid, mps[i]);
        }
        //Console.SetCursorPosition(0, top + 5 + optionCount + 2);
        // 在下方显示操作说明以及当前光标对应的卡牌介绍（来自 README）
        Console.SetCursorPosition(0, Console.WindowHeight - 4);
        Console.WriteLine(new string(' ', width));
        Console.SetCursorPosition(0, Console.WindowHeight - 4);
        Console.WriteLine("说明: ↑/↓ 选择, ←/→ 改变次数, Tab 切换角色, Enter 结束回合".PadRight(width - 1));

        // 显示当前光标对应的操作介绍
        var active = storage.Players[activePid];
        int code = selectedIdx < 2 ? selectedIdx + 1 : 11 + (selectedIdx - 2);
        string desc;
        if (code <= 4)
            desc = GetDescriptionForId(code);
        else
        {
            var card = active.Hand[code - 11];
            desc = card == null ? "(空位)" : GetDescriptionForId(card.Id);
        }
        // 显示上一回合事件（若有），在当前说明之上
        if (storage.LastRoundEvents != null && storage.LastRoundEvents.Count > 0)
        {
            int maxLines = 3;
            int startLine = Console.WindowHeight - 7 - maxLines;
            if (startLine < 0) startLine = 0;
            Console.SetCursorPosition(0, startLine);
            Console.WriteLine("--- 上回合发生 ---".PadRight(width - 1));
            int line = startLine + 1;
            int printed = 0;
            foreach (var e in storage.LastRoundEvents)
            {
                if (printed >= maxLines) break;
                if (line >= Console.WindowHeight - 4) break;
                Console.SetCursorPosition(0, line);
                Console.WriteLine(e.PadRight(width - 1));
                line++;
                printed++;
            }
        }

        Console.SetCursorPosition(0, Console.WindowHeight - 3);
        Console.WriteLine(("当前: " + desc).PadRight(width - 1));
    }

    private static string GetDescriptionForId(int id)
    {
        // 文本来自 README.md 的卡牌描述
        return id switch
        {
            1 => "装填：消耗1点行动点。结算：己方装填20弹药（最多160）。",
            2 => "格挡：消耗1点行动点。结算：获得格挡值（与格挡等级相关），若格挡未被破坏可提升格挡等级。",
            3 => "搏击：2耐久，消耗1点行动点并需20弹药。结算：造成20点伤害。",
            4 => "枪击：2耐久，消耗2点行动点并需80弹药。结算：造成70点伤害，并附带2点重伤（Injury +2）。",
            _ => "未知操作。",
        };
    }

    // 绘制单一玩家面板（含已选次数与光标）
    private static void DrawPlayerPanelRealtime(Player p, int left, int width, int[] arr, int selectedIdx, bool isActivePlayer, int mp)
    {
        int top = 0;
        Console.SetCursorPosition(left, top);
        Console.Write(new string(' ', Math.Max(0, width - 1)));
        Console.SetCursorPosition(left, top);
        Console.ForegroundColor = isActivePlayer ? ConsoleColor.Cyan : ConsoleColor.Gray;
        Console.WriteLine(isActivePlayer ? ">" + p.Name.PadRight(width - 2) : " " + p.Name.PadRight(width - 2));
        Console.ResetColor();
        Console.SetCursorPosition(left, top + 1);
        Console.WriteLine($"HP: {p.Health}/{p.MaxHealth}   Ammo: {p.Ammo}/{p.MaxAmmo}   BlockLevel: {p.BlockLevel}".PadRight(width - 1));
        Console.SetCursorPosition(left, top + 2);
        Console.WriteLine($"Block: {p.Block}  Injury: {p.Injury} (CD:{p.InjuryCooldown})".PadRight(width - 1));
        Console.SetCursorPosition(left, top + 4);
        Console.WriteLine("手牌:".PadRight(width - 1));
        // options
        int optionCount = arr.Length;
        for (int opt = 0; opt < optionCount; opt++)
        {
            Console.SetCursorPosition(left, top + 5 + opt);
            // cursor symbol >> if this is the currently selected option and this player is active
            string cursor = (isActivePlayer && opt == selectedIdx) ? ">>" : "  ";
            int code = opt < 2 ? opt + 1 : 11 + (opt - 2);
            var card = code <= 4 ? Card.From(code) : p.Hand[code - 11];
            string label;
            if (card == null) label = " [ ]";
            else label = $" [{card.Name}]{(card.Endurance>0?" Usage:"+card.Endurance.ToString(): string.Empty)} Points:{card.RequirePoints} Ammo:{card.RequireAmmo}";

            // selected marker: n* ; if cursor is on it and selected>0 show n>
            if (arr[opt] > 0)
            {
                cursor = (isActivePlayer && opt == selectedIdx) ? $"{arr[opt]}>" : $"{arr[opt]}*";
            }
            Console.Write(cursor + label.PadRight(width - 6));
        }

        // planning summary
        Console.SetCursorPosition(left, top + 5 + optionCount + 1);
        Console.WriteLine($"已选总行动点消耗: {p.ActionPoints - mp}/{p.ActionPoints}   剩余行动点: {mp}".PadRight(width - 1));
    }

    private static void FlashMessage(string msg, int ms)
    {
        int w = Console.WindowWidth;
        int left = Math.Max(0, (w - msg.Length) / 2);
        int top = Console.WindowHeight - 5;
        try
        {
            Console.SetCursorPosition(left, top);
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write(msg);
            Console.ResetColor();
            System.Threading.Thread.Sleep(ms);
        }
        catch { }
    }
}
