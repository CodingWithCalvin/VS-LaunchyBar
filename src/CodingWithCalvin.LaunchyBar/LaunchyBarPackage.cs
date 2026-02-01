using System;
using System.Diagnostics;
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

        try
        {
            Debug.WriteLine("LaunchyBar: Initializing services...");

            // Initialize services
            _configurationService = new ConfigurationService();
            Debug.WriteLine("LaunchyBar: ConfigurationService created");

            _launchService = new LaunchService(this);
            Debug.WriteLine("LaunchyBar: LaunchService created");

            // Inject the bar into VS shell with retries
            Debug.WriteLine("LaunchyBar: Creating ShellInjectionService...");
            _shellInjectionService = new ShellInjectionService(_configurationService, _launchService);
            Debug.WriteLine("LaunchyBar: ShellInjectionService created");

            // Retry injection until successful or max attempts reached
            const int maxAttempts = 10;
            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                Debug.WriteLine($"LaunchyBar: Injection attempt {attempt}/{maxAttempts}...");
                await Task.Delay(1000, cancellationToken);
                await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

                var result = _shellInjectionService.Inject();
                if (result)
                {
                    Debug.WriteLine("LaunchyBar: Injection successful!");
                    break;
                }

                Debug.WriteLine($"LaunchyBar: Attempt {attempt} failed, will retry...");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"LaunchyBar: EXCEPTION - {ex.GetType().Name}: {ex.Message}");
            Debug.WriteLine($"LaunchyBar: Stack trace: {ex.StackTrace}");
            if (ex.InnerException != null)
            {
                Debug.WriteLine($"LaunchyBar: Inner exception - {ex.InnerException.GetType().Name}: {ex.InnerException.Message}");
            }
        }
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
