using System.Windows;
using GenoDev.BusinessTracker.ApplicationLogic.Extensions;
using GenoDev.BusinessTracker.Infrastructure.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using GenoDev.BusinessTracker.Wpf.ViewModels;
using GenoDev.BusinessTracker.Wpf.ViewModels.Materials;
using GenoDev.BusinessTracker.Wpf.ViewModels.Production;
using GenoDev.BusinessTracker.Wpf.ViewModels.Sales;

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
        services.AddTransient<MaterialsViewModel>();
        services.AddTransient<MaterialListViewModel>();
        services.AddTransient<SuppliersViewModel>();
        services.AddTransient<MaterialSuppliesViewModel>();
        services.AddTransient<CreateSupplierViewModel>();
        services.AddTransient<ProductionViewModel>();
        services.AddTransient<ProductsViewModel>();
        services.AddTransient<RecipesViewModel>();
        services.AddTransient<ProductionListViewModel>();
        services.AddTransient<SalesViewModel>();
        services.AddTransient<OrdersViewModel>();

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

