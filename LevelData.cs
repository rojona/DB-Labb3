using System.IO;

namespace NET24_Labb2_WPF;

internal class LevelData
{
    private List<LevelElement> elements;
    private List<Wall> visibleWalls = new List<Wall>();
    public IReadOnlyList<LevelElement> Elements => elements.AsReadOnly();
    public IReadOnlyList<Wall> VisibleWalls => visibleWalls.AsReadOnly();
    public (int X, int Y) PlayerStartPosition { get; private set; }
    public (int X, int Y) PlayerPosition { get; set; }
    
    public int Width { get; private set; }
    public int Height { get; private set; }

    public void Load(string filename)
    {
        elements = new List<LevelElement>();
        string[] lines = File.ReadAllLines(filename);
        Height = lines.Length;
        Width = lines.Max(line => line.Length);

        for (int y = 0; y < lines.Length; y++)
        {
            for (int x = 0; x < lines[y].Length; x++)
            {
                char c = lines[y][x];
                switch (c)
                {
                    case '#':
                        elements.Add(new Wall(x, y));
                        break;
                    case 'r':
                        var rat = new Rat(this, x, y);
                        elements.Add(rat);
                        break;
                    case 's':
                        var snake = new Snake(this, x, y);
                        elements.Add(snake);
                        break;
                    case 'x':
                        PlayerStartPosition = (x, y);
                        PlayerPosition = (x, y);
                        break;
                }
            }
        }
    }
    
    public void UpdateVisibleWalls(int playerX, int playerY, int visionRange)
    {
        foreach (var element in elements)
        {
            if (element is Wall wall)
            {
                int distance = (int)Math.Sqrt(Math.Pow(wall.X - playerX, 2) + Math.Pow(wall.Y - playerY, 2));
                if (distance <= visionRange && !visibleWalls.Contains(wall))
                {
                    visibleWalls.Add(wall);
                }
            }
        }
    }

    public bool IsWall(int x, int y)
    {
        return elements.Any(e => e is Wall && e.X == x && e.Y == y);
    }
    
    public Enemy GetEnemyAt(int x, int y)
    {
        return elements.FirstOrDefault(e => e is Enemy && e.X == x && e.Y == y) as Enemy;
    }

    public void RemoveEnemy(Enemy enemy)
    {
        elements.Remove(enemy);
    }

    public void AddElement(LevelElement element)
    {
        elements.Add(element);
    }

    public void ClearEnemies()
    {
        elements.RemoveAll(e => e is Enemy);
    }

    public void ClearVisibleWalls()
    {
        visibleWalls.Clear();
    }
}