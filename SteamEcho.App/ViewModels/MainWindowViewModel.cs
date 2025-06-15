using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Input;
using Microsoft.Win32;
using SteamEcho.Core.Models;

namespace SteamEcho.App.ViewModels;

public class MainWindowViewModel
{
    public ObservableCollection<Game> Games { get; } = [];

    public ICommand AddGameCommand { get; }

    public MainWindowViewModel()
    {
        AddGameCommand = new RelayCommand(AddGame);
    }

    private void AddGame()
    {
        var dialog = new OpenFileDialog
        {
            Filter = "Executable Files (*.exe)|*.exe",
            Title = "Select Game Executable"
        };

        if (dialog.ShowDialog() == true)
        {
            var fileName = Path.GetFileNameWithoutExtension(dialog.FileName);
            Games.Add(new Game(fileName, dialog.FileName));
        }
    }
}