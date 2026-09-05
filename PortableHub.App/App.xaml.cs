using System.Windows;
using System.Windows.Interop;
using Microsoft.Extensions.DependencyInjection;
using PortableHub.App.Services;
using PortableHub.App.ViewModels;
using PortableHub.App.Views;
using PortableHub.Core.Interfaces;
using PortableHub.Core.Models;
using PortableHub.Infrastructure.Data;
using PortableHub.Infrastructure.Repositories;
using PortableHub.Infrastructure.Services;

namespace PortableHub.App;

public partial class App : System.Windows.Application
{
    private IServiceProvider? _serviceProvider;
    private SingleInstanceManager? _singleInstanceManager;
    private TrayService? _trayService;
    private WindowsHotkeyService? _hotkeyService;
    private MainWindow? _mainWindow;
    private QuickLauncherWindow? _quickLauncherWindow;

    private static void LogStartup(string msg)
    {
        try
        {
            var dir = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PortableHub", "logs");
            System.IO.Directory.CreateDirectory(dir);
            System.IO.File.AppendAllText(System.IO.Path.Combine(dir, "startup.log"), $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {msg}\r\n");
        }
        catch { }
    }

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        LogStartup("=== Application OnStartup Started ===");

        // 0. Prevent premature WPF shutdown during async initialization
        ShutdownMode = ShutdownMode.OnExplicitShutdown;

        // Global crash capture
        DispatcherUnhandledException += (s, args) =>
        {
            LogStartup($"DispatcherUnhandledException: {args.Exception}");
            MessageBox.Show($"Portable Hub 运行错误: {args.Exception.Message}\n\n{args.Exception}", "Portable Hub", MessageBoxButton.OK, MessageBoxImage.Error);
            args.Handled = true;
        };
        AppDomain.CurrentDomain.UnhandledException += (s, args) =>
        {
            if (args.ExceptionObject is Exception ex)
            {
                LogStartup($"AppDomain UnhandledException: {ex}");
                MessageBox.Show($"Portable Hub 严重错误: {ex.Message}\n\n{ex}", "Portable Hub", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        };

        try
        {
            // 1. Check command line args
            var isPortable = e.Args.Any(a => a.Equals("--portable", StringComparison.OrdinalIgnoreCase));
            var isMinimized = e.Args.Any(a => a.Equals("--minimized", StringComparison.OrdinalIgnoreCase) ||
                                              a.Equals("--tray", StringComparison.OrdinalIgnoreCase));
            LogStartup($"Args: portable={isPortable}, minimized={isMinimized}");

            // 2. Single instance check
            _singleInstanceManager = new SingleInstanceManager();
            if (!_singleInstanceManager.IsFirstInstance())
            {
                LogStartup("Another instance is running. Notifying existing instance and shutting down.");
                SingleInstanceManager.NotifyExistingInstance();
                Shutdown();
                return;
            }
            LogStartup("Single instance mutex acquired.");

            // 3. Setup Dependency Injection
            var services = new ServiceCollection();
            ConfigureServices(services, isPortable);
            _serviceProvider = services.BuildServiceProvider();
            LogStartup("Service provider built.");

            // 4. Load Settings & Apply Theme & Language
            var settingsService = _serviceProvider.GetRequiredService<ISettingsService>();
            await settingsService.LoadSettingsAsync();
            LogStartup($"Settings loaded. Theme={settingsService.CurrentSettings.Theme}, Lang={settingsService.CurrentSettings.Language}");

            var locService = _serviceProvider.GetRequiredService<ILocalizationService>();
            locService.SetLanguage(settingsService.CurrentSettings.Language);
            LogStartup("Language applied.");

            var themeService = _serviceProvider.GetRequiredService<ThemeService>();
            themeService.ApplyTheme(settingsService.CurrentSettings.Theme);
            LogStartup("Theme applied.");

            // 5. Apply Database Migrations
            var migrator = _serviceProvider.GetRequiredService<DatabaseMigrator>();
            await migrator.MigrateAsync();
            LogStartup("Database migrated.");

            // 6. Run background auto backup check
            var backupService = _serviceProvider.GetRequiredService<IBackupService>();
            _ = backupService.CheckAndPerformAutoBackupAsync();

            // 7. Create Windows and ViewModels
            _mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
            _quickLauncherWindow = _serviceProvider.GetRequiredService<QuickLauncherWindow>();
            LogStartup("Windows resolved from DI.");

            // Wire dialog callbacks for MainViewModel
            var mainVm = _serviceProvider.GetRequiredService<MainViewModel>();
            mainVm.ShowSoftwareEditDialog = ShowSoftwareEditDialogAsync;
            mainVm.ShowCategoryEditDialog = ShowCategoryEditDialogAsync;
            mainVm.ShowScannerDialog = ShowScannerDialogAsync;
            mainVm.ShowSettingsDialog = ShowSettingsDialogAsync;

            // Pre-initialize MainViewModel so SearchService & QuickLauncher have data immediately
            await mainVm.InitializeAsync();

            // 8. Setup Tray Service
            _trayService = _serviceProvider.GetRequiredService<TrayService>();
            var softwareRepo = _serviceProvider.GetRequiredService<ISoftwareRepository>();
            var launchService = _serviceProvider.GetRequiredService<ILaunchService>();

            _trayService.Initialize(
                onShowWindow: () => Dispatcher.Invoke(() => _mainWindow.ShowAndActivate()),
                onShowQuickLauncher: () => Dispatcher.Invoke(() => _quickLauncherWindow.Summon()),
                onOpenSettings: () => Dispatcher.Invoke(async () => await ShowSettingsDialogAsync()),
                onExit: () => Dispatcher.Invoke(() =>
                {
                    _mainWindow.ForceExit();
                    Shutdown();
                }),
                getRecentSoftware: async () =>
                {
                    var all = await softwareRepo.GetAllAsync();
                    return all.Where(s => s.LastLaunchedAt.HasValue)
                              .OrderByDescending(s => s.LastLaunchedAt)
                              .ToList();
                },
                onLaunchSoftware: (s) => Dispatcher.Invoke(async () => await launchService.LaunchAsync(s))
            );
            LogStartup("TrayService initialized.");

            // 9. Setup Global Hotkey on MainWindow handle
            var helper = new WindowInteropHelper(_mainWindow);
            helper.EnsureHandle();
            var hwnd = helper.Handle;

            _hotkeyService = (WindowsHotkeyService)_serviceProvider.GetRequiredService<IHotkeyService>();
            var source = HwndSource.FromHwnd(hwnd);
            source?.AddHook((IntPtr handle, int msg, IntPtr wParam, IntPtr lParam, ref bool handled) =>
            {
                if (msg == 0x0312) // WM_HOTKEY
                {
                    _hotkeyService.ProcessWindowMessage(msg, wParam);
                    handled = true;
                }
                else if (msg == SingleInstanceManager.WM_SHOWME)
                {
                    LogStartup("Received WM_SHOWME in HwndSource hook. Showing MainWindow.");
                    Dispatcher.Invoke(() => _mainWindow.ShowAndActivate());
                    handled = true;
                }
                return IntPtr.Zero;
            });

            // Also hook thread-wide message dispatcher for seamless hotkey and WM_SHOWME delivery
            ComponentDispatcher.ThreadFilterMessage += (ref MSG msg, ref bool handled) =>
            {
                if (msg.message == 0x0312) // WM_HOTKEY
                {
                    _hotkeyService.ProcessWindowMessage(msg.message, msg.wParam);
                    handled = true;
                }
                else if (msg.message == SingleInstanceManager.WM_SHOWME)
                {
                    LogStartup("Received WM_SHOWME in ThreadFilterMessage. Showing MainWindow.");
                    Dispatcher.Invoke(() => _mainWindow.ShowAndActivate());
                    handled = true;
                }
            };

            _hotkeyService.HotkeyPressed += (s, ev) =>
            {
                Dispatcher.Invoke(() => _quickLauncherWindow.Summon());
            };

            var hotkeyRegistered = _hotkeyService.Register(settingsService.CurrentSettings.GlobalHotkey, hwnd);
            LogStartup($"Hotkey registered: {hotkeyRegistered} for key '{settingsService.CurrentSettings.GlobalHotkey}'");

            // 10. Display Main Window
            LogStartup($"Showing MainWindow: isMinimized={isMinimized}");
            if (!isMinimized)
            {
                _mainWindow.Show();
                _mainWindow.Activate();
                LogStartup("MainWindow shown.");
            }
            LogStartup("=== OnStartup Completed Successfully ===");
        }
        catch (Exception ex)
        {
            LogStartup($"Fatal error during OnStartup: {ex}");
            MessageBox.Show($"Portable Hub 启动失败:\n\n{ex.Message}\n\n{ex.StackTrace}", "启动失败", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ConfigureServices(IServiceCollection services, bool isExplicitPortable)
    {
        // Settings & Data
        var settingsService = new SettingsService();
        settingsService.InitializePortableMode(isExplicitPortable);
        services.AddSingleton<ISettingsService>(settingsService);

        services.AddSingleton<DatabaseConnectionFactory>();
        services.AddTransient<DatabaseMigrator>();

        // Repositories
        services.AddTransient<ICategoryRepository, CategoryRepository>();
        services.AddTransient<ISoftwareRepository, SoftwareRepository>();
        services.AddTransient<IRootDirectoryRepository, RootDirectoryRepository>();

        // Services
        services.AddSingleton<IIconService, IconService>();
        services.AddTransient<ILaunchService, LaunchService>();
        services.AddSingleton<ISearchService, SearchService>();
        services.AddTransient<IFileScannerService, FileScannerService>();
        services.AddTransient<IPathRepairService, PathRepairService>();
        services.AddTransient<IBackupService, BackupService>();
        services.AddTransient<IStartupService, StartupService>();
        services.AddSingleton<IHotkeyService, WindowsHotkeyService>();

        services.AddSingleton<ThemeService>();
        services.AddSingleton<TrayService>();
        services.AddSingleton<ILocalizationService, LocalizationService>();
        services.AddSingleton<IUpdateService, GithubUpdateService>();

        // ViewModels
        services.AddSingleton<MainViewModel>();
        services.AddSingleton<QuickLauncherViewModel>();
        services.AddTransient<SettingsViewModel>();
        services.AddTransient<ScannerViewModel>();

        // Windows
        services.AddSingleton<MainWindow>();
        services.AddSingleton<QuickLauncherWindow>();
    }

    private Task<bool> ShowSoftwareEditDialogAsync(Software software)
    {
        var provider = _serviceProvider!;
        var iconService = provider.GetRequiredService<IIconService>();
        var scannerService = provider.GetRequiredService<IFileScannerService>();
        var softwareRepo = provider.GetRequiredService<ISoftwareRepository>();
        var categoryRepo = provider.GetRequiredService<ICategoryRepository>();

        var isEdit = software.Id > 0;
        var vm = new SoftwareEditViewModel(software, isEdit, iconService, scannerService, softwareRepo, categoryRepo);
        var dialog = new SoftwareEditDialog(vm)
        {
            Owner = _mainWindow
        };

        var result = dialog.ShowDialog();
        return Task.FromResult(result == true);
    }

    private Task<Category?> ShowCategoryEditDialogAsync(Category? category)
    {
        var vm = new CategoryEditViewModel(category);
        var dialog = new CategoryEditDialog(vm)
        {
            Owner = _mainWindow
        };

        var result = dialog.ShowDialog();
        return Task.FromResult(result == true ? vm.ResultCategory : null);
    }

    private Task ShowScannerDialogAsync()
    {
        var scannerVm = _serviceProvider!.GetRequiredService<ScannerViewModel>();
        var dialog = new ScannerDialog(scannerVm)
        {
            Owner = _mainWindow
        };

        dialog.ShowDialog();
        return Task.CompletedTask;
    }

    private async Task ShowSettingsDialogAsync()
    {
        var settingsVm = _serviceProvider!.GetRequiredService<SettingsViewModel>();
        await settingsVm.InitializeAsync();
        var dialog = new SettingsWindow(settingsVm);
        if (_mainWindow != null && _mainWindow.IsVisible && _mainWindow.WindowState != WindowState.Minimized)
        {
            dialog.Owner = _mainWindow;
            dialog.WindowStartupLocation = WindowStartupLocation.CenterOwner;
        }
        else
        {
            dialog.WindowStartupLocation = WindowStartupLocation.CenterScreen;
        }

        dialog.ShowDialog();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _hotkeyService?.Dispose();
        _trayService?.Dispose();
        _singleInstanceManager?.Dispose();
        base.OnExit(e);
    }
}
