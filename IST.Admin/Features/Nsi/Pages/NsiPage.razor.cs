using ActualLab.Fusion;
using ActualLab.Fusion.Blazor;
using IST.Contracts.Features.Dictionaries;
using IST.Shared.DTOs.Dictionaries;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace IST.Admin.Features.Nsi.Pages;

public partial class NsiPage : ComputedStateComponent<NsiPage.Model>
{
    public sealed record Model(List<DictionaryDto> Dictionaries)
    {
        public static readonly Model Empty = new(new List<DictionaryDto>());
    }

    [Inject] private IDictionaryQueries _dictQueries { get; set; } = default!;

    private bool _processing;
    private bool _loadingDetail;
    private DictionaryDto? _selectedDictionary;
    private DictionaryDetailDto? _detail;

    protected override ComputedState<Model>.Options GetStateOptions()
        => new() { InitialValue = Model.Empty, UpdateDelayer = FixedDelayer.Get(0) };

    protected override async Task<Model> ComputeState(CancellationToken cancellationToken)
    {
        _processing = true;
        try
        {
            var all = await _dictQueries.GetAllDictionariesAsync(cancellationToken);
            // Фильтруем только системные НСИ справочники
            var nsi = all.Where(d => !d.IsDeleted && (d.Description?.Contains("НСИ") == true))
                         .OrderBy(d => d.Name)
                         .ToList();
            return new Model(nsi);
        }
        finally { _processing = false; }
    }

    private async Task OpenDictionary(DictionaryDto dict)
    {
        _selectedDictionary = dict;
        await LoadDetail();
    }

    private async Task LoadDetail()
    {
        if (_selectedDictionary == null) return;
        
        _loadingDetail = true;
        try
        {
            _detail = await _dictQueries.GetDictionaryDetailAsync(_selectedDictionary.Id);
            StateHasChanged();
        }
        finally
        {
            _loadingDetail = false;
        }
    }
}
