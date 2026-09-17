namespace PixelAutoRace;

public static class GameData
{
    public static readonly CarModel[] Cars =
    {
        new() { Name = "Comet 86", ClassName = "HOT HATCH", Description = "Lekki, zwinny klasyk na każdy zakręt.", Color = "#FF6B35", Price = 0, TopSpeed = 7.2, Acceleration = 7.8, Handling = 8.8, NitroCapacity = 6 },
        new() { Name = "Vandal GT", ClassName = "MUSCLE", Description = "V8, szerokie opony i zero kompromisów.", Color = "#E84855", Price = 2800, TopSpeed = 9.1, Acceleration = 8.2, Handling = 5.8, NitroCapacity = 7 },
        new() { Name = "Neon R", ClassName = "SPORT", Description = "Japońska precyzja w nocnym świetle.", Color = "#23D5D5", Price = 4200, TopSpeed = 8.3, Acceleration = 8.6, Handling = 8.2, NitroCapacity = 8 },
        new() { Name = "Road King", ClassName = "GT", Description = "Długi dystans? Ten rekin zna drogę.", Color = "#5D8BF4", Price = 5200, TopSpeed = 8.8, Acceleration = 7.6, Handling = 7.4, NitroCapacity = 9 },
        new() { Name = "Apex X", ClassName = "SUPER", Description = "Torowy potwór z włókna i iskier.", Color = "#C77DFF", Price = 7600, TopSpeed = 9.6, Acceleration = 9.2, Handling = 8.5, NitroCapacity = 8 },
        new() { Name = "Solaris", ClassName = "HYPER", Description = "Przyszłość wyścigów. Szybkość bez granic.", Color = "#F9C74F", Price = 11000, TopSpeed = 10, Acceleration = 9.6, Handling = 9.2, NitroCapacity = 10 }
    };

    public static readonly TrackModel[] Tracks =
    {
        new() { Name = "NOCNE MIASTO", Subtitle = "Neony, mokry asfalt, 4.2 km", SkyTop = "#10142B", SkyBottom = "#3B1F4B", Accent = "#FF4D8D", Unlocked = true },
        new() { Name = "SŁONECZNA AUTOSTRADA", Subtitle = "Asfalt i kurz, 6.8 km", SkyTop = "#3C7CA8", SkyBottom = "#F2A65A", Accent = "#FFE066" },
        new() { Name = "PRZEŁĘCZ PÓŁNOCNA", Subtitle = "Zakręty i śnieg, 5.5 km", SkyTop = "#20334B", SkyBottom = "#78909C", Accent = "#8DE3FF" }
    };

    public static int Level(Dictionary<int, int> levels, int car) => levels.TryGetValue(car, out var level) ? level : 0;
}
