using System.Windows;
using ExamBilingualReader.Wpf.ViewModels;
using Microsoft.Win32;

namespace ExamBilingualReader.Wpf.Views;

public partial class NotebookWindow : Window
{
    public NotebookWindow()
    {
        InitializeComponent();
    }

    private async void OnRefreshClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is NotebookViewModel vm)
        {
            await vm.LoadEntriesAsync();
        }
    }

    private void OnExportCsvClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is NotebookViewModel vm)
        {
            var sfd = new SaveFileDialog
            {
                Filter = "CSV 文件 (*.csv)|*.csv",
                FileName = $"VocabularyNotebook_{DateTime.Now:yyyyMMdd}.csv"
            };

            if (sfd.ShowDialog() == true)
            {
                vm.ExportToCsv(sfd.FileName);
            }
        }
    }
}
