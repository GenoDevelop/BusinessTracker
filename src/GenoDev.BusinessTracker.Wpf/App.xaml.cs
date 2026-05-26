using System;
using System.Windows;
using GenoDev.BusinessTracker.ApplicationLogic.Extensions;
using GenoDev.BusinessTracker.Infrastructure.Extensions;
using GenoDev.BusinessTracker.Wpf.ViewModels;
using GenoDev.BusinessTracker.Wpf.Views;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace GenoDev.BusinessTracker.Wpf;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private readonly IHost _host;

    public App()
    {
        _host = Host.CreateDefaultBuilder()
            .ConfigureAppConfiguration((context, builder) =>
            {
                builder.AddInfrastructureSettings();
            })
            .ConfigureServices((context, services) =>
            {
                ConfigureServices(context.Configuration, services);
            })
            .Build();
    }

    private void ConfigureServices(IConfiguration configuration, IServiceCollection services)
    {
        services.AddApplicationServices(configuration);
        services.AddInfrastructureServices(configuration);

        // ViewModels
        services.AddSingleton<MainViewModel>();
        services.AddTransient<ProductsViewModel>();
        services.AddTransient<SuppliersViewModel>();
        services.AddTransient<TaxRatesViewModel>();
        services.AddTransient<SalesViewModel>();
        services.AddTransient<ProductSuppliesViewModel>();

        // Views
        services.AddSingleton<MainWindow>(s => new MainWindow
        {
            DataContext = s.GetRequiredService<MainViewModel>()
        });
    }

    protected override async void OnStartup(StartupEventArgs e)
    {
        await _host.StartAsync();

        var mainWindow = _host.Services.GetRequiredService<MainWindow>();
        mainWindow.Show();

        base.OnStartup(e);
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        using (_host)
        {
            await _host.StopAsync(TimeSpan.FromSeconds(5));
        }

        base.OnExit(e);
    }
}

