using System.Windows;
using MongoDB.Driver;
using NET24_Labb2_WPF.Database;
using NET24_Labb2_WPF.Database.Models;

namespace NET24_Labb2_WPF.Dialogs;

public partial class LoadGameDialog
{
    public string SelectedGameId { get; private set; }
    private readonly MongoDBService _mongoDBService;
    private readonly List<SavedGameDocument> _savedGames;

    public LoadGameDialog(List<SavedGameDocument> savedGames, MongoDBService mongoDbService)
    {
        InitializeComponent();
        _mongoDBService = mongoDbService;
        _savedGames = savedGames;
        SavedGamesList.ItemsSource = savedGames;
    }
    
    private async void DeleteButton_Click(object sender, RoutedEventArgs e)
    {
        if (SavedGamesList.SelectedItem is SavedGameDocument selected)
        {
            var result = MessageBox.Show(
                $"Are you sure you want to delete the save game '{selected.PlayerName}'?",
                "Confirm Delete",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    var collection = _mongoDBService.GetCollection<SavedGameDocument>("SavedGames");
                    var filter = Builders<SavedGameDocument>.Filter.Eq(g => g.Id, selected.Id);
                    await collection.DeleteOneAsync(filter);
                    
                    _savedGames.Remove(selected);
                    SavedGamesList.Items.Refresh();
                    
                    MessageBox.Show("Save game deleted successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to delete save game: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        else
        {
            MessageBox.Show("Please select a save game to delete.", "No Selection", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private void LoadButton_Click(object sender, RoutedEventArgs e)
    {
        if (SavedGamesList.SelectedItem is SavedGameDocument selected)
        {
            SelectedGameId = selected.Id;
            DialogResult = true;
        }
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}