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

    public InfoPanelViewModel()
    {
        TimeIcon = IconLoader.Load("Assets/icons/clock.svg");
        BatteryIcon = IconLoader.Load("Assets/icons/battery.svg");
        NetworkIcon = IconLoader.Load("Assets/icons/network.svg");
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
    public IImage? NetworkIcon { get; }

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
        NetworkText = GetNetworkText();
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

    private static string GetNetworkText()
    {
        try
        {
            var active = NetworkInterface.GetAllNetworkInterfaces()
                .Where(n => n.OperationalStatus == OperationalStatus.Up)
                .Where(n => n.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                .OrderByDescending(n => n.Speed)
                .FirstOrDefault();

            if (active is null)
            {
                return "Network: Offline";
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

            return ip is null ? $"Network: {type}" : $"Network: {type} {ip}";
        }
        catch
        {
            return "Network: N/A";
        }
    }
}
