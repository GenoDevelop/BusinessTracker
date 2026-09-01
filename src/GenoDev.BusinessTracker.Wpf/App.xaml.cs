using System.Windows;
using GenoDev.BusinessTracker.ApplicationLogic.Extensions;
using GenoDev.BusinessTracker.Infrastructure.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using GenoDev.BusinessTracker.Wpf.ViewModels;
using GenoDev.BusinessTracker.Wpf.ViewModels.Materials;
using GenoDev.BusinessTracker.Wpf.ViewModels.Notes;
using GenoDev.BusinessTracker.Wpf.ViewModels.Production;
using GenoDev.BusinessTracker.Wpf.ViewModels.Products;
using GenoDev.BusinessTracker.Wpf.ViewModels.Sales;
using GenoDev.BusinessTracker.Wpf.Services;

namespace GenoDev.BusinessTracker.Wpf;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private readonly IHost _host;
    private readonly GlobalExceptionHandler _globalExceptionHandler = new();

    public App()
    {
        DispatcherUnhandledException += _globalExceptionHandler.HandleDispatcherException;
        TaskScheduler.UnobservedTaskException += _globalExceptionHandler.HandleUnobservedTaskException;

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
        services.AddTransient<PackingMaterialListViewModel>();
        services.AddTransient<FixedAssetListViewModel>();
        services.AddTransient<SuppliersViewModel>();
        services.AddTransient<SuppliesViewModel>();
        services.AddTransient<StockAdjustmentsViewModel>();
        services.AddTransient<CreateStockAdjustmentsViewModel>();
        services.AddTransient<CreateSupplyViewModel>();
        services.AddTransient<CreateSupplierViewModel>();
        services.AddTransient<CreateMaterialViewModel>();
        services.AddTransient<ProductionViewModel>();
        services.AddTransient<ProductsViewModel>();
        services.AddTransient<ProductImagesViewModel>();
        services.AddTransient<CreateProductViewModel>();
        services.AddTransient<RecipesViewModel>();
        services.AddTransient<CreateRecipeViewModel>();
        services.AddTransient<AddRecipeMaterialViewModel>();
        services.AddTransient<ProductionListViewModel>();
        services.AddTransient<SalesViewModel>();
        services.AddTransient<OrdersViewModel>();
        services.AddTransient<MailingViewModel>();
        services.AddTransient<MailComposerViewModel>();
        services.AddHostedService<MailOutboxHostedService>();
        services.AddTransient<CreateMaterialVariantViewModel>();
        services.AddTransient<CreatePackingMaterialViewModel>();
        services.AddTransient<CreateFixedAssetViewModel>();
        services.AddTransient<NotesViewModel>();
        services.AddTransient<CreateNoteViewModel>();

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
        DispatcherUnhandledException -= _globalExceptionHandler.HandleDispatcherException;
        TaskScheduler.UnobservedTaskException -= _globalExceptionHandler.HandleUnobservedTaskException;

        using (_host)
        {
            await _host.StopAsync(TimeSpan.FromSeconds(5));
        }

        base.OnExit(e);
    }
}

