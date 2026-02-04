using System;
using System.Linq;
using CodingWithCalvin.LaunchyBar.Models;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;

namespace CodingWithCalvin.LaunchyBar.Services;

/// <summary>
/// Service that monitors debug state and updates the debug item's icon accordingly.
/// </summary>
public sealed class DebugStateService : IDisposable
{
    private readonly DTE2? _dte;
    private readonly DebuggerEvents? _debuggerEvents;
    private readonly IConfigurationService _configurationService;
    private bool _disposed;

    public DebugStateService(AsyncPackage package, IConfigurationService configurationService)
    {
        _configurationService = configurationService;

        ThreadHelper.ThrowIfNotOnUIThread();

        _dte = package.GetService<DTE, DTE2>();
        if (_dte != null)
        {
            _debuggerEvents = _dte.Events.DebuggerEvents;
            _debuggerEvents.OnEnterRunMode += OnEnterRunMode;
            _debuggerEvents.OnEnterBreakMode += OnEnterBreakMode;
            _debuggerEvents.OnEnterDesignMode += OnEnterDesignMode;

            // Set initial state
            UpdateDebugIcon(_dte.Debugger.CurrentMode != dbgDebugMode.dbgDesignMode);
        }
    }

    private void OnEnterRunMode(dbgEventReason reason)
    {
        UpdateDebugIcon(true);
    }

    private void OnEnterBreakMode(dbgEventReason reason, ref dbgExecutionAction executionAction)
    {
        UpdateDebugIcon(true);
    }

    private void OnEnterDesignMode(dbgEventReason reason)
    {
        UpdateDebugIcon(false);
    }

    private void UpdateDebugIcon(bool isDebugging)
    {
        var debugItem = _configurationService.Configuration.Items
            .FirstOrDefault(i => i.Id == "debug" || i.Target == "Debug.Start");

        if (debugItem != null)
        {
            debugItem.IconPath = isDebugging ? "KnownMonikers.Stop" : "KnownMonikers.Run";
            debugItem.Name = isDebugging ? "Stop Debugging" : "Start Debugging";
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        ThreadHelper.ThrowIfNotOnUIThread();

        if (_debuggerEvents != null)
        {
            _debuggerEvents.OnEnterRunMode -= OnEnterRunMode;
            _debuggerEvents.OnEnterBreakMode -= OnEnterBreakMode;
            _debuggerEvents.OnEnterDesignMode -= OnEnterDesignMode;
        }
    }
}
