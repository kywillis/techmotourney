using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using HorizontalAlignment = System.Windows.HorizontalAlignment;
using System.Windows.Forms;
using System.Windows.Threading;
using TecmoScoreGrabber.Configuration;
using TecmoScoreGrabber.Services;

namespace TecmoScoreGrabber;

public partial class MainWindow
{
    private readonly GrabberOptions _opt;
    private readonly ScoreGrabberEngine _engine;
    private readonly DispatcherTimer _timer;
    private readonly HttpClient _http;
    private readonly RollingFileLogger _fileLog;
    private readonly SleepPrevention _sleep = new();
    private readonly System.Windows.Forms.NotifyIcon _tray;
    private bool _paused;
    private bool _loadingMonitors;
    public ObservableCollection<GrabberLogEntry> LogEntries { get; } = new();

    public MainWindow(GrabberOptions opt)
    {
        InitializeComponent();
        _opt = opt;
        DataContext = this;
        LogItems.ItemsSource = LogEntries;

        InitializeMonitorCombo();

        var baseUrl = opt.ApiBaseUrl.TrimEnd('/') + "/";
        _http = new HttpClient { BaseAddress = new Uri(baseUrl), Timeout = TimeSpan.FromMinutes(2) };

        var logDir = Path.Combine(AppContext.BaseDirectory, "logs");
        _fileLog = new RollingFileLogger(logDir, opt.LogMaxBytes);

        var capture = new ScreenCaptureService();
        _engine = new ScoreGrabberEngine(opt, capture, _http, _fileLog);
        _engine.OnLog += OnEngineLog;
        _engine.OnPhase += OnPhase;
        _engine.OnSaveResultDialog += ShowTopMostDialogAsync;
        _engine.OnTieGameSaved += (_, _) => { /* future: notify admin */ };

        var refPath = Path.IsPathRooted(opt.Capture.FssReferenceImagePath)
            ? opt.Capture.FssReferenceImagePath
            : Path.Combine(AppContext.BaseDirectory, opt.Capture.FssReferenceImagePath);
        try
        {
            _engine.LoadReference(refPath);
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Could not load FSS reference image:\n{refPath}\n\n{ex.Message}", "Score Grabber", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(Math.Max(1, opt.Capture.IntervalSeconds)) };
        _timer.Tick += async (_, _) => await RunTickSafeAsync(ignorePause: false);

        _tray = new System.Windows.Forms.NotifyIcon
        {
            Icon = System.Drawing.SystemIcons.Application,
            Visible = true,
            Text = "Tecmo Score Grabber"
        };
        _tray.DoubleClick += (_, _) => { Show(); WindowState = WindowState.Normal; Activate(); };
        var menu = new System.Windows.Forms.ContextMenuStrip();
        menu.Items.Add("Show", null, (_, _) => { Show(); WindowState = WindowState.Normal; });
        menu.Items.Add("Pause / Resume", null, (_, _) => TogglePause());
        menu.Items.Add("Exit", null, (_, _) => Close());
        _tray.ContextMenuStrip = menu;

        Closing += MainWindow_OnClosing;
        Loaded += (_, _) => StartLoop();

        UpdateConfigSnapshot();
    }

    private void InitializeMonitorCombo()
    {
        _loadingMonitors = true;
        try
        {
            var screens = Screen.AllScreens;
            var items = new List<MonitorDisplayItem>();
            for (var i = 0; i < screens.Length; i++)
            {
                var s = screens[i];
                var primary = s.Primary ? " — primary" : "";
                var name = $"{i}: {s.DeviceName.Trim('\\')}{primary} — {s.Bounds.Width}×{s.Bounds.Height} @ ({s.Bounds.Left},{s.Bounds.Top})";
                items.Add(new MonitorDisplayItem { Index = i, DisplayName = name });
            }

            if (items.Count == 0)
            {
                items.Add(new MonitorDisplayItem { Index = 0, DisplayName = "0: (no displays)" });
            }

            MonitorCombo.ItemsSource = items;

            var want = _opt.Capture.MonitorIndex;
            if (want < 0 || want >= items.Count)
                want = 0;
            _opt.Capture.MonitorIndex = want;

            MonitorCombo.SelectedItem = items.First(x => x.Index == want);
        }
        finally
        {
            _loadingMonitors = false;
        }
    }

    private void MonitorCombo_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_loadingMonitors || MonitorCombo.SelectedItem is not MonitorDisplayItem item)
            return;

        _opt.Capture.MonitorIndex = item.Index;
        UpdateConfigSnapshot();
        TryPersistMonitorIndex();
    }

    private void TryPersistMonitorIndex()
    {
        try
        {
            var path = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
            if (!File.Exists(path))
                return;

            var text = File.ReadAllText(path);
            var node = JsonNode.Parse(text);
            if (node is null)
                return;
            if (node["Capture"] is not JsonObject cap)
            {
                cap = new JsonObject();
                node["Capture"] = cap;
            }

            cap["MonitorIndex"] = _opt.Capture.MonitorIndex;
            var opts = new JsonSerializerOptions { WriteIndented = true };
            File.WriteAllText(path, node.ToJsonString(opts));
        }
        catch
        {
            // optional persistence
        }
    }

    private void StartLoop()
    {
        _sleep.Start();
        _timer.Start();
    }

    private async Task RunTickSafeAsync(bool ignorePause = false)
    {
        try
        {
            await _engine.TickAsync(CancellationToken.None, ignorePause);
            Dispatcher.Invoke(() =>
            {
                UpdateUiCounters();
                UpdateLastCaptureImage();
                UpdateVisionInputPreviewImage();
            });
        }
        catch (Exception ex)
        {
            Dispatcher.Invoke(() => AddLog(new GrabberLogEntry
            {
                LocalTime = DateTime.Now,
                Severity = "Error",
                Message = ex.Message,
                Details = ex.ToString()
            }));
        }
    }

    private void UpdateLastCaptureImage()
    {
        using var bmp = _engine.CloneLastScreenGrabForPreview();
        if (bmp == null)
        {
            LastCaptureImage.Source = null;
            return;
        }

        LastCaptureImage.Source = BitmapSourceHelper.FromBitmap(bmp);
    }

    private void UpdateVisionInputPreviewImage()
    {
        using var bmp = _engine.CloneLastVisionInputPreview();
        if (bmp == null)
        {
            VisionInputImage.Source = null;
            VisionInputDimsText.Text = "No vision input yet (FSS must match above threshold).";
        }
        else
        {
            VisionInputImage.Source = BitmapSourceHelper.FromBitmap(bmp);
            VisionInputDimsText.Text = $"Sent to LLM: {bmp.Width}×{bmp.Height} px (full capture, grayscale). Preview below max 500 wide.";
        }
    }

    private void UpdateUiCounters()
    {
        var lastCompare = _engine.LastFssSimilarity.HasValue
            ? $"{Math.Round(_engine.LastFssSimilarity.Value * 100)}%"
            : "—";
        CountersText.Text =
            $"Screenshots: {_engine.Screenshots} · last compare: {lastCompare} · FSS hits: {_engine.FssHits} · LLM: {_engine.LlmCalls} · Saves OK: {_engine.SavesOk} · Failed: {_engine.SavesFailed} · No match: {_engine.NoMatch}";
        ApiPingText.Text = _engine.LastApiSuccessUtc.HasValue
            ? $"Last API success (UTC): {_engine.LastApiSuccessUtc:O}"
            : "Last API success (UTC): —";
        LastSaveText.Text = _engine.LastSaveHttpStatus.HasValue
            ? $"Last save: HTTP {_engine.LastSaveHttpStatus} — {_engine.LastSaveMessage}"
            : "Last save: —";
    }

    private void UpdateConfigSnapshot()
    {
        var key = string.IsNullOrWhiteSpace(_opt.OpenAI.ApiKey) ? "(not set)" : "(set)";
        ConfigSnapshotText.Text =
            $"API: {_http.BaseAddress} · Interval: {_opt.Capture.IntervalSeconds}s · Monitor: {_opt.Capture.MonitorIndex} · FSS threshold: {_opt.Capture.FssSimilarityThreshold:0.###} · OpenAI key {key}";
    }

    private void OnEngineLog(GrabberLogEntry e) => Dispatcher.Invoke(() => AddLog(e));

    private void AddLog(GrabberLogEntry e)
    {
        LogEntries.Insert(0, e);
        while (LogEntries.Count > 500)
            LogEntries.RemoveAt(LogEntries.Count - 1);
    }

    private void OnPhase(GrabberPhase phase)
    {
        Dispatcher.Invoke(() =>
        {
            PhaseText.Text = "Phase: " + phase switch
            {
                GrabberPhase.WaitingForFinalScoreScreen => "Waiting for final score screen",
                GrabberPhase.Sampling => "Sampling",
                GrabberPhase.LlmParsing => "LLM parsing",
                GrabberPhase.ApiMatching => "Matching API game",
                GrabberPhase.Saving => "Saving",
                GrabberPhase.CooldownAfterSave => "Cooldown after save",
                GrabberPhase.TieAwaitingRematch => "Tie saved — game in progress (OT / final save manually)",
                GrabberPhase.NoMatchAwaitingClear => "Game not found — leave final score screen to retry",
                _ => phase.ToString()
            };
            SubStatusText.Text = _paused
                ? "Status: Paused"
                : "Status: Running";
        });
    }

    /// <summary>Headline + body after first blank line for styled score dialogs ("Game Saved" / "Game Not Found").</summary>
    private static bool TrySplitStyledGameDialog(string message, out string headline, out string body)
    {
        headline = "";
        body = message;
        const string sepCrLf = "\r\n\r\n";
        const string sepLf = "\n\n";
        var i = message.IndexOf(sepCrLf, StringComparison.Ordinal);
        var sepLen = sepCrLf.Length;
        if (i < 0)
        {
            i = message.IndexOf(sepLf, StringComparison.Ordinal);
            sepLen = sepLf.Length;
        }
        if (i <= 0)
            return false;
        var head = message[..i].Trim();
        if (!string.Equals(head, "Game Saved", StringComparison.Ordinal)
            && !string.Equals(head, "Game Not Found", StringComparison.Ordinal))
            return false;
        headline = head;
        body = message[(i + sepLen)..];
        return true;
    }

    private static TextBlock BuildStyledGameDialogTextBlock(string message)
    {
        var baseSize = System.Windows.SystemFonts.MessageFontSize;
        var tb = new TextBlock
        {
            TextWrapping = TextWrapping.Wrap,
            TextAlignment = TextAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center,
            MaxWidth = 400,
            Foreground = System.Windows.Media.Brushes.White,
            FontSize = baseSize
        };
        if (!TrySplitStyledGameDialog(message, out var headline, out var body))
        {
            tb.Text = message;
            return tb;
        }

        // Was 2× base; +20% → 2.4×
        tb.Inlines.Add(new Run(headline) { FontWeight = FontWeights.Bold, FontSize = baseSize * 2.4 });
        tb.Inlines.Add(new LineBreak());
        tb.Inlines.Add(new LineBreak());
        tb.Inlines.Add(new Run(body));
        return tb;
    }

    private static string AutoCloseDialogCaption(int remainingSeconds) =>
        $"This window is closing in {remainingSeconds} seconds";

    private static TextBlock BuildStyledAutoCloseHintTextBlock(int remainingSeconds)
    {
        var baseSize = System.Windows.SystemFonts.MessageFontSize;
        return new TextBlock
        {
            Text = AutoCloseDialogCaption(remainingSeconds),
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 2, 16, 0),
            Foreground = System.Windows.Media.Brushes.White,
            FontSize = baseSize,
            TextWrapping = TextWrapping.Wrap,
            MaxWidth = 260
        };
    }

    private static void ApplyStyledGameSavedChrome(System.Windows.Controls.Button btn)
    {
        btn.Background = System.Windows.Media.Brushes.White;
        btn.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x18, 0x63, 0xD6));
        btn.BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x12, 0x4a, 0xA8));
    }

    private async Task ShowTopMostDialogAsync(string title, string message, bool gameSavedLayout)
    {
        await Dispatcher.InvokeAsync(() =>
        {
            var w = new Window
            {
                Title = title,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                Topmost = true,
                ShowInTaskbar = true
            };
            if (gameSavedLayout)
            {
                w.MinWidth = 420;
                w.Width = 440;
                w.SizeToContent = SizeToContent.Height;
                // Match tecmo-tourney `styles.less` @background / @text-color
                var webBlue = System.Windows.Media.Color.FromRgb(0x18, 0x63, 0xD6);
                w.Background = new System.Windows.Media.SolidColorBrush(webBlue);
            }
            else
            {
                w.Width = 440;
                w.Height = 220;
            }

            var panel = new StackPanel
            {
                Margin = new Thickness(20, 20, 20, 16),
                HorizontalAlignment = gameSavedLayout ? HorizontalAlignment.Center : HorizontalAlignment.Stretch,
                Background = gameSavedLayout ? System.Windows.Media.Brushes.Transparent : null
            };
            panel.Children.Add(
                gameSavedLayout
                    ? BuildStyledGameDialogTextBlock(message)
                    : new TextBlock
                    {
                        Text = message,
                        TextWrapping = TextWrapping.Wrap,
                        TextAlignment = TextAlignment.Left,
                        HorizontalAlignment = HorizontalAlignment.Stretch,
                        MaxWidth = double.PositiveInfinity
                    });
            System.Windows.Controls.Button btn;
            if (gameSavedLayout)
            {
                var autoSecs = Math.Max(0, _opt.Ui.StyledDialogAutoCloseSeconds);
                if (autoSecs > 0)
                {
                    TextBlock countdownTb = BuildStyledAutoCloseHintTextBlock(autoSecs);

                    btn = new System.Windows.Controls.Button { Content = "Keep Open", Margin = new Thickness(0, 2, 0, 0) };
                    ApplyStyledGameSavedChrome(btn);

                    var footer = new StackPanel
                    {
                        Orientation = System.Windows.Controls.Orientation.Horizontal,
                        Margin = new Thickness(0, 18, 0, 0),
                        HorizontalAlignment = HorizontalAlignment.Center
                    };
                    footer.Children.Add(countdownTb);
                    footer.Children.Add(btn);
                    panel.Children.Add(footer);

                    var pinned = false;
                    var remaining = autoSecs;

                    DispatcherTimer timer = new() { Interval = TimeSpan.FromSeconds(1) };

                    RoutedEventHandler? keepOrCloseClick = null;
                    keepOrCloseClick = (_, _) =>
                    {
                        if (!pinned)
                        {
                            pinned = true;
                            timer.Stop();
                            countdownTb.Visibility = Visibility.Collapsed;
                            btn.Click -= keepOrCloseClick;
                            btn.Content = "Close";
                            btn.Click += (_, _) => w.Close();
                        }
                    };

                    timer.Tick += (_, _) =>
                    {
                        remaining--;
                        if (remaining <= 0)
                        {
                            timer.Stop();
                            w.Close();
                            return;
                        }

                        countdownTb.Text = AutoCloseDialogCaption(remaining);
                    };

                    btn.Click += keepOrCloseClick;
                    w.Closed += (_, _) => timer.Stop();
                    timer.Start();
                }
                else
                {
                    btn = new System.Windows.Controls.Button
                    {
                        Content = "Close",
                        Width = 90,
                        Margin = new Thickness(0, 18, 0, 0),
                        HorizontalAlignment = HorizontalAlignment.Center
                    };
                    ApplyStyledGameSavedChrome(btn);
                    btn.Click += (_, _) => w.Close();
                    panel.Children.Add(btn);
                }
            }
            else
            {
                btn = new System.Windows.Controls.Button
                {
                    Content = "OK",
                    Width = 90,
                    Margin = new Thickness(0, 18, 0, 0),
                    HorizontalAlignment = HorizontalAlignment.Right
                };
                btn.Click += (_, _) => w.Close();
                panel.Children.Add(btn);
            }

            w.Content = panel;
            w.ShowDialog();
        });
    }

    private async void CaptureNow_OnClick(object sender, RoutedEventArgs e)
    {
        CaptureNowBtn.IsEnabled = false;
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(3)).ConfigureAwait(true);
            await RunTickSafeAsync(ignorePause: true).ConfigureAwait(true);
        }
        finally
        {
            CaptureNowBtn.IsEnabled = true;
        }
    }

    private void PauseBtn_OnClick(object sender, RoutedEventArgs e) => TogglePause();

    private void TogglePause()
    {
        _paused = !_paused;
        _engine.SetPaused(_paused);
        PauseBtn.Content = _paused ? "Resume" : "Pause";
        SubStatusText.Text = _paused ? "Status: Paused" : "Status: Running";
        if (_paused)
            _sleep.Stop();
        else
            _sleep.Start();
    }

    private void ExportLog_OnClick(object sender, RoutedEventArgs e)
    {
        var dlg = new Microsoft.Win32.SaveFileDialog { Filter = "Log files (*.log)|*.log|All files (*.*)|*.*", FileName = "score-grabber-export.log" };
        if (dlg.ShowDialog() != true)
            return;
        var lines = LogEntries.Select(x =>
            $"{x.LocalTime:O}\t{x.Severity}\t{x.CorrelationId}\t{x.Message}\t{x.Details?.Replace('\t', ' ')}");
        File.WriteAllLines(dlg.FileName, lines);
    }

    private void OpenDebug_OnClick(object sender, RoutedEventArgs e)
    {
        var path = Path.Combine(AppContext.BaseDirectory, _opt.Capture.DebugFolder);
        Directory.CreateDirectory(path);
        Process.Start(new ProcessStartInfo { FileName = path, UseShellExecute = true });
    }

    private void MainWindow_OnClosing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        if (!_paused)
        {
            var r = System.Windows.MessageBox.Show("Capture is active. Stop and exit?", "Score Grabber", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (r != MessageBoxResult.Yes)
            {
                e.Cancel = true;
                return;
            }
        }
        _timer.Stop();
        _sleep.Dispose();
        _tray.Visible = false;
        _tray.Dispose();
        _engine.Dispose();
        _http.Dispose();
    }
}
