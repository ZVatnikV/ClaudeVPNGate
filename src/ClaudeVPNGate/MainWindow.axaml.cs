using Avalonia.Controls;
using Avalonia.Interactivity;
using ClaudeVPNGate.Services;
using FluentAvalonia.UI.Controls;

namespace ClaudeVPNGate;

public partial class MainWindow : Window
{
    private readonly GuardController _guard = App.Guard;
    private bool _allowClose;
    private string _lastLog = "";

    public MainWindow()
    {
        InitializeComponent();
        Loc.Lang = _guard.Settings.Language == "ru" ? "ru" : "en";
        Nav.SelectedItem = NavDash;
        Nav.SelectionChanged += (_, _) => Show(Nav.SelectedItem);
        Loc.Changed += ApplyText;
        _guard.Changed += () => Avalonia.Threading.Dispatcher.UIThread.Post(Render);
        Closing += OnClosing;
        ApplyText();
        FillSettings();
        Render();
        Opened += (_, _) => { _guard.Start(); RefreshAdapters(); };
    }

    private void Show(object? item)
    {
        PageDash.IsVisible = ReferenceEquals(item, NavDash);
        PageSet.IsVisible = ReferenceEquals(item, NavSet);
        PageAd.IsVisible = ReferenceEquals(item, NavAd);
        PageTitle.Text = Loc.T(ReferenceEquals(item, NavSet) ? "settings" : ReferenceEquals(item, NavAd) ? "adapters" : "dash");
        PageHint.Text = Loc.T(ReferenceEquals(item, NavSet) ? "hintset" : ReferenceEquals(item, NavAd) ? "hintad" : "hint");
        if (PageAd.IsVisible) RefreshAdapters();
    }

    private void ApplyText()
    {
        NavDash.Content = Loc.T("dash");
        NavSet.Content = Loc.T("settings");
        NavAd.Content = Loc.T("adapters");
        QuitButton.Content = Loc.T("quit");
        AdminBar.Title = Loc.T("adminT");
        AdminBar.Message = Loc.T("adminB");
        AdminButton.Content = Loc.T("adminBtn");
        RowVpn.Description = Loc.T("vpn") + " — " + Loc.T("vpnD");
        RowClaude.Description = Loc.T("claude") + " — " + Loc.T("claudeD");
        RowGuard.Description = Loc.T("guard") + " — " + Loc.T("guardD");
        RowAdapter.Description = Loc.T("adapter") + " — " + Loc.T("adapterD");
        EventsTitle.Text = Loc.T("events");
        LblInterval.Text = Loc.T("interval");
        LblKeys.Text = Loc.T("keys");
        LblIgnore.Text = Loc.T("ignore");
        LoginSwitch.Content = Loc.T("login");
        SaveButton.Content = Loc.T("save");
        SetNote.Text = Loc.T("note");
        ScanButton.Content = Loc.T("scan");
        Show(Nav.SelectedItem ?? NavDash);
        Render();
    }

    private void Render()
    {
        var s = _guard.Snapshot;
        var open = s.IsAdmin && s.TunnelUp && !s.Sealed;
        StateLabel.Text = !s.IsAdmin ? Loc.T("wait") : open ? Loc.T("open") : Loc.T("sealed");
        StateSub.Text = !s.IsAdmin ? Loc.T("subW") : open ? Loc.T("subO") : Loc.T("subS");
        AdminBar.IsOpen = !s.IsAdmin;
        RowVpn.Footer = s.TunnelUp ? Loc.T("on") : Loc.T("off");
        RowClaude.Footer = !s.IsAdmin ? Loc.T("arm") : s.Sealed || !s.TunnelUp ? Loc.T("gS") : Loc.T("gO");
        RowGuard.Footer = s.IsAdmin ? Loc.T("run") : Loc.T("arm");
        RowAdapter.Footer = string.IsNullOrEmpty(s.Adapter) ? "—" : s.Adapter;
        if (!string.IsNullOrWhiteSpace(s.Message) && s.Message != _lastLog)
        {
            _lastLog = s.Message;
            var line = $"{DateTime.Now:HH:mm:ss}  {s.Message}";
            if (!string.IsNullOrWhiteSpace(s.Error)) line += " (" + s.Error + ")";
            EventLog.Text = line + (string.IsNullOrEmpty(EventLog.Text) ? "" : "\n" + EventLog.Text);
        }
    }

    private void FillSettings()
    {
        var s = _guard.Settings;
        PollBox.Value = (decimal)s.PollIntervalSec;
        KeysBox.Text = string.Join(", ", s.CustomAdapterKeywords);
        IgnoreBox.Text = string.Join(", ", s.IgnoreAdapters);
        LoginSwitch.IsChecked = s.LaunchAtLogin;
    }

    private void RefreshAdapters()
    {
        _guard.Snapshot.GetType();
        var vpn = _guard.Snapshot.Adapter;
        AdapterList.Items.Clear();
        foreach (var row in VpnDetector.ListAdapters())
        {
            var mark = row.Name == vpn ? Loc.T("route") : row.IsUp ? Loc.T("up") : Loc.T("down");
            AdapterList.Items.Add($"{row.Name}   {mark}   {string.Join(' ', row.Addresses)}");
        }
    }

    private void OnLang(object? sender, RoutedEventArgs e)
    {
        Loc.Set(ReferenceEquals(sender, LangRu) ? "ru" : "en");
        _guard.Settings.Language = Loc.Lang;
        _guard.Settings.Save();
    }

    private void OnSave(object? sender, RoutedEventArgs e)
    {
        var s = _guard.Settings;
        s.PollIntervalSec = (double)(PollBox.Value ?? 0.8m);
        s.CustomAdapterKeywords = Split(KeysBox.Text);
        s.IgnoreAdapters = Split(IgnoreBox.Text);
        s.LaunchAtLogin = LoginSwitch.IsChecked == true;
        s.Language = Loc.Lang;
        _guard.Save(s);
        SaveNote.Text = Loc.T("saved");
    }

    private void OnScan(object? sender, RoutedEventArgs e) => RefreshAdapters();

    private void OnAdmin(object? sender, RoutedEventArgs e)
    {
        SingleInstance.Release();
        Privilege.RelaunchElevated();
        _allowClose = true;
        Close();
    }

    private void OnQuit(object? sender, RoutedEventArgs e)
    {
        _allowClose = true;
        _guard.SealForExit();
        Close();
    }

    private void OnClosing(object? sender, WindowClosingEventArgs e)
    {
        if (_allowClose) return;
        e.Cancel = true;
        WindowState = WindowState.Minimized;
    }

    private static List<string> Split(string? text)
        => (text ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
}
