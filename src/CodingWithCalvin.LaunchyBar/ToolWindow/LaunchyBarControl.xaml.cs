using System.Windows.Controls;
using CodingWithCalvin.LaunchyBar.ViewModels;
using Microsoft.VisualStudio.Shell;

namespace CodingWithCalvin.LaunchyBar.ToolWindow;

/// <summary>
/// Interaction logic for LaunchyBarControl.xaml
/// </summary>
public partial class LaunchyBarControl : UserControl
{
    public LaunchyBarControl()
    {
        InitializeComponent();

        Loaded += OnLoaded;
    }

    private async void OnLoaded(object sender, System.Windows.RoutedEventArgs e)
    {
        var package = LaunchyBarPackage.Instance;
        if (package?.ConfigurationService != null && package?.LaunchService != null)
        {
            var viewModel = new LaunchyBarViewModel(
                package.ConfigurationService,
                package.LaunchService);

            DataContext = viewModel;

            await ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
            {
                await viewModel.InitializeAsync();
            });
        }
    }
}
