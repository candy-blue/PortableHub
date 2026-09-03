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

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // 1. Check command line args
        var isPortable = e.Args.Any(a => a.Equals("--portable", StringComparison.OrdinalIgnoreCase));
        var isMinimized = e.Args.Any(a => a.Equals("--minimized", StringComparison.OrdinalIgnoreCase) ||
                                          a.Equals("--tray", StringComparison.OrdinalIgnoreCase));

        // 2. Single instance check
        _singleInstanceManager = new SingleInstanceManager();
        if (!_singleInstanceManager.IsFirstInstance())
        {
            SingleInstanceManager.NotifyExistingInstance();
            Shutdown();
            return;
        }

        // 3. Setup Dependency Injection
        var services = new ServiceCollection();
        ConfigureServices(services, isPortable);
        _serviceProvider = services.BuildServiceProvider();

        // 4. Load Settings & Apply Theme
        var settingsService = _serviceProvider.GetRequiredService<ISettingsService>();
        await settingsService.LoadSettingsAsync();

        var themeService = _serviceProvider.GetRequiredService<ThemeService>();
        themeService.ApplyTheme(settingsService.CurrentSettings.Theme);

        // 5. Apply Database Migrations
        var migrator = _serviceProvider.GetRequiredService<DatabaseMigrator>();
        await migrator.MigrateAsync();

        // 6. Run background auto backup check
        var backupService = _serviceProvider.GetRequiredService<IBackupService>();
        _ = backupService.CheckAndPerformAutoBackupAsync();

        // 7. Create Windows and ViewModels
        _mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
        _quickLauncherWindow = _serviceProvider.GetRequiredService<QuickLauncherWindow>();

        // Wire dialog callbacks for MainViewModel
        var mainVm = _serviceProvider.GetRequiredService<MainViewModel>();
        mainVm.ShowSoftwareEditDialog = ShowSoftwareEditDialogAsync;
        mainVm.ShowCategoryEditDialog = ShowCategoryEditDialogAsync;
        mainVm.ShowScannerDialog = ShowScannerDialogAsync;
        mainVm.ShowSettingsDialog = ShowSettingsDialogAsync;

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

        // 9. Setup Global Hotkey on MainWindow handle
        var helper = new WindowInteropHelper(_mainWindow);
        helper.EnsureHandle();
        var hwnd = helper.Handle;

        _hotkeyService = (WindowsHotkeyService)_serviceProvider.GetRequiredService<IHotkeyService>();
        var source = HwndSource.FromHwnd(hwnd);
        source?.AddHook((IntPtr handle, int msg, IntPtr wParam, IntPtr lParam, ref bool handled) =>
        {
            _hotkeyService.ProcessWindowMessage(msg, wParam);
            return IntPtr.Zero;
        });

        _hotkeyService.HotkeyPressed += (s, ev) =>
        {
            Dispatcher.Invoke(() => _quickLauncherWindow.Summon());
        };

        _hotkeyService.Register(settingsService.CurrentSettings.GlobalHotkey, hwnd);

        // 10. Display Main Window
        if (!isMinimized && !settingsService.CurrentSettings.StartMinimizedToTray)
        {
            _mainWindow.Show();
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

    private Task ShowSettingsDialogAsync()
    {
        var settingsVm = _serviceProvider!.GetRequiredService<SettingsViewModel>();
        var dialog = new SettingsWindow(settingsVm)
        {
            Owner = _mainWindow
        };

        dialog.ShowDialog();
        return Task.CompletedTask;
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _hotkeyService?.Dispose();
        _trayService?.Dispose();
        _singleInstanceManager?.Dispose();
        base.OnExit(e);
    }
}
