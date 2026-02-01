using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using CodingWithCalvin.LaunchyBar.Models;

namespace CodingWithCalvin.LaunchyBar.Services;

/// <summary>
/// Service for managing LaunchyBar configuration persistence.
/// </summary>
public sealed class ConfigurationService : IConfigurationService
{
    private static readonly string ConfigDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "CodingWithCalvin",
        "LaunchyBar");

    private static readonly string ConfigFilePath = Path.Combine(ConfigDirectory, "config.json");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private LaunchyBarConfiguration _configuration = LaunchyBarConfiguration.CreateDefault();

    /// <inheritdoc/>
    public LaunchyBarConfiguration Configuration => _configuration;

    /// <inheritdoc/>
    public event EventHandler? ConfigurationChanged;

    /// <inheritdoc/>
    public async Task LoadAsync()
    {
        try
        {
            if (!File.Exists(ConfigFilePath))
            {
                _configuration = LaunchyBarConfiguration.CreateDefault();
                await SaveAsync();
                return;
            }

            var json = await ReadFileAsync(ConfigFilePath);
            var config = JsonSerializer.Deserialize<LaunchyBarConfiguration>(json, JsonOptions);

            _configuration = config ?? LaunchyBarConfiguration.CreateDefault();
            ConfigurationChanged?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception)
        {
            _configuration = LaunchyBarConfiguration.CreateDefault();
        }
    }

    /// <inheritdoc/>
    public async Task SaveAsync()
    {
        try
        {
            Directory.CreateDirectory(ConfigDirectory);

            var json = JsonSerializer.Serialize(_configuration, JsonOptions);
            await WriteFileAsync(ConfigFilePath, json);

            ConfigurationChanged?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception)
        {
            // Log error - telemetry will capture this
        }
    }

    /// <inheritdoc/>
    public async Task ResetToDefaultsAsync()
    {
        _configuration = LaunchyBarConfiguration.CreateDefault();
        await SaveAsync();
    }

    private static async Task<string> ReadFileAsync(string path)
    {
        using var reader = new StreamReader(path);
        return await reader.ReadToEndAsync();
    }

    private static async Task WriteFileAsync(string path, string content)
    {
        using var writer = new StreamWriter(path, false);
        await writer.WriteAsync(content);
    }
}
