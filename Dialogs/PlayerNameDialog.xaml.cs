using System.Windows;

namespace NET24_Labb2_WPF.Dialogs;

public partial class PlayerNameDialog
{
    public string PlayerName { get; private set; }

    public PlayerNameDialog()
    {
        InitializeComponent();
        NameTextBox.Focus();
    }

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrWhiteSpace(NameTextBox.Text))
        {
            PlayerName = NameTextBox.Text;
            DialogResult = true;
        }
        else
        {
            MessageBox.Show("Please enter a name.", "Name Required", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}