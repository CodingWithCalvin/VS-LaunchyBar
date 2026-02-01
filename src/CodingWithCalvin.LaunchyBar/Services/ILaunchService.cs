using System.Threading.Tasks;
using CodingWithCalvin.LaunchyBar.Models;

namespace CodingWithCalvin.LaunchyBar.Services;

/// <summary>
/// Service for executing launch item actions.
/// </summary>
public interface ILaunchService
{
    /// <summary>
    /// Executes the action associated with a launch item.
    /// </summary>
    /// <param name="item">The launch item to execute.</param>
    Task ExecuteAsync(LaunchItem item);
}
