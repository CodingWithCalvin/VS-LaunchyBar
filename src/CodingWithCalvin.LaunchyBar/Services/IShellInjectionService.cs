using System;

namespace CodingWithCalvin.LaunchyBar.Services;

/// <summary>
/// Service for injecting custom UI into the Visual Studio shell.
/// </summary>
public interface IShellInjectionService : IDisposable
{
    /// <summary>
    /// Injects the LaunchyBar into the VS shell.
    /// </summary>
    /// <returns>True if injection succeeded, false otherwise.</returns>
    bool Inject();

    /// <summary>
    /// Removes the LaunchyBar from the VS shell.
    /// </summary>
    void Remove();

    /// <summary>
    /// Gets whether the bar is currently injected.
    /// </summary>
    bool IsInjected { get; }
}
