using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using CodingWithCalvin.LaunchyBar.Models;
using CodingWithCalvin.Otel4Vsix;
using Community.VisualStudio.Toolkit;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace CodingWithCalvin.LaunchyBar.Services;

/// <summary>
/// Service for executing launch item actions.
/// </summary>
public sealed class LaunchService : ILaunchService
{
    private readonly AsyncPackage _package;

    /// <summary>
    /// Maps VS View commands to their tool window GUIDs for toggle support.
    /// </summary>
    private static readonly Dictionary<string, Guid> ToolWindowGuids = new(StringComparer.OrdinalIgnoreCase)
    {
        { "View.SolutionExplorer", new Guid(ToolWindowGuids80.SolutionExplorer) },
        { "View.Output", new Guid(ToolWindowGuids80.Outputwindow) },
        { "View.ErrorList", new Guid(ToolWindowGuids80.ErrorList) },
        { "View.TaskList", new Guid(ToolWindowGuids80.TaskList) },
        { "View.Toolbox", new Guid(ToolWindowGuids80.Toolbox) },
        { "View.PropertiesWindow", new Guid(ToolWindowGuids80.PropertiesWindow) },
        { "View.ClassView", new Guid(ToolWindowGuids80.ClassView) },
        // VS 2022 Terminal (Developer PowerShell)
        { "View.Terminal", new Guid("d212f56b-c48a-434c-a121-1c5d80b59b9f") },
        // VS 2022 Git Changes
        { "View.GitWindow", new Guid("1c64b9c2-e352-428e-a56d-0ace190b99a6") },
    };

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
                    await ToggleToolWindowAsync(item);
                    break;

                case LaunchItemType.VsCommand:
                    // Special handling for debug commands
                    if (item.Target.Equals("Debug.Start", StringComparison.OrdinalIgnoreCase))
                    {
                        await ToggleDebugAsync();
                    }
                    else
                    {
                        await ExecuteVsCommandAsync(item);
                    }
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

        System.Diagnostics.Process.Start(startInfo);
        return Task.CompletedTask;
    }

    private async Task ToggleToolWindowAsync(LaunchItem item)
    {
        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

        if (ToolWindowGuids.TryGetValue(item.Target, out var guid))
        {
            var shell = await _package.GetServiceAsync(typeof(SVsUIShell)) as IVsUIShell;
            if (shell != null)
            {
                // Find the window by enumerating frames and matching GUID
                shell.GetToolWindowEnum(out var windowEnum);
                if (windowEnum != null)
                {
                    var frames = new IVsWindowFrame[1];
                    while (windowEnum.Next(1, frames, out var fetched) == 0 && fetched == 1)
                    {
                        var frame = frames[0];
                        if (frame == null) continue;

                        try
                        {
                            frame.GetGuidProperty((int)__VSFPROPID.VSFPROPID_GuidPersistenceSlot, out var persistGuid);
                            if (persistGuid == guid)
                            {
                                frame.IsOnScreen(out var isOnScreen);
                                if (isOnScreen != 0)
                                {
                                    frame.Hide();
                                    return;
                                }
                            }
                        }
                        catch
                        {
                            // Some frames may throw
                        }
                    }
                }
            }
        }

        // Window not found or hidden - show it via command
        await VS.Commands.ExecuteAsync(item.Target);
    }

    private async Task ToggleDebugAsync()
    {
        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

        var dte = await _package.GetServiceAsync(typeof(DTE)) as DTE2;
        if (dte == null)
        {
            await VS.Commands.ExecuteAsync("Debug.Start");
            return;
        }

        // Check if debugger is running
        if (dte.Debugger.CurrentMode == dbgDebugMode.dbgRunMode ||
            dte.Debugger.CurrentMode == dbgDebugMode.dbgBreakMode)
        {
            // Debugger is running - stop it
            await VS.Commands.ExecuteAsync("Debug.StopDebugging");
        }
        else
        {
            // Not debugging - start
            await VS.Commands.ExecuteAsync("Debug.Start");
        }
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
