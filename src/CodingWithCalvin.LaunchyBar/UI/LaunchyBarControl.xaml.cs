using System;
using System.Windows.Controls;
using CodingWithCalvin.LaunchyBar.Services;
using CodingWithCalvin.LaunchyBar.ViewModels;

namespace CodingWithCalvin.LaunchyBar.UI;

/// <summary>
/// The LaunchyBar control that gets injected into the VS shell.
/// </summary>
public partial class LaunchyBarControl : UserControl
{
    private readonly LaunchyBarViewModel _viewModel;

    public LaunchyBarControl(IConfigurationService configurationService, ILaunchService launchService)
    {
        InitializeComponent();

        _viewModel = new LaunchyBarViewModel(configurationService, launchService);
        DataContext = _viewModel;

        Loaded += OnLoaded;
    }

#pragma warning disable VSTHRD100 // Avoid async void - required by event handler signature
    private async void OnLoaded(object sender, System.Windows.RoutedEventArgs e)
#pragma warning restore VSTHRD100
    {
        try
        {
            await _viewModel.InitializeAsync();
        }
        catch (Exception)
        {
            // Swallow exceptions to prevent crash - initialization errors are non-fatal
        }
    }
}
