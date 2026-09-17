using System.Text.Json.Serialization;

namespace PixelAutoRace;

public sealed class CarModel
{
    public string Name { get; init; } = "";
    public string ClassName { get; init; } = "";
    public string Description { get; init; } = "";
    public string Color { get; init; } = "#FF6B35";
    public int Price { get; init; }
    public double TopSpeed { get; init; }
    public double Acceleration { get; init; }
    public double Handling { get; init; }
    public double NitroCapacity { get; init; }
}

public sealed class TrackModel
{
    public string Name { get; init; } = "";
    public string Subtitle { get; init; } = "";
    public string SkyTop { get; init; } = "#10182F";
    public string SkyBottom { get; init; } = "#233B62";
    public string Accent { get; init; } = "#FFCA3A";
    public bool Unlocked { get; set; }
}

public sealed class PlayerSave
{
    public int Money { get; set; } = 1250;
    public int SelectedCar { get; set; }
    public int SelectedTrack { get; set; }
    public int Difficulty { get; set; }
    public List<int> OwnedCars { get; set; } = new() { 0 };
    public List<int> OwnedTracks { get; set; } = new() { 0 };
    public Dictionary<int, int> EngineLevels { get; set; } = new();
    public Dictionary<int, int> TurboLevels { get; set; } = new();
    public Dictionary<int, int> TireLevels { get; set; } = new();
    public Dictionary<int, int> NitroLevels { get; set; } = new();
    public Dictionary<int, string> Paints { get; set; } = new();
    public Dictionary<int, int> BestTimes { get; set; } = new();
    [JsonIgnore]
    public int TotalRaces { get; set; }
}
