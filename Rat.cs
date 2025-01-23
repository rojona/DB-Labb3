using System.Windows.Media;

namespace NET24_Labb2_WPF;

internal class Rat : Enemy
{
    private static Random _random = new Random();

    public Rat(LevelData level, int x, int y) : base(level, x, y, 'R', Colors.Red)
    {
        Health = 10;
        Name = "Rat";
        AttackDice = new Dice(1, 6, 3);
        DefenceDice = new Dice(1, 6, 1);
    }

    public override void Update()
    {
        int direction = _random.Next(4);
        int newX = X;
        int newY = Y;

        switch (direction)
        {
            case 0: newY--; break; // Up
            case 1: newY++; break; // Down
            case 2: newX--; break; // Left
            case 3: newX++; break; // Right
        }

        if (!Level.IsWall(newX, newY) && Level.GetEnemyAt(newX, newY) == null)
        {
            X = newX;
            Y = newY;
        }
    }
}