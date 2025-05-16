using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reactive;
using System.Threading.Tasks;
using Avalonia.Interactivity;
using DravusSensorPanel.Models;
using DravusSensorPanel.Services;
using DynamicData;
using MsBox.Avalonia;
using MsBox.Avalonia.Base;
using MsBox.Avalonia.Dto;
using MsBox.Avalonia.Enums;
using MsBox.Avalonia.Models;
using Octokit;
using ReactiveUI;

namespace DravusSensorPanel.Views.Windows;

public partial class SensorPanelFinderWindow : WindowViewModel {
    private readonly GithubService? _githubService;
    private readonly UtilService? _utilService;

    private RepoSearchSort _sort = RepoSearchSort.Stars;
    private SortDirection _direction = SortDirection.Descending;
    private int _page;
    private bool _hasNextPage;
    private bool _showMoreInfo;
    private string? _readme;
    private GithubSensorPanel? _selected;

    public ReactiveCommand<GithubSensorPanel, Unit> ViewItemCommand { get; init; }
    public ReactiveCommand<GithubSensorPanel, Unit> OpenItemOnBrowserCommand { get; init; }

    public RepoSearchSort Sort {
        get => _sort;
        set => SetField(ref _sort, value);
    }

    public SortDirection Direction {
        get => _direction;
        set => SetField(ref _direction, value);
    }

    public bool HasNextPage {
        get => _hasNextPage;
        set => SetField(ref _hasNextPage, value);
    }

    public GithubSensorPanel? Selected {
        get => _selected;
        set {
            if ( !SetField(ref _selected, value) ) return;
            OnPropertyChanged(nameof(ShowMoreInfo));
        }
    }

    public bool ShowMoreInfo => Selected != null;

    public string? Readme {
        get => _readme;
        set => SetField(ref _readme, value);
    }


    public ObservableCollection<GithubSensorPanel> Items { get; } = new();

    public SensorPanelFinderWindow() : this(null, null) {
    }

    public SensorPanelFinderWindow(GithubService? githubService, UtilService utilService) {
        DataContext = this;

        _githubService = githubService;
        _utilService = utilService;

        InitializeComponent();

        LoadItems();

        ViewItemCommand = ReactiveCommand.Create<GithubSensorPanel>((item) => { SeeItem(item); });
        OpenItemOnBrowserCommand = ReactiveCommand.Create<GithubSensorPanel>((item) => { OpenItemOnBrowser(item); });
    }

    private async Task LoadItems() {
        if ( _githubService == null ) return;

        try {
            (bool, List<GithubSensorPanel>) result = await _githubService.ListPanels(Sort, Direction, _page);
            Items.AddRange(result.Item2);
            HasNextPage = result.Item1;
        }
        catch ( RateLimitExceededException ex ) {
            ShowRateLimitPopup();
        }
    }

    private async void LoadMoreItemsClick(object? sender, RoutedEventArgs e) {
        _page++;
        await LoadItems();
    }

    private async Task SeeItem(GithubSensorPanel item) {
        Readme = "";

        if ( _githubService == null ) return;

        Selected = item;

        try {
            await _githubService.GetReadme(item);

            Readme = item.Readme;
        }
        catch ( RateLimitExceededException ex ) {
            ShowRateLimitPopup();
        }
    }

    private void OpenItemOnBrowser(GithubSensorPanel item) {
        _utilService?.OpenSite(this, item.Url);
    }

    private async void DownloadClick(object? sender, RoutedEventArgs e) {
        if ( Selected == null || _githubService == null ) return;

        IMsBox<ButtonResult> confirmationBox = MessageBoxManager
            .GetMessageBoxStandard(
                "Confirmation",
                $"Are you sure that you want to download {{Selected.Name}}? This will delete your actual sensor panel and is irreversible.",
                ButtonEnum.YesNo);

        ButtonResult result = await confirmationBox.ShowAsPopupAsync(this);

        if ( result == ButtonResult.Yes ) {
            try {
                string? path = await _githubService.DownloadSensorPanel(Selected);

                if ( path == null ) {
                    await MessageBoxManager
                          .GetMessageBoxStandard(
                              "Error",
                              $"Sensor panel not imported.",
                              ButtonEnum.Ok).ShowAsPopupAsync(this);
                }
                else {
                    Close(path);
                }
            }
            catch ( RateLimitExceededException ex ) {
                ShowRateLimitPopup();
            }
        }
    }

    private void BackClick(object? sender, RoutedEventArgs e) {
        Selected = null;
    }

    private async void ShowRateLimitPopup() {
        IMsBox<string>? popup = MessageBoxManager
            .GetMessageBoxCustom(
                new MessageBoxCustomParams {
                    ButtonDefinitions = new List<ButtonDefinition> {
                        new() { Name = "Cancel", IsCancel = true },
                        new() { Name = "Open GitHub", IsDefault = true },
                    },
                    ContentTitle = "Help",
                    ContentMessage = """
                                     Unfortunately, you have reached the request limit to GitHub.
                                     This limitation lasts for 1 hour.
                                     However, you can download new Panels directly from GitHub via your browser!
                                     """,
                });

        string choice = await popup.ShowAsPopupAsync(this);

        if ( choice == "Open GitHub" ) {
            _utilService?.OpenGithubPanels(this);
        }
        else {
            Close();
        }
    }
}
