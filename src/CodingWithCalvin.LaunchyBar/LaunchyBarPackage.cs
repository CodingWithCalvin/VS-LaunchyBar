using System;
using System.Runtime.InteropServices;
using System.Threading;
using CodingWithCalvin.LaunchyBar.Options;
using CodingWithCalvin.LaunchyBar.Services;
using CodingWithCalvin.LaunchyBar.ToolWindow;
using CodingWithCalvin.Otel4Vsix;
using Community.VisualStudio.Toolkit;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Task = System.Threading.Tasks.Task;

namespace CodingWithCalvin.LaunchyBar;

[PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
[Guid(VSCommandTableVsct.guidLaunchyBarPackageString)]
[ProvideMenuResource("Menus.ctmenu", 1)]
[ProvideToolWindow(
    typeof(LaunchyBarWindow),
    Style = VsDockStyle.MDI,
    Orientation = ToolWindowOrientation.Left,
    Width = 60,
    Height = 600,
    Window = "3ae79031-e1bc-11d0-8f78-00a0c9110057")]
[ProvideToolWindowVisibility(typeof(LaunchyBarWindow), VSConstants.UICONTEXT.NoSolution_string)]
[ProvideToolWindowVisibility(typeof(LaunchyBarWindow), VSConstants.UICONTEXT.SolutionExists_string)]
[ProvideToolWindowVisibility(typeof(LaunchyBarWindow), VSConstants.UICONTEXT.EmptySolution_string)]
[ProvideAutoLoad(VSConstants.UICONTEXT.ShellInitialized_string, PackageAutoLoadFlags.BackgroundLoad)]
[ProvideAutoLoad(VSConstants.UICONTEXT.NoSolution_string, PackageAutoLoadFlags.BackgroundLoad)]
[ProvideAutoLoad(VSConstants.UICONTEXT.SolutionExists_string, PackageAutoLoadFlags.BackgroundLoad)]
[ProvideOptionPage(typeof(OptionsProvider.GeneralOptionsPage), "LaunchyBar", "General", 0, 0, true)]
public sealed class LaunchyBarPackage : AsyncPackage
{
    public static LaunchyBarPackage? Instance { get; private set; }

    internal IConfigurationService? ConfigurationService { get; private set; }
    internal ILaunchService? LaunchService { get; private set; }

    protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
    {
        Instance = this;

        await base.InitializeAsync(cancellationToken, progress);
        await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        // Initialize telemetry
        var builder = VsixTelemetry.Configure()
            .WithServiceName(VsixInfo.DisplayName)
            .WithServiceVersion(VsixInfo.Version)
            .WithVisualStudioAttributes(this)
            .WithEnvironmentAttributes();

#if !DEBUG
        builder
            .WithOtlpHttp("https://api.honeycomb.io")
            .WithHeader("x-honeycomb-team", HoneycombConfig.ApiKey);
#endif

        builder.Initialize();

        ConfigurationService = new ConfigurationService();
        LaunchService = new LaunchService(this);

        await this.RegisterCommandsAsync();

        await ShowToolWindowAsync(cancellationToken);
    }

    private async Task ShowToolWindowAsync(CancellationToken cancellationToken)
    {
        await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        var window = await FindToolWindowAsync(
            typeof(LaunchyBarWindow),
            0,
            create: true,
            cancellationToken);

        if (window?.Frame is IVsWindowFrame windowFrame)
        {
            Microsoft.VisualStudio.ErrorHandler.ThrowOnFailure(windowFrame.Show());
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            Instance = null;
            VsixTelemetry.Shutdown();
        }

        base.Dispose(disposing);
    }
}
