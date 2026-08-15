using CommunityToolkit.Mvvm.Input;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.StockAdjustments.GetStockAdjustments;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.StockAdjustments.Update;
using MediatR;

namespace GenoDev.BusinessTracker.Wpf.ViewModels.Materials;

public partial class EditStockAdjustmentViewModel(
    IMediator mediator,
    StockAdjustmentDto adjustment) : StockAdjustmentEditorViewModelBase(mediator)
{
    public event Action<EditorCloseResult>? RequestClose;

    public async Task InitializeAsync()
    {
        Date = adjustment.Date.ToDateTime(TimeOnly.MinValue);
        SelectedType = adjustment.ItemType;
        Quantity = adjustment.Amount;
        IsPrivate = adjustment.IsPrivate;
        Description = adjustment.Description;
        IsBusy = true;
        try { await InitializeOptionsAsync(adjustment.ItemId); }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task Save()
    {
        var input = CreateInput();
        if (input is null) return;
        ClearValidationErrors();
        IsBusy = true;
        try
        {
            await Mediator.Send(new UpdateStockAdjustmentCommand(
                adjustment.Id, DateOnly.FromDateTime(Date), input.ItemType, input.ItemId, input.Amount,
                input.IsPrivate, Description));
            RequestClose?.Invoke(EditorCloseResult.Saved());
        }
        catch (ApplicationLogic.Exceptions.RequestValidationException exception) { ApplyValidationErrors(exception); }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private void Cancel() => RequestClose?.Invoke(EditorCloseResult.Cancelled);
}
