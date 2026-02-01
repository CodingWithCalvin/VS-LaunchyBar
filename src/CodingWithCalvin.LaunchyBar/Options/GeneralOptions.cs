using System.ComponentModel;
using System.Runtime.InteropServices;
using Community.VisualStudio.Toolkit;
using Microsoft.VisualStudio.Shell;

namespace CodingWithCalvin.LaunchyBar.Options;

internal partial class OptionsProvider
{
    [ComVisible(true)]
    public class GeneralOptionsPage : BaseOptionPage<GeneralOptions> { }
}

public class GeneralOptions : BaseOptionModel<GeneralOptions>
{
    [Category("General")]
    [DisplayName("Bar Width")]
    [Description("Width of the LaunchyBar in pixels.")]
    [DefaultValue(48)]
    public int BarWidth { get; set; } = 48;

    [Category("General")]
    [DisplayName("Show on Startup")]
    [Description("Automatically show LaunchyBar when Visual Studio starts.")]
    [DefaultValue(true)]
    public bool ShowOnStartup { get; set; } = true;

    [Category("General")]
    [DisplayName("Icon Size")]
    [Description("Size of icons in the LaunchyBar (pixels).")]
    [DefaultValue(24)]
    public int IconSize { get; set; } = 24;
}
