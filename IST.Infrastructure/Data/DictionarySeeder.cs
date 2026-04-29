using IST.Core.Entities.Dictionaries;
using IST.Core.Entities.Dictionaries.Enums;
using Microsoft.EntityFrameworkCore;

namespace IST.Infrastructure.Data;

public static class DictionarySeeder
{
    public static async Task SeedNsiDictionariesAsync(AppDbContext context)
    {
        var existingDicts = await context.Dictionaries.Select(d => d.Slug).ToListAsync();

        var nsiList = new List<(string Name, string Slug, List<(string FieldKey, string DisplayName, DictionaryFieldType Type, int SortOrder)> Fields)>
        {
            ("Типы СЗИ", "szi_types", new()
            {
                ("code", "Код", DictionaryFieldType.Text, 1),
                ("name", "Наименование", DictionaryFieldType.Text, 2),
                ("description", "Описание", DictionaryFieldType.Text, 3)
            }),
            ("Уровни доверия", "trust_levels", new()
            {
                ("code", "Код", DictionaryFieldType.Text, 1),
                ("name", "Наименование", DictionaryFieldType.Text, 2),
                ("level", "Уровень", DictionaryFieldType.Number, 3),
                ("description", "Описание", DictionaryFieldType.Text, 4)
            }),
            ("Требования безопасности", "security_requirements", new()
            {
                ("code", "Код", DictionaryFieldType.Text, 1),
                ("name", "Наименование", DictionaryFieldType.Text, 2),
                ("regulatory_document", "НПА", DictionaryFieldType.Text, 3)
            }),
            ("Схемы сертификации", "certification_schemes", new()
            {
                ("code", "Код", DictionaryFieldType.Text, 1),
                ("name", "Наименование", DictionaryFieldType.Text, 2),
                ("description", "Описание", DictionaryFieldType.Text, 3)
            }),
            ("Согласующие органы", "approving_authorities", new()
            {
                ("short_name", "Краткое название", DictionaryFieldType.Text, 1),
                ("full_name", "Полное название", DictionaryFieldType.Text, 2),
                ("contact_info", "Контакты", DictionaryFieldType.Text, 3),
                ("website", "Сайт", DictionaryFieldType.Text, 4)
            }),
            ("Испытательные лаборатории", "testing_laboratories", new()
            {
                ("short_name", "Краткое название", DictionaryFieldType.Text, 1),
                ("full_name", "Полное название", DictionaryFieldType.Text, 2),
                ("accreditation_number", "№ аккредитации", DictionaryFieldType.Text, 3),
                ("is_accredited", "Аккредитована", DictionaryFieldType.Boolean, 4)
            }),
            ("Статусы заявок", "application_statuses", new()
            {
                ("code", "Код", DictionaryFieldType.Text, 1),
                ("name", "Наименование", DictionaryFieldType.Text, 2),
                ("sort_order", "Порядок", DictionaryFieldType.Number, 3)
            }),
            ("Статусы сертификатов", "certificate_statuses", new()
            {
                ("code", "Код", DictionaryFieldType.Text, 1),
                ("name", "Наименование", DictionaryFieldType.Text, 2),
                ("sort_order", "Порядок", DictionaryFieldType.Number, 3)
            }),
            ("Страны", "countries", new()
            {
                ("code", "ISO Код", DictionaryFieldType.Text, 1),
                ("name", "Название", DictionaryFieldType.Text, 2),
                ("name_en", "Название (EN)", DictionaryFieldType.Text, 3)
            }),
            ("Валюты", "currencies", new()
            {
                ("code", "ISO Код", DictionaryFieldType.Text, 1),
                ("name", "Название", DictionaryFieldType.Text, 2),
                ("symbol", "Символ", DictionaryFieldType.Text, 3)
            }),
            ("Области", "regions", new()
            {
                ("code", "Код", DictionaryFieldType.Text, 1),
                ("name", "Название", DictionaryFieldType.Text, 2)
            }),
            ("Районы", "districts", new()
            {
                ("code", "Код", DictionaryFieldType.Text, 1),
                ("name", "Название", DictionaryFieldType.Text, 2)
            })
        };

        bool anyAdded = false;

        foreach (var nsi in nsiList)
        {
            if (!existingDicts.Contains(nsi.Slug))
            {
                var dict = new DictionaryEntity
                {
                    Name = nsi.Name,
                    Slug = nsi.Slug,
                    Description = "Системный справочник (НСИ)"
                };

                foreach (var field in nsi.Fields)
                {
                    dict.Fields.Add(new DictionaryFieldEntity
                    {
                        FieldKey = field.FieldKey,
                        DisplayName = field.DisplayName,
                        FieldType = field.Type,
                        SortOrder = field.SortOrder,
                        IsRequired = true
                    });
                }

                context.Dictionaries.Add(dict);
                anyAdded = true;
            }
        }

        if (anyAdded)
        {
            await context.SaveChangesAsync();
        }
    }
}
