using ForbiddenPsalmBuilder.Core.Models.Warband;
using ForbiddenPsalmBuilder.Core.Models.GameData;
using ForbiddenPsalmBuilder.Core.Models.Character;

namespace ForbiddenPsalmBuilder.Core.Services.State;

public class GlobalGameState
{
    // Game configuration
    public string SelectedGameVariant { get; set; } = "end-times";
    public Dictionary<string, GameConfig> GameConfigs { get; set; } = new();
    public Dictionary<string, List<Equipment>> GameEquipment { get; set; } = new();
    public Dictionary<string, object> SharedGameData { get; set; } = new();

    // Warband management
    public Dictionary<string, Warband> Warbands { get; set; } = new();
    public string? ActiveWarbandId { get; set; }

    // Character builder state
    public Character? CharacterBeingBuilt { get; set; }
    public string? CharacterBuilderWarbandId { get; set; }

    // UI state
    public bool IsLoading { get; set; }
    public string? ErrorMessage { get; set; }
    public Dictionary<string, object> UIState { get; set; } = new();

    // State change notifications
    public event Action? StateChanged;
    public event Action<string>? WarbandChanged;
    public event Action<string>? GameVariantChanged;
    public event Action<string?>? ActiveWarbandChanged;

    // Helper properties
    public Warband? ActiveWarband =>
        ActiveWarbandId != null && Warbands.TryGetValue(ActiveWarbandId, out var warband)
            ? warband
            : null;

    public GameConfig? CurrentGameConfig =>
        GameConfigs.TryGetValue(SelectedGameVariant, out var config)
            ? config
            : null;

    public List<Equipment> CurrentGameEquipment =>
        GameEquipment.TryGetValue(SelectedGameVariant, out var equipment)
            ? equipment
            : new List<Equipment>();

    // State modification methods
    public void NotifyStateChanged()
    {
        StateChanged?.Invoke();
    }

    public void NotifyWarbandChanged(string warbandId)
    {
        WarbandChanged?.Invoke(warbandId);
        NotifyStateChanged();
    }

    public void NotifyGameVariantChanged(string gameVariant)
    {
        GameVariantChanged?.Invoke(gameVariant);
        NotifyStateChanged();
    }

    public void NotifyActiveWarbandChanged(string? warbandId)
    {
        ActiveWarbandChanged?.Invoke(warbandId);
        NotifyStateChanged();
    }

    public void SetError(string? error)
    {
        ErrorMessage = error;
        NotifyStateChanged();
    }

    public void ClearError()
    {
        ErrorMessage = null;
        NotifyStateChanged();
    }

    public void SetLoading(bool loading)
    {
        IsLoading = loading;
        NotifyStateChanged();
    }

    // Get available game variants
    public List<string> AvailableGameVariants => GameConfigs.Keys.ToList();

    // Get warbands for current game variant
    public List<Warband> GetWarbandsForCurrentGame() =>
        Warbands.Values.Where(w => w.GameVariant == SelectedGameVariant).ToList();

    // Validate current state
    public bool IsValidState()
    {
        // Check if active warband exists and belongs to current game
        if (ActiveWarbandId != null)
        {
            if (!Warbands.TryGetValue(ActiveWarbandId, out var warband))
                return false;

            if (warband.GameVariant != SelectedGameVariant)
                return false;
        }

        return true;
    }
}