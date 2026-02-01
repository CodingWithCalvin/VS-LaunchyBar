using System;
using System.Runtime.InteropServices;
using System.Threading;
using CodingWithCalvin.LaunchyBar.Options;
using CodingWithCalvin.LaunchyBar.Services;
using CodingWithCalvin.Otel4Vsix;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Task = System.Threading.Tasks.Task;

namespace CodingWithCalvin.LaunchyBar;

[PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
[Guid(VSCommandTableVsct.guidLaunchyBarPackageString)]
[ProvideAutoLoad(VSConstants.UICONTEXT.ShellInitialized_string, PackageAutoLoadFlags.BackgroundLoad)]
[ProvideOptionPage(typeof(OptionsProvider.GeneralOptionsPage), "LaunchyBar", "General", 0, 0, true)]
public sealed class LaunchyBarPackage : AsyncPackage
{
    public static LaunchyBarPackage? Instance { get; private set; }

    private IConfigurationService? _configurationService;
    private ILaunchService? _launchService;
    private IShellInjectionService? _shellInjectionService;

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

        // Initialize services
        _configurationService = new ConfigurationService();
        _launchService = new LaunchService(this);

        // Delay injection slightly to ensure VS UI is fully loaded
        await Task.Delay(1000, cancellationToken);
        await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        // Inject the bar into VS shell
        _shellInjectionService = new ShellInjectionService(_configurationService, _launchService);
        _shellInjectionService.Inject();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _shellInjectionService?.Dispose();
            Instance = null;
            VsixTelemetry.Shutdown();
        }

        base.Dispose(disposing);
    }
}
