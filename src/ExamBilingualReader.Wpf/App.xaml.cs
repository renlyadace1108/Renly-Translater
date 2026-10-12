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

        // 防止首次弹窗关闭时触发 WPF 自动退出
        ShutdownMode = ShutdownMode.OnExplicitShutdown;

        var configService = Services.GetRequiredService<AppConfigService>();
        await configService.InitializeAsync();

        var activeProvider = configService.GetActiveProvider();
        if (activeProvider == null || string.IsNullOrWhiteSpace(activeProvider.ApiKey))
        {
            // 首次打开软件强制配置 API Key，未配置不让进入
            var settingsWindow = Services.GetRequiredService<SettingsWindow>();
            var settingsVm = Services.GetRequiredService<SettingsViewModel>();
            settingsWindow.DataContext = settingsVm;
            settingsWindow.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            settingsWindow.ShowDialog();

            // 再次校验是否成功配置
            activeProvider = configService.GetActiveProvider();
            if (activeProvider == null || string.IsNullOrWhiteSpace(activeProvider.ApiKey))
            {
                MessageBox.Show(
                    "使用本软件必须配置 API Key。由于未配置有效密钥，程序即将退出。", 
                    "提示", 
                    MessageBoxButton.OK, 
                    MessageBoxImage.Information);
                Shutdown();
                return;
            }
        }

        var mainWindow = Services.GetRequiredService<MainWindow>();
        var mainVm = Services.GetRequiredService<MainViewModel>();
        mainWindow.DataContext = mainVm;

        MainWindow = mainWindow;
        ShutdownMode = ShutdownMode.OnMainWindowClose;

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
