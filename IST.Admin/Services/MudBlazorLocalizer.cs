using Microsoft.Extensions.Localization;
using MudBlazor;

namespace IST.Admin.Services;

/// <summary>
/// Кастомный MudBlazor-локализатор. Переопределяет встроенные английские строки
/// (фильтры DataGrid, пейджер, кнопки диалогов и т.д.) на ru/kg в зависимости от
/// <see cref="LanguageService.CurrentLang"/>. Регистрируется как scoped — один на circuit.
///
/// Список ключей соответствует ресурсам MudBlazor.Resources.LanguageResource.resx
/// версии 9.4.0. Если ключа нет в нашем словаре — возвращаем ResourceNotFound=true,
/// и MudBlazor падает обратно на встроенный английский.
/// </summary>
public sealed class MudBlazorLocalizer : MudLocalizer
{
    private readonly LanguageService _lang;

    public MudBlazorLocalizer(LanguageService lang) => _lang = lang;

    public override LocalizedString this[string key]
    {
        get
        {
            if (Translations.TryGetValue(key, out var pair))
            {
                var value = _lang.IsKg ? pair.Kg : pair.Ru;
                return new LocalizedString(key, value, resourceNotFound: false);
            }
            return new LocalizedString(key, key, resourceNotFound: true);
        }
    }

    private static readonly Dictionary<string, (string Ru, string Kg)> Translations = new()
    {
        // ─── Converters ───
        ["Converter_ConversionError"]          = ("Ошибка преобразования: {0}",                "Конвертациялоо катасы: {0}"),
        ["Converter_ConversionFailed"]         = ("Не удалось преобразовать {0} в {1}: {2}",   "{0} мааниси {1} түрүнө айланган жок: {2}"),
        ["Converter_ConversionNotImplemented"] = ("Преобразование к типу {0} не реализовано",  "{0} түрүнө айландыруу ишке ашырылган эмес"),
        ["Converter_InvalidBoolean"]           = ("Не является логическим значением",          "Туура логикалык маани эмес"),
        ["Converter_InvalidDateTime"]          = ("Неверная дата/время",                       "Туура дата же убакыт эмес"),
        ["Converter_InvalidGUID"]              = ("Неверный GUID",                             "Туура GUID эмес"),
        ["Converter_InvalidNumber"]            = ("Неверное число",                            "Туура сан эмес"),
        ["Converter_InvalidTimeSpan"]          = ("Неверный временной интервал",               "Туура убакыт аралыгы эмес"),
        ["Converter_InvalidType"]              = ("Неверный {0}",                              "Туура эмес {0}"),
        ["Converter_NotValueOf"]               = ("Не является значением {0}",                 "{0} мааниси эмес"),
        ["Converter_UnableToConvert"]          = ("Невозможно преобразовать к {0} из типа {1}","{1} түрүнөн {0} түрүнө айландыруу мүмкүн эмес"),

        // ─── HeatMap ───
        ["HeatMap_Less"] = ("Меньше", "Азыраак"),
        ["HeatMap_More"] = ("Больше", "Көбүрөөк"),

        // ─── Common close/open ───
        ["MudAlert_Close"]    = ("Закрыть", "Жабуу"),
        ["MudChip_Close"]     = ("Закрыть", "Жабуу"),
        ["MudDialog_Close"]   = ("Закрыть", "Жабуу"),
        ["MudSnackbar_Close"] = ("Закрыть", "Жабуу"),

        // ─── DatePicker ───
        ["MudBaseDatePicker_NextMonth"] = ("Следующий месяц {0}",  "Кийинки ай {0}"),
        ["MudBaseDatePicker_NextYear"]  = ("Следующий год {0}",    "Кийинки жыл {0}"),
        ["MudBaseDatePicker_Open"]      = ("Открыть",              "Ачуу"),
        ["MudBaseDatePicker_PrevMonth"] = ("Предыдущий месяц {0}", "Мурунку ай {0}"),
        ["MudBaseDatePicker_PrevYear"]  = ("Предыдущий год {0}",   "Мурунку жыл {0}"),
        ["MudDateRangePicker_End"]      = ("Дата окончания",       "Аякталуу күнү"),
        ["MudDateRangePicker_Start"]    = ("Дата начала",          "Башталуу күнү"),
        ["MudTimePicker_Open"]          = ("Открыть",              "Ачуу"),

        // ─── Carousel ───
        ["MudCarousel_Index"]    = ("Слайд {0}", "Слайд {0}"),
        ["MudCarousel_Next"]     = ("Далее",     "Кийинки"),
        ["MudCarousel_Previous"] = ("Назад",     "Мурунку"),

        // ─── ColorPicker ───
        ["MudColorPicker_AlphaSlider"]  = ("Прозрачность",     "Тунуктук"),
        ["MudColorPicker_Close"]        = ("Закрыть",          "Жабуу"),
        ["MudColorPicker_GridView"]     = ("Сетка",            "Тор"),
        ["MudColorPicker_HideSwatches"] = ("Скрыть образцы",   "Үлгүлөрдү жашыруу"),
        ["MudColorPicker_HueSlider"]    = ("Оттенок",          "Түс"),
        ["MudColorPicker_ModeSwitch"]   = ("Сменить режим",    "Режимди өзгөртүү"),
        ["MudColorPicker_Open"]         = ("Открыть",          "Ачуу"),
        ["MudColorPicker_PaletteView"]  = ("Палитра",          "Палитра"),
        ["MudColorPicker_ShowSwatches"] = ("Показать образцы", "Үлгүлөрдү көрсөтүү"),
        ["MudColorPicker_SpectrumView"] = ("Спектр",           "Спектр"),

        // ─── DataGrid ───
        ["MudDataGrid_AddFilter"]            = ("Добавить фильтр",        "Чыпка кошуу"),
        ["MudDataGrid_Apply"]                = ("Применить",              "Колдонуу"),
        ["MudDataGrid_Cancel"]               = ("Отмена",                 "Жокко чыгаруу"),
        ["MudDataGrid_Clear"]                = ("Очистить",               "Тазалоо"),
        ["MudDataGrid_ClearFilter"]          = ("Очистить фильтр",        "Чыпканы тазалоо"),
        ["MudDataGrid_CollapseAllGroups"]    = ("Свернуть все группы",    "Бардык топторду жыйноо"),
        ["MudDataGrid_CollapseGroup"]        = ("Свернуть группу",        "Топту жыйноо"),
        ["MudDataGrid_Column"]               = ("Колонка",                "Тилке"),
        ["MudDataGrid_Columns"]              = ("Колонки",                "Тилкелер"),
        ["MudDataGrid_Contains"]             = ("содержит",               "камтыйт"),
        ["MudDataGrid_EndsWith"]             = ("заканчивается на",       "менен бүтөт"),
        ["MudDataGrid_Equals"]               = ("равно",                  "барабар"),
        ["MudDataGrid_EqualSign"]            = ("=",                      "="),
        ["MudDataGrid_ExpandAllGroups"]      = ("Развернуть все группы",  "Бардык топторду ачуу"),
        ["MudDataGrid_ExpandGroup"]          = ("Развернуть группу",      "Топту ачуу"),
        ["MudDataGrid_False"]                = ("нет",                    "жок"),
        ["MudDataGrid_Filter"]               = ("Фильтр",                 "Чыпка"),
        ["MudDataGrid_FilterValue"]          = ("Значение фильтра",       "Чыпка мааниси"),
        ["MudDataGrid_GreaterThanOrEqualSign"] = (">=",                   ">="),
        ["MudDataGrid_GreaterThanSign"]      = (">",                      ">"),
        ["MudDataGrid_Group"]                = ("Группа",                 "Топ"),
        ["MudDataGrid_Hide"]                 = ("Скрыть",                 "Жашыруу"),
        ["MudDataGrid_HideAll"]              = ("Скрыть все",             "Баарын жашыруу"),
        ["MudDataGrid_Is"]                   = ("равно",                  "барабар"),
        ["MudDataGrid_IsAfter"]              = ("после",                  "андан кийин"),
        ["MudDataGrid_IsBefore"]             = ("до",                     "андан мурун"),
        ["MudDataGrid_IsEmpty"]              = ("пусто",                  "бош"),
        ["MudDataGrid_IsNot"]                = ("не равно",               "барабар эмес"),
        ["MudDataGrid_IsNotEmpty"]           = ("не пусто",               "бош эмес"),
        ["MudDataGrid_IsOnOrAfter"]          = ("в эту дату или после",   "ушул күнү же кийин"),
        ["MudDataGrid_IsOnOrBefore"]         = ("в эту дату или ранее",   "ушул күнү же мурун"),
        ["MudDataGrid_LessThanOrEqualSign"]  = ("<=",                     "<="),
        ["MudDataGrid_LessThanSign"]         = ("<",                      "<"),
        ["MudDataGrid_Loading"]              = ("Загрузка...",            "Жүктөлүүдө..."),
        ["MudDataGrid_MoveDown"]             = ("Переместить вниз",       "Төмөн жылдыруу"),
        ["MudDataGrid_MoveUp"]               = ("Переместить вверх",      "Жогору жылдыруу"),
        ["MudDataGrid_NotContains"]          = ("не содержит",            "камтыбайт"),
        ["MudDataGrid_NotEquals"]            = ("не равно",               "барабар эмес"),
        ["MudDataGrid_NotEqualSign"]         = ("!=",                     "!="),
        ["MudDataGrid_OpenFilters"]          = ("Открыть фильтры",        "Чыпкаларды ачуу"),
        ["MudDataGrid_Operator"]             = ("Оператор",               "Оператор"),
        ["MudDataGrid_RefreshData"]          = ("Обновить",               "Жаңылоо"),
        ["MudDataGrid_RemoveFilter"]         = ("Удалить фильтр",         "Чыпканы өчүрүү"),
        ["MudDataGrid_Save"]                 = ("Сохранить",              "Сактоо"),
        ["MudDataGrid_SelectAllRows"]        = ("Выбрать все строки",     "Бардык саптарды тандоо"),
        ["MudDataGrid_SelectRow"]            = ("Выбрать строку",         "Сапты тандоо"),
        ["MudDataGrid_ShowAll"]              = ("Показать все",           "Баарын көрсөтүү"),
        ["MudDataGrid_ShowColumnOptions"]    = ("Настройки колонки",      "Тилке параметрлери"),
        ["MudDataGrid_Sort"]                 = ("Сортировка",             "Иргөө"),
        ["MudDataGrid_StartsWith"]           = ("начинается с",           "менен башталат"),
        ["MudDataGrid_ToggleGroupExpansion"] = ("Переключить группу",     "Топту которуу"),
        ["MudDataGrid_True"]                 = ("да",                     "ооба"),
        ["MudDataGrid_Ungroup"]              = ("Разгруппировать",        "Топтон чыгаруу"),
        ["MudDataGrid_Unsort"]               = ("Отменить сортировку",    "Иргөөнү жокко чыгаруу"),
        ["MudDataGrid_Value"]                = ("Значение",               "Мааниси"),

        // ─── DataGrid Pager ───
        ["MudDataGridPager_AllItems"]    = ("Все",                "Баары"),
        ["MudDataGridPager_FirstPage"]   = ("Первая страница",    "Биринчи бет"),
        ["MudDataGridPager_InfoFormat"]  = ("{0}-{1} из {2}",     "{2}: {0}-{1}"),
        ["MudDataGridPager_LastPage"]    = ("Последняя страница", "Акыркы бет"),
        ["MudDataGridPager_NextPage"]    = ("Следующая страница", "Кийинки бет"),
        ["MudDataGridPager_PreviousPage"]= ("Предыдущая страница","Мурунку бет"),
        ["MudDataGridPager_RowsPerPage"] = ("Строк на странице:", "Бетке саптар:"),

        // ─── ExitPrompt ───
        ["MudExitPrompt_Cancel"] = ("Отмена",                                                       "Жокко чыгаруу"),
        ["MudExitPrompt_Exit"]   = ("ОК",                                                           "ОК"),
        ["MudExitPrompt_Text"]   = ("Покинуть страницу? Внесённые изменения могут быть не сохранены.", "Беттен чыгасызбы? Киргизилген өзгөртүүлөр сакталбай калышы мүмкүн."),
        ["MudExitPrompt_Title"]  = ("Подтвердите переход",                                          "Өтүүнү ырастаңыз"),

        // ─── FileUpload ───
        ["MudFileUpload_Button"]                  = ("Выбрать файлы",                                        "Файлдарды тандоо"),
        ["MudFileUpload_DragAndDrop"]             = ("Перетащите файлы сюда или нажмите для выбора",         "Файлдарды бул жерге сүйрөп таштаңыз же тандоо үчүн басыңыз"),
        ["MudFileUpload_FileSizeError"]           = ("Файл '{0}' превышает максимально допустимый размер {1} байт.", "'{0}' файлы уруксат берилген {1} байт өлчөмүнөн ашты."),
        ["MudFileUpload_MaximumFileCountExceeded"]= ("Выбрано {0} файлов, превышено максимум {1}",            "{0} файл тандалды, бул {1} деген максимумдан ашты"),

        // ─── Input ───
        ["MudInput_Clear"]     = ("Очистить",  "Тазалоо"),
        ["MudInput_Decrement"] = ("Уменьшить", "Азайтуу"),
        ["MudInput_Increment"] = ("Увеличить", "Көбөйтүү"),

        // ─── NavGroup / Content nav ───
        ["MudNavGroup_ToggleExpand"]         = ("Переключить {0}", "{0} ачуу/жабуу"),
        ["MudPageContentNavigation_NavMenu"] = ("Содержание",      "Мазмуну"),

        // ─── Pagination ───
        ["MudPagination_CurrentPage"]  = ("Текущая страница {0}",  "Учурдагы бет {0}"),
        ["MudPagination_FirstPage"]    = ("Первая страница",       "Биринчи бет"),
        ["MudPagination_LastPage"]     = ("Последняя страница",    "Акыркы бет"),
        ["MudPagination_NextPage"]     = ("Следующая страница",    "Кийинки бет"),
        ["MudPagination_PageIndex"]    = ("Страница {0}",          "{0} бет"),
        ["MudPagination_PreviousPage"] = ("Предыдущая страница",   "Мурунку бет"),

        // ─── RangeInput ───
        ["MudRangeInput_End"]   = ("Конец",  "Аягы"),
        ["MudRangeInput_Start"] = ("Начало", "Башы"),

        // ─── Rating ───
        ["MudRatingItem_Label"] = ("Оценка {0}", "{0} баа"),

        // ─── Stepper ───
        ["MudStepper_Complete"] = ("Завершить", "Бүтүрүү"),
        ["MudStepper_Next"]     = ("Далее",     "Кийинки"),
        ["MudStepper_Previous"] = ("Назад",     "Мурунку"),
        ["MudStepper_Reset"]    = ("Сбросить",  "Кайра баштоо"),
        ["MudStepper_Skip"]     = ("Пропустить","Өткөрүп жиберүү"),

        // ─── Table ───
        ["MudTable_CancelEditRow"]   = ("Отменить",         "Жокко чыгаруу"),
        ["MudTable_CollapseGroup"]   = ("Свернуть группу",  "Топту жыйноо"),
        ["MudTable_CommitRow"]       = ("Сохранить",        "Сактоо"),
        ["MudTable_EditRow"]         = ("Редактировать",    "Түзөтүү"),
        ["MudTable_ExpandGroup"]     = ("Развернуть группу","Топту ачуу"),
        ["MudTable_Loading"]         = ("Загрузка...",      "Жүктөлүүдө..."),
        ["MudTablePager_FirstPage"]    = ("Первая страница",    "Биринчи бет"),
        ["MudTablePager_LastPage"]     = ("Последняя страница", "Акыркы бет"),
        ["MudTablePager_NextPage"]     = ("Следующая страница", "Кийинки бет"),
        ["MudTablePager_PreviousPage"] = ("Предыдущая страница","Мурунку бет"),

        // ─── Tabs ───
        ["MudTabs_AddTab"]      = ("Добавить вкладку",       "Өтмөк кошуу"),
        ["MudTabs_CloseTab"]    = ("Закрыть вкладку",        "Өтмөктү жабуу"),
        ["MudTabs_ScrollDown"]  = ("Прокрутить вкладки вниз","Өтмөктөрдү ылдый сыдыруу"),
        ["MudTabs_ScrollLeft"]  = ("Прокрутить влево",       "Солго сыдыруу"),
        ["MudTabs_ScrollRight"] = ("Прокрутить вправо",      "Оңго сыдыруу"),
        ["MudTabs_ScrollUp"]    = ("Прокрутить вверх",       "Жогору сыдыруу"),

        // ─── TreeView ───
        ["MudTreeView_CollapseItem"] = ("Свернуть",   "Жыйноо"),
        ["MudTreeView_ExpandItem"]   = ("Развернуть", "Ачуу"),
    };
}
