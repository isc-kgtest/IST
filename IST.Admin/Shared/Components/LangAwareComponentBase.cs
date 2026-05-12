using IST.Admin.Services;
using Microsoft.AspNetCore.Components;

namespace IST.Admin.Shared.Components;

/// <summary>
/// База для компонентов, отображающих локализованные строки.
/// Подписывается на <see cref="LanguageService.Changed"/> и перерендеривается
/// при смене языка — без этого Blazor пропускает перерендер дочерних компонентов,
/// когда родитель вызывает StateHasChanged (параметры ведь не изменились).
/// </summary>
public abstract class LangAwareComponentBase : ComponentBase, IDisposable
{
    [Inject] protected LanguageService Lang { get; set; } = default!;

    protected override void OnInitialized() => Lang.Changed += OnLangChanged;

    private void OnLangChanged() => InvokeAsync(StateHasChanged);

    public virtual void Dispose() => Lang.Changed -= OnLangChanged;
}
