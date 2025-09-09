using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using SteamEcho.Core.Models;

namespace SteamEcho.App.Views;

public partial class ScreenshotViewer : Window
{
    public event Action? NextRequested;
    public event Action? PreviousRequested;

    public ScreenshotViewer(INotifyPropertyChanged viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    private void NextButton_Click(object sender, RoutedEventArgs e)
    {
        NextRequested?.Invoke();
    }

    private void PreviousButton_Click(object sender, RoutedEventArgs e)
    {
        PreviousRequested?.Invoke();
    }
}