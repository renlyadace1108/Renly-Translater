using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ExamBilingualReader.Core.Models.Config;

public class ReaderStyleConfig : INotifyPropertyChanged
{
    private double _fontSize = 16.0;
    private string _fontFamily = "Segoe UI, Microsoft YaHei, Calibri";
    private string _fontWeight = "Normal"; // Normal, Medium, SemiBold, Bold
    private string _textColor = "#ECEEF2";
    private string _readingBackground = "#121316";
    private string _pageBackground = "#1E2025";
    private string _sidebarBackground = "#18191D";
    private string _themePreset = "DarkMorandi";

    public double FontSize
    {
        get => _fontSize;
        set => SetField(ref _fontSize, Math.Clamp(value, 12.0, 36.0));
    }

    public string FontFamily
    {
        get => _fontFamily;
        set => SetField(ref _fontFamily, value);
    }

    public string FontWeight
    {
        get => _fontWeight;
        set => SetField(ref _fontWeight, value);
    }

    public string TextColor
    {
        get => _textColor;
        set => SetField(ref _textColor, value);
    }

    public string ReadingBackground
    {
        get => _readingBackground;
        set => SetField(ref _readingBackground, value);
    }

    public string PageBackground
    {
        get => _pageBackground;
        set => SetField(ref _pageBackground, value);
    }

    public string SidebarBackground
    {
        get => _sidebarBackground;
        set => SetField(ref _sidebarBackground, value);
    }

    public string ThemePreset
    {
        get => _themePreset;
        set => SetField(ref _themePreset, value);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    public void ApplyPreset(string presetKey)
    {
        ThemePreset = presetKey;
        switch (presetKey)
        {
            case "DarkMorandi":
                // 莫兰迪暗夜极简 (默认)
                TextColor = "#ECEEF2";
                ReadingBackground = "#121316";
                PageBackground = "#1E2025";
                SidebarBackground = "#18191D";
                break;
            case "PureBlack":
                // OLED 纯黑极客
                TextColor = "#E6E6E6";
                ReadingBackground = "#000000";
                PageBackground = "#121212";
                SidebarBackground = "#0A0A0A";
                break;
            case "WarmPaper":
                // 羊皮纸暖阳护眼
                TextColor = "#2C2623";
                ReadingBackground = "#F6F1E5";
                PageBackground = "#EDE6D6";
                SidebarBackground = "#E5DEC9";
                break;
            case "SageGreen":
                // 森林绿意舒缓护眼 (暗色)
                TextColor = "#D8EED8";
                ReadingBackground = "#101D15";
                PageBackground = "#192B20";
                SidebarBackground = "#14241B";
                break;
            case "NordDracula":
                // 极地北欧冷灰蓝
                TextColor = "#ECEFF4";
                ReadingBackground = "#1E222A";
                PageBackground = "#282C34";
                SidebarBackground = "#21252B";
                break;
            case "CleanWhite":
                // 墨水屏明亮高对比
                TextColor = "#1C1D21";
                ReadingBackground = "#F5F6F9";
                PageBackground = "#FFFFFF";
                SidebarBackground = "#EAECF0";
                break;
        }
    }
}
