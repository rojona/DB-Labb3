using System.Windows.Media;

namespace NET24_Labb2_WPF;

internal class Snake : Enemy
{
    public Snake(LevelData level, int x, int y) : base(level, x, y, 'S', Colors.Green)
    {
        Health = 25;
        Name = "Snake";
        AttackDice = new Dice(2, 4, 2);
        DefenceDice = new Dice(1, 8, 2);
    }

    public override void Update()
    {
        (int playerX, int playerY) = Level.PlayerPosition;
        int distance = (int)Math.Sqrt(Math.Pow(playerX - X, 2) + Math.Pow(playerY - Y, 2));

        if (distance <= 2)
        {
            int newX = X + Math.Sign(playerX - X);
            int newY = Y + Math.Sign(playerY - Y);

            if (!Level.IsWall(newX, newY) && Level.GetEnemyAt(newX, newY) == null)
            {
                X = newX;
                Y = newY;
            }
        }
    }
}