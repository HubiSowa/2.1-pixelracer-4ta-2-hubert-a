using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace PixelAutoRace;

public partial class MainWindow : Window
{
    private readonly string savePath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PixelAutoRace", "save.json");
    private readonly DispatcherTimer raceTimer = new() { Interval = TimeSpan.FromMilliseconds(16) };
    private readonly Random random = new();
    private PlayerSave save = new();
    private int selectedCar;
    private int selectedTrack;
    private int difficulty;
    private double playerSpeed;
    private double playerDistance;
    private double playerLane = 1;
    private double nitro;
    private double raceSeconds;
    private double countdownRemaining;
    private double cameraDistance;
    private double roadScroll;
    private DateTime lastFrameAt;
    private bool raceStarted;
    private bool nitroActive;
    private bool paused;
    private bool accelerate;
    private bool brake;
    private bool left;
    private bool right;
    private readonly List<Racer> racers = new();
    private readonly List<TrackObject> objects = new();

    public MainWindow()
    {
        InitializeComponent();
        LoadSave();
        raceTimer.Tick += RaceTimer_Tick;
        OpponentsSlider.ValueChanged += (_, _) => OpponentsText.Text = $"{(int)OpponentsSlider.Value} rywali";
        TrackCombo.ItemsSource = GameData.Tracks;
        TrackCombo.DisplayMemberPath = "Name";
        ShowMenu();
    }

    private void LoadSave()
    {
        try
        {
            if (System.IO.File.Exists(savePath)) save = JsonSerializer.Deserialize<PlayerSave>(System.IO.File.ReadAllText(savePath)) ?? new PlayerSave();
        }
        catch { save = new PlayerSave(); }
        selectedCar = Math.Clamp(save.SelectedCar, 0, GameData.Cars.Length - 1);
        selectedTrack = Math.Clamp(save.SelectedTrack, 0, GameData.Tracks.Length - 1);
        difficulty = Math.Clamp(save.Difficulty, 0, 2);
        MoneyText.Text = $"$ {save.Money:N0}".Replace(',', ' ');
    }

    private void SaveProgress()
    {
        save.SelectedCar = selectedCar;
        save.SelectedTrack = selectedTrack;
        save.Difficulty = difficulty;
        System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(savePath)!);
        System.IO.File.WriteAllText(savePath, JsonSerializer.Serialize(save, new JsonSerializerOptions { WriteIndented = true }));
        MoneyText.Text = $"$ {save.Money:N0}".Replace(',', ' ');
    }

    private void ShowOnly(UIElement view)
    {
        MenuView.Visibility = Visibility.Collapsed;
        GarageView.Visibility = Visibility.Collapsed;
        RaceSetupView.Visibility = Visibility.Collapsed;
        InstructionsView.Visibility = Visibility.Collapsed;
        RaceView.Visibility = Visibility.Collapsed;
        view.Visibility = Visibility.Visible;
    }

    private void ShowMenu() => ShowOnly(MenuView);
    private void Home_Click(object sender, RoutedEventArgs e)
    {
        raceTimer.Stop();
        paused = false;
        ShowMenu();
    }
    private void QuickRace_Click(object sender, RoutedEventArgs e) => ShowRaceSetup();
    private void Garage_Click(object sender, RoutedEventArgs e) { ShowOnly(GarageView); BuildGarage(); }
    private void Instructions_Click(object sender, RoutedEventArgs e) => ShowOnly(InstructionsView);

    private void ShowRaceSetup()
    {
        ShowOnly(RaceSetupView);
        TrackCombo.SelectedIndex = selectedTrack;
        SetupCarText.Text = GameData.Cars[selectedCar].Name;
        UpdateDifficultyButtons();
    }

    private void BuildGarage()
    {
        CarList.Children.Clear();
        for (var index = 0; index < GameData.Cars.Length; index++)
        {
            var car = GameData.Cars[index];
            var owned = save.OwnedCars.Contains(index);
            var card = new Button { Width = 190, Height = 168, Margin = new Thickness(0, 0, 14, 14), Padding = new Thickness(14), Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(index == selectedCar ? "#2B3B5A" : "#151D32")), BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(index == selectedCar ? "#FFCA3A" : "#2D3B5D")), BorderThickness = new Thickness(1), Tag = index, Cursor = Cursors.Hand };
            var panel = new StackPanel();
            panel.Children.Add(new TextBlock { Text = car.Name, FontSize = 17, FontWeight = FontWeights.Bold, Foreground = Brushes.White });
            panel.Children.Add(new TextBlock { Text = car.ClassName, FontSize = 10, Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(car.Color)), Margin = new Thickness(0, 4, 0, 12) });
            var preview = new Canvas { Width = 160, Height = 48, Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#10192D")), Margin = new Thickness(0, 0, 0, 8) };
            panel.Children.Add(preview);
            DrawCar(preview, car, 28, 28, .58, save.Paints.TryGetValue(index, out var previewPaint) ? previewPaint : null);
            panel.Children.Add(new TextBlock { Text = owned ? "W GARAŻU  /  PODGLĄD" : $"PODGLĄD  /  $ {car.Price:N0}".Replace(',', ' '), FontSize = 11, Foreground = owned ? new SolidColorBrush(Colors.LightGreen) : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#8994AC")) });
            card.Content = panel;
            card.Click += CarCard_Click;
            CarList.Children.Add(card);
        }
        UpdateGarageDetails();
    }

    private void CarCard_Click(object sender, RoutedEventArgs e)
    {
        var index = (int)((Button)sender).Tag;
        selectedCar = index;
        BuildGarage();
    }

    private void BuyCar_Click(object sender, RoutedEventArgs e)
    {
        var car = GameData.Cars[selectedCar];
        if (save.OwnedCars.Contains(selectedCar) || save.Money < car.Price) return;
        save.Money -= car.Price;
        save.OwnedCars.Add(selectedCar);
        SaveProgress();
        BuildGarage();
    }

    private void UpdateGarageDetails()
    {
        var car = GameData.Cars[selectedCar];
        GarageCarName.Text = car.Name;
        GarageCarClass.Text = car.ClassName;
        GarageCarDescription.Text = car.Description;
        SpeedBar.Value = car.TopSpeed * 10;
        AccelerationBar.Value = car.Acceleration * 10;
        HandlingBar.Value = car.Handling * 10;
        NitroBar.Value = car.NitroCapacity * 10;
        GarageCarCanvas.Children.Clear();
        DrawCar(GarageCarCanvas, car, 55, 85, 1.45, save.Paints.TryGetValue(selectedCar, out var paint) ? paint : null);
        BuyCarButton.Visibility = save.OwnedCars.Contains(selectedCar) ? Visibility.Collapsed : Visibility.Visible;
        BuyCarButton.Content = save.Money >= car.Price ? $"KUP SAMOCHÓD  •  $ {car.Price:N0}".Replace(',', ' ') : "ZA MAŁO GOTÓWKI";
        BuyCarButton.IsEnabled = save.Money >= car.Price;
        var level = GameData.Level(save.EngineLevels, selectedCar);
        UpgradeButton.Content = level >= 4 ? "SILNIK MAKSYMALNY" : $"ULEPSZ SILNIK  •  $ {650 + level * 300}";
        UpgradeButton.IsEnabled = level < 4 && save.Money >= 650 + level * 300;
    }

    private void UpgradeEngine_Click(object sender, RoutedEventArgs e)
    {
        var level = GameData.Level(save.EngineLevels, selectedCar);
        var price = 650 + level * 300;
        if (level >= 4 || save.Money < price) return;
        save.Money -= price;
        save.EngineLevels[selectedCar] = level + 1;
        SaveProgress();
        UpdateGarageDetails();
    }

    private void UpgradePart_Click(object sender, RoutedEventArgs e)
    {
        var part = (string)((Button)sender).Tag;
        var levels = part == "TurboLevels" ? save.TurboLevels : part == "TireLevels" ? save.TireLevels : save.NitroLevels;
        var level = GameData.Level(levels, selectedCar);
        var price = 450 + level * 250;
        if (level >= 4 || save.Money < price) return;
        save.Money -= price;
        levels[selectedCar] = level + 1;
        SaveProgress();
    }

    private void Paint_Click(object sender, RoutedEventArgs e)
    {
        save.Paints[selectedCar] = (string)((Button)sender).Tag;
        SaveProgress();
        BuildGarage();
    }

    private void TrackCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (TrackCombo.SelectedIndex < 0) return;
        selectedTrack = TrackCombo.SelectedIndex;
        RaceTrackTitle.Text = GameData.Tracks[selectedTrack].Name;
    }

    private void Difficulty_Click(object sender, RoutedEventArgs e)
    {
        difficulty = ((Button)sender).Name switch { "EasyButton" => 0, "HardButton" => 2, _ => 1 };
        UpdateDifficultyButtons();
    }

    private void UpdateDifficultyButtons()
    {
        EasyButton.Background = difficulty == 0 ? Brushes.Gold : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#151D32"));
        NormalButton.Background = difficulty == 1 ? Brushes.Gold : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#151D32"));
        HardButton.Background = difficulty == 2 ? Brushes.Gold : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#151D32"));
        EasyButton.Foreground = NormalButton.Foreground = HardButton.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(difficulty == 0 ? "#15121A" : "#AAB4CA"));
    }

    private void StartRace_Click(object sender, RoutedEventArgs e)
    {
        SaveProgress();
        ShowOnly(RaceView);
        StartRace();
    }

    private void StartRace()
    {
        RaceTrackTitle.Text = GameData.Tracks[selectedTrack].Name;
        playerSpeed = 0; playerDistance = 0; cameraDistance = 0; roadScroll = 0; playerLane = 1; nitro = GameData.Cars[selectedCar].NitroCapacity; raceSeconds = 0; countdownRemaining = 3.5; raceStarted = false; paused = false; lastFrameAt = DateTime.UtcNow; PauseOverlay.Visibility = Visibility.Collapsed;
        racers.Clear(); objects.Clear();
        var count = (int)OpponentsSlider.Value;
        var startingLanes = new[] { 0, 2, 0, 1, 2 };
        for (var index = 0; index < count; index++)
        {
            var startingRow = index < 2 ? 0 : 1 + (index - 2) / 3;
            var startingLane = startingRow == 0 ? startingLanes[index] : (index - 2) % 3;
            racers.Add(new Racer { Lane = startingLane, Distance = -startingRow * 58, Speed = 4.5 + index * .4 + difficulty * .55, Color = GameData.Cars[(index + 1) % GameData.Cars.Length].Color });
        }
        for (var index = 0; index < 10; index++) objects.Add(new TrackObject { Lane = random.Next(3), Distance = 180 + index * 85, Kind = index % 4 == 0 ? ObjectKind.Nitro : index % 5 == 0 ? ObjectKind.Shield : index % 3 == 0 ? ObjectKind.Roadwork : ObjectKind.Oil });
        NitroRaceBar.Maximum = GameData.Cars[selectedCar].NitroCapacity;
        NitroRaceBar.Value = nitro;
        raceTimer.Start();
    }

    private void RaceTimer_Tick(object? sender, EventArgs e)
    {
        var now = DateTime.UtcNow;
        var deltaTime = Math.Clamp((now - lastFrameAt).TotalSeconds, 0.001, 0.05);
        lastFrameAt = now;
        if (paused) return;
        if (!raceStarted)
        {
            countdownRemaining -= deltaTime;
            StartCountdownText.Text = countdownRemaining > 0.4 ? Math.Ceiling(countdownRemaining).ToString() : "START!";
            StartOverlay.Visibility = countdownRemaining > 0 ? Visibility.Visible : Visibility.Collapsed;
            if (countdownRemaining <= 0) raceStarted = true;
            RenderRace();
            return;
        }
        var frameScale = deltaTime / 0.033;
        raceSeconds += deltaTime;
        var car = GameData.Cars[selectedCar];
        var engineBoost = GameData.Level(save.EngineLevels, selectedCar) * .35;
        var targetSpeed = accelerate ? car.TopSpeed + engineBoost : car.TopSpeed * .36;
        if (brake) targetSpeed = 1.2;
        playerSpeed += (targetSpeed - playerSpeed) * (1 - Math.Pow(.94, frameScale));
        if (nitroActive && nitro > 0) { playerSpeed += .12 * frameScale; nitro -= .055 * frameScale; } else nitroActive = false;
        var steering = (right ? 1 : 0) - (left ? 1 : 0);
        playerLane = Math.Clamp(playerLane + steering * (.013 + car.Handling * .0012) * frameScale, 0, 2);
        playerDistance += playerSpeed * .52 * frameScale;
        cameraDistance += (playerDistance - cameraDistance) * (1 - Math.Pow(.82, frameScale));
        roadScroll += playerSpeed * 1.4 * frameScale;
        foreach (var racer in racers) racer.Distance += racer.Speed * .52 * frameScale;
        CheckCollisions();
        var position = 1 + racers.Count(r => r.Distance > playerDistance);
        PositionText.Text = $"{position} / {racers.Count + 1}";
        RaceTimeText.Text = TimeSpan.FromSeconds(raceSeconds).ToString(@"mm\:ss\.ff");
        NitroRaceBar.Value = nitro;
        SpeedometerBar.Value = Math.Min(SpeedometerBar.Maximum, playerSpeed * 20);
        SpeedText.Text = $"{playerSpeed * 20:0} KM/H";
        RenderRace();
        if (playerDistance >= 1100) FinishRace(position);
    }

    private void CheckCollisions()
    {
        foreach (var item in objects.Where(o => !o.Collected && Math.Abs(o.Distance - playerDistance) < 14 && Math.Abs(o.Lane - playerLane) < .55))
        {
            item.Collected = true;
            if (item.Kind == ObjectKind.Nitro) nitro = Math.Min(GameData.Cars[selectedCar].NitroCapacity, nitro + 2.2);
            else if (item.Kind == ObjectKind.Shield) playerSpeed += .7;
            else playerSpeed *= item.Kind == ObjectKind.Oil ? .48 : .7;
        }
        foreach (var racer in racers.Where(r => Math.Abs(r.Distance - playerDistance) < 14 && Math.Abs(r.Lane - playerLane) < .5)) playerSpeed *= .65;
    }

    private void RenderRace()
    {
        RaceCanvas.Children.Clear();
        var width = RaceCanvas.ActualWidth > 0 ? RaceCanvas.ActualWidth : 1000;
        var height = RaceCanvas.ActualHeight > 0 ? RaceCanvas.ActualHeight : 580;
        var track = GameData.Tracks[selectedTrack];
        RaceCanvas.Background = new LinearGradientBrush((Color)ColorConverter.ConvertFromString(track.SkyTop), (Color)ColorConverter.ConvertFromString(track.SkyBottom), 90);
        var speedRatio = Math.Clamp(playerSpeed / 10.5, 0, 1);
        for (var i = 0; i < 12; i++)
        {
            var buildingX = i * width / 12 - roadScroll * .18 % (width / 12);
            AddRect(width / 18, 80 + i % 3 * 20, new SolidColorBrush((Color)ColorConverter.ConvertFromString(i % 3 == 0 ? "#111B35" : "#17223D")), buildingX, 80 + i % 2 * 35);
            AddRect(4, 5, new SolidColorBrush((Color)ColorConverter.ConvertFromString(i % 2 == 0 ? "#FFCA3A" : "#23D5D5")), buildingX + 12, 100 + i % 4 * 18, .55);
            AddRect(4, 5, new SolidColorBrush((Color)ColorConverter.ConvertFromString(i % 2 == 0 ? "#FF4D8D" : "#FFE066")), buildingX + 28, 120 + i % 3 * 17, .45);
        }
        DrawSpeedLines(width, height, speedRatio);
        var roadTop = height * .34; var roadHeight = height * .58;
        AddRect(width, roadHeight, new SolidColorBrush((Color)ColorConverter.ConvertFromString("#242B3B")), 0, roadTop);
        AddRect(width, 7, new SolidColorBrush((Color)ColorConverter.ConvertFromString(track.Accent)), 0, roadTop + roadHeight - 6, .25);
        for (var lane = 1; lane < 3; lane++) for (var dash = 0; dash < 14; dash++)
        {
            var separator = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#A7B7D1"));
            AddRect(72, 3, separator, dash * 110 - (roadScroll % 110), roadTop + lane * roadHeight / 3, .58);
            AddRect(72, 1, new SolidColorBrush((Color)ColorConverter.ConvertFromString(track.Accent)), dash * 110 - (roadScroll % 110), roadTop + lane * roadHeight / 3 + 4, .22);
        }
        AddRect(width, 5, new SolidColorBrush((Color)ColorConverter.ConvertFromString(track.Accent)), 0, roadTop);
        DrawStarter(RaceCanvas, width * .47, roadTop - 82);
        foreach (var item in objects.Where(o => !o.Collected && o.Distance > playerDistance - 40 && o.Distance < playerDistance + 150))
        { var x = width * .16 + (item.Distance - cameraDistance) * 2.8; var y = roadTop + (item.Lane + .5) * roadHeight / 3 - 12; var color = item.Kind == ObjectKind.Nitro ? "#23D5D5" : item.Kind == ObjectKind.Shield ? "#8DE3FF" : item.Kind == ObjectKind.Oil ? "#171B28" : "#E84855"; if (x > -50 && x < width + 50) AddRect(24, 24, new SolidColorBrush((Color)ColorConverter.ConvertFromString(color)), x, y); }
        foreach (var racer in racers)
        {
            var racerX = width * .16 + (racer.Distance - cameraDistance) * 2.8;
            if (racerX > -150 && racerX < width + 150) DrawCar(RaceCanvas, GameData.Cars[0], racerX, roadTop + (racer.Lane + .5) * roadHeight / 3 - 20, .8, racer.Color);
        }
        var finishX = width * .16 + (1100 - cameraDistance) * 2.8;
        if (finishX > -80 && finishX < width + 80) DrawFinishLine(RaceCanvas, finishX, roadTop, roadHeight);
        var playerScreenX = width * .16 + (playerDistance - cameraDistance) * 2.8;
        DrawExhaust(RaceCanvas, playerScreenX, roadTop + (playerLane + .5) * roadHeight / 3 + 5, speedRatio);
        DrawCar(RaceCanvas, GameData.Cars[selectedCar], playerScreenX, roadTop + (playerLane + .5) * roadHeight / 3 - 22, .9, save.Paints.TryGetValue(selectedCar, out var paint) ? paint : null);
        for (var i = 0; i < 16; i++)
        {
            var particleX = playerScreenX - 12 - i * (5 + speedRatio * 8);
            var particleY = roadTop + (playerLane + .5) * roadHeight / 3 + ((i * 23) % 31) - 15;
            AddRect(2 + (i % 3), 2 + (i % 4), new SolidColorBrush((Color)ColorConverter.ConvertFromString(i % 3 == 0 ? "#D4E2EE" : "#8994AC")), particleX, particleY, .2 + speedRatio * .35);
        }
    }

    private void DrawSpeedLines(double width, double height, double speedRatio)
    {
        if (speedRatio < .12) return;
        for (var index = 0; index < 22; index++)
        {
            var lane = index % 5;
            var lineX = (index * 97 - roadScroll * (1.4 + lane * .08)) % (width + 150);
            if (lineX < -150) lineX += width + 150;
            var lineY = 105 + (index * 43) % Math.Max(100, (int)(height * .48));
            var length = 18 + speedRatio * (48 + (index % 4) * 14);
            AddRect(length, index % 3 == 0 ? 3 : 2, new SolidColorBrush((Color)ColorConverter.ConvertFromString(index % 2 == 0 ? "#8DE3FF" : "#FFCA3A")), lineX, lineY, .12 + speedRatio * .3);
        }
    }

    private void DrawExhaust(Canvas canvas, double x, double y, double speedRatio)
    {
        var flame = nitroActive ? "#FFCA3A" : "#FF6B35";
        var glow = new SolidColorBrush((Color)ColorConverter.ConvertFromString(flame));
        var size = 5 + speedRatio * 9 + (nitroActive ? 8 : 0);
        AddRectTo(canvas, size * 1.8, 5, glow, x - size * 1.8, y, .35);
        AddRectTo(canvas, size, 3, glow, x - size * 2.5, y + 1, .8);
        if (nitroActive) AddRectTo(canvas, size * .7, 2, Brushes.White, x - size * 2.8, y + 1, .9);
    }

    private void DrawStarter(Canvas canvas, double x, double y)
    {
        var skin = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F5B97A"));
        var jacket = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#23D5D5"));
        var dark = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#101522"));
        AddRectTo(canvas, 14, 14, skin, x + 15, y);
        AddRectTo(canvas, 20, 5, dark, x + 12, y - 3);
        AddRectTo(canvas, 18, 25, jacket, x + 13, y + 14);
        AddRectTo(canvas, 6, 22, jacket, x + 6, y + 15);
        AddRectTo(canvas, 6, 22, jacket, x + 32, y + 15);
        AddRectTo(canvas, 7, 20, dark, x + 14, y + 39);
        AddRectTo(canvas, 7, 20, dark, x + 25, y + 39);
        AddRectTo(canvas, 10, 4, new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFCA3A")), x + 39, y + 19);
        AddRectTo(canvas, 10, 4, new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFCA3A")), x + 48, y + 19);
        AddRectTo(canvas, 3, 3, Brushes.White, x + 42, y + 20);
        AddRectTo(canvas, 3, 3, Brushes.White, x + 51, y + 20);
    }

    private void DrawFinishLine(Canvas canvas, double x, double roadTop, double roadHeight)
    {
        var tileSize = 16d;
        for (var row = 0; row < 12; row++)
            for (var column = 0; column < 2; column++)
            {
                var color = (row + column) % 2 == 0 ? "#F7F4EA" : "#101522";
                AddRectTo(canvas, tileSize, roadHeight / 12 + 1, new SolidColorBrush((Color)ColorConverter.ConvertFromString(color)), x + column * tileSize, roadTop + row * roadHeight / 12);
            }
        AddRectTo(canvas, 4, roadHeight, new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFCA3A")), x - 5, roadTop);
        AddRectTo(canvas, 4, roadHeight, new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFCA3A")), x + tileSize * 2 + 1, roadTop);
    }

    private void DrawCar(Canvas canvas, CarModel car, double x, double y, double scale, string? overrideColor = null)
    {
        var color = new SolidColorBrush((Color)ColorConverter.ConvertFromString(overrideColor ?? car.Color));
        var shadow = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#080D18"));
        var glass = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#8DE3FF"));
        var highlight = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFF1B8"));
        var darkColor = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#9B2948"));
        AddRectTo(canvas, 112 * scale, 8 * scale, shadow, x - 8 * scale, y + 32 * scale, .65);
        AddRectTo(canvas, 92 * scale, 28 * scale, color, x, y);
        AddRectTo(canvas, 48 * scale, 20 * scale, color, x + 20 * scale, y - 15 * scale);
        AddRectTo(canvas, 26 * scale, 10 * scale, glass, x + 28 * scale, y - 10 * scale);
        AddRectTo(canvas, 4 * scale, 10 * scale, darkColor, x + 54 * scale, y - 10 * scale);
        AddRectTo(canvas, 92 * scale, 5 * scale, darkColor, x, y + 23 * scale);
        AddRectTo(canvas, 16 * scale, 16 * scale, shadow, x + 14 * scale, y + 20 * scale);
        AddRectTo(canvas, 16 * scale, 16 * scale, shadow, x + 70 * scale, y + 20 * scale);
        AddRectTo(canvas, 8 * scale, 8 * scale, new SolidColorBrush((Color)ColorConverter.ConvertFromString("#35415D")), x + 18 * scale, y + 24 * scale);
        AddRectTo(canvas, 8 * scale, 8 * scale, new SolidColorBrush((Color)ColorConverter.ConvertFromString("#35415D")), x + 74 * scale, y + 24 * scale);
        AddRectTo(canvas, 8 * scale, 5 * scale, highlight, x + 4 * scale, y + 6 * scale);
        AddRectTo(canvas, 8 * scale, 5 * scale, new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF4D8D")), x + 84 * scale, y + 7 * scale);
        AddRectTo(canvas, 14 * scale, 5 * scale, darkColor, x - 7 * scale, y - 7 * scale);
        AddRectTo(canvas, 5 * scale, 8 * scale, shadow, x + 94 * scale, y + 14 * scale);
    }

    private void AddRectTo(Canvas canvas, double width, double height, Brush brush, double left, double top, double opacity = 1)
    {
        var rectangle = new Rectangle { Width = width, Height = height, Fill = brush, Opacity = opacity };
        Canvas.SetLeft(rectangle, left); Canvas.SetTop(rectangle, top); canvas.Children.Add(rectangle);
    }

    private void AddRect(double width, double height, Brush brush, double left, double top, double opacity = 1) => AddRectTo(RaceCanvas, width, height, brush, left, top, opacity);

    private void FinishRace(int position)
    {
        raceTimer.Stop();
        var reward = position switch { 1 => 1200, 2 => 750, 3 => 450, _ => 200 };
        save.Money += reward;
        save.TotalRaces++;
        if (!save.BestTimes.TryGetValue(selectedTrack, out var best) || best == 0 || raceSeconds < best) save.BestTimes[selectedTrack] = (int)(raceSeconds * 100);
        SaveProgress();
        MessageBox.Show($"META! Zająłeś miejsce {position}.\nNagroda: $ {reward:N0}".Replace(',', ' '), "Pixel Auto Race", MessageBoxButton.OK, MessageBoxImage.Information);
        ShowRaceSetup();
    }

    private void ExitRace_Click(object sender, RoutedEventArgs e) { raceTimer.Stop(); ShowMenu(); }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (RaceView.Visibility != Visibility.Visible) return;
        if (e.Key is Key.P) { paused = !paused; PauseOverlay.Visibility = paused ? Visibility.Visible : Visibility.Collapsed; return; }
        if (e.Key is Key.Space) { nitroActive = true; e.Handled = true; }
        if (e.Key is Key.W or Key.Up) accelerate = true;
        if (e.Key is Key.S or Key.Down) brake = true;
        if (e.Key is Key.A or Key.Left) left = true;
        if (e.Key is Key.D or Key.Right) right = true;
    }

    protected override void OnKeyUp(KeyEventArgs e)
    {
        base.OnKeyUp(e);
        if (e.Key is Key.W or Key.Up) accelerate = false;
        if (e.Key is Key.S or Key.Down) brake = false;
        if (e.Key is Key.A or Key.Left) left = false;
        if (e.Key is Key.D or Key.Right) right = false;
    }

    private sealed class Racer { public int Lane; public double Distance; public double Speed; public string Color = "#23D5D5"; }
    private sealed class TrackObject { public int Lane; public double Distance; public ObjectKind Kind; public bool Collected; }
    private enum ObjectKind { Oil, Roadwork, Nitro, Shield }
}
