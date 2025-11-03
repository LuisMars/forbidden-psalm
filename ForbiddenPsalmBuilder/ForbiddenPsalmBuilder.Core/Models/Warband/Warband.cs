using ForbiddenPsalmBuilder.Core.Models.Character;

namespace ForbiddenPsalmBuilder.Core.Models.Warband;

public class Warband
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string GameVariant { get; set; } = string.Empty;
    public List<Character.Character> Members { get; set; } = new();
    public List<Character.Character> DeadMembers { get; set; } = new(); // Fallen heroes
    public List<Character.Character> FiredMembers { get; set; } = new(); // Released members
    public List<Character.Equipment> Stash { get; set; } = new(); // Warband equipment storage
    public List<Character.Equipment> CustomItems { get; set; } = new(); // User-created custom items for this warband
    public int Gold { get; set; } = 50; // Starting budget
    public int Experience { get; set; }
    public List<string> UpgradesPurchased { get; set; } = new();
    public List<string> AcquiredRelicIds { get; set; } = new(); // Track unique relics acquired in campaign
    public DateTime Created { get; set; } = DateTime.UtcNow;
    public DateTime LastModified { get; set; } = DateTime.UtcNow;

    public Warband() { }

    public Warband(string name, string gameVariant)
    {
        Name = name;
        GameVariant = gameVariant;
    }

    // Validation
    public bool IsValid => Members.Count <= 5 && Members.Count > 0;

    // Get total warband value (gold + character equipment + stash equipment)
    public int TotalValue => Gold + Members.Sum(m => m.TotalEquipmentValue) + StashValue;

    // Get total stash equipment value
    public int StashValue => Stash.Sum(e => e.Cost);

    // Check if warband can afford a purchase
    public bool CanAfford(int cost) => Gold >= cost;

    // Check warband composition rules
    public bool CanAddMember => Members.Count < 5;

    public void UpdateLastModified() => LastModified = DateTime.UtcNow;
}