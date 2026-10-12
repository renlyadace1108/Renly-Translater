using System.Windows;
using ExamBilingualReader.Wpf.ViewModels;

namespace ExamBilingualReader.Wpf.Views;

public partial class SettingsWindow : Window
{
    public SettingsWindow()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue is SettingsViewModel vm)
        {
            vm.RequestClose += () =>
            {
                DialogResult = true;
                Close();
            };
        }
    }

    private void OnTabModelClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is SettingsViewModel vm)
        {
            vm.SelectedTabIndex = 0;
        }
    }

    private void OnTabThemeClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is SettingsViewModel vm)
        {
            vm.SelectedTabIndex = 1;
        }
    }
}
