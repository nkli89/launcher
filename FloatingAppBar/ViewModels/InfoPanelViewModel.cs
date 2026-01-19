using System;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Windows.Forms;
using Avalonia.Media;
using Avalonia.Threading;
using FloatingAppBar.Services;

namespace FloatingAppBar.ViewModels;

public sealed class InfoPanelViewModel : ViewModelBase
{
    private readonly DispatcherTimer _timer;
    private string _timeText = string.Empty;
    private string _timeOnlyText = string.Empty;
    private string _dateText = string.Empty;
    private string _batteryText = string.Empty;
    private string _networkText = string.Empty;
    private IImage? _networkIcon;
    private readonly IImage? _networkIconOnline;
    private readonly IImage? _networkIconOffline;
    private readonly string? _networkAdapter;

    public InfoPanelViewModel() : this(null)
    {
    }

    public InfoPanelViewModel(string? networkAdapter)
    {
        _networkAdapter = string.IsNullOrWhiteSpace(networkAdapter) ? null : networkAdapter.Trim();
        TimeIcon = IconLoader.Load("Assets/icons/clock.svg");
        BatteryIcon = IconLoader.Load("Assets/icons/battery.svg");
        _networkIconOnline = IconLoader.Load("Assets/icons/network_color.svg");
        _networkIconOffline = IconLoader.Load("Assets/icons/network_offline.svg");
        _networkIcon = _networkIconOnline;
        UpdateAll();
        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _timer.Tick += (_, _) => UpdateAll();
        _timer.Start();
    }

    public IImage? TimeIcon { get; }
    public IImage? BatteryIcon { get; }
    public IImage? NetworkIcon
    {
        get => _networkIcon;
        private set => SetProperty(ref _networkIcon, value);
    }

    public string TimeText
    {
        get => _timeText;
        private set => SetProperty(ref _timeText, value);
    }

    public string TimeOnlyText
    {
        get => _timeOnlyText;
        private set => SetProperty(ref _timeOnlyText, value);
    }

    public string DateText
    {
        get => _dateText;
        private set => SetProperty(ref _dateText, value);
    }

    public string BatteryText
    {
        get => _batteryText;
        private set => SetProperty(ref _batteryText, value);
    }

    public string NetworkText
    {
        get => _networkText;
        private set => SetProperty(ref _networkText, value);
    }

    private void UpdateAll()
    {
        var now = DateTime.Now;
        TimeText = now.ToString("g");
        TimeOnlyText = now.ToString("HH:mm");
        DateText = now.ToString("dd/MM/yyyy");
        BatteryText = GetBatteryText();
        var networkStatus = GetNetworkStatus(_networkAdapter);
        NetworkText = networkStatus.Text;
        NetworkIcon = networkStatus.IsOnline ? _networkIconOnline : _networkIconOffline;
    }

    private static string GetBatteryText()
    {
        if (!OperatingSystem.IsWindows())
        {
            return "Battery: N/A";
        }

        try
        {
            var status = SystemInformation.PowerStatus;
            var percent = (int)Math.Round(status.BatteryLifePercent * 100);
            var power = status.PowerLineStatus == PowerLineStatus.Online ? "AC" : "Battery";
            return $"Battery: {percent}% ({power})";
        }
        catch
        {
            return "Battery: N/A";
        }
    }

    private static NetworkStatus GetNetworkStatus(string? networkAdapter)
    {
        try
        {
            var interfaces = NetworkInterface.GetAllNetworkInterfaces()
                .Where(n => n.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                .ToList();

            NetworkInterface? active;
            if (!string.IsNullOrWhiteSpace(networkAdapter))
            {
                active = interfaces.FirstOrDefault(n => MatchesAdapter(n, networkAdapter));
                if (active is null)
                {
                    return new NetworkStatus($"Network: Adapter not found ({networkAdapter})", false);
                }

                if (active.OperationalStatus != OperationalStatus.Up)
                {
                    return new NetworkStatus($"Network: {active.Name} Offline", false);
                }
            }
            else
            {
                active = interfaces
                    .Where(n => n.OperationalStatus == OperationalStatus.Up)
                    .OrderByDescending(n => n.Speed)
                    .FirstOrDefault();
                if (active is null)
                {
                    return new NetworkStatus("Network: Offline", false);
                }
            }

            var ip = active.GetIPProperties().UnicastAddresses
                .FirstOrDefault(a => a.Address.AddressFamily == AddressFamily.InterNetwork)?
                .Address.ToString();

            var type = active.NetworkInterfaceType switch
            {
                NetworkInterfaceType.Wireless80211 => "Wi-Fi",
                NetworkInterfaceType.Ethernet => "Ethernet",
                _ => active.NetworkInterfaceType.ToString()
            };

            var baseText = string.IsNullOrWhiteSpace(networkAdapter)
                ? $"Network: {type}"
                : $"Network: {active.Name} ({type})";

            return ip is null
                ? new NetworkStatus(baseText, true)
                : new NetworkStatus($"{baseText} {ip}", true);
        }
        catch
        {
            return new NetworkStatus("Network: N/A", false);
        }
    }

    private static bool MatchesAdapter(NetworkInterface adapter, string selector)
    {
        return adapter.Id.Equals(selector, StringComparison.OrdinalIgnoreCase)
               || adapter.Name.Equals(selector, StringComparison.OrdinalIgnoreCase)
               || adapter.Description.Contains(selector, StringComparison.OrdinalIgnoreCase);
    }

    private sealed record NetworkStatus(string Text, bool IsOnline);
}
