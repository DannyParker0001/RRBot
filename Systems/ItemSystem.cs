﻿namespace RRBot.Systems;
public static class ItemSystem
{
    public static readonly Crate[] crates =
    {
        new("Daily", 0, 1, cash: 1500),
        new("Bronze", 5000, 2 ),
        new("Silver", 10000, 3, 1),
        new("Gold", 15000, 5, 2),
        new("Diamond", 25000, 10, 3)
    };

    public static readonly Consumable[] consumables =
    {
        new("Cocaine", "Snorting a line of this funny sugar makes you HYPER and has some crazy effects. It wears off after 1 hour.", "You have a chance of overdosing, which will make you lose all your remaining cocaine and money, as well as not be able to use economy commands for a certain amount of time. The chance of overdosing and how long you can't use economy commands depends on how many lines you have in your system.", "Cooldowns are reduced by 10% for each line snorted.", 0, 3600),
        new("Romanian Flag", "A neat little good luck charm for $rob. Your Romanian pride makes stealing wallets much easier! Wears off after 1 hour. Can only carry one at a time.", "A Romanian might notice you and take some of your money.", "$rob chance increased by 10%.", 0, 3600)
    };

    public static readonly Perk[] perks =
    {
        new("Enchanter", "Tasks are 20% more effective, but your tools have a 2% chance of breaking after use.", 5000, 172800),
        new("Speed Demon", "Cooldowns are 15% shorter, but you have a 5% higher chance of failing any command that can fail.", 5000, 172800),
        new("Multiperk", "Grants the ability to equip 2 perks, not including this one.", 10000, 604800),
        new("Pacifist", "You are immune to all crimes, but you cannot use any crime commands and you also cannot appear on the leaderboard. Cannot be stacked with other perks, even if you have the Multiperk. Can be discarded, but cannot be used again for 3 days.", 0, -1)
    };

    public static readonly Tool[] tools =
    {
        new("Wooden Pickaxe", 4500),
        new("Stone Pickaxe", 6000, mult: Constants.MINE_STONE_MULTIPLIER),
        new("Iron Pickaxe", 7500, mult: Constants.MINE_IRON_MULTIPLIER),
        new("Diamond Pickaxe", 9000, mult: Constants.MINE_DIAMOND_MULTIPLIER),
        new("Wooden Sword", 4500, Constants.GENERIC_TASK_WOOD_MIN * 2.5, Constants.GENERIC_TASK_WOOD_MAX * 2.5),
        new("Stone Sword", 6000, Constants.GENERIC_TASK_STONE_MIN * 2.5, Constants.GENERIC_TASK_STONE_MAX * 2.5),
        new("Iron Sword", 7500, Constants.GENERIC_TASK_IRON_MIN * 2.5, Constants.GENERIC_TASK_IRON_MAX * 2.5),
        new("Diamond Sword", 9000, Constants.GENERIC_TASK_DIAMOND_MIN * 2.5, Constants.GENERIC_TASK_DIAMOND_MAX * 2.5),
        new("Wooden Shovel", 4500, Constants.GENERIC_TASK_WOOD_MIN * 2.5, Constants.GENERIC_TASK_WOOD_MAX * 2.5),
        new("Stone Shovel", 6000, Constants.GENERIC_TASK_STONE_MIN * 2.5, Constants.GENERIC_TASK_STONE_MAX * 2.5),
        new("Iron Shovel", 7500, Constants.GENERIC_TASK_IRON_MIN * 2.5, Constants.GENERIC_TASK_IRON_MAX * 2.5),
        new("Diamond Shovel", 9000, Constants.GENERIC_TASK_DIAMOND_MIN * 2.5, Constants.GENERIC_TASK_DIAMOND_MAX * 2.5),
        new("Wooden Axe", 4500, Constants.GENERIC_TASK_WOOD_MIN * 2.5, Constants.GENERIC_TASK_WOOD_MAX * 2.5),
        new("Stone Axe", 6000, Constants.GENERIC_TASK_STONE_MIN * 2.5, Constants.GENERIC_TASK_STONE_MAX * 2.5),
        new("Iron Axe", 7500, Constants.GENERIC_TASK_IRON_MIN * 2.5, Constants.GENERIC_TASK_IRON_MAX * 2.5),
        new("Diamond Axe", 9000, Constants.GENERIC_TASK_DIAMOND_MIN * 2.5, Constants.GENERIC_TASK_DIAMOND_MAX * 2.5),
        new("Wooden Hoe", 4500, Constants.GENERIC_TASK_WOOD_MIN * 2.5, Constants.GENERIC_TASK_WOOD_MAX * 2.5),
        new("Stone Hoe", 6000, Constants.GENERIC_TASK_STONE_MIN * 2.5, Constants.GENERIC_TASK_STONE_MAX * 2.5),
        new("Iron Hoe", 7500, Constants.GENERIC_TASK_IRON_MIN * 2.5, Constants.GENERIC_TASK_IRON_MAX * 2.5),
        new("Diamond Hoe", 9000, Constants.GENERIC_TASK_DIAMOND_MIN * 2.5, Constants.GENERIC_TASK_DIAMOND_MAX * 2.5),
        new("Fishing Rod", 7500, Constants.FISH.First().Value * 7, Constants.FISH.Last().Value * 15)
    };

    public static Item GetItem(string name) => Array.Find(crates.Cast<Item>().Concat(consumables).Concat(perks).Concat(tools).ToArray(), i => i.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

    public static async Task<RuntimeResult> BuyCrate(Crate crate, SocketUser user, SocketGuild guild, ISocketMessageChannel channel, bool notify = true)
    {
        DbUser dbUser = await DbUser.GetById(guild.Id, user.Id);
        if (dbUser.UsingSlots)
            return CommandResult.FromError("You appear to be currently gambling. I cannot do any transactions at the moment.");
        if (dbUser.Crates.Count(s => s == crate.Name) == 10)
            return CommandResult.FromError($"You already have the maximum amount of {crate} crates (10).");
        if (crate.Price > dbUser.Cash)
            return CommandResult.FromError($"You do not have enough to buy a {crate} crate!");

        dbUser.Crates.Add(crate.Name);
        await dbUser.SetCash(user, dbUser.Cash - crate.Price);
        if (notify)
            await user.NotifyAsync(channel, $"You got yourself a {crate} crate for **{crate.Price:C2}**!");
        return CommandResult.FromSuccess();
    }

    public static async Task<RuntimeResult> BuyPerk(Perk perk, SocketUser user, SocketGuild guild, ISocketMessageChannel channel)
    {
        DbUser dbUser = await DbUser.GetById(guild.Id, user.Id);
        if (dbUser.UsingSlots)
            return CommandResult.FromError("You appear to be currently gambling. I cannot do any transactions at the moment.");
        if (dbUser.Perks.ContainsKey("Pacifist"))
            return CommandResult.FromError("You have the Pacifist perk and cannot buy another.");
        if (dbUser.Perks.ContainsKey("Multiperk") && dbUser.Perks.Count == 1 && !(perk.Name is "Pacifist" or "Multiperk"))
            return CommandResult.FromError("You already have a perk.");
        if (dbUser.Perks.ContainsKey("Multiperk") && dbUser.Perks.Count == 3 && !(perk.Name is "Pacicist" or "Multiperk"))
            return CommandResult.FromError("You already have 2 perks.");

        if (!dbUser.Perks.ContainsKey(perk.Name))
        {
            if (perk.Name == "Pacifist")
            {
                if (dbUser.PacifistCooldown != 0)
                {
                    if (dbUser.PacifistCooldown > DateTimeOffset.UtcNow.ToUnixTimeSeconds())
                    {
                        return CommandResult.FromError("You bought the Pacifist perk later than 3 days ago." +
                            $" You still have to wait {TimeSpan.FromSeconds(dbUser.PacifistCooldown - DateTimeOffset.UtcNow.ToUnixTimeSeconds()).FormatCompound()}.");
                    }
                    dbUser.PacifistCooldown = 0;
                }

                foreach (string key in dbUser.Perks.Keys)
                {
                    Perk keyPerk = GetItem(key) as Perk;
                    dbUser.Cash += keyPerk.Price;
                    dbUser.Perks.Remove(key);
                }
            }

            if (perk.Price <= dbUser.Cash)
            {
                dbUser.Perks.Add(perk.Name, DateTimeOffset.UtcNow.ToUnixTimeSeconds(perk.Duration));
                await dbUser.SetCash(user, dbUser.Cash - perk.Price);

                StringBuilder notification = new($"You got yourself the {perk} perk for **{perk.Price:C2}**!");
                if (perk.Name == "Pacifist")
                    notification.Append(" Additionally, as you bought the Pacifist perk, any perks you previously had have been refunded.");

                await user.NotifyAsync(channel, notification.ToString());
                return CommandResult.FromSuccess();
            }

            return CommandResult.FromError($"You do not have enough to buy {perk}!");
        }

        return CommandResult.FromError($"You already have {perk}!");
    }

    public static async Task<RuntimeResult> BuyTool(Tool tool, SocketUser user, SocketGuild guild, ISocketMessageChannel channel)
    {
        DbUser dbUser = await DbUser.GetById(guild.Id, user.Id);
        if (dbUser.UsingSlots)
            return CommandResult.FromError("You appear to be currently gambling. I cannot do any transactions at the moment.");

        if (!dbUser.Tools.Contains(tool.Name))
        {
            if (tool.Price <= dbUser.Cash)
            {
                dbUser.Tools.Add(tool.Name);
                await dbUser.SetCash(user, dbUser.Cash - tool.Price);
                await user.NotifyAsync(channel, $"You got yourself a fresh {tool} for **{tool.Price:C2}**!");
                return CommandResult.FromSuccess();
            }

            return CommandResult.FromError($"You do not have enough to buy a {tool}!");
        }

        return CommandResult.FromError($"You already have a {tool}!");
    }

    public static string GetBestTool(List<string> tools, string type)
    {
        IEnumerable<string> toolsOfType = tools.Where(tool => tool.EndsWith(type));
        return toolsOfType.OrderByDescending(tool => GetItem(tool).Price).First();
    }
}