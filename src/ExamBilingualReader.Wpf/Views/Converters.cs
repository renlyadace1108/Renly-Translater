using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ExamBilingualReader.Wpf.Views;

public class BooleanToVisibilityConverter : IValueConverter
{
    public static readonly BooleanToVisibilityConverter Instance = new();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool b)
        {
            return b ? Visibility.Visible : Visibility.Collapsed;
        }
        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class MultiBooleanToVisibilityConverter : IMultiValueConverter
{
    public static readonly MultiBooleanToVisibilityConverter Instance = new();

    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values == null || values.Length < 2) return Visibility.Collapsed;
        var isExamZone = values[0] is ExamBilingualReader.Wpf.ViewModels.ReadingZone zone && zone == ExamBilingualReader.Wpf.ViewModels.ReadingZone.ExamPreparation;
        var isDrawerOpen = values[1] is bool b && b;
        return (isExamZone && isDrawerOpen) ? Visibility.Visible : Visibility.Collapsed;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class DrawerToggleTextConverter : IValueConverter
{
    public static readonly DrawerToggleTextConverter Instance = new();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool isOpen && isOpen)
        {
            return "◀ 收起真题";
        }
        return "📁 展开真题";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class LayoutModeToButtonTextConverter : IValueConverter
{
    public static readonly LayoutModeToButtonTextConverter Instance = new();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is ExamBilingualReader.Wpf.ViewModels.ReadingLayoutMode mode)
        {
            return mode switch
            {
                ExamBilingualReader.Wpf.ViewModels.ReadingLayoutMode.FullArticleContinuous => "📖 原文全文(拖选精读)",
                ExamBilingualReader.Wpf.ViewModels.ReadingLayoutMode.ContinuousParagraph => "📑 自然段落考纲精读",
                ExamBilingualReader.Wpf.ViewModels.ReadingLayoutMode.SentenceBySentence => "🗂 逐句双语对照卡片",
                _ => "📖 排版模式"
            };
        }
        return "📖 排版模式";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class ListJoinConverter : IValueConverter
{
    public static readonly ListJoinConverter Instance = new();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is IEnumerable<string> list)
        {
            return string.Join(", ", list);
        }
        return string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class EnumEqualsToVisibilityConverter : IValueConverter
{
    public static readonly EnumEqualsToVisibilityConverter Instance = new();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value == null || parameter == null) return Visibility.Collapsed;
        return value.ToString() == parameter.ToString() ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class BooleanToInverseBooleanConverter : IValueConverter
{
    public static readonly BooleanToInverseBooleanConverter Instance = new();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool b)
        {
            return !b;
        }
        return true;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool b)
        {
            return !b;
        }
        return false;
    }
}

public class StringToVisibilityConverter : IValueConverter
{
    public static readonly StringToVisibilityConverter Instance = new();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return string.IsNullOrWhiteSpace(value as string) ? Visibility.Collapsed : Visibility.Visible;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class HexToBrushConverter : IValueConverter
{
    public static readonly HexToBrushConverter Instance = new();
    private static readonly System.Windows.Media.BrushConverter BrushConverter = new();
    private static readonly Dictionary<string, System.Windows.Media.SolidColorBrush> Cache = new();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string hex && !string.IsNullOrWhiteSpace(hex))
        {
            if (Cache.TryGetValue(hex, out var cached)) return cached;
            try
            {
                var brush = (System.Windows.Media.SolidColorBrush)BrushConverter.ConvertFromString(hex)!;
                brush.Freeze();
                Cache[hex] = brush;
                return brush;
            }
            catch
            {
                // 解析失败时回退默认
            }
        }
        return System.Windows.Media.Brushes.Transparent;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
