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
        services.AddSingleton<IExamHubService, GitHubExamHubService>();

        // 注册 ViewModels
        services.AddTransient<MainViewModel>();
        services.AddTransient<SettingsViewModel>();
        services.AddTransient<NotebookViewModel>();

        // 注册 Views
        services.AddTransient<MainWindow>();
        services.AddTransient<SettingsWindow>();
        services.AddTransient<NotebookWindow>();

        Services = services.BuildServiceProvider();
    }

    private static Mutex? _appMutex;

    protected override async void OnStartup(StartupEventArgs e)
    {
        _appMutex = new Mutex(true, "Renly_Translater_Desktop_Mutex", out bool createdNew);

        base.OnStartup(e);

        var mainWindow = Services.GetRequiredService<MainWindow>();
        var mainVm = Services.GetRequiredService<MainViewModel>();
        mainWindow.DataContext = mainVm;

        await mainVm.InitializeAsync();
        mainWindow.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        if (_appMutex != null)
        {
            try
            {
                _appMutex.ReleaseMutex();
            }
            catch
            {
                // ignore
            }
            _appMutex.Dispose();
        }
        base.OnExit(e);
    }
}
