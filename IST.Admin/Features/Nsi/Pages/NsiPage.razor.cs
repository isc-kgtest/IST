using ActualLab.Fusion;
using ActualLab.Fusion.Blazor;
using IST.Contracts.Features.Dictionaries;
using IST.Shared.DTOs.Dictionaries;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace IST.Admin.Features.Nsi.Pages;

public partial class NsiPage : ComputedStateComponent<NsiPage.Model>
{
    public sealed record Model(List<DictionaryDto> Dictionaries, DictionaryDetailDto? Detail)
    {
        public static readonly Model Empty = new(new List<DictionaryDto>(), null);
    }

    [Inject] private IDictionaryQueries _dictQueries { get; set; } = default!;

    private bool _processing;
    private DictionaryDto? _selectedDictionary;
    private string _searchString = string.Empty;

    private Func<DictionaryDto, bool> _filter => x =>
    {
        if (string.IsNullOrWhiteSpace(_searchString)) return true;
        if (x.Name.Contains(_searchString, StringComparison.OrdinalIgnoreCase)) return true;
        if (x.Description?.Contains(_searchString, StringComparison.OrdinalIgnoreCase) == true) return true;
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
            // Фильтруем только системные НСИ справочники
            var nsi = all.Where(d => !d.IsDeleted && (d.Description?.Contains("НСИ") == true))
                         .OrderBy(d => d.Name)
                         .ToList();

            DictionaryDetailDto? detail = null;
            if (_selectedDictionary != null)
            {
                detail = await _dictQueries.GetDictionaryDetailAsync(_selectedDictionary.Id, cancellationToken);
            }

            return new Model(nsi, detail);
        }
        finally { _processing = false; }
    }

    private async Task RefreshAsync() => await State.Recompute();

    private void OpenDictionary(DictionaryDto dict)
    {
        _selectedDictionary = dict;
        _ = State.Recompute();
    }
}
