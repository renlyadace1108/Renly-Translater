using System.Windows;
using ExamBilingualReader.Infrastructure.Data;
using ExamBilingualReader.Infrastructure.Services;
using ExamBilingualReader.Wpf.ViewModels;
using ExamBilingualReader.Wpf.Views;
using Microsoft.Extensions.DependencyInjection;

namespace ExamBilingualReader.Wpf;

public partial class App : Application
{
    public static IServiceProvider Services { get; private set; } = null!;

    public App()
    {
        var services = new ServiceCollection();

        // 注册服务层
        services.AddSingleton<AppConfigService>();
        services.AddSingleton<IDictionaryRepository, SqliteDictionaryRepository>();
        services.AddSingleton<IOpenAiClient, OpenAiCompatibleClient>();
        services.AddSingleton<IArticleProcessingService, ArticleProcessingService>();

        // 注册 ViewModels
        services.AddTransient<MainViewModel>();
        services.AddTransient<SettingsViewModel>();

        // 注册 Views
        services.AddTransient<MainWindow>();
        services.AddTransient<SettingsWindow>();

        Services = services.BuildServiceProvider();
    }

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var mainWindow = Services.GetRequiredService<MainWindow>();
        var mainVm = Services.GetRequiredService<MainViewModel>();
        mainWindow.DataContext = mainVm;

        await mainVm.InitializeAsync();
        mainWindow.Show();
    }
}
