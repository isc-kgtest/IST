using Microsoft.AspNetCore.Http;
using Microsoft.JSInterop;

namespace IST.Admin.Services;

/// <summary>
/// Простая словарная локализация без .resx / IStringLocalizer.
/// Истинный источник — <c>localStorage['lang']</c> (см. <c>lang-storage.js</c>).
/// Параллельно язык зеркалится в cookie <c>lang</c>, чтобы SSR pre-render
/// сразу отрендерился на правильном языке (иначе виден RU→KG flash).
/// Регистрируется как scoped — один инстанс на circuit.
/// </summary>
public class LanguageService
{
    private readonly IJSRuntime _js;
    private string _lang = "ru";
    private bool _initialized;

    public LanguageService(IJSRuntime js, IHttpContextAccessor httpContextAccessor)
    {
        _js = js;
        // Во время SSR pre-render HttpContext доступен и в нём уже есть cookie.
        // Подхватываем язык сразу, чтобы первая отрисовка была на верном языке.
        var cookieLang = httpContextAccessor.HttpContext?.Request.Cookies["lang"];
        if (cookieLang is "ru" or "kg") _lang = cookieLang;
    }

    public string CurrentLang => _lang;
    public bool IsKg => _lang == "kg";
    public bool IsRu => _lang == "ru";

    public async Task InitAsync()
    {
        if (_initialized) return;
        _initialized = true;
        try
        {
            var lang = await _js.InvokeAsync<string>("langStorage.get");
            if (lang is "ru" or "kg") _lang = lang;
        }
        catch { /* JS interop недоступен при SSR — оставляем дефолт */ }
    }

    public async Task ToggleAsync()
    {
        _lang = _lang == "ru" ? "kg" : "ru";
        await _js.InvokeVoidAsync("langStorage.set", _lang);
    }

    /// <summary>Прямая двуязычная строка — для одноразовых текстов.</summary>
    public string T(string ru, string kg)
        => _lang == "kg" && !string.IsNullOrEmpty(kg) ? kg : ru;

    /// <summary>Локализация по ключу из словаря <see cref="Strings"/>.</summary>
    public string L(string key)
        => Strings.TryGetValue(key, out var p) ? (_lang == "kg" ? p.Kg : p.Ru) : key;

    private static readonly Dictionary<string, (string Ru, string Kg)> Strings = new()
    {
        // AppBar
        ["app_title"]         = ("Админ-панель",                   "Башкаруу панели"),
        ["app_toggle_theme"]  = ("Сменить тему",                   "Теманы өзгөртүү"),
        ["app_logout"]        = ("Выйти",                          "Чыгуу"),

        // Drawer header
        ["drawer_brand"]      = ("IST Admin",                      "IST Admin"),

        // Nav menu — основные
        ["nav_dashboard"]     = ("Дашборд",                        "Башкы такта"),
        ["nav_nsi"]           = ("Управление НСИ",                 "НСИ башкаруу"),
        ["nav_dictionaries"]  = ("Справочники",                    "Справочниктер"),
        ["nav_requests"]      = ("Управление заявками",            "Арыздарды башкаруу"),
        ["nav_trials"]        = ("Управление испытаниями",         "Сыноолорду башкаруу"),
        ["nav_authorities"]   = ("Взаимодействие с органами",      "Органдар менен өз ара аракеттенүү"),
        ["nav_reports"]       = ("Отчеты и аналитика",             "Отчёттор жана аналитика"),
        ["nav_exchange"]      = ("Обмен данными",                  "Маалымат алмашуу"),

        // Nav menu — система
        ["nav_system"]        = ("СИСТЕМА",                        "СИСТЕМА"),
        ["nav_organization"]  = ("Оргструктура",                   "Уюм структурасы"),
        ["nav_access"]        = ("Управление доступом",            "Жетүүнү башкаруу"),
        ["nav_permissions"]   = ("Привилегии ролей",               "Ролдордун укуктары"),
        ["nav_audit"]         = ("Журнал безопасности",            "Коопсуздук журналы"),
    };
}
