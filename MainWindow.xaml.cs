using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using NET24_Labb2_WPF.Database;
using NET24_Labb2_WPF.Dialogs;

namespace NET24_Labb2_WPF;

public partial class MainWindow
{
    private readonly ObservableCollection<TextBlock> _logMessages;
    private readonly MongoDBService _mongoDBService;
    
    private GameEngine _gameEngine;
    private const int CellSize = 15;

    public MainWindow()
    {
        InitializeComponent();
        
        _logMessages = new ObservableCollection<TextBlock>();
        LogItemsControl.ItemsSource = _logMessages;

        var settings = new MongoDBSettings();
        _mongoDBService = new MongoDBService(settings);
        
        _gameEngine = new GameEngine(_mongoDBService);
        _gameEngine.GameUpdated += GameEngine_GameUpdated;
        _gameEngine.LogMessageAdded += GameEngine_LogMessageAdded;
        _gameEngine.SaveGameCompleted += GameEngine_SaveGameCompleted;
        _gameEngine.LoadGameCompleted += GameEngine_LoadGameCompleted;
        _gameEngine.ErrorOccurred += GameEngine_ErrorOccurred;
        _gameEngine.ClearGameLog += GameEngine_ClearGameLog;
        App.GameEngine = _gameEngine;

        Loaded += MainWindow_Loaded;
        Loaded += (s, e) => Focus();
        KeyDown += MainWindow_KeyDown;
        
        ShowTitleScreen();
    }

    private void ShowTitleScreen()
    {
        TitleScreen.Visibility = Visibility.Visible;
        GameScreen.Visibility = Visibility.Collapsed;
    }
    
    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        await CheckForAutoSavedGame();
    }
    
    private void ShowGameScreen()
    {
        TitleScreen.Visibility = Visibility.Collapsed;
        GameScreen.Visibility = Visibility.Visible;
    }
    
    protected override async void OnClosing(CancelEventArgs e)
    {
        if (TitleScreen.Visibility == Visibility.Visible)
        {
            return;
        }
        
        System.Diagnostics.Debug.WriteLine($"OnClosing - Screen visibility: {GameScreen.Visibility}");
        System.Diagnostics.Debug.WriteLine($"OnClosing - Game state before dialog: Player: {_gameEngine.player != null}, PlayerName: {_gameEngine.CurrentPlayerName}, Running: {_gameEngine._gameRunning}");

        if (_gameEngine.HasActivePlayer)
        {
            var result = MessageBox.Show(
                "Do you want to quit? The game will be automatically saved.", 
                "Confirm Exit", 
                MessageBoxButton.YesNo, 
                MessageBoxImage.Question);

            if (result == MessageBoxResult.No)
            {
                e.Cancel = true;
                return;
            }
            
            try
            {
                await _gameEngine.SaveGame("AutoSave", true);
                System.Diagnostics.Debug.WriteLine("Autosave completed in OnClosing");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Autosave failed in OnClosing: {ex}");
            }
        }
    }
    
    private async Task CheckForAutoSavedGame()
    {
        var savedGames = await App.GameEngine.GetSavedGames();
        var autoSavedGame = savedGames.FirstOrDefault(sg => sg.PlayerName == "AutoSave");

        if (autoSavedGame != null)
        {
            var result = MessageBox.Show(
                $"A saved game exists for player {autoSavedGame.OriginalPlayerName}. Do you want to continue this game?", 
                "Continue Saved Game", 
                MessageBoxButton.YesNo, 
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                await _gameEngine.LoadGame(autoSavedGame.Id);
                ShowGameScreen();
            }
        }
    }
    
    public async void SaveGameOnExit()
    {
        if (_gameEngine != null && _gameEngine.SavedGame != null)
        {
            await _gameEngine.SaveGame("AutoSave");
        }
    }
    
    private void GameEngine_ClearGameLog(object sender, EventArgs e)
    {
        System.Diagnostics.Debug.WriteLine("Clearing game log");
        _logMessages.Clear();
    }

    private void MainWindow_KeyDown(object sender, KeyEventArgs e)
    {
        _gameEngine.ProcessInput(e.Key);
    }

    private void GameEngine_GameUpdated(object? sender, EventArgs e)
    {
        UpdateGameDisplay();
    }

    private void GameEngine_LogMessageAdded(object sender, GameEngine.ColoredMessage message)
    {
        System.Diagnostics.Debug.WriteLine($"Received log message: {message.Text}");
        
        var textBlock = new TextBlock
        {
            Text = message.Text,
            Foreground = new SolidColorBrush(message.Color),
            FontFamily = new FontFamily("Consolas"),
            Margin = new Thickness(5, 2, 5, 2)
        };

        _logMessages.Add(textBlock);
        LogScroller.ScrollToBottom();
    }

    private void UpdateGameDisplay()
    {
        GameCanvas.Children.Clear();

        foreach (var wall in _gameEngine.VisibleWalls)
        {
            DrawGameObject(wall);
        }

        foreach (var element in _gameEngine.GameElements)
        {
            if (element is Enemy enemy)
            {
                int distance = (int)Math.Sqrt(
                    Math.Pow(enemy.X - _gameEngine.Player.X, 2) + 
                    Math.Pow(enemy.Y - _gameEngine.Player.Y, 2));
            
                if (distance <= 5)
                {
                    DrawGameObject(enemy);
                }
            }
        }

        DrawGameObject(_gameEngine.Player);

        HealthText.Text = _gameEngine.Player.Health.ToString();
        TurnText.Text = _gameEngine.TurnCount.ToString();
    }

    private void DrawGameObject(LevelElement element)
    {
        var text = new TextBlock
        {
            Text = element.GetSymbol().ToString(),
            Foreground = new SolidColorBrush(element.GetColor()),
            FontFamily = new FontFamily("Consolas"),
            FontSize = 20,
            TextAlignment = TextAlignment.Center
        };

        Canvas.SetLeft(text, element.X * CellSize);
        Canvas.SetTop(text, element.Y * CellSize);
        GameCanvas.Children.Add(text);
    }
    
    private void NewGame_Click(object sender, RoutedEventArgs e)
    {
        var nameDialog = new PlayerNameDialog();
        if (nameDialog.ShowDialog() == true)
        {
            _logMessages.Clear();
            _gameEngine = new GameEngine(_mongoDBService);
            ConnectGameEngineEvents();
            _gameEngine.StartNewGame(nameDialog.PlayerName);
            UpdateGameDisplay();
            ShowGameScreen();
        }
    }
    
    private void ConnectGameEngineEvents()
    {
        _gameEngine.GameUpdated += GameEngine_GameUpdated;
        _gameEngine.LogMessageAdded += GameEngine_LogMessageAdded;
        _gameEngine.SaveGameCompleted += GameEngine_SaveGameCompleted;
        _gameEngine.LoadGameCompleted += GameEngine_LoadGameCompleted;
        _gameEngine.ErrorOccurred += GameEngine_ErrorOccurred;
    }
    
    private void GameEngine_SaveGameCompleted(object sender, string message)
    {
        MessageBox.Show(message, "Saved", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void GameEngine_LoadGameCompleted(object sender, string message)
    {
        UpdateGameDisplay();
        ShowGameScreen();
        System.Diagnostics.Debug.WriteLine("Game load completed, switched to game screen");
    }

    private void GameEngine_ErrorOccurred(object sender, string message)
    {
        MessageBox.Show(message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
    }

    private async void SaveGame_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new InputDialog("Save Game", "Name of save game:");
        if (dialog.ShowDialog() == true)
        {
            await _gameEngine.SaveGame(dialog.ResponseText);
        }
    }

    private async void LoadGame_Click(object sender, RoutedEventArgs e)
    {
        var savedGames = await _gameEngine.GetSavedGames();
        System.Diagnostics.Debug.WriteLine($"Found {savedGames.Count} games to load");
        
        if (!savedGames.Any())
        {
            MessageBox.Show("No saved games found.", "Load Game", 
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var dialog = new LoadGameDialog(savedGames, _mongoDBService);
        if (dialog.ShowDialog() == true)
        {
            System.Diagnostics.Debug.WriteLine($"Loading game with ID: {dialog.SelectedGameId}");
            await _gameEngine.LoadGame(dialog.SelectedGameId);
            ShowGameScreen();
        }
    }
    
    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);
        Application.Current.Shutdown();
    }
}