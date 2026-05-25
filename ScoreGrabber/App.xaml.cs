using System.IO;
using System.Windows;
using Application = System.Windows.Application;
using Microsoft.Extensions.Configuration;
using TecmoScoreGrabber.Configuration;

namespace TecmoScoreGrabber;

public partial class App : Application
{
    private Mutex? _mutex;

    protected override void OnStartup(StartupEventArgs e)
    {
        const string mutexName = @"Global\TecmoScoreGrabber_SingleInstance";
        _mutex = new Mutex(true, mutexName, out var created);
        if (!created)
        {
            System.Windows.MessageBox.Show("Tecmo Score Grabber is already running.", "Score Grabber", MessageBoxButton.OK, MessageBoxImage.Information);
            Shutdown();
            return;
        }

        var builder = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
#if DEBUG
        builder.AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: true);
#else
        builder.AddJsonFile("appsettings.Production.json", optional: false, reloadOnChange: true);
#endif
        var config = builder.Build();
        var options = new GrabberOptions();
        config.Bind(options);
        if (string.IsNullOrWhiteSpace(options.OpenAI.ApiKey))
        {
            var env = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
            if (!string.IsNullOrWhiteSpace(env))
                options.OpenAI.ApiKey = env;
        }

        Resources["GrabberOptions"] = options;

        base.OnStartup(e);
        MainWindow = new MainWindow(options);
        MainWindow.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _mutex?.ReleaseMutex();
        _mutex?.Dispose();
        base.OnExit(e);
    }
}
