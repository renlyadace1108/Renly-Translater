using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ExamBilingualReader.Core.Models.Vocab;
using ExamBilingualReader.Infrastructure.Data;

namespace ExamBilingualReader.Wpf.ViewModels;

public partial class NotebookViewModel : ObservableObject
{
    private readonly IDictionaryRepository _dictionaryRepo;

    [ObservableProperty]
    private ObservableCollection<VocabularyNotebookEntry> _entries = [];

    [ObservableProperty]
    private VocabularyNotebookEntry? _selectedEntry;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    public NotebookViewModel(IDictionaryRepository dictionaryRepo)
    {
        _dictionaryRepo = dictionaryRepo;
    }

    public async Task LoadEntriesAsync()
    {
        var items = await _dictionaryRepo.GetNotebookEntriesAsync();
        Entries.Clear();
        foreach (var item in items)
        {
            Entries.Add(item);
        }
        StatusMessage = $"共记录 {Entries.Count} 个生词。";
    }

    [RelayCommand]
    public async Task DeleteEntryAsync(VocabularyNotebookEntry? entry)
    {
        if (entry == null) return;
        var ok = await _dictionaryRepo.DeleteNotebookEntryAsync(entry.Id);
        if (ok)
        {
            Entries.Remove(entry);
            StatusMessage = $"已删除单词: {entry.Word}";
        }
    }

    [RelayCommand]
    public void ExportToCsv(string targetPath)
    {
        try
        {
            var sb = new StringBuilder();
            sb.AppendLine("Word,Phonetic,Level,Translation,ContextSentence,ContextTranslation,AddedAt");
            foreach (var e in Entries)
            {
                sb.AppendLine($"\"{e.Word}\",\"{e.Phonetic}\",\"{e.LevelTag}\",\"{e.Translation?.Replace("\"", "\"\"")}\",\"{e.ContextSentence.Replace("\"", "\"\"")}\",\"{e.ContextTranslation?.Replace("\"", "\"\"")}\",\"{e.AddedAt:yyyy-MM-dd HH:mm}\"");
            }
            File.WriteAllText(targetPath, sb.ToString(), Encoding.UTF8);
            StatusMessage = $"生词本已成功导出至: {targetPath}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"导出失败: {ex.Message}";
        }
    }
}
