using ActualLab.Fusion;
using ActualLab.Fusion.Blazor;
using IST.Admin.Features.Dictionaries.Components;
using IST.Contracts.Features.Dictionaries;
using IST.Contracts.Features.Dictionaries.Commands;
using IST.Shared.DTOs.Dictionaries;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor;
using IST.UI.Components;

namespace IST.Admin.Features.Dictionaries.Pages;

public partial class DictionariesPage : ComputedStateComponent<DictionariesPage.Model>
{
    public sealed record Model(List<DictionaryDto> Dictionaries, DictionaryDetailDto? Detail)
    {
        public static readonly Model Empty = new(new List<DictionaryDto>(), null);
    }

    [Inject] private IDictionaryCommands _dictCommands { get; set; } = default!;
    [Inject] private IDictionaryQueries _dictQueries { get; set; } = default!;
    [Inject] private IST.Admin.Auth.SessionAccessor _session { get; set; } = default!;

    private bool _processing;
    private string _searchString = string.Empty;
    private DictionaryDto? _selectedDictionary;

    private Func<DictionaryDto, bool> _filter => x =>
    {
        if (string.IsNullOrWhiteSpace(_searchString)) return true;
        if (x.Name.Contains(_searchString, StringComparison.OrdinalIgnoreCase)) return true;
        if (x.Slug.Contains(_searchString, StringComparison.OrdinalIgnoreCase)) return true;
        return false;
    };

    protected override ComputedState<Model>.Options GetStateOptions()
        => new() { InitialValue = Model.Empty, UpdateDelayer = FixedDelayer.Get(0) };

    protected override async Task<Model> ComputeState(CancellationToken cancellationToken)
    {
        _processing = true;
        try
        {
            var all = await _dictQueries.GetAllDictionariesAsync(cancellationToken);
            var dictionaries = all.Where(d => !d.IsDeleted).OrderByDescending(d => d.CreatedAt).ToList();

            DictionaryDetailDto? detail = null;
            if (_selectedDictionary != null)
            {
                detail = await _dictQueries.GetDictionaryDetailAsync(_selectedDictionary.Id, cancellationToken);
            }

            return new Model(dictionaries, detail);
        }
        finally { _processing = false; }
    }

    private async Task RefreshAsync() => await State.Recompute();

    // ═══ Open detail ═══

    private void OpenDictionary(DictionaryDto dict)
    {
        _selectedDictionary = dict;
        _ = State.Recompute();
    }

    private void CloseDictionary()
    {
        _selectedDictionary = null;
        _ = State.Recompute();
    }

    // ═══ Create dictionary ═══

    private async Task ShowCreateDictionaryDialog()
    {
        var parameters = new DialogParameters
        {
            ["ChildContent"] = (RenderFragment)(builder =>
            {
                builder.OpenComponent<CreateDictionaryForm>(0);
                builder.CloseComponent();
            })
        };
        var options = new DialogOptions { CloseButton = true, MaxWidth = MaxWidth.Medium };
        var dialogRef = await _dialogService.ShowAsync<MudDynamicDialog>("Создать справочник", parameters, options);
        await dialogRef.Result;
    }

    // ═══ Edit dictionary ═══

    private async Task ShowEditDictionaryDialog(DictionaryDto dict)
    {
        var parameters = new DialogParameters
        {
            ["ChildContent"] = (RenderFragment)(builder =>
            {
                builder.OpenComponent<EditDictionaryForm>(0);
                builder.AddAttribute(1, "Dictionary", dict);
                builder.CloseComponent();
            })
        };
        var options = new DialogOptions { CloseButton = true, MaxWidth = MaxWidth.Medium };
        var dialogRef = await _dialogService.ShowAsync<MudDynamicDialog>("Редактировать справочник", parameters, options);
        await dialogRef.Result;
    }

    // ═══ Delete dictionary ═══

    private async Task ShowDeleteDictionaryDialog(Guid id, string name)
    {
        IDialogReference dialogRef = default!;
        void Cancel() => dialogRef.Close(DialogResult.Cancel());
        void Confirm() => dialogRef.Close(DialogResult.Ok(true));

        var parameters = new DialogParameters
        {
            ["ButtonOk"] = (RenderFragment)(builder =>
            {
                builder.OpenComponent<MudButton>(0);
                builder.AddAttribute(1, "Color", Color.Error);
                builder.AddAttribute(2, "Variant", Variant.Filled);
                builder.AddAttribute(3, "OnClick", EventCallback.Factory.Create<MouseEventArgs>(this, Confirm));
                builder.AddAttribute(4, "ChildContent", (RenderFragment)(b => b.AddContent(5, "Удалить")));
                builder.CloseComponent();
            }),
            ["ButtonCancel"] = (RenderFragment)(builder =>
            {
                builder.OpenComponent<MudButton>(0);
                builder.AddAttribute(1, "Color", Color.Default);
                builder.AddAttribute(2, "OnClick", EventCallback.Factory.Create<MouseEventArgs>(this, Cancel));
                builder.AddAttribute(3, "ChildContent", (RenderFragment)(b => b.AddContent(4, "Отмена")));
                builder.CloseComponent();
            }),
        };

        var options = new DialogOptions { CloseButton = false, MaxWidth = MaxWidth.Small };
        dialogRef = await _dialogService.ShowAsync<MudDynamicDialog>($"Удалить справочник \"{name}\"?", parameters, options);
        var result = await dialogRef.Result;
        if (result.Canceled) return;

        try
        {
            var res = await _dictCommands.DeleteDictionaryAsync(new DeleteDictionaryCommand(await _session.GetAsync(), id));
            if (res.Status) { _snackbar.Add($"Справочник удалён", Severity.Success); await RefreshAsync(); }
            else _snackbar.Add($"Ошибка: {res.StatusMessage}", Severity.Warning);
        }
        catch (Exception ex) { _snackbar.Add($"Ошибка: {ex.Message}", Severity.Error); }
    }

    // ═══ Field dialogs ═══

    private async Task ShowAddFieldDialog()
    {
        if (_selectedDictionary == null) return;
        var parameters = new DialogParameters
        {
            ["ChildContent"] = (RenderFragment)(builder =>
            {
                builder.OpenComponent<DictionaryFieldForm>(0);
                builder.AddAttribute(1, "DictionaryId", _selectedDictionary.Id);
                builder.CloseComponent();
            })
        };
        var options = new DialogOptions { CloseButton = true, MaxWidth = MaxWidth.Small };
        var dialogRef = await _dialogService.ShowAsync<MudDynamicDialog>("Добавить поле", parameters, options);
        var result = await dialogRef.Result;
        if (!result.Canceled) await RefreshAsync();
    }

    private async Task ShowEditFieldDialog(DictionaryFieldDto field)
    {
        var parameters = new DialogParameters
        {
            ["ChildContent"] = (RenderFragment)(builder =>
            {
                builder.OpenComponent<DictionaryFieldForm>(0);
                builder.AddAttribute(1, "DictionaryId", field.DictionaryId);
                builder.AddAttribute(2, "Field", field);
                builder.CloseComponent();
            })
        };
        var options = new DialogOptions { CloseButton = true, MaxWidth = MaxWidth.Small };
        var dialogRef = await _dialogService.ShowAsync<MudDynamicDialog>("Редактировать поле", parameters, options);
        var result = await dialogRef.Result;
        if (!result.Canceled) await RefreshAsync();
    }

    private async Task ShowDeleteFieldDialog(Guid fieldId, string fieldName)
    {
        IDialogReference dialogRef = default!;
        void Cancel() => dialogRef.Close(DialogResult.Cancel());
        void Confirm() => dialogRef.Close(DialogResult.Ok(true));

        var parameters = new DialogParameters
        {
            ["ButtonOk"] = (RenderFragment)(builder =>
            {
                builder.OpenComponent<MudButton>(0);
                builder.AddAttribute(1, "Color", Color.Error);
                builder.AddAttribute(2, "Variant", Variant.Filled);
                builder.AddAttribute(3, "OnClick", EventCallback.Factory.Create<MouseEventArgs>(this, Confirm));
                builder.AddAttribute(4, "ChildContent", (RenderFragment)(b => b.AddContent(5, "Удалить")));
                builder.CloseComponent();
            }),
            ["ButtonCancel"] = (RenderFragment)(builder =>
            {
                builder.OpenComponent<MudButton>(0);
                builder.AddAttribute(1, "Color", Color.Default);
                builder.AddAttribute(2, "OnClick", EventCallback.Factory.Create<MouseEventArgs>(this, Cancel));
                builder.AddAttribute(3, "ChildContent", (RenderFragment)(b => b.AddContent(4, "Отмена")));
                builder.CloseComponent();
            }),
        };

        var options = new DialogOptions { CloseButton = false, MaxWidth = MaxWidth.Small };
        dialogRef = await _dialogService.ShowAsync<MudDynamicDialog>($"Удалить поле \"{fieldName}\"?", parameters, options);
        var result = await dialogRef.Result;
        if (result.Canceled) return;

        try
        {
            var res = await _dictCommands.DeleteFieldAsync(new DeleteDictionaryFieldCommand(await _session.GetAsync(), fieldId));
            if (res.Status) { _snackbar.Add("Поле удалено", Severity.Success); await RefreshAsync(); }
            else _snackbar.Add($"Ошибка: {res.StatusMessage}", Severity.Warning);
        }
        catch (Exception ex) { _snackbar.Add($"Ошибка: {ex.Message}", Severity.Error); }
    }
}
