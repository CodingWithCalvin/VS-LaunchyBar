using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using CodingWithCalvin.LaunchyBar.Models;
using CodingWithCalvin.LaunchyBar.Services;

namespace CodingWithCalvin.LaunchyBar.ViewModels;

/// <summary>
/// ViewModel for the LaunchyBar control.
/// </summary>
public sealed class LaunchyBarViewModel : ViewModelBase
{
    private readonly IConfigurationService _configurationService;
    private readonly ILaunchService _launchService;

    /// <summary>
    /// Items displayed at the top of the bar.
    /// </summary>
    public ObservableCollection<LaunchItem> TopItems { get; } = new();

    /// <summary>
    /// Items displayed at the bottom of the bar.
    /// </summary>
    public ObservableCollection<LaunchItem> BottomItems { get; } = new();

    /// <summary>
    /// Command to execute a launch item.
    /// </summary>
    public ICommand LaunchCommand { get; }

    /// <summary>
    /// Creates a new LaunchyBarViewModel.
    /// </summary>
    public LaunchyBarViewModel(IConfigurationService configurationService, ILaunchService launchService)
    {
        _configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
        _launchService = launchService ?? throw new ArgumentNullException(nameof(launchService));

        LaunchCommand = new AsyncRelayCommand(ExecuteLaunchAsync);

        _configurationService.ConfigurationChanged += OnConfigurationChanged;
    }

    /// <summary>
    /// Initializes the ViewModel by loading configuration.
    /// </summary>
    public async Task InitializeAsync()
    {
        await _configurationService.LoadAsync();
        RefreshItems();
    }

    private void OnConfigurationChanged(object? sender, EventArgs e)
    {
        RefreshItems();
    }

    private void RefreshItems()
    {
        TopItems.Clear();
        BottomItems.Clear();

        var config = _configurationService.Configuration;

        foreach (var item in config.Items.Where(i => i.Position == LaunchItemPosition.Top).OrderBy(i => i.Order))
        {
            TopItems.Add(item);
        }

        foreach (var item in config.Items.Where(i => i.Position == LaunchItemPosition.Bottom).OrderBy(i => i.Order))
        {
            BottomItems.Add(item);
        }
    }

    private async Task ExecuteLaunchAsync(object? parameter)
    {
        if (parameter is LaunchItem item)
        {
            await _launchService.ExecuteAsync(item);
        }
    }
}
