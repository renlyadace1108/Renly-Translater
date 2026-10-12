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
            Filter = "文本文件 (*.txt)|*.txt|所有文件 (*.*)|*.*"
        };
        if (ofd.ShowDialog() == true)
        {
            try
            {
                TxtArticle.Text = File.ReadAllText(ofd.FileName);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"读取文件失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
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
