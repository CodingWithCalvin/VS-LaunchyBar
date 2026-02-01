namespace CodingWithCalvin.LaunchyBar.Models;

/// <summary>
/// Defines the type of action a launch item performs.
/// </summary>
public enum LaunchItemType
{
    /// <summary>
    /// Launch an external executable program.
    /// </summary>
    ExternalProgram,

    /// <summary>
    /// Toggle a Visual Studio tool window.
    /// </summary>
    ToolWindow,

    /// <summary>
    /// Execute a Visual Studio command by ID.
    /// </summary>
    VsCommand,

    /// <summary>
    /// User-defined custom action.
    /// </summary>
    CustomAction
}
