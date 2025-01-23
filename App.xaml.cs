using System.Diagnostics;
using System.Windows;

namespace NET24_Labb2_WPF;

public partial class App
{
    public static GameEngine GameEngine { get; set; }
    
    protected override void OnExit(ExitEventArgs e)
    {
        base.OnExit(e); Debug.WriteLine("OnExit called - cleaning up");
        try
        {
            GameEngine?.Dispose();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Cleanup failed: {ex}");
        }
    }
}