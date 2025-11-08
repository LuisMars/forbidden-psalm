using ForbiddenPsalmBuilder.Core.Models.Character;
using ForbiddenPsalmBuilder.Core.Models.Selection;
using ForbiddenPsalmBuilder.Core.Models.Warband;
using System.Text;

namespace ForbiddenPsalmBuilder.Core.Services;

public class WarbandExportFormatterService
{
    public string FormatAsMarkdown(Warband warband, ExportOptions options,
        List<FeatFlawOption>? featOptions = null,
        List<FeatFlawOption>? flawOptions = null,
        List<InjuryOption>? injuryOptions = null,
        List<SpellOption>? spellOptions = null)
    {
        var sb = new StringBuilder();

        // Header with visual flair
        sb.AppendLine($"# {warband.Name}");
        sb.AppendLine();
        sb.AppendLine($"*{FormatGameVariantName(warband.GameVariant)}*");
        sb.AppendLine();
        sb.AppendLine("---");
        sb.AppendLine();

        // Warband Summary
        sb.AppendLine("## Warband Summary");
        sb.AppendLine();
        sb.AppendLine($"- **Treasury:** {warband.Gold} gold");
        sb.AppendLine($"- **Experience:** {warband.Experience} XP");
        if (options.IncludeMembers && warband.Members.Any())
        {
            sb.AppendLine($"- **Active Members:** {warband.Members.Count}");
        }
        if (options.IncludeDeadMembers && warband.DeadMembers.Any())
        {
            sb.AppendLine($"- **Fallen Heroes:** {warband.DeadMembers.Count}");
        }
        if (options.IncludeFiredMembers && warband.FiredMembers.Any())
        {
            sb.AppendLine($"- **Dismissed Members:** {warband.FiredMembers.Count}");
        }
        sb.AppendLine();
        sb.AppendLine("---");
        sb.AppendLine();

        // Warband Pet
        if (warband.Pet != null)
        {
            sb.AppendLine("## Warband Pet");
            sb.AppendLine();
            FormatPetAsMarkdown(sb, warband.Pet);
            sb.AppendLine();
            sb.AppendLine("---");
            sb.AppendLine();
        }

        // Dead Pet
        if (warband.DeadPet != null)
        {
            sb.AppendLine("## Fallen Pet");
            sb.AppendLine();
            sb.AppendLine($"*{warband.DeadPet.Name} died in battle.*");
            sb.AppendLine();
            FormatPetAsMarkdown(sb, warband.DeadPet);
            sb.AppendLine();
            sb.AppendLine("---");
            sb.AppendLine();
        }

        // Fired Pet
        if (warband.FiredPet != null)
        {
            sb.AppendLine("## Dismissed Pet");
            sb.AppendLine();
            sb.AppendLine($"*{warband.FiredPet.Name} was released from the warband.*");
            sb.AppendLine();
            FormatPetAsMarkdown(sb, warband.FiredPet);
            sb.AppendLine();
            sb.AppendLine("---");
            sb.AppendLine();
        }

        // Active Members
        if (options.IncludeMembers && warband.Members.Any())
        {
            sb.AppendLine("## Active Members");
            sb.AppendLine();
            for (int i = 0; i < warband.Members.Count; i++)
            {
                FormatCharacterAsMarkdown(sb, warband.Members[i], options, featOptions, flawOptions, injuryOptions, spellOptions);
                if (i < warband.Members.Count - 1)
                {
                    sb.AppendLine("---");
                    sb.AppendLine();
                }
            }
        }

        // Dead Members
        if (options.IncludeDeadMembers && warband.DeadMembers.Any())
        {
            sb.AppendLine("---");
            sb.AppendLine();
            sb.AppendLine("## Fallen Heroes");
            sb.AppendLine();
            sb.AppendLine("*These brave souls gave their lives in service to the warband.*");
            sb.AppendLine();
            for (int i = 0; i < warband.DeadMembers.Count; i++)
            {
                FormatCharacterAsMarkdown(sb, warband.DeadMembers[i], options, featOptions, flawOptions, injuryOptions, spellOptions);
                if (i < warband.DeadMembers.Count - 1)
                {
                    sb.AppendLine("---");
                    sb.AppendLine();
                }
            }
        }

        // Fired Members
        if (options.IncludeFiredMembers && warband.FiredMembers.Any())
        {
            sb.AppendLine("---");
            sb.AppendLine();
            sb.AppendLine("## Dismissed Members");
            sb.AppendLine();
            for (int i = 0; i < warband.FiredMembers.Count; i++)
            {
                FormatCharacterAsMarkdown(sb, warband.FiredMembers[i], options, featOptions, flawOptions, injuryOptions, spellOptions);
                if (i < warband.FiredMembers.Count - 1)
                {
                    sb.AppendLine("---");
                    sb.AppendLine();
                }
            }
        }

        // Stash
        if (options.IncludeStash && warband.Stash.Any())
        {
            sb.AppendLine("---");
            sb.AppendLine();
            sb.AppendLine("## Warband Stash");
            sb.AppendLine();

            var groupedStash = warband.Stash.GroupBy(i => i.Type).OrderBy(g => g.Key);
            foreach (var group in groupedStash)
            {
                sb.AppendLine($"### {FormatItemTypeName(group.Key)}");
                sb.AppendLine();
                FormatStashTable(sb, group.OrderBy(i => i.Name).ToList());
            }
        }

        // Custom Items
        if (options.IncludeCustomItems && warband.CustomItems.Any())
        {
            sb.AppendLine("---");
            sb.AppendLine();
            sb.AppendLine("## Custom Items");
            sb.AppendLine();
            FormatCustomItemsTable(sb, warband.CustomItems.OrderBy(i => i.Name).ToList());
        }

        sb.AppendLine("---");
        sb.AppendLine();
        sb.AppendLine($"*Exported from Forbidden Psalm Builder on {DateTime.Now:yyyy-MM-dd}*");

        return sb.ToString();
    }

    private string FormatGameVariantName(string gameVariant)
    {
        return gameVariant switch
        {
            "end-times" => "Forbidden Psalm: End Times",
            "28-psalms" => "Forbidden Psalm: 28 Psalms",
            "last-war" => "Forbidden Psalm: Last War",
            _ => gameVariant
        };
    }

    private string FormatItemTypeName(string? itemType)
    {
        if (string.IsNullOrEmpty(itemType)) return "Items";

        return itemType switch
        {
            "weapon" => "Weapons",
            "armor" => "Armor",
            "item" => "Items",
            "relic" => "Relics",
            "ammo" => "Ammunition",
            _ => char.ToUpper(itemType[0]) + itemType.Substring(1) + "s"
        };
    }

    private void FormatCharacterAsMarkdown(StringBuilder sb, Character character, ExportOptions options,
        List<FeatFlawOption>? featOptions, List<FeatFlawOption>? flawOptions, List<InjuryOption>? injuryOptions, List<SpellOption>? spellOptions)
    {
        sb.AppendLine($"### {character.Name}");
        sb.AppendLine();

        if (options.IncludeDetailedStats)
        {
            // Calculate effective stats (with feat/flaw/injury/equipment modifiers)
            var effectiveStats = character.GetEffectiveStats(
                featOptions ?? new List<FeatFlawOption>(),
                flawOptions ?? new List<FeatFlawOption>(),
                injuryOptions ?? new List<InjuryOption>()
            );

            // Main stats (line 1) - AGI, PRE, STR, TGH (with modifiers applied)
            sb.AppendLine("| AGI | PRE | STR | TGH |");
            sb.AppendLine("|:---:|:---:|:---:|:---:|");
            sb.AppendLine($"| {Pad(effectiveStats.Agility.ToString(), 3)} | {Pad(effectiveStats.Presence.ToString(), 3)} | {Pad(effectiveStats.Strength.ToString(), 3)} | {Pad(effectiveStats.Toughness.ToString(), 3)} |");
            sb.AppendLine();

            // Calculated stats (line 2) - HP, AV, MOV, TRG (if > 0)
            var totalArmor = character.Equipment.Where(e => e.IsArmor).Sum(e => e.ArmorValue);
            var effectiveHP = effectiveStats.HP;
            var effectiveMovement = effectiveStats.Movement;

            if (character.Tragedies > 0)
            {
                sb.AppendLine("| HP  | AV  | MOV | TRG |");
                sb.AppendLine("|:---:|:---:|:---:|:---:|");
                sb.AppendLine($"| {Pad(effectiveHP.ToString(), 3)} | {Pad(totalArmor.ToString(), 3)} | {Pad(effectiveMovement.ToString(), 3)} | {Pad(character.Tragedies.ToString(), 3)} |");
            }
            else
            {
                sb.AppendLine("| HP  | AV  | MOV |");
                sb.AppendLine("|:---:|:---:|:---:|");
                sb.AppendLine($"| {Pad(effectiveHP.ToString(), 3)} | {Pad(totalArmor.ToString(), 3)} | {Pad(effectiveMovement.ToString(), 3)} |");
            }
            sb.AppendLine();

            // Slots and Gold
            var usedSlots = character.Equipment.Sum(e => e.CalculateRequiredSlots());
            var maxSlots = character.GetEffectiveEquipmentSlots();
            sb.AppendLine($"**Slots:** {usedSlots}/{maxSlots}  ");
            sb.AppendLine($"**Gold:** {character.CurrentGold}g  ");
            sb.AppendLine();

            // Traits section with full descriptions
            var hasTraits = character.Feats.Any() || character.Flaws.Any() || character.Injuries.Any() || character.Spells.Any();
            if (hasTraits)
            {
                if (character.Feats.Any())
                {
                    sb.AppendLine("**Feats:**");
                    sb.AppendLine();
                    foreach (var featName in character.Feats)
                    {
                        var feat = featOptions?.FirstOrDefault(f => f.Name == featName);
                        if (feat != null)
                        {
                            sb.AppendLine($"- **{feat.Name}**: {feat.Effect}");
                        }
                        else
                        {
                            sb.AppendLine($"- {featName}");
                        }
                    }
                    sb.AppendLine();
                }
                if (character.Flaws.Any())
                {
                    sb.AppendLine("**Flaws:**");
                    sb.AppendLine();
                    foreach (var flawName in character.Flaws)
                    {
                        var flaw = flawOptions?.FirstOrDefault(f => f.Name == flawName);
                        if (flaw != null)
                        {
                            sb.AppendLine($"- **{flaw.Name}**: {flaw.Effect}");
                        }
                        else
                        {
                            sb.AppendLine($"- {flawName}");
                        }
                    }
                    sb.AppendLine();
                }
                if (character.Injuries.Any())
                {
                    sb.AppendLine("**Injuries:**");
                    sb.AppendLine();
                    foreach (var injuryName in character.Injuries)
                    {
                        var injury = injuryOptions?.FirstOrDefault(i => i.Name == injuryName);
                        if (injury != null)
                        {
                            sb.AppendLine($"- **{injury.Name}**: {injury.Effect}");
                        }
                        else
                        {
                            sb.AppendLine($"- {injuryName}");
                        }
                    }
                    sb.AppendLine();
                }
                if (character.Spells.Any())
                {
                    sb.AppendLine("**Spells:**");
                    sb.AppendLine();
                    foreach (var spellName in character.Spells)
                    {
                        var spell = spellOptions?.FirstOrDefault(s => s.Name == spellName);
                        if (spell != null)
                        {
                            sb.AppendLine($"- **{spell.Name}**: {spell.Effect}");
                        }
                        else
                        {
                            sb.AppendLine($"- {spellName}");
                        }
                    }
                    sb.AppendLine();
                }
            }
        }

        // Equipment - better table format (exclude spells, they're shown in traits)
        if (options.IncludeEquipment && character.Equipment.Any())
        {
            var nonSpellEquipment = character.Equipment.Where(e => e.Type != "spell").ToList();
            var equippedItems = nonSpellEquipment.Where(e => e.IsEquipped).ToList();
            var inventoryItems = nonSpellEquipment.Where(e => !e.IsEquipped).ToList();

            if (equippedItems.Any())
            {
                sb.AppendLine("**Equipped:**");
                sb.AppendLine();
                FormatEquipmentTable(sb, equippedItems);
            }

            if (inventoryItems.Any())
            {
                sb.AppendLine("**Inventory:**");
                sb.AppendLine();
                FormatEquipmentTable(sb, inventoryItems);
            }
        }
    }

    private void FormatPetAsMarkdown(StringBuilder sb, Equipment pet)
    {
        sb.AppendLine($"### {pet.Name}");
        sb.AppendLine();

        // Pet Type and Cost
        sb.AppendLine($"**Type:** {FormatItemTypeName(pet.Type).TrimEnd('s')}");
        if (pet.Cost > 0)
            sb.AppendLine($"**Cost:** {pet.Cost}g");
        sb.AppendLine();

        // Stat Modifiers
        var hasStatModifiers = pet.StatModifierStrength != 0 ||
                               pet.StatModifierAgility != 0 ||
                               pet.StatModifierPresence != 0 ||
                               pet.StatModifierToughness != 0;

        if (hasStatModifiers)
        {
            sb.AppendLine("**Stat Modifiers:**");
            sb.AppendLine();

            var modifiers = new List<string>();
            if (pet.StatModifierStrength != 0)
                modifiers.Add($"STR {(pet.StatModifierStrength > 0 ? "+" : "")}{pet.StatModifierStrength}");
            if (pet.StatModifierAgility != 0)
                modifiers.Add($"AGI {(pet.StatModifierAgility > 0 ? "+" : "")}{pet.StatModifierAgility}");
            if (pet.StatModifierPresence != 0)
                modifiers.Add($"PRE {(pet.StatModifierPresence > 0 ? "+" : "")}{pet.StatModifierPresence}");
            if (pet.StatModifierToughness != 0)
                modifiers.Add($"TGH {(pet.StatModifierToughness > 0 ? "+" : "")}{pet.StatModifierToughness}");

            foreach (var modifier in modifiers)
            {
                sb.AppendLine($"- {modifier}");
            }
            sb.AppendLine();
        }

        // Damage
        if (!string.IsNullOrEmpty(pet.Damage))
        {
            sb.AppendLine($"**Damage:** {pet.Damage}");
            sb.AppendLine();
        }

        // Properties and Special Abilities
        if (pet.Properties != null && pet.Properties.Any())
        {
            sb.AppendLine("**Properties:**");
            sb.AppendLine();
            foreach (var prop in pet.Properties)
            {
                sb.AppendLine($"- {prop}");
            }
            sb.AppendLine();
        }

        if (!string.IsNullOrEmpty(pet.Special))
        {
            sb.AppendLine($"**Special:** {pet.Special}");
            sb.AppendLine();
        }

        if (!string.IsNullOrEmpty(pet.Effect))
        {
            sb.AppendLine($"**Effect:** {pet.Effect}");
            sb.AppendLine();
        }

        // Pet Equipment/Armor
        if (pet.EquippedItems != null && pet.EquippedItems.Any())
        {
            sb.AppendLine("**Pet Equipment:**");
            sb.AppendLine();
            FormatEquipmentTable(sb, pet.EquippedItems);
        }
    }

    private void FormatEquipmentTable(StringBuilder sb, List<Equipment> equipment)
    {
        var rows = equipment.Select(eq => new
        {
            Name = eq.Name,
            Type = FormatItemTypeName(eq.Type).TrimEnd('s'),
            Details = FormatEquipmentDetailsForTable(eq)
        }).ToList();

        // Calculate column widths
        var nameWidth = Math.Max("Item".Length, rows.Max(r => r.Name.Length));
        var typeWidth = Math.Max("Type".Length, rows.Max(r => r.Type.Length));
        var detailsWidth = Math.Max("Details".Length, rows.Max(r => r.Details.Length));

        // Header
        sb.AppendLine($"| {PadRight("Item", nameWidth)} | {PadRight("Type", typeWidth)} | {PadRight("Details", detailsWidth)} |");
        sb.AppendLine($"|{new string('-', nameWidth + 2)}|{new string('-', typeWidth + 2)}|{new string('-', detailsWidth + 2)}|");

        // Rows
        foreach (var row in rows)
        {
            sb.AppendLine($"| {PadRight(row.Name, nameWidth)} | {PadRight(row.Type, typeWidth)} | {PadRight(row.Details, detailsWidth)} |");
        }
        sb.AppendLine();
    }

    private void FormatStashTable(StringBuilder sb, List<Equipment> items)
    {
        var rows = items.Select(item => new
        {
            Name = item.Name,
            Cost = item.Cost > 0 ? $"{item.Cost}g" : "-",
            Slots = item.Slots > 0 ? item.Slots.ToString() : "-"
        }).ToList();

        // Calculate column widths
        var nameWidth = Math.Max("Item".Length, rows.Max(r => r.Name.Length));
        var costWidth = Math.Max("Cost".Length, rows.Max(r => r.Cost.Length));
        var slotsWidth = Math.Max("Slots".Length, rows.Max(r => r.Slots.Length));

        // Header
        sb.AppendLine($"| {PadRight("Item", nameWidth)} | {Pad("Cost", costWidth)} | {Pad("Slots", slotsWidth)} |");
        sb.AppendLine($"|{new string('-', nameWidth + 2)}|{new string('-', costWidth + 2)}:{new string('-', slotsWidth + 2)}:|");

        // Rows
        foreach (var row in rows)
        {
            sb.AppendLine($"| {PadRight(row.Name, nameWidth)} | {Pad(row.Cost, costWidth)} | {Pad(row.Slots, slotsWidth)} |");
        }
        sb.AppendLine();
    }

    private void FormatCustomItemsTable(StringBuilder sb, List<Equipment> items)
    {
        var rows = items.Select(item => new
        {
            Name = item.Name,
            Type = FormatItemTypeName(item.Type).TrimEnd('s'),
            Cost = item.Cost > 0 ? $"{item.Cost}g" : "-",
            Slots = item.Slots > 0 ? item.Slots.ToString() : "-",
            Effect = !string.IsNullOrEmpty(item.Effect) ? item.Effect : "-"
        }).ToList();

        // Calculate column widths
        var nameWidth = Math.Max("Name".Length, rows.Max(r => r.Name.Length));
        var typeWidth = Math.Max("Type".Length, rows.Max(r => r.Type.Length));
        var costWidth = Math.Max("Cost".Length, rows.Max(r => r.Cost.Length));
        var slotsWidth = Math.Max("Slots".Length, rows.Max(r => r.Slots.Length));
        var effectWidth = Math.Max("Effect".Length, rows.Max(r => r.Effect.Length));

        // Header
        sb.AppendLine($"| {PadRight("Name", nameWidth)} | {PadRight("Type", typeWidth)} | {Pad("Cost", costWidth)} | {Pad("Slots", slotsWidth)} | {PadRight("Effect", effectWidth)} |");
        sb.AppendLine($"|{new string('-', nameWidth + 2)}|{new string('-', typeWidth + 2)}|{new string('-', costWidth + 2)}:|{new string('-', slotsWidth + 2)}:|{new string('-', effectWidth + 2)}|");

        // Rows
        foreach (var row in rows)
        {
            sb.AppendLine($"| {PadRight(row.Name, nameWidth)} | {PadRight(row.Type, typeWidth)} | {Pad(row.Cost, costWidth)} | {Pad(row.Slots, slotsWidth)} | {PadRight(row.Effect, effectWidth)} |");
        }
        sb.AppendLine();
    }

    private string FormatEquipmentDetailsForTable(Equipment eq)
    {
        var details = new List<string>();

        // Add stat (most important for weapons)
        if (!string.IsNullOrEmpty(eq.Stat))
            details.Add($"[{eq.Stat.ToUpper()}]");

        // Add damage
        if (!string.IsNullOrEmpty(eq.Damage))
            details.Add($"Dmg {eq.Damage}");

        // Add range
        if (!string.IsNullOrEmpty(eq.Range))
            details.Add($"Rng {eq.Range}");

        // Add armor value
        if (eq.ArmorValue > 0)
            details.Add($"Armor +{eq.ArmorValue}");

        // Add ammo tracking
        if (eq.MaxShots > 0)
            details.Add($"{eq.CurrentShots}/{eq.MaxShots} shots");

        // Add properties (like "Two-handed", "Reload", etc.)
        if (eq.Properties != null && eq.Properties.Any())
            details.Add(string.Join(", ", eq.Properties));

        // Add special abilities
        if (!string.IsNullOrEmpty(eq.Special))
            details.Add($"Special: {eq.Special}");

        // Add effect
        if (!string.IsNullOrEmpty(eq.Effect))
            details.Add(eq.Effect);

        return details.Any() ? string.Join(", ", details) : "-";
    }

    // Helper methods for padding table columns
    private string Pad(string text, int width)
    {
        if (text.Length >= width) return text;
        var totalPadding = width - text.Length;
        var leftPadding = totalPadding / 2;
        var rightPadding = totalPadding - leftPadding;
        return new string(' ', leftPadding) + text + new string(' ', rightPadding);
    }

    private string PadRight(string text, int width)
    {
        if (text.Length >= width)
            return text.Substring(0, width);
        return text + new string(' ', width - text.Length);
    }
}
