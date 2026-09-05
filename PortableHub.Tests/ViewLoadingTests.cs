using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
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
    public static Exception? LastUnhandledException;

    public StaTestFixture()
    {
        _thread = new Thread(() =>
        {
            if (Application.Current == null)
            {
                var app = new Application();
                app.Resources.MergedDictionaries.Add(new iNKORE.UI.WPF.Modern.ThemeResources());
                app.Resources.MergedDictionaries.Add(new iNKORE.UI.WPF.Modern.Controls.XamlControlsResources());
                try
                {
                    app.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri("pack://application:,,,/PortableHub.App;component/Themes/Colors.xaml", UriKind.Absolute) });
                    app.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri("pack://application:,,,/PortableHub.App;component/Themes/Brushes.xaml", UriKind.Absolute) });
                    app.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri("pack://application:,,,/PortableHub.App;component/Themes/Typography.xaml", UriKind.Absolute) });
                    app.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri("pack://application:,,,/PortableHub.App;component/Themes/Spacing.xaml", UriKind.Absolute) });
                    app.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri("pack://application:,,,/PortableHub.App;component/Themes/IconStyles.xaml", UriKind.Absolute) });
                    app.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri("pack://application:,,,/PortableHub.App;component/Themes/ControlStyles.xaml", UriKind.Absolute) });
                    app.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri("pack://application:,,,/PortableHub.App;component/Themes/CardStyles.xaml", UriKind.Absolute) });
                    app.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri("pack://application:,,,/PortableHub.App;component/Themes/NavigationStyles.xaml", UriKind.Absolute) });
                    app.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri("pack://application:,,,/PortableHub.App;component/Themes/DialogStyles.xaml", UriKind.Absolute) });
                    app.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri("pack://application:,,,/PortableHub.App;component/Themes/Controls.xaml", UriKind.Absolute) });
                    app.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri("pack://application:,,,/PortableHub.App;component/Themes/Localization/Strings.zh-CN.xaml", UriKind.Absolute) });
                }
                catch
                {
                    // If pack URI cannot be resolved in test runner domain
                }
                app.DispatcherUnhandledException += (s, args) =>
                {
                    LastUnhandledException = args.Exception;
                    args.Handled = true;
                };
                app.Resources.Add("ComparisonConverter", new ComparisonConverter());
                app.Resources.Add("StringToImageSourceConverter", new StringToImageSourceConverter());
                app.Resources.Add("CardSizeToDimensionConverter", new CardSizeToDimensionConverter());
                app.Resources.Add("ColorHexToBrushConverter", new ColorHexToBrushConverter());
                app.Resources.Add("InverseBooleanConverter", new InverseBooleanConverter());
                app.Resources.Add("InverseBooleanToVisibilityConverter", new InverseBooleanToVisibilityConverter());
                app.Resources.Add("NullToVisibilityConverter", new NullToVisibilityConverter());
                app.Resources.Add("BooleanToVisibilityConverter", new BooleanToVisibilityConverter());
                app.Resources.Add("ComparisonToVisibilityConverter", new ComparisonToVisibilityConverter());
                ThemeService.UpdateDynamicThemeColors(true);
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

    public async Task RunAsync(Action action)
    {
        await _dispatcher!.InvokeAsync(action);
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
    public async Task SettingsWindow_TabsAndControls_CanBeLoadedAndSwitched()
    {
        using var env = new TestEnvironment();
        await env.InitializeAsync();
        var provider = CreateServiceProvider(env);
        var vm = provider.GetRequiredService<SettingsViewModel>();
        await vm.InitializeAsync();

        await _fixture.RunAsync(() =>
        {
            var window = new SettingsWindow(vm);
            try
            {
                window.Show();
                window.Measure(new Size(860, 700));
                window.Arrange(new Rect(0, 0, 860, 700));
                window.UpdateLayout();

                var tabControl = FindVisualChild<TabControl>(window);
                Assert.NotNull(tabControl);
                Assert.Equal(6, tabControl.Items.Count);

                var navListBox = window.FindName("SettingsNavListBox") as ListBox;
                Assert.NotNull(navListBox);
                Assert.Equal(6, navListBox.Items.Count);

                for (int t = 0; t < 6; t++)
                {
                    navListBox.SelectedIndex = t;
                    window.UpdateLayout();
                    Assert.Equal(t, tabControl.SelectedIndex);
                    Assert.NotNull(navListBox.SelectedItem);
                }
            }
            finally
            {
                window.Close();
            }
        });
    }

    [Fact]
    public async Task SettingsViewModel_ThemeRollbackOnCancel()
    {
        using var env = new TestEnvironment();
        await env.InitializeAsync();
        var provider = CreateServiceProvider(env);
        var vm = provider.GetRequiredService<SettingsViewModel>();
        var themeService = provider.GetRequiredService<ThemeService>();

        await _fixture.RunAsync(async () =>
        {
            await vm.InitializeAsync();
            var original = vm.Theme;

            // Change theme
            var target = original == "Dark" ? "Light" : "Dark";
            vm.Theme = target;
            Assert.Equal(target, themeService.CurrentTheme);

            // Cancel
            vm.CancelCommand.Execute(null);
            Assert.Equal(original, themeService.CurrentTheme);
        });
    }

    [Fact]
    public async Task SettingsWindow_ClosingViaWindowClose_DoesNotThrow_AndRollsBackTheme()
    {
        using var env = new TestEnvironment();
        await env.InitializeAsync();
        var provider = CreateServiceProvider(env);
        var vm = provider.GetRequiredService<SettingsViewModel>();
        var themeService = provider.GetRequiredService<ThemeService>();

        await _fixture.RunAsync(async () =>
        {
            await vm.InitializeAsync();
            var original = vm.Theme;
            var window = new SettingsWindow(vm);
            window.Show();

            // Simulate changing theme while window is open
            var target = original == "Dark" ? "Light" : "Dark";
            vm.Theme = target;
            Assert.Equal(target, themeService.CurrentTheme);

            // Directly close the window (simulating title bar [X] or Alt+F4)
            var ex = Record.Exception(() => window.Close());
            Assert.Null(ex);

            // Verify theme rolled back to original
            Assert.Equal(original, themeService.CurrentTheme);
        });
    }

    [Fact]
    public async Task SettingsWindow_ClosingViaCancelCommand_DoesNotThrow_AndRollsBackTheme()
    {
        using var env = new TestEnvironment();
        await env.InitializeAsync();
        var provider = CreateServiceProvider(env);
        var vm = provider.GetRequiredService<SettingsViewModel>();
        var themeService = provider.GetRequiredService<ThemeService>();

        await _fixture.RunAsync(async () =>
        {
            await vm.InitializeAsync();
            var original = vm.Theme;
            var window = new SettingsWindow(vm);
            window.Show();

            // Simulate changing theme
            var target = original == "Dark" ? "Light" : "Dark";
            vm.Theme = target;
            Assert.Equal(target, themeService.CurrentTheme);

            // Execute CancelCommand (simulating clicking "取消" button or pressing ESC)
            var ex = Record.Exception(() => vm.CancelCommand.Execute(null));
            Assert.Null(ex);

            // Verify theme rolled back
            Assert.Equal(original, themeService.CurrentTheme);
        });
    }

    [Fact]
    public async Task SettingsWindow_ClosingViaSaveAndClose_DoesNotThrow_AndKeepsTheme()
    {
        using var env = new TestEnvironment();
        await env.InitializeAsync();
        var provider = CreateServiceProvider(env);
        var vm = provider.GetRequiredService<SettingsViewModel>();
        var themeService = provider.GetRequiredService<ThemeService>();

        await _fixture.RunAsync(async () =>
        {
            await vm.InitializeAsync();
            var original = vm.Theme;
            var window = new SettingsWindow(vm);
            window.Show();

            // Simulate changing theme
            var target = original == "Dark" ? "Light" : "Dark";
            vm.Theme = target;
            Assert.Equal(target, themeService.CurrentTheme);

            // Execute SaveAndCloseCommand (simulating clicking "保存" button)
            var ex = await Record.ExceptionAsync(async () => await vm.SaveAndCloseAsync());
            Assert.Null(ex);

            // Verify theme kept as target
            Assert.Equal(target, themeService.CurrentTheme);
        });
    }

    [Fact]
    public async Task SettingsViewModel_SaveAppliesAndPersists()
    {
        using var env = new TestEnvironment();
        await env.InitializeAsync();
        var provider = CreateServiceProvider(env);
        var vm = provider.GetRequiredService<SettingsViewModel>();
        var settingsService = provider.GetRequiredService<ISettingsService>();

        await _fixture.RunAsync(async () =>
        {
            await vm.InitializeAsync();
            vm.CardSize = "Large";
            vm.ViewMode = "List";
            vm.LaunchClickMode = "SingleClick";

            await vm.SaveAndCloseAsync();

            Assert.Equal("Large", settingsService.CurrentSettings.CardSize);
            Assert.Equal("List", settingsService.CurrentSettings.ViewMode);
            Assert.Equal("SingleClick", settingsService.CurrentSettings.LaunchClickMode);
        });
    }

    [Fact]
    public async Task SettingsViewModel_HotkeyValidation()
    {
        using var env = new TestEnvironment();
        await env.InitializeAsync();
        var provider = CreateServiceProvider(env);
        var vm = provider.GetRequiredService<SettingsViewModel>();

        await _fixture.RunAsync(async () =>
        {
            await vm.InitializeAsync();

            vm.GlobalHotkey = "Ctrl+Alt+Space";
            Assert.True(vm.IsHotkeyValid);
            Assert.Contains("✓", vm.HotkeyStatus);

            vm.GlobalHotkey = "";
            Assert.False(vm.IsHotkeyValid);
            Assert.Contains("⚠", vm.HotkeyStatus);

            vm.GlobalHotkey = "鼠标中键";
            Assert.True(vm.IsHotkeyValid);
            Assert.Contains("ℹ", vm.HotkeyStatus);
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
            var titleBar = window.FindName("AppTitleBar") as iNKORE.UI.WPF.Modern.Controls.Primitives.TitleBarControl;
            Assert.NotNull(titleBar);
            Assert.True(titleBar.IsIconVisible);
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
        Assert.True(Enum.IsDefined(typeof(iNKORE.UI.WPF.Modern.Controls.Symbol), "Library"));
        Assert.True(Enum.IsDefined(typeof(iNKORE.UI.WPF.Modern.Controls.Symbol), "Find"));
        Assert.True(Enum.IsDefined(typeof(iNKORE.UI.WPF.Modern.Controls.Symbol), "Folder"));
        Assert.True(Enum.IsDefined(typeof(iNKORE.UI.WPF.Modern.Controls.Symbol), "SaveLocal"));
        Assert.True(Enum.IsDefined(typeof(iNKORE.UI.WPF.Modern.Controls.Symbol), "Sync"));
        Assert.True(Enum.IsDefined(typeof(iNKORE.UI.WPF.Modern.Controls.Symbol), "AllApps"));
        Assert.True(Enum.IsDefined(typeof(iNKORE.UI.WPF.Modern.Controls.Symbol), "ViewAll"));
        Assert.True(Enum.IsDefined(typeof(iNKORE.UI.WPF.Modern.Controls.Symbol), "More"));
        Assert.True(Enum.IsDefined(typeof(iNKORE.UI.WPF.Modern.Controls.Symbol), "List"));
        Assert.True(Enum.IsDefined(typeof(iNKORE.UI.WPF.Modern.Controls.Symbol), "Copy"));
        Assert.True(Enum.IsDefined(typeof(iNKORE.UI.WPF.Modern.Controls.Symbol), "Delete"));
    }

    [Fact]
    public void ThemeService_UpdateDynamicThemeColors_SetsBothColorsAndBrushes()
    {
        _fixture.Run(() =>
        {
            ThemeService.UpdateDynamicThemeColors(true);
            Assert.True(Application.Current.Resources.Contains("AppBackground"));
            Assert.True(Application.Current.Resources.Contains("BrushBackground"));
            Assert.True(Application.Current.Resources.Contains("AppFavorite"));
            Assert.True(Application.Current.Resources.Contains("BrushFavorite"));
            Assert.IsType<System.Windows.Media.Color>(Application.Current.Resources["AppBackground"]);
            Assert.IsType<System.Windows.Media.SolidColorBrush>(Application.Current.Resources["BrushBackground"]);
            Assert.IsType<System.Windows.Media.SolidColorBrush>(Application.Current.Resources["BrushFavorite"]);

            ThemeService.UpdateDynamicThemeColors(false);
            Assert.True(Application.Current.Resources.Contains("AppBackground"));
            Assert.True(Application.Current.Resources.Contains("BrushBackground"));
            Assert.True(Application.Current.Resources.Contains("AppFavorite"));
            Assert.True(Application.Current.Resources.Contains("BrushFavorite"));
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
        Assert.Equal(iNKORE.UI.WPF.Modern.Controls.Symbol.Home, home.Symbol);
        Assert.Equal("\uE80F", home.Glyph);
    }

    private static string GetAppDirectory()
    {
        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
        var appDir = Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "..", "PortableHub.App"));
        if (!Directory.Exists(appDir))
        {
            appDir = Path.GetFullPath(Path.Combine(baseDir, "..", "..", "PortableHub.App"));
        }
        return appDir;
    }

    [Fact]
    public void Verify_NoFractionalFontSizes_AcrossAllXamlFiles()
    {
        var appDir = GetAppDirectory();
        Assert.True(Directory.Exists(appDir), $"PortableHub.App directory not found at {appDir}");
        var xamlFiles = Directory.GetFiles(appDir, "*.xaml", SearchOption.AllDirectories);
        Assert.NotEmpty(xamlFiles);

        var regex = new Regex(@"FontSize\s*=\s*""\d+\.\d+""", RegexOptions.IgnoreCase);
        foreach (var file in xamlFiles)
        {
            var content = File.ReadAllText(file);
            var match = regex.Match(content);
            Assert.False(match.Success, $"Fractional font size found in {Path.GetFileName(file)}: '{match.Value}'");
        }
    }

    [Fact]
    public void Verify_NoForcedClearType_AcrossAllXamlFiles()
    {
        var appDir = GetAppDirectory();
        var xamlFiles = Directory.GetFiles(appDir, "*.xaml", SearchOption.AllDirectories);
        Assert.NotEmpty(xamlFiles);

        foreach (var file in xamlFiles)
        {
            var content = File.ReadAllText(file);
            Assert.DoesNotContain("ClearType", content, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void Verify_DesignTokensAndTypography_Integrity()
    {
        var appDir = GetAppDirectory();
        var colorsXaml = File.ReadAllText(Path.Combine(appDir, "Themes", "Colors.xaml"));
        var typoXaml = File.ReadAllText(Path.Combine(appDir, "Themes", "Typography.xaml"));
        var brushesXaml = File.ReadAllText(Path.Combine(appDir, "Themes", "Brushes.xaml"));

        // Colors tokens
        Assert.Contains("ControlHeightSmall", colorsXaml);
        Assert.Contains("ControlHeightDefault", colorsXaml);
        Assert.Contains("ControlHeightLarge", colorsXaml);
        Assert.Contains("IconSizeSmall", colorsXaml);
        Assert.Contains("IconSizeDefault", colorsXaml);
        Assert.Contains("IconSizeLarge", colorsXaml);
        Assert.Contains("CornerRadiusWindow", colorsXaml);
        Assert.Contains("CornerRadiusDialog", colorsXaml);
        Assert.Contains("CornerRadiusCard", colorsXaml);
        Assert.Contains("CornerRadiusSearchBox", colorsXaml);
        Assert.Contains("CornerRadiusButton", colorsXaml);
        Assert.Contains("CornerRadiusIconButton", colorsXaml);
        Assert.Contains("CornerRadiusComboBox", colorsXaml);
        Assert.Contains("CornerRadiusBadge", colorsXaml);
        Assert.Contains("CornerRadiusContextMenu", colorsXaml);
        Assert.Contains("AppFavorite", colorsXaml);

        // Brushes tokens
        Assert.Contains("BrushFavorite", brushesXaml);

        // Typography scale
        Assert.Contains("AppFontFamily", typoXaml);
        Assert.Contains("FontSizeDisplay", typoXaml);
        Assert.Contains("FontSizeHeading", typoXaml);
        Assert.Contains("FontSizeDialogTitle", typoXaml);
        Assert.Contains("FontSizeSection", typoXaml);
        Assert.Contains("FontSizeBodyStrong", typoXaml);
        Assert.Contains("FontSizeBody", typoXaml);
        Assert.Contains("FontSizeSecondary", typoXaml);
        Assert.Contains("FontSizeCaption", typoXaml);
        Assert.Contains("FontSizeTiny", typoXaml);
    }

    [Fact]
    public void Verify_DialogMetrics_WindowCornerPreference()
    {
        var appDir = GetAppDirectory();
        var dialogFiles = new[]
        {
            "CategoryEditDialog.xaml",
            "SoftwareEditDialog.xaml",
            "ScannerDialog.xaml",
            "SettingsWindow.xaml",
            "MainWindow.xaml"
        };

        foreach (var name in dialogFiles)
        {
            var content = File.ReadAllText(Path.Combine(appDir, "Views", name));
            Assert.Contains("ui:WindowHelper.UseModernWindowStyle=\"True\"", content);
            Assert.Contains("UseLayoutRounding=\"True\"", content);
            Assert.Contains("SnapsToDevicePixels=\"True\"", content);
        }

        // Dialog TitleBars have Height 44 and FontSize 18
        var dialogTitles = new[] { "CategoryEditDialog.xaml", "SoftwareEditDialog.xaml", "ScannerDialog.xaml" };
        foreach (var name in dialogTitles)
        {
            var content = File.ReadAllText(Path.Combine(appDir, "Views", name));
            Assert.Contains("Height=\"44\"", content);
            Assert.Contains("FontSize=\"18\"", content);
            Assert.Contains("FontWeight=\"SemiBold\"", content);
        }
    }

    [Fact]
    public void Verify_PrimaryButtons_HaveWhiteTextAndBrushes()
    {
        _fixture.Run(() =>
        {
            // Verify ThemeService provides pure white TextOnAccent in both light and dark modes
            ThemeService.UpdateDynamicThemeColors(true);
            Assert.True(Application.Current.Resources.Contains("TextOnAccentFillColorPrimaryBrush"));
            var darkBrush = Application.Current.Resources["TextOnAccentFillColorPrimaryBrush"] as System.Windows.Media.SolidColorBrush;
            Assert.NotNull(darkBrush);
            Assert.Equal(System.Windows.Media.Colors.White, darkBrush.Color);

            ThemeService.UpdateDynamicThemeColors(false);
            Assert.True(Application.Current.Resources.Contains("TextOnAccentFillColorPrimaryBrush"));
            var lightBrush = Application.Current.Resources["TextOnAccentFillColorPrimaryBrush"] as System.Windows.Media.SolidColorBrush;
            Assert.NotNull(lightBrush);
            Assert.Equal(System.Windows.Media.Colors.White, lightBrush.Color);
        });

        // Verify XAML views declare white foreground on Primary buttons
        var appDir = GetAppDirectory();
        var mainWindowXaml = File.ReadAllText(Path.Combine(appDir, "Views", "MainWindow.xaml"));
        var categoryDialogXaml = File.ReadAllText(Path.Combine(appDir, "Views", "CategoryEditDialog.xaml"));
        var softwareDialogXaml = File.ReadAllText(Path.Combine(appDir, "Views", "SoftwareEditDialog.xaml"));
        var scannerDialogXaml = File.ReadAllText(Path.Combine(appDir, "Views", "ScannerDialog.xaml"));
        var settingsXaml = File.ReadAllText(Path.Combine(appDir, "Views", "SettingsWindow.xaml"));
        var typoXaml = File.ReadAllText(Path.Combine(appDir, "Themes", "Typography.xaml"));
        var controlsXaml = File.ReadAllText(Path.Combine(appDir, "Themes", "Controls.xaml"));

        Assert.Contains("Foreground=\"#FFFFFF\"", mainWindowXaml);
        Assert.Contains("Foreground=\"#FFFFFF\"", categoryDialogXaml);
        Assert.Contains("Foreground=\"#FFFFFF\"", softwareDialogXaml);
        Assert.Contains("Foreground=\"#FFFFFF\"", scannerDialogXaml);
        Assert.Contains("Foreground=\"#FFFFFF\"", settingsXaml);

        // Verify Typography.xaml does not pollute default TextBlock style with hardcoded dark foreground
        Assert.DoesNotContain("<Setter Property=\"Foreground\" Value=\"{DynamicResource TextFillColorPrimaryBrush}\" />\r\n        <Setter Property=\"FontFamily\"", typoXaml);
        Assert.DoesNotContain("<Setter Property=\"Foreground\" Value=\"{DynamicResource TextFillColorPrimaryBrush}\" />\n        <Setter Property=\"FontFamily\"", typoXaml);

        // Verify Controls.xaml ensures child TextBlock gets white foreground in PrimaryButton and ui:Button
        Assert.Contains("<Setter Property=\"Foreground\" Value=\"#FFFFFF\" />", controlsXaml);
        Assert.Contains("<Setter Property=\"FontWeight\" Value=\"SemiBold\" />", controlsXaml);
    }

    [Fact]
    public void Verify_SidebarCollapse_StructureAndNoScrollbar()
    {
        var appDir = GetAppDirectory();
        var mainWindowXaml = File.ReadAllText(Path.Combine(appDir, "Views", "MainWindow.xaml"));

        // Verify SidebarBorder has ClipToBounds="True" to prevent overflow bleeding during animation
        Assert.Contains("x:Name=\"SidebarBorder\"", mainWindowXaml);
        Assert.Contains("ClipToBounds=\"True\"", mainWindowXaml);

        // Verify Header toggle button is placed in Column 0 (40px fixed slot) so it never jumps horizontally
        Assert.Contains("<Button Grid.Column=\"0\" Style=\"{StaticResource SubtleIconButton}\" Command=\"{Binding ToggleSidebarCommand}\"", mainWindowXaml);

        // Verify ScrollViewer hides scrollbar when collapsed
        Assert.Contains("DataTrigger Binding=\"{Binding IsSidebarCollapsed}\" Value=\"True\"", mainWindowXaml);
        Assert.Contains("Setter Property=\"VerticalScrollBarVisibility\" Value=\"Hidden\"", mainWindowXaml);

        // Verify nested ListBoxes strip their internal ScrollViewer via custom ControlTemplate
        Assert.Contains("<ControlTemplate TargetType=\"ListBox\">", mainWindowXaml);
        Assert.Contains("<ItemsPresenter />", mainWindowXaml);

        // Verify centered 40px icon columns for sidebar items
        Assert.Contains("<ColumnDefinition Width=\"40\" />", mainWindowXaml);
        Assert.Contains("HorizontalAlignment=\"Center\" VerticalAlignment=\"Center\"", mainWindowXaml);
    }

    [Fact]
    public async Task MainWindow_Sidebar_GeometryAndLayoutVerification()
    {
        using var env = new TestEnvironment();
        await env.InitializeAsync();
        var provider = CreateServiceProvider(env);
        var vm = provider.GetRequiredService<MainViewModel>();
        var settingsService = provider.GetRequiredService<ISettingsService>();

        // Initialize ViewModel directly
        await vm.InitializeAsync();

        await _fixture.RunAsync(() =>
        {
            var window = new MainWindow(vm, settingsService);
            Assert.NotNull(window);

            window.Measure(new Size(1200, 800));
            window.Arrange(new Rect(0, 0, 1200, 800));
            window.UpdateLayout();

            // Locate SidebarBorder
            var sidebar = window.FindName("SidebarBorder") as System.Windows.Controls.Border;
            Assert.NotNull(sidebar);
            Assert.True(sidebar.ClipToBounds);
            Assert.Equal(224.0, sidebar.Width);

            // Verify toggle command
            vm.ToggleSidebarCommand.Execute(null);
            Assert.True(vm.IsSidebarCollapsed);

            // Locate NavItemsListBox and CategoriesListBox
            var navListBox = window.FindName("NavItemsListBox") as System.Windows.Controls.ListBox;
            Assert.NotNull(navListBox);
            var catListBox = window.FindName("CategoriesListBox") as System.Windows.Controls.ListBox;
            Assert.NotNull(catListBox);

            // Initially, vm.SelectedNav is Home (in navItems)
            Assert.Equal(vm.NavItems[0], vm.SelectedNav);
            Assert.True(vm.NavItems[0].IsSelected);
            Assert.Equal(vm.NavItems[0], navListBox.SelectedItem);
            Assert.Null(catListBox.SelectedItem);

            // Now select a custom category via VM
            if (vm.CustomCategories.Count > 0)
            {
                var targetCat = vm.CustomCategories[0];
                vm.SelectedNav = targetCat;

                // Assert mutual exclusivity and IsSelected synchronization
                Assert.Equal(targetCat, vm.SelectedNav);
                Assert.True(targetCat.IsSelected);
                Assert.False(vm.NavItems[0].IsSelected);
                Assert.Equal(targetCat, catListBox.SelectedItem);
                Assert.Null(navListBox.SelectedItem);

                // Now select back to a NavItem via navListBox
                navListBox.SelectedItem = vm.NavItems[1]; // 全部软件
                Assert.Equal(vm.NavItems[1], vm.SelectedNav);
                Assert.True(vm.NavItems[1].IsSelected);
                Assert.False(targetCat.IsSelected);
                Assert.Equal(vm.NavItems[1], navListBox.SelectedItem);
                Assert.Null(catListBox.SelectedItem);
            }
        });
    }

    [Fact]
    public void Verify_FluentDesignTokens_SpacingAndIcons()
    {
        var appDir = GetAppDirectory();
        var spacingXaml = File.ReadAllText(Path.Combine(appDir, "Themes", "Spacing.xaml"));
        var iconStylesXaml = File.ReadAllText(Path.Combine(appDir, "Themes", "IconStyles.xaml"));
        var controlStylesXaml = File.ReadAllText(Path.Combine(appDir, "Themes", "ControlStyles.xaml"));

        // Spacing system tokens (Multiples of 4)
        Assert.Contains("x:Key=\"SpaceXS\">4<", spacingXaml);
        Assert.Contains("x:Key=\"SpaceSM\">8<", spacingXaml);
        Assert.Contains("x:Key=\"SpaceMD\">12<", spacingXaml);
        Assert.Contains("x:Key=\"SpaceLG\">16<", spacingXaml);
        Assert.Contains("x:Key=\"SpaceXL\">24<", spacingXaml);
        Assert.Contains("x:Key=\"Space2XL\">32<", spacingXaml);
        Assert.Contains("x:Key=\"Space3XL\">40<", spacingXaml);

        // Icon tokens and button styles
        Assert.Contains("x:Key=\"IconSizeDefault\">20<", iconStylesXaml);
        Assert.Contains("x:Key=\"IconSizeLarge\">24<", iconStylesXaml);
        Assert.Contains("x:Key=\"IconButton\"", iconStylesXaml);
        Assert.Contains("x:Key=\"IconButtonSmall\"", iconStylesXaml);

        // Control styles
        Assert.Contains("x:Key=\"ModernButton\"", controlStylesXaml);
        Assert.Contains("x:Key=\"PrimaryButton\"", controlStylesXaml);
        Assert.Contains("x:Key=\"ModernTextBox\"", controlStylesXaml);

        // Obsolete duplicate Styles.xaml must not exist
        Assert.False(File.Exists(Path.Combine(appDir, "Themes", "Styles.xaml")));
    }

    [Fact]
    public void Verify_TypographyScale_NoTextBelow12px()
    {
        var appDir = GetAppDirectory();
        var views = Directory.GetFiles(Path.Combine(appDir, "Views"), "*.xaml");
        foreach (var viewFile in views)
        {
            var content = File.ReadAllText(viewFile);
            Assert.DoesNotContain("FontSize=\"11\"", content);
            Assert.DoesNotContain("FontSize=\"10\"", content);
        }

        var typoXaml = File.ReadAllText(Path.Combine(appDir, "Themes", "Typography.xaml"));
        Assert.Contains("Segoe UI Variable", typoXaml);
    }

    [Fact]
    public void Verify_ToolbarMetrics_And_ComboBox()
    {
        var appDir = GetAppDirectory();
        var mainWindowXaml = File.ReadAllText(Path.Combine(appDir, "Views", "MainWindow.xaml"));
        var colorsXaml = File.ReadAllText(Path.Combine(appDir, "Themes", "Colors.xaml"));

        // SearchBox Height 40 and ComboBox MinWidth 160 Width Auto Height 40
        Assert.Contains("x:Name=\"TopSearchBox\"", mainWindowXaml);
        Assert.Contains("Height=\"40\"", mainWindowXaml);
        Assert.Contains("MinWidth=\"160\"", mainWindowXaml);
        Assert.Contains("Width=\"Auto\"", mainWindowXaml);

        // Card CornerRadius >= 10
        Assert.Contains("CornerRadiusCard\">10<", colorsXaml);
    }

    [Fact]
    public void Verify_StringToImageSourceConverter_IconCache()
    {
        _fixture.Run(() =>
        {
            var appDir = GetAppDirectory();
            var iconPath = Path.Combine(appDir, "Assets", "app.ico");
            if (File.Exists(iconPath))
            {
                var converter = new StringToImageSourceConverter();
                var result1 = converter.Convert(iconPath, typeof(System.Windows.Media.ImageSource), null, System.Globalization.CultureInfo.InvariantCulture);
                var result2 = converter.Convert(iconPath, typeof(System.Windows.Media.ImageSource), null, System.Globalization.CultureInfo.InvariantCulture);

                Assert.NotNull(result1);
                Assert.Same(result1, result2); // Must be identical cached instance
            }
        });
    }

    [Fact]
    public void Verify_ThemeService_TokenAliases()
    {
        _fixture.Run(() =>
        {
            ThemeService.UpdateDynamicThemeColors(true);
            var res = Application.Current.Resources;
            Assert.True(res.Contains("AppSurfaceBrush"));
            Assert.True(res.Contains("AppPrimaryTextBrush"));
            Assert.True(res.Contains("AppBackgroundBrush"));
            Assert.True(res.Contains("AppBorderBrush"));
        });
    }

    [Fact]
    public void Verify_SymbolHelper_Resolves_LegacyAndModernNames()
    {
        // Legacy WPF-UI names mapping
        Assert.Equal(iNKORE.UI.WPF.Modern.Controls.Symbol.AllApps, PortableHub.App.Helpers.SymbolHelper.ResolveSymbol("Apps24"));
        Assert.Equal(iNKORE.UI.WPF.Modern.Controls.Symbol.Setting, PortableHub.App.Helpers.SymbolHelper.ResolveSymbol("Settings24"));
        Assert.Equal(iNKORE.UI.WPF.Modern.Controls.Symbol.Favorite, PortableHub.App.Helpers.SymbolHelper.ResolveSymbol("Star24"));
        Assert.Equal(iNKORE.UI.WPF.Modern.Controls.Symbol.Upload, PortableHub.App.Helpers.SymbolHelper.ResolveSymbol("Cloud24"));
        Assert.Equal(iNKORE.UI.WPF.Modern.Controls.Symbol.Clock, PortableHub.App.Helpers.SymbolHelper.ResolveSymbol("History24"));
        Assert.Equal(iNKORE.UI.WPF.Modern.Controls.Symbol.Repair, PortableHub.App.Helpers.SymbolHelper.ResolveSymbol("Code24"));
        Assert.Equal(iNKORE.UI.WPF.Modern.Controls.Symbol.Permissions, PortableHub.App.Helpers.SymbolHelper.ResolveSymbol("Shield24"));
        Assert.Equal(iNKORE.UI.WPF.Modern.Controls.Symbol.FontColor, PortableHub.App.Helpers.SymbolHelper.ResolveSymbol("Color"));
        Assert.Equal(iNKORE.UI.WPF.Modern.Controls.Symbol.FontColor, PortableHub.App.Helpers.SymbolHelper.ResolveSymbol("Color24"));

        // Modern names and case-insensitivity
        Assert.Equal(iNKORE.UI.WPF.Modern.Controls.Symbol.Folder, PortableHub.App.Helpers.SymbolHelper.ResolveSymbol("folder"));
        Assert.Equal(iNKORE.UI.WPF.Modern.Controls.Symbol.Play, PortableHub.App.Helpers.SymbolHelper.ResolveSymbol("PLAY"));

        // Null, empty, and unknown fallback
        Assert.Equal(iNKORE.UI.WPF.Modern.Controls.Symbol.Folder, PortableHub.App.Helpers.SymbolHelper.ResolveSymbol(null));
        Assert.Equal(iNKORE.UI.WPF.Modern.Controls.Symbol.Folder, PortableHub.App.Helpers.SymbolHelper.ResolveSymbol("   "));
        Assert.Equal(iNKORE.UI.WPF.Modern.Controls.Symbol.Setting, PortableHub.App.Helpers.SymbolHelper.ResolveSymbol("NonExistent_XYZ", iNKORE.UI.WPF.Modern.Controls.Symbol.Setting));
    }

    [Fact]
    public void Verify_Dialogs_TitleBar_And_FooterButtonOrder()
    {
        var appDir = GetAppDirectory();
        var dialogs = new[] { "CategoryEditDialog.xaml", "SettingsWindow.xaml", "SoftwareEditDialog.xaml", "ScannerDialog.xaml" };

        foreach (var dlg in dialogs)
        {
            var content = File.ReadAllText(Path.Combine(appDir, "Views", dlg));

            // TitleBar FontSize="20" and IsIconVisible="True"
            Assert.Contains("FontSize=\"20\"", content);
            Assert.Contains("IsIconVisible=\"True\"", content);

            // Footer uses DialogFooterBarStyle
            Assert.Contains("StaticResource DialogFooterBarStyle", content);

            // In the footer, Cancel appears before Save / Add button (Secondary then Primary)
            var footerIdx = content.IndexOf("DialogFooterBarStyle", StringComparison.Ordinal);
            Assert.True(footerIdx > 0, $"{dlg} must have DialogFooterBarStyle");

            var footerSection = content.Substring(footerIdx);
            var cancelIdx = footerSection.IndexOf("Loc_Btn_Cancel", StringComparison.Ordinal);
            if (cancelIdx < 0)
            {
                cancelIdx = footerSection.IndexOf("取消", StringComparison.Ordinal);
            }
            Assert.True(cancelIdx > 0, $"{dlg} footer must contain 取消 / Loc_Btn_Cancel button");

            var saveOrAddIdx = footerSection.IndexOf("PrimaryButton", StringComparison.Ordinal);
            Assert.True(saveOrAddIdx > 0, $"{dlg} footer must contain PrimaryButton");

            Assert.True(cancelIdx < saveOrAddIdx, $"{dlg} footer must place Cancel before Primary action button");
        }
    }

    [Fact]
    public void Verify_CardStyles_And_NavigationStyles_Resources()
    {
        _fixture.Run(() =>
        {
            var res = Application.Current.Resources;
            Assert.True(res.Contains("ModernCard"), "Resources must contain ModernCard");
            Assert.True(res.Contains("ModernElevatedCard"), "Resources must contain ModernElevatedCard");
            Assert.True(res.Contains("NavListBoxItemStyle"), "Resources must contain NavListBoxItemStyle");
            Assert.True(res.Contains("SidebarActionButtonStyle"), "Resources must contain SidebarActionButtonStyle");
            Assert.True(res.Contains("DialogFooterBarStyle"), "Resources must contain DialogFooterBarStyle");
        });
    }

    [Fact]
    public void Verify_MainWindow_And_Dialogs_TitleBar_IsIconVisible()
    {
        var appDir = GetAppDirectory();
        var allViews = new[] { "MainWindow.xaml", "CategoryEditDialog.xaml", "SettingsWindow.xaml", "SoftwareEditDialog.xaml", "ScannerDialog.xaml", "ModernDialog.xaml", "ColorPickerDialog.xaml" };

        foreach (var view in allViews)
        {
            var content = File.ReadAllText(Path.Combine(appDir, "Views", view));
            Assert.Contains("ui:TitleBarControl", content);
            Assert.Contains("IsIconVisible=\"True\"", content);
            Assert.Contains("Icon=\"pack://application:,,,/PortableHub.App;component/Assets/app.png\"", content);
        }
    }

    [Fact]
    public void Verify_TitleBarControl_VisualTree_IconVisibility()
    {
        _fixture.Run(() =>
        {
            var tb = new iNKORE.UI.WPF.Modern.Controls.Primitives.TitleBarControl
            {
                Title = "Test Window",
                Icon = new System.Windows.Media.Imaging.BitmapImage(new Uri("pack://application:,,,/PortableHub.App;component/Assets/app.png", UriKind.Absolute)),
                IsIconVisible = true
            };

            var window = new Window
            {
                Content = tb,
                Width = 400,
                Height = 200
            };

            window.Show();
            tb.UpdateLayout();

            var iconImage = FindVisualChild<System.Windows.Controls.Image>(tb);
            Assert.NotNull(iconImage);
            Assert.Equal(Visibility.Visible, iconImage.Visibility);

            tb.IsIconVisible = false;
            tb.UpdateLayout();
            Assert.Equal(Visibility.Collapsed, iconImage.Visibility);

            window.Close();
        });
    }

    private static T? FindVisualChild<T>(System.Windows.DependencyObject parent) where T : System.Windows.DependencyObject
    {
        for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);
            if (child is T typedChild) return typedChild;
            var result = FindVisualChild<T>(child);
            if (result != null) return result;
        }
        return null;
    }

    [Fact]
    public void Verify_RadioButton_VisualProperties_InLightAndDarkThemes()
    {
        _fixture.Run(() =>
        {
            // 1. Verify Light Theme Colors
            ThemeService.UpdateDynamicThemeColors(false);
            var lightControlBorder = (SolidColorBrush)Application.Current.Resources["AppControlBorderBrush"];
            var lightTextPrim = (SolidColorBrush)Application.Current.Resources["AppPrimaryTextBrush"];
            var lightAccent = (SolidColorBrush)Application.Current.Resources["AppAccentBrush"];
            Assert.Equal(Color.FromRgb(0x8A, 0x88, 0x86), lightControlBorder.Color);
            Assert.Equal(Color.FromRgb(0x18, 0x18, 0x1B), lightTextPrim.Color);
            Assert.Equal(Color.FromRgb(0x00, 0x78, 0xD4), lightAccent.Color);

            // 2. Verify Dark Theme Colors
            ThemeService.UpdateDynamicThemeColors(true);
            var darkControlBorder = (SolidColorBrush)Application.Current.Resources["AppControlBorderBrush"];
            var darkTextPrim = (SolidColorBrush)Application.Current.Resources["AppPrimaryTextBrush"];
            var darkAccent = (SolidColorBrush)Application.Current.Resources["AppAccentBrush"];
            Assert.Equal(Color.FromRgb(0x70, 0x70, 0x70), darkControlBorder.Color);
            Assert.Equal(Color.FromRgb(0xFF, 0xFF, 0xFF), darkTextPrim.Color);
            Assert.Equal(Color.FromRgb(0x00, 0x78, 0xD4), darkAccent.Color);

            // 3. Render RadioButtons in Light Mode to verify visually
            ThemeService.UpdateDynamicThemeColors(false);
            var card = new Border
            {
                Background = (Brush)Application.Current.Resources["AppCardBrush"],
                BorderBrush = (Brush)Application.Current.Resources["AppBorderBrush"],
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(16, 14, 16, 14),
                Width = 450,
                Height = 80
            };

            var sp = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            var rbUnchecked = new RadioButton { Content = "跟随系统", IsChecked = false, Margin = new Thickness(0, 0, 16, 0) };
            var rbChecked = new RadioButton { Content = "浅色模式", IsChecked = true, Margin = new Thickness(0, 0, 16, 0) };
            var rbUnchecked2 = new RadioButton { Content = "深色模式", IsChecked = false };
            sp.Children.Add(rbUnchecked);
            sp.Children.Add(rbChecked);
            sp.Children.Add(rbUnchecked2);
            card.Child = sp;

            card.Measure(new Size(450, 80));
            card.Arrange(new Rect(0, 0, 450, 80));
            card.UpdateLayout();

            var rtb = new System.Windows.Media.Imaging.RenderTargetBitmap(450, 80, 96, 96, PixelFormats.Pbgra32);
            rtb.Render(card);
            var encoder = new System.Windows.Media.Imaging.PngBitmapEncoder();
            encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(rtb));
            using (var fs = System.IO.File.Create(@"C:\Users\cst\.gemini\antigravity\brain\23260f3a-bf13-4538-8d5c-8d0de67518a6\light_radiobuttons_sample.png"))
            {
                encoder.Save(fs);
            }

            // 4. Render RadioButtons in Dark Mode to verify visually
            ThemeService.UpdateDynamicThemeColors(true);
            var darkCard = new Border
            {
                Background = (Brush)Application.Current.Resources["AppCardBrush"],
                BorderBrush = (Brush)Application.Current.Resources["AppBorderBrush"],
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(16, 14, 16, 14),
                Width = 450,
                Height = 80
            };

            var darkSp = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            var darkRbUnchecked = new RadioButton { Content = "跟随系统", IsChecked = false, Margin = new Thickness(0, 0, 16, 0) };
            var darkRbChecked = new RadioButton { Content = "深色模式", IsChecked = true, Margin = new Thickness(0, 0, 16, 0) };
            var darkRbUnchecked2 = new RadioButton { Content = "浅色模式", IsChecked = false };
            darkSp.Children.Add(darkRbUnchecked);
            darkSp.Children.Add(darkRbChecked);
            darkSp.Children.Add(darkRbUnchecked2);
            darkCard.Child = darkSp;

            darkCard.Measure(new Size(450, 80));
            darkCard.Arrange(new Rect(0, 0, 450, 80));
            darkCard.UpdateLayout();

            var darkRtb = new System.Windows.Media.Imaging.RenderTargetBitmap(450, 80, 96, 96, PixelFormats.Pbgra32);
            darkRtb.Render(darkCard);
            var darkEncoder = new System.Windows.Media.Imaging.PngBitmapEncoder();
            darkEncoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(darkRtb));
            using (var fs = System.IO.File.Create(@"C:\Users\cst\.gemini\antigravity\brain\23260f3a-bf13-4538-8d5c-8d0de67518a6\dark_radiobuttons_sample.png"))
            {
                darkEncoder.Save(fs);
            }
        });
    }

    [Fact]
    public void Test_ControlAndCardAnimationStyles_LoadSuccessfully()
    {
        _fixture.Run(() =>
        {
            // 1. Verify ModernCard & ModernListItem styles exist and have hover triggers
            var modernCardStyle = Application.Current.TryFindResource("ModernCard") as Style;
            Assert.NotNull(modernCardStyle);
            Assert.NotEmpty(modernCardStyle.Triggers);

            var modernListItemStyle = Application.Current.TryFindResource("ModernListItem") as Style;
            Assert.NotNull(modernListItemStyle);
            Assert.NotEmpty(modernListItemStyle.Triggers);

            // 2. Verify ModernButton template and scale transform
            var button = new Button { Style = Application.Current.TryFindResource("ModernButton") as Style };
            button.ApplyTemplate();
            Assert.NotNull(button.Template);

            // 3. Verify ToggleSwitch template and triggers
            var toggleStyle = Application.Current.TryFindResource(typeof(iNKORE.UI.WPF.Modern.Controls.ToggleSwitch)) as Style;
            Assert.NotNull(toggleStyle);
            var toggleTemplate = toggleStyle.Setters.OfType<Setter>().FirstOrDefault(s => s.Property == Control.TemplateProperty)?.Value as ControlTemplate;
            Assert.NotNull(toggleTemplate);
            Assert.NotEmpty(toggleTemplate.Triggers);

            // 4. Verify RadioButton template and triggers
            var rbStyle = Application.Current.TryFindResource(typeof(RadioButton)) as Style;
            Assert.NotNull(rbStyle);
            var rbTemplate = rbStyle.Setters.OfType<Setter>().FirstOrDefault(s => s.Property == Control.TemplateProperty)?.Value as ControlTemplate;
            Assert.NotNull(rbTemplate);
            Assert.NotEmpty(rbTemplate.Triggers);

            // 5. Verify NavListBoxItemStyle and SettingsNavListBoxItemStyle
            var navStyle = Application.Current.TryFindResource("NavListBoxItemStyle") as Style;
            Assert.NotNull(navStyle);
            var navTemplate = navStyle.Setters.OfType<Setter>().FirstOrDefault(s => s.Property == Control.TemplateProperty)?.Value as ControlTemplate;
            Assert.NotNull(navTemplate);
            Assert.NotEmpty(navTemplate.Triggers);

            var settingsNavStyle = Application.Current.TryFindResource("SettingsNavListBoxItemStyle") as Style;
            Assert.NotNull(settingsNavStyle);
            var settingsNavTemplate = settingsNavStyle.Setters.OfType<Setter>().FirstOrDefault(s => s.Property == Control.TemplateProperty)?.Value as ControlTemplate;
            Assert.NotNull(settingsNavTemplate);
            Assert.NotEmpty(settingsNavTemplate.Triggers);
        });
    }

    [Fact]
    public void Test_CategoryContextMenu_And_AnimationTargets_Valid()
    {
        var themeDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "PortableHub.App", "Themes"));
        if (Directory.Exists(themeDir))
        {
            var xamlFiles = Directory.GetFiles(themeDir, "*.xaml");
            foreach (var file in xamlFiles)
            {
                var content = File.ReadAllText(file);
                // Verify no Storyboard targets Freezable/Transform objects directly
                Assert.DoesNotMatch(@"Storyboard\.TargetName=""(PillScale|SettingsPillScale|ActionBtnScale|IconBtnScale|ButtonScale|ThumbTranslate|CheckDotScale)""", content);
            }
        }

        var mainWindowXaml = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "PortableHub.App", "Views", "MainWindow.xaml"));
        if (File.Exists(mainWindowXaml))
        {
            var xaml = File.ReadAllText(mainWindowXaml);
            Assert.Contains("EditCategoryMenuItem_Click", xaml);
            Assert.Contains("DeleteCategoryMenuItem_Click", xaml);
            Assert.Contains("CategoryItem_PreviewMouseRightButtonDown", xaml);
            Assert.Contains("SoftwareContentHost", xaml);
        }
    }

    [Fact]
    public void Test_ModernDialog_And_ColorPicker_CanBeConstructed()
    {
        _fixture.Run(() =>
        {
            var dlg = new ModernDialog("测试消息", "测试标题", ModernDialogType.Danger, isConfirm: true);
            Assert.NotNull(dlg);
            Assert.Equal("测试标题", dlg.Title);

            var colorPicker = new ColorPickerDialog("#10B981");
            Assert.NotNull(colorPicker);
            Assert.Equal("#10B981", colorPicker.SelectedHex);
            Assert.NotEmpty(ColorPickerDialog.FluentPresetColors);

            // Test HSV to RGB color calculations
            var red = ColorPickerDialog.HsvToRgb(0, 1.0, 1.0);
            Assert.Equal(255, red.R);
            Assert.Equal(0, red.G);
            Assert.Equal(0, red.B);

            var white = ColorPickerDialog.HsvToRgb(0, 0.0, 1.0);
            Assert.Equal(255, white.R);
            Assert.Equal(255, white.G);
            Assert.Equal(255, white.B);

            var black = ColorPickerDialog.HsvToRgb(180, 1.0, 0.0);
            Assert.Equal(0, black.R);
            Assert.Equal(0, black.G);
            Assert.Equal(0, black.B);

            // Verify visual layout and element presence
            colorPicker.Measure(new Size(480, 520));
            colorPicker.Arrange(new Rect(0, 0, 480, 520));
            colorPicker.UpdateLayout();

            var spectrum = colorPicker.FindName("SpectrumContainer") as FrameworkElement;
            Assert.NotNull(spectrum);
            var hue = colorPicker.FindName("HueContainer") as FrameworkElement;
            Assert.NotNull(hue);
            var hexBox = colorPicker.FindName("HexInputBox") as TextBox;
            Assert.NotNull(hexBox);
            Assert.Equal("#10B981", hexBox.Text);
        });
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

    private class BindingErrorListener : System.Diagnostics.TraceListener
    {
        private readonly Action<string> _onError;
        public BindingErrorListener(Action<string> onError) => _onError = onError;
        public override void Write(string? message) {}
        public override void WriteLine(string? message) { if (!string.IsNullOrWhiteSpace(message)) _onError(message); }
    }
}
