using System.Windows.Input;
using System.Windows.Media;
using MongoDB.Driver;
using NET24_Labb2_WPF.Database;
using NET24_Labb2_WPF.Database.Models;

namespace NET24_Labb2_WPF;

public class GameEngine : IDisposable
{
    private LevelData _level;
    public Player player;
    public bool _gameRunning;
    private int _turnCount;

    private readonly MongoDBService _mongoDBService;
    public string CurrentPlayerName { get; private set; }
    private string CurrentGameID { get; set; }
    public SavedGameDocument SavedGame { get; set; }

    public event EventHandler GameUpdated;
    public event EventHandler<ColoredMessage> LogMessageAdded;
    public event EventHandler GameOver;
    public event EventHandler ClearGameLog;

    public event EventHandler<string> SaveGameCompleted;
    public event EventHandler<string> LoadGameCompleted;
    public event EventHandler<string> ErrorOccurred; 

    public IReadOnlyList<LevelElement> GameElements => _level.Elements;
    public IReadOnlyList<Wall> VisibleWalls => _level.VisibleWalls;
    public Player Player => player;
    public int TurnCount => _turnCount;
    public bool IsGameOver => !_gameRunning;
    
    public bool HasActivePlayer
    {
        get
        {
            System.Diagnostics.Debug.WriteLine($"HasActivePlayer check - Player: {player != null}, PlayerName: {CurrentPlayerName}, Running: {_gameRunning}");
            return player != null && !string.IsNullOrEmpty(CurrentPlayerName) && _gameRunning;
        }
    }

    public GameEngine(MongoDBService mongoDBService)
    {
        _mongoDBService = mongoDBService;
        SavedGame = new SavedGameDocument();
        _gameRunning = false;
    }
    
    public void Dispose()
    {
        _mongoDBService?.Dispose();
    }

    private void InitializeGame(string playerName)
    {
        CurrentPlayerName = playerName;
        _level = new LevelData();
        _level.Load("Levels/Level1.txt");
        player = new Player(_level.PlayerStartPosition.X, _level.PlayerStartPosition.Y, CurrentPlayerName);
        _gameRunning = true;
        _turnCount = 0;
        _level.UpdateVisibleWalls(player.X, player.Y, 5);
        SavedGame = new SavedGameDocument();
        OnGameUpdated();
    }

    public void StartNewGame(string playerName)
    {
        System.Diagnostics.Debug.WriteLine($"Starting new game with player name: {playerName}");
        InitializeGame(playerName);
        System.Diagnostics.Debug.WriteLine($"Game initialized - Player: {player != null}, PlayerName: {CurrentPlayerName}, Running: {_gameRunning}");
    }
    
    public bool IsGameActive
    {
        get
        {
            System.Diagnostics.Debug.WriteLine($"IsGameActive check - _gameRunning: {_gameRunning}");
            return _gameRunning;
        }
    }

    public void ProcessInput(Key key)
    {
        if (!_gameRunning) return;

        int newX = player.X;
        int newY = player.Y;

        switch (key)
        {
            case Key.Up: newY--; break;
            case Key.Down: newY++; break;
            case Key.Left: newX--; break;
            case Key.Right: newX++; break;
            case Key.Escape:
                _gameRunning = false;
                OnGameOver();
                return;
        }

        if (!IsWall(newX, newY))
        {
            var enemy = GetEnemyAt(newX, newY);
            if (enemy != null)
            {
                Attack(player, enemy);
            }
            else
            {
                player.X = newX;
                player.Y = newY;
                _level.PlayerPosition = (newX, newY);
                _level.UpdateVisibleWalls(player.X, player.Y, 5);
                _turnCount++;
            }

            UpdateEnemies();

            if (player.Health <= 0)
            {
                _gameRunning = false;
                HandleGameOver();
                return;
            }

            OnGameUpdated();
        }
    }
    
    private void LogMessage(string text, Color color)
    {
        OnLogMessageAdded(new ColoredMessage { Text = text, Color = color });

        if (SavedGame == null)
        {
            SavedGame = new SavedGameDocument
            {
                GameLog = new List<LogEntry>()
            };
        }
        
        SavedGame.GameLog.Add(new LogEntry
        {
            Text = text,
            Color = color.ToString()
        });

        System.Diagnostics.Debug.WriteLine($"Added log message: {text} - Total messages: {SavedGame.GameLog.Count}");
    }
    
    public class ColoredMessage
    {
        public string Text { get; set; }
        public Color Color { get; set; }
    }

    private async void Attack(LevelElement attacker, LevelElement defender)
    {
        if (!(attacker is Enemy || attacker is Player) || 
            !(defender is Enemy || defender is Player))
            return;

        Dice attackDice = attacker is Enemy enemyAtk ? enemyAtk.AttackDice : player.AttackDice;
        Dice defenceDice = defender is Enemy enemyDef ? enemyDef.DefenceDice : player.DefenceDice;

        string attackerName = attacker is Enemy enemy ? enemy.Name : CurrentPlayerName;
        string defenderName = defender is Enemy enemy2 ? enemy2.Name : CurrentPlayerName;
        
        LogMessage("", Colors.White);
        LogMessage($"{attackerName} attacks!", Colors.White);

        int attackRoll = attackDice.Throw();
        int defenceRoll = defenceDice.Throw();

        Color attackOutcomeColor = GetAttackOutcomeColor(attacker, attackRoll, defenceRoll);

        LogMessage($"{attackerName} rolls an attack power of {attackRoll}  ({attackDice})", Colors.White );
        LogMessage($"{defenderName} rolls a defense power of {defenceRoll}  ({defenceDice})", Colors.White );

        if (attackRoll > defenceRoll)
        {
            int damage = attackRoll - defenceRoll;
            LogMessage($"{attackRoll} to {defenceRoll} = {damage} damage to {defenderName}", attackOutcomeColor );

            ApplyDamage(defender, damage, defenderName);
        }
        else
        {
            Color missColor = attacker is Player ? Colors.SandyBrown : Colors.DeepSkyBlue;
            LogMessage($"{attackRoll} to {defenceRoll} = {attackerName} misses", missColor);
        }

        if (GetHealth(defender) > 0)
        {
            LogMessage("", Colors.White);
            LogMessage($"{defenderName} retaliates!", Colors.White);

            int retaliationRoll = defenceDice.Throw();
            int retaliationDefenceRoll = attackDice.Throw();

            Color retaliationOutcomeColor = GetAttackOutcomeColor(defender, retaliationRoll, retaliationDefenceRoll);

            LogMessage($"{defenderName} rolls an attack power of {retaliationRoll}  ({defenceDice})", Colors.White );
            LogMessage($"{attackerName} rolls a defense power of {retaliationDefenceRoll}  ({attackDice})", Colors.White );

            if (retaliationRoll > retaliationDefenceRoll)
            {
                int damage = retaliationRoll - retaliationDefenceRoll;
                LogMessage($"{retaliationRoll} to {retaliationDefenceRoll} = {damage} damage to {attackerName}", retaliationOutcomeColor );

                ApplyDamage(attacker, damage, attackerName);
            }
            else
            {
                Color missColor = defender is Player ? Colors.SandyBrown : Colors.DeepSkyBlue;
                LogMessage($"{retaliationRoll} to {retaliationDefenceRoll} = {defenderName} misses", missColor );
            }
        }

        if (GetHealth(attacker) > 0 && GetHealth(defender) > 0)
        {
            LogMessage("", Colors.White);
            LogMessage($"{attackerName} has {GetHealth(attacker)} health.", attacker is Player ? Colors.GreenYellow : Colors.OrangeRed
            );
    
            LogMessage($"{defenderName} has {GetHealth(defender)} health.", defender is Player ? Colors.GreenYellow : Colors.OrangeRed
            );
            LogMessage("", Colors.White);
            LogMessage("*******************************************", Colors.White);
        }
    }

    private void ApplyDamage(LevelElement element, int damage, string elementName)
    {
        if (element is Enemy enemyElement)
        {
            enemyElement.Health = Math.Max(0, enemyElement.Health - damage);
            if (enemyElement.Health <= 0)
            {
                LogMessage($"{elementName} dies!", Colors.GreenYellow);
                LogMessage("", Colors.White);
                LogMessage("*******************************************", Colors.White);
                RemoveEnemy(enemyElement);
            }
        }
        else if (element is Player)
        {
            player.Health = Math.Max(0, player.Health - damage);
            if (player.Health <= 0)
            {
                LogMessage($"{CurrentPlayerName} died!", Colors.OrangeRed );
                LogMessage("", Colors.White);
                LogMessage("*******************************************", Colors.White);
                HandleGameOver();
            }
        }
    }

    private Color GetAttackOutcomeColor(LevelElement attacker, int attackRoll, int defenceRoll)
    {
        if (attacker is Player)
        {
            return attackRoll > defenceRoll ? Colors.DeepSkyBlue :
                   attackRoll < defenceRoll ? Colors.SandyBrown : Colors.White;
        }
        else
        {
            return attackRoll > defenceRoll ? Colors.SandyBrown :
                   attackRoll < defenceRoll ? Colors.DeepSkyBlue : Colors.White;
        }
    }
    
    private int GetHealth(LevelElement element)
    {
        return element switch
        {
            Enemy enemy => enemy.Health,
            Player player => player.Health,
            _ => throw new ArgumentException("Invalid element type")
        };
    }

    private void UpdateEnemies()
    {
        foreach (var element in _level.Elements.ToList())
        {
            if (element is Enemy enemy)
            {
                int oldX = enemy.X;
                int oldY = enemy.Y;

                enemy.Update();

                if (enemy.X == player.X && enemy.Y == player.Y)
                {
                    Attack(enemy, player);
                    enemy.X = oldX;
                    enemy.Y = oldY;
                }
                else if (IsWall(enemy.X, enemy.Y) || GetEnemyAt(enemy.X, enemy.Y) != enemy)
                {
                    enemy.X = oldX;
                    enemy.Y = oldY;
                }
            }
        }
    }

    private bool IsWall(int x, int y) => _level.IsWall(x, y);

    private Enemy GetEnemyAt(int x, int y) => _level.GetEnemyAt(x, y);

    private void RemoveEnemy(Enemy enemy) => _level.RemoveEnemy(enemy);

    protected virtual void OnGameUpdated()
    {
        GameUpdated?.Invoke(this, EventArgs.Empty);
    }

    protected virtual void OnLogMessageAdded(ColoredMessage message)
    {
        LogMessageAdded?.Invoke(this, message);
    }
    
    protected virtual void OnClearGameLog()
    {
        ClearGameLog?.Invoke(this, EventArgs.Empty);
    }
    
    private async Task HandleGameOver()
    {
        _gameRunning = false;
    
        if (!string.IsNullOrEmpty(CurrentGameID))
        {
            try
            {
                var collection = _mongoDBService.GetCollection<SavedGameDocument>("SavedGames");
                
                await collection.DeleteManyAsync(g => g.PlayerName == "AutoSave");

                if (!string.IsNullOrEmpty(CurrentGameID))
                {
                    await collection.DeleteOneAsync(g => g.Id == CurrentGameID);
                }
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(this, $"Could not update saved games: {ex.Message}");
            }
        }
    
        OnGameOver();
    }

    protected virtual void OnGameOver()
    {
        GameOver?.Invoke(this, EventArgs.Empty);
    }

    public async Task SaveGame(string saveName, bool isAutoSave = false)
    {
        if (!_gameRunning)
        {
            ErrorOccurred?.Invoke(this, $"Can't save game after death!");
            return;
        }
        
        try
        {
            var collection = _mongoDBService.GetCollection<SavedGameDocument>("SavedGames");

            if (saveName == "AutoSave")
            {
                await collection.DeleteManyAsync(g => g.PlayerName == "AutoSave");
            }
            
            var savedGame = new SavedGameDocument
            {
                PlayerName = saveName,
                OriginalPlayerName = CurrentPlayerName,
                PlayerHealth = player.Health,
                PlayerX = player.X,
                PlayerY = player.Y,
                TurnCount = _turnCount,
                IsAlive = true,
                SavedAt = DateTime.Now,
                GameLog = SavedGame.GameLog,
                
                Enemies = _level.Elements
                    .OfType<Enemy>()
                    .Select(e => new SavedEnemyDocument
                    {
                        Type = e.GetType().Name,
                        Health = e.Health,
                        X = e.X,
                        Y = e.Y
                    })
                    .ToList(),

                VisibleWalls = _level.VisibleWalls
                    .Select(w => new SavedWallDocument
                    {
                        X = w.X,
                        Y = w.Y
                    })
                    .ToList(),
            };

            await collection.InsertOneAsync(savedGame);
            
            CurrentGameID = savedGame.Id;
            
            SaveGameCompleted?.Invoke(this, "Game saved!");
        }
        catch (Exception ex)
        {
            ErrorOccurred?.Invoke(this, $"Could not save game: {ex.Message}");
        }
    }

    public async Task<List<SavedGameDocument>> GetSavedGames()
    {
        try
        {
            var collection = _mongoDBService.GetCollection<SavedGameDocument>("SavedGames");
            return await collection.Find(g => g.IsAlive)
                                 .SortByDescending(g => g.SavedAt)
                                 .ToListAsync();
        }
        catch (Exception ex)
        {
            ErrorOccurred?.Invoke(this, $"Failed fetching saved games: {ex.Message}");
            return new List<SavedGameDocument>();
        }
    }

    public async Task LoadGame(string gameId)
    {
        try
        {
            var collection = _mongoDBService.GetCollection<SavedGameDocument>("SavedGames");
            var savedGame = await collection.Find(g => g.Id == gameId).FirstOrDefaultAsync();

            System.Diagnostics.Debug.WriteLine($"Loading game with ID: {gameId}");
            
            if (savedGame == null)
            {
                System.Diagnostics.Debug.WriteLine("Could not find saved game");
                ErrorOccurred?.Invoke(this, "Could not find saved game.");
                return;
            }
            
            System.Diagnostics.Debug.WriteLine($"Found game for player: {savedGame.OriginalPlayerName}");
            
            _gameRunning = true;
            _level = new LevelData();
            _level.Load("Levels/Level1.txt");
            
            player = new Player(savedGame.PlayerX, savedGame.PlayerY, CurrentPlayerName)
            {
                Health = savedGame.PlayerHealth
            };

            _turnCount = savedGame.TurnCount;
            CurrentGameID = savedGame.Id;
            CurrentPlayerName = savedGame.OriginalPlayerName;

            _level.ClearEnemies();
            foreach (var enemyDoc in savedGame.Enemies)
            {
                Enemy enemy = enemyDoc.Type switch
                {
                    "Rat" => new Rat(_level, enemyDoc.X, enemyDoc.Y),
                    "Snake" => new Snake(_level, enemyDoc.X, enemyDoc.Y),
                    _ => throw new InvalidOperationException($"Unknown enemy type: {enemyDoc.Type}")
                };
                enemy.Health = enemyDoc.Health;
                _level.AddElement(enemy);
            }

            _level.ClearVisibleWalls();
            foreach (var wallDoc in savedGame.VisibleWalls)
            {
                _level.UpdateVisibleWalls(wallDoc.X, wallDoc.Y, 0);
            }
            
            SavedGame = new SavedGameDocument
            {
                GameLog = savedGame.GameLog
            };

            OnClearGameLog();
            
            System.Diagnostics.Debug.WriteLine($"Reloading {savedGame.GameLog.Count} log messages");

            foreach (var entry in savedGame.GameLog)
            {
                OnLogMessageAdded(new ColoredMessage
                {
                    Text = entry.Text,
                    Color = (Color)ColorConverter.ConvertFromString(entry.Color),
                });
            }

            OnGameUpdated();
            LoadGameCompleted?.Invoke(this, "Game loaded successfully!");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading game: {ex}");
            ErrorOccurred?.Invoke(this, $"Could not load game: {ex.Message}");
        }
    }
}