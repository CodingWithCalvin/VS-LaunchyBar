using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Community.VisualStudio.Toolkit;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Shell;

namespace CodingWithCalvin.LaunchyBar.ToolWindow;

/// <summary>
/// This class implements the tool window exposed by this package and hosts a user control.
/// </summary>
[Guid(VSCommandTableVsct.guidLaunchyBarToolWindowString)]
public sealed class LaunchyBarWindow : ToolWindowPane
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LaunchyBarWindow"/> class.
    /// </summary>
    public LaunchyBarWindow() : base(null)
    {
        Caption = "LaunchyBar";
        BitmapImageMoniker = KnownMonikers.ToolWindow;
        Content = new LaunchyBarControl();
    }

    /// <summary>
    /// Shows the tool window.
    /// </summary>
    public static async Task ShowAsync()
    {
        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

        var package = LaunchyBarPackage.Instance;
        if (package == null) return;

        var window = await package.FindToolWindowAsync(
            typeof(LaunchyBarWindow),
            0,
            create: true,
            package.DisposalToken);

        if (window?.Frame is Microsoft.VisualStudio.Shell.Interop.IVsWindowFrame frame)
        {
            Microsoft.VisualStudio.ErrorHandler.ThrowOnFailure(frame.Show());
        }
    }
}
