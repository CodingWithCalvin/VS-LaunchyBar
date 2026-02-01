using System;
using System.Threading.Tasks;
using CodingWithCalvin.LaunchyBar.Models;

namespace CodingWithCalvin.LaunchyBar.Services;

/// <summary>
/// Service for managing LaunchyBar configuration persistence.
/// </summary>
public interface IConfigurationService
{
    /// <summary>
    /// Gets the current configuration.
    /// </summary>
    LaunchyBarConfiguration Configuration { get; }

    /// <summary>
    /// Loads configuration from storage.
    /// </summary>
    Task LoadAsync();

    /// <summary>
    /// Saves configuration to storage.
    /// </summary>
    Task SaveAsync();

    /// <summary>
    /// Resets configuration to defaults.
    /// </summary>
    Task ResetToDefaultsAsync();

    /// <summary>
    /// Event raised when configuration changes.
    /// </summary>
    event EventHandler? ConfigurationChanged;
}
