using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PortableHub.Core.Interfaces;
using PortableHub.Core.Models;

namespace PortableHub.App.ViewModels;

public partial class QuickLauncherViewModel : ObservableObject
{
    private readonly ISearchService _searchService;
    private readonly ILaunchService _launchService;

    public event EventHandler? RequestClose;

    [ObservableProperty]
    private string _searchQuery = string.Empty;

    [ObservableProperty]
    private int _selectedIndex = 0;

    [ObservableProperty]
    private bool _hasNoResults;

    public ObservableCollection<Software> Results { get; } = [];

    public QuickLauncherViewModel(ISearchService searchService, ILaunchService launchService)
    {
        _searchService = searchService;
        _launchService = launchService;
    }

    public void OnOpened()
    {
        SearchQuery = string.Empty;
        UpdateResults();
    }

    partial void OnSearchQueryChanged(string value)
    {
        UpdateResults();
    }

    private void UpdateResults()
    {
        var list = _searchService.SearchQuickLauncher(SearchQuery, maxResults: 10);
        Results.Clear();
        foreach (var item in list)
        {
            Results.Add(item);
        }
        SelectedIndex = Results.Count > 0 ? 0 : -1;
        HasNoResults = Results.Count == 0 && !string.IsNullOrWhiteSpace(SearchQuery);
    }

    public void MoveSelection(int delta)
    {
        if (Results.Count == 0) return;
        var next = SelectedIndex + delta;
        if (next < 0) next = Results.Count - 1;
        if (next >= Results.Count) next = 0;
        SelectedIndex = next;
    }

    [RelayCommand]
    public async Task LaunchSelectedAsync()
    {
        if (SelectedIndex >= 0 && SelectedIndex < Results.Count)
        {
            var target = Results[SelectedIndex];
            await LaunchItemAsync(target);
        }
    }

    public async Task LaunchByIndexAsync(int index)
    {
        if (index >= 0 && index < Results.Count)
        {
            var target = Results[index];
            await LaunchItemAsync(target);
        }
    }

    public event EventHandler<string>? LaunchFailed;
    public Action<string, string>? ShowErrorAction { get; set; }

    private async Task LaunchItemAsync(Software target)
    {
        if (target == null) return;

        var result = await _launchService.LaunchAsync(target);
        if (result.Success)
        {
            RequestClose?.Invoke(this, EventArgs.Empty);
        }
        else
        {
            var msg = result.ErrorMessage ?? "未知启动错误";
            LaunchFailed?.Invoke(this, msg);

            var errorDetails = $"无法启动软件 \"{target.Name}\"：\n\n{msg}";
            if (ShowErrorAction != null)
            {
                ShowErrorAction(errorDetails, "启动失败");
            }
            else
            {
                Views.ModernDialog.ShowError(errorDetails, "启动失败");
            }
        }
    }
}
