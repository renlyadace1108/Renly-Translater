using System.Windows;
using System.Windows.Input;
using ExamBilingualReader.Core.Models.Reader;
using ExamBilingualReader.Wpf.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace ExamBilingualReader.Wpf.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void OnSentenceClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement element && element.DataContext is BilingualSentencePair pair)
        {
            if (DataContext is MainViewModel vm)
            {
                vm.SelectSentenceCommand.Execute(pair);
            }
        }
    }

    private void OnOpenSettingsClick(object sender, RoutedEventArgs e)
    {
        var settingsWindow = App.Services.GetRequiredService<SettingsWindow>();
        settingsWindow.DataContext = App.Services.GetRequiredService<SettingsViewModel>();
        settingsWindow.Owner = this;
        settingsWindow.ShowDialog();
    }

    private async void OnOpenNotebookClick(object sender, RoutedEventArgs e)
    {
        var notebookWindow = App.Services.GetRequiredService<NotebookWindow>();
        var vm = App.Services.GetRequiredService<NotebookViewModel>();
        notebookWindow.DataContext = vm;
        notebookWindow.Owner = this;
        await vm.LoadEntriesAsync();
        notebookWindow.ShowDialog();
    }

    private async void OnImportArticleClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel vm)
        {
            var dialog = new ImportArticleWindow(vm.ArticleInput)
            {
                Owner = this
            };

            if (dialog.ShowDialog() == true && !string.IsNullOrWhiteSpace(dialog.ResultText))
            {
                vm.ArticleInput = dialog.ResultText;
                await vm.LoadArticleCommand.ExecuteAsync(null);
            }
        }
    }
}
