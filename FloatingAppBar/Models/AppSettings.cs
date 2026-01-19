namespace FloatingAppBar.Models;

public sealed class AppSettings
{
    public string BarShape { get; set; } = "rounded";
    public double CornerRadius { get; set; } = 10;
    public string? NetworkAdapter { get; set; }
    public string? NetworkAdapterWifi { get; set; }
    public string? NetworkAdapterLan { get; set; }
}
