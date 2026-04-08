using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Threading;
using CodingWithCalvin.LaunchyBar.Models;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace CodingWithCalvin.LaunchyBar.Services;

/// <summary>
/// Monitors tool window visibility and updates IsActive on configured launch items.
/// </summary>
public sealed class ToolWindowMonitorService : IDisposable
{
    private readonly AsyncPackage _package;
    private readonly IConfigurationService _configurationService;
    private readonly DispatcherTimer _timer;
    private bool _disposed;

    private static readonly Dictionary<string, Guid> ToolWindowGuids = new(StringComparer.OrdinalIgnoreCase)
    {
        { "View.SolutionExplorer", new Guid(ToolWindowGuids80.SolutionExplorer) },
        { "View.Output", new Guid(ToolWindowGuids80.Outputwindow) },
        { "View.ErrorList", new Guid(ToolWindowGuids80.ErrorList) },
        { "View.TaskList", new Guid(ToolWindowGuids80.TaskList) },
        { "View.Toolbox", new Guid(ToolWindowGuids80.Toolbox) },
        { "View.PropertiesWindow", new Guid(ToolWindowGuids80.PropertiesWindow) },
        { "View.ClassView", new Guid(ToolWindowGuids80.ClassView) },
        { "View.Terminal", new Guid("d212f56b-c48a-434c-a121-1c5d80b59b9f") },
        { "View.GitWindow", new Guid("1c64b9c2-e352-428e-a56d-0ace190b99a6") },
    };

    public ToolWindowMonitorService(AsyncPackage package, IConfigurationService configurationService)
    {
        _package = package;
        _configurationService = configurationService;

        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(500)
        };
        _timer.Tick += OnTimerTick;
        _timer.Start();
    }

    private void OnTimerTick(object sender, EventArgs e)
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        UpdateActiveStates();
    }

    private void UpdateActiveStates()
    {
        ThreadHelper.ThrowIfNotOnUIThread();

        var shell = _package.GetService<SVsUIShell, IVsUIShell>();
        if (shell == null) return;

        // Build a set of visible tool window GUIDs
        var visibleGuids = new HashSet<Guid>();
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
                    frame.IsOnScreen(out var isOnScreen);
                    if (isOnScreen != 0)
                    {
                        visibleGuids.Add(persistGuid);
                    }
                }
                catch
                {
                    // Some frames may throw
                }
            }
        }

        // Update IsActive on each configured tool window item
        foreach (var item in _configurationService.Configuration.Items
                     .Where(i => i.Type == LaunchItemType.ToolWindow))
        {
            if (ToolWindowGuids.TryGetValue(item.Target, out var guid))
            {
                item.IsActive = visibleGuids.Contains(guid);
            }
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _timer.Stop();
        _timer.Tick -= OnTimerTick;
    }
}
