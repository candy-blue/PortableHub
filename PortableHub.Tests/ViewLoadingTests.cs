using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Microsoft.Extensions.DependencyInjection;
using PortableHub.App.Converters;
using PortableHub.App.Services;
using PortableHub.App.ViewModels;
using PortableHub.App.Views;
using PortableHub.Core.Interfaces;
using PortableHub.Core.Models;
using PortableHub.Infrastructure.Data;
using PortableHub.Infrastructure.Repositories;
using PortableHub.Infrastructure.Services;
using Xunit;

namespace PortableHub.Tests;

public class StaTestFixture : IDisposable
{
    private readonly Thread _thread;
    private readonly TaskCompletionSource<Dispatcher> _tcs = new();
    private Dispatcher? _dispatcher;

    public StaTestFixture()
    {
        _thread = new Thread(() =>
        {
            if (Application.Current == null)
            {
                var app = new Application();
                app.Resources.MergedDictionaries.Add(new Wpf.Ui.Markup.ThemesDictionary { Theme = Wpf.Ui.Appearance.ApplicationTheme.Dark });
                app.Resources.MergedDictionaries.Add(new Wpf.Ui.Markup.ControlsDictionary());
                try
                {
                    app.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri("pack://application:,,,/PortableHub.App;component/Themes/Colors.xaml", UriKind.Absolute) });
                    app.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri("pack://application:,,,/PortableHub.App;component/Themes/Brushes.xaml", UriKind.Absolute) });
                    app.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri("pack://application:,,,/PortableHub.App;component/Themes/Typography.xaml", UriKind.Absolute) });
                    app.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri("pack://application:,,,/PortableHub.App;component/Themes/Controls.xaml", UriKind.Absolute) });
                }
                catch
                {
                    // If pack URI cannot be resolved in test runner domain
                }
                app.Resources.Add("ComparisonConverter", new ComparisonConverter());
                app.Resources.Add("StringToImageSourceConverter", new StringToImageSourceConverter());
                app.Resources.Add("CardSizeToDimensionConverter", new CardSizeToDimensionConverter());
                app.Resources.Add("ColorHexToBrushConverter", new ColorHexToBrushConverter());
                app.Resources.Add("InverseBooleanConverter", new InverseBooleanConverter());
                app.Resources.Add("InverseBooleanToVisibilityConverter", new InverseBooleanToVisibilityConverter());
                app.Resources.Add("NullToVisibilityConverter", new NullToVisibilityConverter());
                app.Resources.Add("BooleanToVisibilityConverter", new BooleanToVisibilityConverter());
                app.Resources.Add("ComparisonToVisibilityConverter", new ComparisonToVisibilityConverter());
            }
            _tcs.SetResult(Dispatcher.CurrentDispatcher);
            Dispatcher.Run();
        });
        _thread.SetApartmentState(ApartmentState.STA);
        _thread.IsBackground = true;
        _thread.Start();
        _dispatcher = _tcs.Task.Result;
    }

    public void Run(Action action)
    {
        _dispatcher!.Invoke(action);
    }

    public void Dispose()
    {
        _dispatcher?.InvokeShutdown();
    }
}

public class ViewLoadingTests : IClassFixture<StaTestFixture>
{
    private readonly StaTestFixture _fixture;

    public ViewLoadingTests(StaTestFixture fixture)
    {
        _fixture = fixture;
    }

    private static IServiceProvider CreateServiceProvider(TestEnvironment env)
    {
        var services = new ServiceCollection();
        services.AddSingleton<ISettingsService>(env.SettingsService);
        services.AddSingleton(env.ConnectionFactory);
        services.AddTransient<DatabaseMigrator>();
        services.AddTransient<ICategoryRepository, CategoryRepository>();
        services.AddTransient<ISoftwareRepository, SoftwareRepository>();
        services.AddTransient<IRootDirectoryRepository, RootDirectoryRepository>();
        services.AddSingleton<IIconService, IconService>();
        services.AddTransient<ILaunchService, LaunchService>();
        services.AddSingleton<ISearchService, SearchService>();
        services.AddTransient<IFileScannerService, FileScannerService>();
        services.AddTransient<IPathRepairService, PathRepairService>();
        services.AddTransient<IBackupService, BackupService>();
        services.AddTransient<IStartupService, StartupService>();
        services.AddSingleton<IHotkeyService, FakeHotkeyService>();
        services.AddSingleton<ThemeService>();
        services.AddTransient<SettingsViewModel>();
        services.AddSingleton<MainViewModel>();
        services.AddSingleton<QuickLauncherViewModel>();
        services.AddTransient<ScannerViewModel>();

        return services.BuildServiceProvider();
    }

    [Fact]
    public void SettingsWindow_CanBeConstructed_WithoutXamlExceptions()
    {
        _fixture.Run(() =>
        {
            using var env = new TestEnvironment();
            var provider = CreateServiceProvider(env);
            var vm = provider.GetRequiredService<SettingsViewModel>();

            var window = new SettingsWindow(vm);
            Assert.NotNull(window);
            Assert.Equal("系统设置", window.Title);
        });
    }

    [Fact]
    public void MainWindow_CanBeConstructed_WithoutXamlExceptions()
    {
        _fixture.Run(() =>
        {
            using var env = new TestEnvironment();
            var provider = CreateServiceProvider(env);
            var vm = provider.GetRequiredService<MainViewModel>();
            var settingsService = provider.GetRequiredService<ISettingsService>();

            var window = new MainWindow(vm, settingsService);
            Assert.NotNull(window);
        });
    }

    [Fact]
    public void QuickLauncherWindow_CanBeConstructed_WithoutXamlExceptions()
    {
        _fixture.Run(() =>
        {
            using var env = new TestEnvironment();
            var provider = CreateServiceProvider(env);
            var vm = provider.GetRequiredService<QuickLauncherViewModel>();

            var window = new QuickLauncherWindow(vm);
            Assert.NotNull(window);
        });
    }

    [Fact]
    public void ScannerDialog_CanBeConstructed_WithoutXamlExceptions()
    {
        _fixture.Run(() =>
        {
            using var env = new TestEnvironment();
            var provider = CreateServiceProvider(env);
            var vm = provider.GetRequiredService<ScannerViewModel>();

            var dialog = new ScannerDialog(vm);
            Assert.NotNull(dialog);
        });
    }

    [Fact]
    public void CategoryEditDialog_CanBeConstructed_WithoutXamlExceptions()
    {
        _fixture.Run(() =>
        {
            var vm = new CategoryEditViewModel(null);
            var dialog = new CategoryEditDialog(vm);
            Assert.NotNull(dialog);
        });
    }

    [Fact]
    public void SoftwareEditDialog_CanBeConstructed_WithoutXamlExceptions()
    {
        _fixture.Run(() =>
        {
            using var env = new TestEnvironment();
            var provider = CreateServiceProvider(env);
            var iconService = provider.GetRequiredService<IIconService>();
            var scannerService = provider.GetRequiredService<IFileScannerService>();
            var softwareRepo = provider.GetRequiredService<ISoftwareRepository>();
            var categoryRepo = provider.GetRequiredService<ICategoryRepository>();

            var vm = new SoftwareEditViewModel(new Software { Name = "Test" }, false, iconService, scannerService, softwareRepo, categoryRepo);
            var dialog = new SoftwareEditDialog(vm);
            Assert.NotNull(dialog);
        });
    }

    [Fact]
    public void VerifySymbolsExist()
    {
        Assert.True(Enum.IsDefined(typeof(Wpf.Ui.Controls.SymbolRegular), "Database24"));
        Assert.True(Enum.IsDefined(typeof(Wpf.Ui.Controls.SymbolRegular), "Search24"));
        Assert.True(Enum.IsDefined(typeof(Wpf.Ui.Controls.SymbolRegular), "FolderOpen24"));
        Assert.True(Enum.IsDefined(typeof(Wpf.Ui.Controls.SymbolRegular), "Save24"));
        Assert.True(Enum.IsDefined(typeof(Wpf.Ui.Controls.SymbolRegular), "ArrowSync24"));
        Assert.True(Enum.IsDefined(typeof(Wpf.Ui.Controls.SymbolRegular), "Apps24"));
        Assert.True(Enum.IsDefined(typeof(Wpf.Ui.Controls.SymbolRegular), "Grid24"));
        Assert.True(Enum.IsDefined(typeof(Wpf.Ui.Controls.SymbolRegular), "MoreVertical24"));
        Assert.True(Enum.IsDefined(typeof(Wpf.Ui.Controls.SymbolRegular), "TextBulletListSquare24"));
        Assert.True(Enum.IsDefined(typeof(Wpf.Ui.Controls.SymbolRegular), "Copy24"));
        Assert.True(Enum.IsDefined(typeof(Wpf.Ui.Controls.SymbolRegular), "Delete24"));
    }

    [Fact]
    public void ThemeService_UpdateDynamicThemeColors_SetsBothColorsAndBrushes()
    {
        _fixture.Run(() =>
        {
            ThemeService.UpdateDynamicThemeColors(true);
            Assert.True(Application.Current.Resources.Contains("AppBackground"));
            Assert.True(Application.Current.Resources.Contains("BrushBackground"));
            Assert.IsType<System.Windows.Media.Color>(Application.Current.Resources["AppBackground"]);
            Assert.IsType<System.Windows.Media.SolidColorBrush>(Application.Current.Resources["BrushBackground"]);

            ThemeService.UpdateDynamicThemeColors(false);
            Assert.True(Application.Current.Resources.Contains("AppBackground"));
            Assert.True(Application.Current.Resources.Contains("BrushBackground"));
        });
    }

    [Fact]
    public void MainViewModel_ToggleSidebar_TogglesState()
    {
        _fixture.Run(() =>
        {
            using var env = new TestEnvironment();
            var provider = CreateServiceProvider(env);
            var vm = provider.GetRequiredService<MainViewModel>();

            Assert.False(vm.IsSidebarCollapsed);
            vm.ToggleSidebarCommand.Execute(null);
            Assert.True(vm.IsSidebarCollapsed);
            vm.ToggleSidebarCommand.Execute(null);
            Assert.False(vm.IsSidebarCollapsed);
        });
    }

    [Fact]
    public void CategoryNavModel_HomeMode_HasCorrectGlyphAndSymbol()
    {
        var home = new CategoryNavModel { Name = "首页", Icon = "Home", NavMode = "Home", Count = 5 };
        Assert.Equal("Home", home.NavMode);
        Assert.Equal("首页", home.Name);
        Assert.Equal(5, home.Count);
        Assert.Equal(Wpf.Ui.Controls.SymbolRegular.Home24, home.Symbol);
        Assert.Equal("\uE80F", home.Glyph);
    }

    private class FakeHotkeyService : IHotkeyService
    {
        public event EventHandler? HotkeyPressed { add {} remove {} }
        public bool IsRegistered => false;
        public string CurrentHotkey => "Ctrl+Alt+Space";
        public bool Register(string hotkeyString, IntPtr windowHandle) => true;
        public void Unregister(IntPtr windowHandle) {}
        public bool TestHotkeyAvailable(string hotkeyString) => true;
        public void Dispose() {}
    }
}
