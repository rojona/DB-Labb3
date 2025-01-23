using System.Windows;

namespace NET24_Labb2_WPF.Dialogs;

public partial class InputDialog
{
    public string ResponseText { get; private set; }

    public InputDialog(string title, string question)
    {
        InitializeComponent();
        Title = title;
        QuestionText.Text = question;
        ResponseTextBox.Focus();
    }

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrWhiteSpace(ResponseTextBox.Text))
        {
            ResponseText = ResponseTextBox.Text;
            DialogResult = true;
        }
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}