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

    private void OnExamZoneClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel vm)
        {
            vm.SwitchZoneCommand.Execute(ReadingZone.ExamPreparation);
        }
    }

    private void OnGeneralArticleZoneClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel vm)
        {
            vm.SwitchZoneCommand.Execute(ReadingZone.GeneralArticle);
        }
    }

    private async void OnExamPaperSelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (sender is System.Windows.Controls.ListBox list && list.SelectedItem is ExamBilingualReader.Core.Models.ExamHub.GitHubExamPaperItem paper)
        {
            if (DataContext is MainViewModel vm)
            {
                await vm.SelectExamPaperCommand.ExecuteAsync(paper);
            }
        }
    }

    private async void OnAnalysisSnippetMouseUp(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is not MainViewModel vm) return;
        if (sender is not System.Windows.Controls.TextBlock tb) return;

        // 获取用户选中或点击位置的词
        var text = tb.Text;
        if (string.IsNullOrWhiteSpace(text)) return;

        // 如果用户有高亮框选文本，以框选文本优先
        var targetWord = string.Empty;
        var p = e.GetPosition(tb);
        // 按空格简单拆词，寻找最近的单词
        var words = text.Split([' ', ',', '.', ';', ':', '!', '?', '(', ')', '"', '\''], StringSplitOptions.RemoveEmptyEntries);
        if (words.Length > 0)
        {
            // 如果仅有一个词或简短词汇，直接查询
            targetWord = words[0];
            // 若用户点击了特定词，尝试更精确的字符命中
            var charIndex = (int)(p.X / (tb.FontSize * 0.55));
            if (charIndex >= 0 && charIndex < text.Length)
            {
                int start = charIndex;
                while (start > 0 && char.IsLetter(text[start - 1])) start--;
                int end = charIndex;
                while (end < text.Length && char.IsLetter(text[end])) end++;
                if (end > start)
                {
                    targetWord = text[start..end];
                }
            }
        }

        if (!string.IsNullOrWhiteSpace(targetWord))
        {
            await vm.LookupWordCommand.ExecuteAsync(targetWord);
        }
    }

    private async void OnVocabSnippetClick(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is not MainViewModel vm) return;
        if (sender is FrameworkElement fe && fe.DataContext is KeyVocabularyItem vocab)
        {
            await vm.LookupWordCommand.ExecuteAsync(vocab.Word);
        }
    }
}
