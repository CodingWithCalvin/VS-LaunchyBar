using System;
using System.Runtime.InteropServices;
using System.Threading;
using CodingWithCalvin.LaunchyBar.Services;
using CodingWithCalvin.Otel4Vsix;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Task = System.Threading.Tasks.Task;

namespace CodingWithCalvin.LaunchyBar;

[PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
[Guid("F1D884BA-D328-4A15-89F4-ECACCCD022D1")]
[ProvideAutoLoad(VSConstants.UICONTEXT.ShellInitialized_string, PackageAutoLoadFlags.BackgroundLoad)]
public sealed class LaunchyBarPackage : AsyncPackage
{
    public static LaunchyBarPackage? Instance { get; private set; }

    private IConfigurationService? _configurationService;
    private ILaunchService? _launchService;
    private IShellInjectionService? _shellInjectionService;
    private DebugStateService? _debugStateService;

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

        try
        {
            // Initialize services
            _configurationService = new ConfigurationService();
            _launchService = new LaunchService(this);
            _shellInjectionService = new ShellInjectionService(_configurationService, _launchService);

            // Try injection immediately, then retry with increasing delays
            const int maxAttempts = 8;
            int delayMs = 100;

            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                if (_shellInjectionService.Inject())
                {
                    _debugStateService = new DebugStateService(this, _configurationService);
                    break;
                }

                // Wait before next attempt (100, 200, 400, 800, 1000, 1000, 1000, 1000)
                await Task.Delay(delayMs, cancellationToken);
                await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
                delayMs = Math.Min(delayMs * 2, 1000);
            }
        }
        catch
        {
            // Initialization failed - extension will not be functional
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _debugStateService?.Dispose();
            _shellInjectionService?.Dispose();
            Instance = null;
            VsixTelemetry.Shutdown();
        }

        base.Dispose(disposing);
    }
}
