using System.IO;
using System.Windows;
using Microsoft.Win32;

namespace ExamBilingualReader.Wpf.Views;

public partial class ImportArticleWindow : Window
{
    public string? ResultText { get; private set; }

    public ImportArticleWindow(string currentText)
    {
        InitializeComponent();
        TxtArticle.Text = currentText;
    }

    private void OnOpenFileClick(object sender, RoutedEventArgs e)
    {
        var ofd = new OpenFileDialog
        {
            Filter = "所有支持的文档 (*.pdf;*.md;*.txt;*.html)|*.pdf;*.md;*.txt;*.html|PDF 文件 (*.pdf)|*.pdf|Markdown 文件 (*.md;*.markdown)|*.md;*.markdown|纯文本文件 (*.txt)|*.txt|所有文件 (*.*)|*.*"
        };
        if (ofd.ShowDialog() == true)
        {
            try
            {
                TxtArticle.Text = ExamBilingualReader.Core.TextProcessing.DocumentTextExtractor.ExtractFromFile(ofd.FileName);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"读取文件失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private async void OnFetchGitHubPapersClick(object sender, RoutedEventArgs e)
    {
        PnlGitHub.Visibility = Visibility.Visible;
        var examHub = App.Services.GetRequiredService<ExamBilingualReader.Infrastructure.Services.IExamHubService>();
        try
        {
            var papers = await examHub.FetchLatestExamPapersAsync();
            LstGitHubPapers.ItemsSource = papers;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"获取真题列表失败: {ex.Message}", "网络提示", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void OnCloseGitHubPanelClick(object sender, RoutedEventArgs e)
    {
        PnlGitHub.Visibility = Visibility.Collapsed;
    }

    private async void OnGitHubPaperSelected(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (LstGitHubPapers.SelectedItem is ExamBilingualReader.Core.Models.ExamHub.GitHubExamPaperItem paper)
        {
            var examHub = App.Services.GetRequiredService<ExamBilingualReader.Infrastructure.Services.IExamHubService>();
            try
            {
                TxtArticle.Text = "正在从 GitHub 下载真题内容，请稍候...";
                var content = await examHub.DownloadPaperContentAsync(paper.DownloadUrl);
                TxtArticle.Text = content;
                PnlGitHub.Visibility = Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"下载真题内容失败: {ex.Message}", "网络错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private void OnLoadSampleIeltsClick(object sender, RoutedEventArgs e)
    {
        TxtArticle.Text = """
According to recent investigations by Dr. Henderson et al., cognitive architectures in multilingual subjects exhibit substantial neuroplasticity. This phenomenon is particularly evident when individuals engage in complex academic tasks (e.g., simultaneous interpretation or dense textual synthesis).

Although conventional linguistic frameworks have posited that lexical retrieval relies on rigid syntactic hierarchies, contemporary empirical data suggest that bilingual cognitive control can mitigate age-related cognitive decline, which confirms findings originally reported by Fig. 4 in the Cambridge journal.
""";
    }

    private void OnLoadSampleCetClick(object sender, RoutedEventArgs e)
    {
        TxtArticle.Text = """
In recent years, the ubiquity of artificial intelligence tools has reshaped academic integrity across universities. As Prof. Miller observed, many undergraduates who rely excessively on automated translation systems tend to show a decrease in autonomous critical thinking.

To address this challenge, educators suggest that students should analyze syntactic structures deliberately, rather than passively accepting machine translations.
""";
    }

    private void OnConfirmClick(object sender, RoutedEventArgs e)
    {
        ResultText = TxtArticle.Text;
        DialogResult = true;
        Close();
    }

    private void OnCancelClick(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
