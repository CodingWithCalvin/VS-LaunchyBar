using System;
using System.Diagnostics;
using System.Threading.Tasks;
using CodingWithCalvin.LaunchyBar.Models;
using CodingWithCalvin.Otel4Vsix;
using Community.VisualStudio.Toolkit;
using Microsoft.VisualStudio.Shell;

namespace CodingWithCalvin.LaunchyBar.Services;

/// <summary>
/// Service for executing launch item actions.
/// </summary>
public sealed class LaunchService : ILaunchService
{
    private readonly AsyncPackage _package;

    public LaunchService(AsyncPackage package)
    {
        _package = package;
    }

    /// <inheritdoc/>
    public async Task ExecuteAsync(LaunchItem item)
    {
        if (!item.IsEnabled)
        {
            return;
        }

        using var activity = VsixTelemetry.StartActivity("LaunchItem.Execute");
        activity?.SetTag("item.id", item.Id);
        activity?.SetTag("item.type", item.Type.ToString());

        try
        {
            switch (item.Type)
            {
                case LaunchItemType.ExternalProgram:
                    await ExecuteExternalProgramAsync(item);
                    break;

                case LaunchItemType.ToolWindow:
                case LaunchItemType.VsCommand:
                    await ExecuteVsCommandAsync(item);
                    break;

                case LaunchItemType.CustomAction:
                    await ExecuteCustomActionAsync(item);
                    break;
            }

            activity?.SetStatus(System.Diagnostics.ActivityStatusCode.Ok);
        }
        catch (Exception ex)
        {
            activity?.SetStatus(System.Diagnostics.ActivityStatusCode.Error, ex.Message);
            await VS.StatusBar.ShowMessageAsync($"LaunchyBar: Failed to execute '{item.Name}'");
        }
    }

    private Task ExecuteExternalProgramAsync(LaunchItem item)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = item.Target,
            Arguments = item.Arguments ?? string.Empty,
            WorkingDirectory = item.WorkingDirectory ?? string.Empty,
            UseShellExecute = true
        };

        Process.Start(startInfo);
        return Task.CompletedTask;
    }

    private async Task ExecuteVsCommandAsync(LaunchItem item)
    {
        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

        try
        {
            await VS.Commands.ExecuteAsync(item.Target);
        }
        catch (Exception)
        {
            // Command may not exist or may be disabled - status bar will show error
            throw;
        }
    }

    private Task ExecuteCustomActionAsync(LaunchItem item)
    {
        // Extensibility point for future custom actions
        return Task.CompletedTask;
    }
}
