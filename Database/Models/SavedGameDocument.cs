using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace NET24_Labb2_WPF.Database.Models;

public class SavedGameDocument
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; }
    
    public string PlayerName { get; set; }
    public string OriginalPlayerName { get; set; }
    public int PlayerHealth { get; set; }
    public int PlayerX { get; set; }
    public int PlayerY { get; set; }
    public int TurnCount { get; set; }
    public bool IsAlive { get; set; }
    public DateTime SavedAt { get; set; }
    
    public List<SavedEnemyDocument> Enemies { get; set; } = new();
    public List<SavedWallDocument> VisibleWalls { get; set; } = new();
    public List<LogEntry> GameLog { get; set; } = new();
}