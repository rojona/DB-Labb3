namespace NET24_Labb2_WPF;

public class Dice
{
    private int NumbersOfDice { get; init; }
    private int SidesPerDice { get; init; }
    private int Modifier { get; init; }
    private static Random _random = new Random();

    public Dice(int numbersOfDice, int sidesPerDice, int modifier)
    {
        NumbersOfDice = numbersOfDice;
        SidesPerDice = sidesPerDice;
        Modifier = modifier;
    }

    public int Throw()
    {
        int diceThrows = 0;
        
        for (int d = 0; d < NumbersOfDice; d++)
        {
            var diceThrow = _random.Next(1, SidesPerDice + 1);
            diceThrows += diceThrow;
        }

        diceThrows += Modifier;

        return diceThrows;
    }

    public override string ToString()
    {
        return $"{NumbersOfDice.ToString()}d{SidesPerDice.ToString()} + {Modifier.ToString()}";
    }
}