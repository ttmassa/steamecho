using SteamEcho.Core.Services;
using System.Net.NetworkInformation;
using System.Timers;

namespace SteamEcho.App.Services;

public class InternetService : IInternetService
{
    private bool _hasInternet = true;
    private bool _isCheckingInternet = false;
    private readonly System.Timers.Timer _timer;
    public bool HasInternet
    {
        get => _hasInternet;
        private set
        {
            if (_hasInternet != value)
            {
                _hasInternet = value;
                InternetStatusChanged?.Invoke(_hasInternet);
            }
        }
    }

    public event Action<bool>? InternetStatusChanged;

    public InternetService()
    {
        _timer = new System.Timers.Timer(5000);
        _timer.Elapsed += async (s, e) => await CheckAndUpdateStatusAsync();
        _timer.AutoReset = true;
        NetworkChange.NetworkAvailabilityChanged += OnNetworkAvailabilityChanged;
        NetworkChange.NetworkAddressChanged += OnNetworkAddressChanged;
    }

    public void StartMonitoring()
    {
        _timer.Start();
        _ = CheckAndUpdateStatusAsync();
    }

    public void StopMonitoring()
    {
        _timer.Stop();
    }

    private void OnNetworkAvailabilityChanged(object? sender, NetworkAvailabilityEventArgs e)
    {
        _ = CheckAndUpdateStatusAsync();
    }

    private void OnNetworkAddressChanged(object? sender, EventArgs e)
    {
        _ = CheckAndUpdateStatusAsync();
    }

    private async Task CheckAndUpdateStatusAsync()
    {
        if (_isCheckingInternet) return;
        _isCheckingInternet = true;
        try
        {
            HasInternet = await CheckInternetConnectivityAsync();
        }
        finally
        {
            _isCheckingInternet = false;
        }
    }

    public async Task<bool> CheckInternetConnectivityAsync()
    {
        if (!NetworkInterface.GetIsNetworkAvailable())
            return false;

        try
        {
            using var ping = new Ping();
            var reply = await ping.SendPingAsync("8.8.8.8", 3000);
            return reply.Status == IPStatus.Success;
        }
        catch
        {
            return false;
        }
    }
}