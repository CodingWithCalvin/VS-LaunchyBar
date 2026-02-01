using System.Threading.Tasks;
using CodingWithCalvin.LaunchyBar.ToolWindow;
using Community.VisualStudio.Toolkit;
using Microsoft.VisualStudio.Shell;

namespace CodingWithCalvin.LaunchyBar.Commands;

[Command(VSCommandTableVsct.guidLaunchyBarCmdSet.GuidString, VSCommandTableVsct.guidLaunchyBarCmdSet.LaunchyBarWindowCommandId)]
internal sealed class ShowLaunchyBarCommand : BaseCommand<ShowLaunchyBarCommand>
{
    protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
    {
        await LaunchyBarWindow.ShowAsync();
    }
}
