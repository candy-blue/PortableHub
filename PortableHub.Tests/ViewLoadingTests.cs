using System;
using System.IO;
using System.Text.RegularExpressions;
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
        Assert.Equal(Wpf.Ui.Controls.SymbolRegular.Home24, home.Symbol);
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
            Assert.Contains("WindowCornerPreference=\"Round\"", content);
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
        Assert.Contains("<ui:Button Grid.Column=\"0\" Appearance=\"Transparent\" Command=\"{Binding ToggleSidebarCommand}\"", mainWindowXaml);

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
    public void MainWindow_Sidebar_GeometryAndLayoutVerification()
    {
        _fixture.Run(() =>
        {
            using var env = new TestEnvironment();
            var provider = CreateServiceProvider(env);
            var vm = provider.GetRequiredService<MainViewModel>();
            var settingsService = provider.GetRequiredService<ISettingsService>();

            var window = new MainWindow(vm, settingsService);
            Assert.NotNull(window);

            // Locate SidebarBorder
            var sidebar = window.FindName("SidebarBorder") as System.Windows.Controls.Border;
            Assert.NotNull(sidebar);
            Assert.True(sidebar.ClipToBounds);
            Assert.Equal(224.0, sidebar.Width);

            // Verify toggle command
            vm.ToggleSidebarCommand.Execute(null);
            Assert.True(vm.IsSidebarCollapsed);

            // Locate CategoriesListBox and verify items
            var catListBox = window.FindName("CategoriesListBox") as System.Windows.Controls.ListBox;
            Assert.NotNull(catListBox);

            window.Close();
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
}
