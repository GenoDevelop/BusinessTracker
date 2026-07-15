using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Suppliers;
using MediatR;
using System;
using System.Threading.Tasks;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Suppliers.Create;

namespace GenoDev.BusinessTracker.Wpf.ViewModels.Materials;

public partial class CreateSupplierViewModel(IMediator mediator) : ViewModelBase
{
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CreateCommand))]
    private string _name = string.Empty;

    [ObservableProperty]
    private string? _nip;

    [ObservableProperty]
    private string? _description;

    [ObservableProperty]
    private string? _websiteUrl;

    public event Action? RequestClose;

    [RelayCommand(CanExecute = nameof(CanCreate))]
    private async Task Create()
    {
        IsBusy = true;
        try
        {
            var command = new CreateSupplierCommand(Name, Nip, Description, WebsiteUrl);
            await mediator.Send(command);
            RequestClose?.Invoke();
        }
        finally
        {
            IsBusy = false;
        }
    }

    private bool CanCreate() => !string.IsNullOrWhiteSpace(Name);

    [RelayCommand]
    private void Cancel()
    {
        RequestClose?.Invoke();
    }
}
