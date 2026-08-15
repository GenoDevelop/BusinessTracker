using CommunityToolkit.Mvvm.Input;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.StockAdjustments.Create;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.StockAdjustments.GetOptions;
using MediatR;
using System.Collections.ObjectModel;

namespace GenoDev.BusinessTracker.Wpf.ViewModels.Materials;

public record StockAdjustmentDraftItem(StockAdjustmentOptionDto Option, double Amount, bool IsPrivate)
{
    public string Sign => Amount >= 0 ? "+" : "-";
    public double AbsoluteAmount => Math.Abs(Amount);
}

public partial class CreateStockAdjustmentsViewModel(IMediator mediator) : StockAdjustmentEditorViewModelBase(mediator)
{
    public ObservableCollection<StockAdjustmentDraftItem> Items { get; } = new();
    public event Action<EditorCloseResult>? RequestClose;

    public async Task InitializeAsync()
    {
        IsBusy = true;
        try { await InitializeOptionsAsync(); }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private void AddItem()
    {
        var input = CreateInput();
        if (input is null || SelectedOption is null) return;
        Items.Add(new StockAdjustmentDraftItem(SelectedOption, input.Amount, input.IsPrivate));
        SaveCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand]
    private void RemoveItem(StockAdjustmentDraftItem? item)
    {
        if (item is null) return;
        Items.Remove(item);
        SaveCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand(CanExecute = nameof(CanSave))]
    private async Task Save()
    {
        ClearValidationErrors();
        IsBusy = true;
        try
        {
            var inputs = Items.Select(x => new StockAdjustmentInput(
                x.Option.ItemType, x.Option.Id, x.Amount, x.IsPrivate)).ToList();
            var ids = await Mediator.Send(new CreateStockAdjustmentsCommand(
                DateOnly.FromDateTime(Date), inputs, Description));
            RequestClose?.Invoke(EditorCloseResult.Saved(ids.FirstOrDefault()));
        }
        catch (ApplicationLogic.Exceptions.RequestValidationException exception) { ApplyValidationErrors(exception); }
        finally { IsBusy = false; }
    }

    private bool CanSave() => Items.Count > 0;

    [RelayCommand]
    private void Cancel() => RequestClose?.Invoke(EditorCloseResult.Cancelled);
}
