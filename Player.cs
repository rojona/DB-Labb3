
using System.Windows.Media;

namespace NET24_Labb2_WPF;

public class Player : LevelElement
{
    public Player(int x, int y, string name) : base(x, y, 'X', Colors.Yellow)
    {
        Health = 100;
        Name = name;
        AttackDice = new Dice(2, 6, 2);
        DefenceDice = new Dice(2, 6, 0);
    }

    public int Health { get; set; }
    public string Name { get; set; }
    public Dice AttackDice { get; }
    public Dice DefenceDice { get; }
}