using ActualLab.CommandR;
using ActualLab.CommandR.Configuration;
using ActualLab.Fusion;
using ActualLab.Fusion.EntityFramework;
using ClosedXML.Excel;
using IST.Contracts.Features.Dictionaries;
using IST.Contracts.Features.Dictionaries.Commands;
using IST.Core.Entities.Dictionaries;
using IST.Core.Entities.Dictionaries.Enums;
using IST.Infrastructure.Data;
using IST.Shared.DTOs.Common;
using IST.Shared.DTOs.Dictionaries;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace IST.Services.Features.Dictionaries;

public class DictionaryCommands : IDictionaryCommands
{
    private readonly DbHub<AppDbContext> _dbHub;
    private readonly IDictionaryQueries _queries;
    private readonly IMapper _mapper;

    public DictionaryCommands(DbHub<AppDbContext> dbHub, IDictionaryQueries queries, IMapper mapper)
    {
        _dbHub = dbHub;
        _queries = queries;
        _mapper = mapper;
    }

    [CommandHandler]
    public virtual async Task<ResponseDTO<DictionaryDto>> CreateDictionaryAsync(CreateDictionaryCommand command, CancellationToken cancellationToken = default)
    {
        if (Invalidation.IsActive)
        {
            _ = _queries.GetAllDictionariesAsync(default);
            _ = _queries.GetDictionaryBySlugAsync(command.Request.Slug, default);
            return default!;
        }

        await using var dbContext = await _dbHub.CreateOperationDbContext(cancellationToken);
        
        var exists = await dbContext.Dictionaries.AnyAsync(d => d.Slug == command.Request.Slug, cancellationToken);
        if (exists) return new ResponseDTO<DictionaryDto> { Status = false, StatusMessage = "Справочник с таким кодом уже существует" };

        var entity = new DictionaryEntity
        {
            Name = command.Request.Name,
            Description = command.Request.Description,
            Slug = command.Request.Slug
        };

        dbContext.Dictionaries.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new ResponseDTO<DictionaryDto> { Status = true, Data = _mapper.Map<DictionaryDto>(entity) };
    }

    [CommandHandler]
    public virtual async Task<ResponseDTO<DictionaryDto>> UpdateDictionaryAsync(UpdateDictionaryCommand command, CancellationToken cancellationToken = default)
    {
        if (Invalidation.IsActive)
        {
            _ = _queries.GetAllDictionariesAsync(default);
            _ = _queries.GetDictionaryByIdAsync(command.Request.Id, default);
            _ = _queries.GetDictionaryDetailAsync(command.Request.Id, default);
            return default!;
        }

        await using var dbContext = await _dbHub.CreateOperationDbContext(cancellationToken);
        
        var entity = await dbContext.Dictionaries.FirstOrDefaultAsync(d => d.Id == command.Request.Id, cancellationToken);
        if (entity == null) return new ResponseDTO<DictionaryDto> { Status = false, StatusMessage = "Справочник не найден" };

        entity.Name = command.Request.Name;
        entity.Description = command.Request.Description;
        entity.Type = command.Request.Type;
        // Slug обычно не меняют, но если нужно:
        // entity.Slug = command.Slug;

        await dbContext.SaveChangesAsync(cancellationToken);

        return new ResponseDTO<DictionaryDto> { Status = true, Data = _mapper.Map<DictionaryDto>(entity) };
    }

    [CommandHandler]
    public virtual async Task<ResponseDTO<string>> DeleteDictionaryAsync(DeleteDictionaryCommand command, CancellationToken cancellationToken = default)
    {
        if (Invalidation.IsActive)
        {
            _ = _queries.GetAllDictionariesAsync(default);
            _ = _queries.GetDictionaryByIdAsync(command.DictionaryId, default);
            _ = _queries.GetDictionaryDetailAsync(command.DictionaryId, default);
            return default!;
        }

        await using var dbContext = await _dbHub.CreateOperationDbContext(cancellationToken);
        
        var entity = await dbContext.Dictionaries.FirstOrDefaultAsync(d => d.Id == command.DictionaryId, cancellationToken);
        if (entity == null) return new ResponseDTO<string> { Status = false, StatusMessage = "Справочник не найден" };

        dbContext.Dictionaries.Remove(entity);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new ResponseDTO<string> { Status = true, Data = "Удалено" };
    }

    [CommandHandler]
    public virtual async Task<ResponseDTO<DictionaryFieldDto>> SaveFieldAsync(SaveDictionaryFieldCommand command, CancellationToken cancellationToken = default)
    {
        var req = command.Request;
        if (Invalidation.IsActive)
        {
            _ = _queries.GetFieldsByDictionaryIdAsync(command.DictionaryId, default);
            _ = _queries.GetDictionaryDetailAsync(command.DictionaryId, default);
            return default!;
        }

        await using var dbContext = await _dbHub.CreateOperationDbContext(cancellationToken);
        
        DictionaryFieldEntity entity;
        if (req.Id.HasValue && req.Id.Value != Guid.Empty)
        {
            var existing = await dbContext.DictionaryFields.FirstOrDefaultAsync(f => f.Id == req.Id.Value, cancellationToken);
            if (existing == null) return new ResponseDTO<DictionaryFieldDto> { Status = false, StatusMessage = "Поле не найдено" };
            entity = existing;
        }
        else
        {
            entity = new DictionaryFieldEntity { DictionaryId = command.DictionaryId };
            dbContext.DictionaryFields.Add(entity);
        }

        entity.FieldKey = req.FieldKey;
        entity.DisplayName = req.DisplayName;
        entity.FieldType = req.FieldType;
        entity.IsRequired = req.IsRequired;
        entity.SortOrder = req.SortOrder;

        await dbContext.SaveChangesAsync(cancellationToken);

        return new ResponseDTO<DictionaryFieldDto> { Status = true, Data = _mapper.Map<DictionaryFieldDto>(entity) };
    }

    [CommandHandler]
    public virtual async Task<ResponseDTO<string>> DeleteFieldAsync(DeleteDictionaryFieldCommand command, CancellationToken cancellationToken = default)
    {
        var dbContextForFind = await _dbHub.CreateDbContext(cancellationToken);
        var field = await dbContextForFind.DictionaryFields.AsNoTracking().FirstOrDefaultAsync(f => f.Id == command.FieldId, cancellationToken);
        var dictId = field?.DictionaryId ?? Guid.Empty;

        if (Invalidation.IsActive)
        {
            if (dictId != Guid.Empty)
            {
                _ = _queries.GetFieldsByDictionaryIdAsync(dictId, default);
                _ = _queries.GetDictionaryDetailAsync(dictId, default);
            }
            return default!;
        }

        await using var dbContext = await _dbHub.CreateOperationDbContext(cancellationToken);
        var entity = await dbContext.DictionaryFields.FirstOrDefaultAsync(f => f.Id == command.FieldId, cancellationToken);
        if (entity == null) return new ResponseDTO<string> { Status = false, StatusMessage = "Поле не найдено" };

        dbContext.DictionaryFields.Remove(entity);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new ResponseDTO<string> { Status = true, Data = "Удалено" };
    }

    [CommandHandler]
    public virtual async Task<ResponseDTO<DictionaryRecordDto>> SaveRecordAsync(SaveDictionaryRecordCommand command, CancellationToken cancellationToken = default)
    {
        var req = command.Request;
        if (Invalidation.IsActive)
        {
            _ = _queries.GetRecordsByDictionaryIdAsync(req.DictionaryId, default);
            _ = _queries.GetDictionaryDetailAsync(req.DictionaryId, default);
            if (req.Id.HasValue) _ = _queries.GetRecordByIdAsync(req.Id.Value, default);
            return default!;
        }

        await using var dbContext = await _dbHub.CreateOperationDbContext(cancellationToken);
        
        DictionaryRecordEntity entity;
        if (req.Id.HasValue && req.Id.Value != Guid.Empty)
        {
            var existing = await dbContext.DictionaryRecords.FirstOrDefaultAsync(r => r.Id == req.Id.Value, cancellationToken);
            if (existing == null) return new ResponseDTO<DictionaryRecordDto> { Status = false, StatusMessage = "Запись не найдена" };
            entity = existing;
        }
        else
        {
            entity = new DictionaryRecordEntity { DictionaryId = req.DictionaryId };
            dbContext.DictionaryRecords.Add(entity);
        }

        entity.Data = req.Data;

        await dbContext.SaveChangesAsync(cancellationToken);

        return new ResponseDTO<DictionaryRecordDto> { Status = true, Data = _mapper.Map<DictionaryRecordDto>(entity) };
    }

    [CommandHandler]
    public virtual async Task<ResponseDTO<string>> DeleteRecordAsync(DeleteDictionaryRecordCommand command, CancellationToken cancellationToken = default)
    {
        var dbContextForFind = await _dbHub.CreateDbContext(cancellationToken);
        var record = await dbContextForFind.DictionaryRecords.AsNoTracking().FirstOrDefaultAsync(r => r.Id == command.RecordId, cancellationToken);
        var dictId = record?.DictionaryId ?? Guid.Empty;

        if (Invalidation.IsActive)
        {
            if (dictId != Guid.Empty)
            {
                _ = _queries.GetRecordsByDictionaryIdAsync(dictId, default);
                _ = _queries.GetDictionaryDetailAsync(dictId, default);
            }
            _ = _queries.GetRecordByIdAsync(command.RecordId, default);
            return default!;
        }

        await using var dbContext = await _dbHub.CreateOperationDbContext(cancellationToken);
        var entity = await dbContext.DictionaryRecords.FirstOrDefaultAsync(r => r.Id == command.RecordId, cancellationToken);
        if (entity == null) return new ResponseDTO<string> { Status = false, StatusMessage = "Запись не найдена" };

        dbContext.DictionaryRecords.Remove(entity);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new ResponseDTO<string> { Status = true, Data = "Удалено" };
    }

    [CommandHandler]
    public virtual async Task<ResponseDTO<ImportResult>> ImportRecordsAsync(ImportDictionaryRecordsCommand command, CancellationToken cancellationToken = default)
    {
        if (Invalidation.IsActive)
        {
            _ = _queries.GetRecordsByDictionaryIdAsync(command.DictionaryId, default);
            _ = _queries.GetDictionaryDetailAsync(command.DictionaryId, default);
            return default!;
        }

        await using var dbContext = await _dbHub.CreateOperationDbContext(cancellationToken);

        var fields = await dbContext.DictionaryFields.AsNoTracking()
            .Where(f => f.DictionaryId == command.DictionaryId && !f.IsDeleted)
            .OrderBy(f => f.SortOrder)
            .ToListAsync(cancellationToken);

        if (fields.Count == 0)
            return new ResponseDTO<ImportResult> { Status = false, StatusMessage = "Структура полей справочника не задана — импорт невозможен" };

        List<string> headers;
        List<List<string>> dataRows;
        try
        {
            var ext = Path.GetExtension(command.FileName).ToLowerInvariant();
            if (ext == ".xlsx") (headers, dataRows) = ParseXlsx(command.FileContent);
            else if (ext == ".csv") (headers, dataRows) = ParseCsv(command.FileContent);
            else return new ResponseDTO<ImportResult> { Status = false, StatusMessage = "Неподдерживаемый формат файла (ожидается .xlsx или .csv)" };
        }
        catch (Exception ex)
        {
            return new ResponseDTO<ImportResult> { Status = false, StatusMessage = $"Ошибка чтения файла: {ex.Message}" };
        }

        if (headers.Count == 0)
            return new ResponseDTO<ImportResult> { Status = false, StatusMessage = "В файле не найдена строка заголовков" };

        // Соответствие колонок к полям: сначала по FieldKey, потом по DisplayName (case-insensitive)
        var columnToField = new DictionaryFieldEntity?[headers.Count];
        for (var i = 0; i < headers.Count; i++)
        {
            var h = headers[i].Trim();
            if (string.IsNullOrEmpty(h)) continue;
            columnToField[i] =
                fields.FirstOrDefault(f => string.Equals(f.FieldKey, h, StringComparison.OrdinalIgnoreCase))
                ?? fields.FirstOrDefault(f => string.Equals(f.DisplayName, h, StringComparison.OrdinalIgnoreCase));
        }

        var result = new ImportResult();
        var rowNumber = 1; // строка заголовков
        foreach (var row in dataRows)
        {
            rowNumber++;
            if (row.All(string.IsNullOrWhiteSpace)) continue;

            var values = new Dictionary<string, JsonNode?>();
            var rowErrors = new List<string>();

            for (var col = 0; col < headers.Count; col++)
            {
                var field = columnToField[col];
                if (field == null) continue;

                var raw = col < row.Count ? row[col] : string.Empty;
                if (string.IsNullOrWhiteSpace(raw))
                {
                    if (field.IsRequired) rowErrors.Add($"поле «{field.DisplayName}» обязательно");
                    continue;
                }

                if (TryConvertValue(raw, field.FieldType, out var node, out var convertError))
                    values[field.FieldKey] = node;
                else
                    rowErrors.Add($"поле «{field.DisplayName}»: {convertError}");
            }

            // Проверяем обязательные поля, которых вообще не было в файле
            foreach (var f in fields.Where(f => f.IsRequired))
            {
                if (!values.ContainsKey(f.FieldKey) && !rowErrors.Any(e => e.Contains($"«{f.DisplayName}»")))
                    rowErrors.Add($"поле «{f.DisplayName}» обязательно");
            }

            if (rowErrors.Count > 0)
            {
                result.SkippedCount++;
                if (result.Errors.Count < 50) // лимит, чтобы не раздувать ответ
                    result.Errors.Add($"Строка {rowNumber}: {string.Join("; ", rowErrors)}");
                continue;
            }

            var json = JsonSerializer.Serialize(values);
            dbContext.DictionaryRecords.Add(new DictionaryRecordEntity
            {
                DictionaryId = command.DictionaryId,
                Data = json
            });
            result.ImportedCount++;
        }

        if (result.ImportedCount > 0)
            await dbContext.SaveChangesAsync(cancellationToken);

        return new ResponseDTO<ImportResult>
        {
            Status = true,
            Data = result,
            StatusMessage = result.SkippedCount > 0
                ? $"Импортировано: {result.ImportedCount}, пропущено: {result.SkippedCount}"
                : $"Импортировано: {result.ImportedCount}"
        };
    }

    [CommandHandler]
    public virtual async Task<ResponseDTO<ExportResult>> ExportRecordsAsync(ExportDictionaryRecordsCommand command, CancellationToken cancellationToken = default)
    {
        if (Invalidation.IsActive) return default!;

        await using var dbContext = await _dbHub.CreateDbContext(cancellationToken);

        var dict = await dbContext.Dictionaries.AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == command.DictionaryId, cancellationToken);
        if (dict == null) return new ResponseDTO<ExportResult> { Status = false, StatusMessage = "Справочник не найден" };

        var fields = await dbContext.DictionaryFields.AsNoTracking()
            .Where(f => f.DictionaryId == command.DictionaryId && !f.IsDeleted)
            .OrderBy(f => f.SortOrder)
            .ToListAsync(cancellationToken);

        var records = await dbContext.DictionaryRecords.AsNoTracking()
            .Where(r => r.DictionaryId == command.DictionaryId && !r.IsDeleted)
            .ToListAsync(cancellationToken);

        var format = (command.Format ?? "xlsx").ToLowerInvariant();
        var safeSlug = string.IsNullOrWhiteSpace(dict.Slug) ? command.DictionaryId.ToString() : dict.Slug;

        byte[] bytes;
        string contentType;
        string fileName;

        if (format == "csv")
        {
            bytes = BuildCsv(fields, records);
            contentType = "text/csv; charset=utf-8";
            fileName = $"{safeSlug}.csv";
        }
        else
        {
            bytes = BuildXlsx(fields, records, dict.Name);
            contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
            fileName = $"{safeSlug}.xlsx";
        }

        return new ResponseDTO<ExportResult>
        {
            Status = true,
            Data = new ExportResult { FileName = fileName, ContentType = contentType, FileContent = bytes }
        };
    }

    // ── Парсинг файлов ────────────────────────────────────────────────────────

    private static (List<string> headers, List<List<string>> rows) ParseXlsx(byte[] content)
    {
        using var ms = new MemoryStream(content);
        using var wb = new XLWorkbook(ms);
        var ws = wb.Worksheets.FirstOrDefault() ?? throw new InvalidOperationException("В файле нет листов");
        var range = ws.RangeUsed();
        if (range == null) return (new(), new());

        var rows = range.RowsUsed().ToList();
        if (rows.Count == 0) return (new(), new());

        var colCount = rows.Max(r => r.LastCellUsed()?.Address.ColumnNumber ?? 0);
        var headerRow = rows[0];
        var headers = new List<string>(colCount);
        for (var c = 1; c <= colCount; c++)
            headers.Add(headerRow.Cell(c).GetString().Trim());

        var dataRows = new List<List<string>>(rows.Count - 1);
        for (var r = 1; r < rows.Count; r++)
        {
            var row = new List<string>(colCount);
            for (var c = 1; c <= colCount; c++)
            {
                var cell = rows[r].Cell(c);
                // Для дат и чисел используем форматированную строку, чтобы не зависеть от локали
                if (cell.DataType == XLDataType.DateTime)
                    row.Add(cell.GetDateTime().ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture));
                else if (cell.DataType == XLDataType.Number)
                    row.Add(cell.GetDouble().ToString("R", CultureInfo.InvariantCulture));
                else if (cell.DataType == XLDataType.Boolean)
                    row.Add(cell.GetBoolean() ? "true" : "false");
                else
                    row.Add(cell.GetString());
            }
            dataRows.Add(row);
        }
        return (headers, dataRows);
    }

    private static (List<string> headers, List<List<string>> rows) ParseCsv(byte[] content)
    {
        // Снимаем BOM, если есть
        var text = content.Length >= 3 && content[0] == 0xEF && content[1] == 0xBB && content[2] == 0xBF
            ? Encoding.UTF8.GetString(content, 3, content.Length - 3)
            : Encoding.UTF8.GetString(content);

        var lines = SplitCsvLines(text);
        if (lines.Count == 0) return (new(), new());

        // Автодетект разделителя по первой непустой строке: `;` или `,`
        var sep = lines[0].Count(c => c == ';') > lines[0].Count(c => c == ',') ? ';' : ',';

        var headers = ParseCsvLine(lines[0], sep);
        var rows = new List<List<string>>(lines.Count - 1);
        for (var i = 1; i < lines.Count; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i])) continue;
            rows.Add(ParseCsvLine(lines[i], sep));
        }
        return (headers, rows);
    }

    /// <summary>Разбиение CSV-текста на строки с учётом кавычек (внутри кавычек могут быть переводы строк).</summary>
    private static List<string> SplitCsvLines(string text)
    {
        var lines = new List<string>();
        var sb = new StringBuilder();
        var inQuotes = false;
        for (var i = 0; i < text.Length; i++)
        {
            var ch = text[i];
            if (ch == '"')
            {
                inQuotes = !inQuotes;
                sb.Append(ch);
            }
            else if ((ch == '\n' || ch == '\r') && !inQuotes)
            {
                if (sb.Length > 0) { lines.Add(sb.ToString()); sb.Clear(); }
                if (ch == '\r' && i + 1 < text.Length && text[i + 1] == '\n') i++;
            }
            else sb.Append(ch);
        }
        if (sb.Length > 0) lines.Add(sb.ToString());
        return lines;
    }

    private static List<string> ParseCsvLine(string line, char sep)
    {
        var result = new List<string>();
        var sb = new StringBuilder();
        var inQuotes = false;
        for (var i = 0; i < line.Length; i++)
        {
            var ch = line[i];
            if (inQuotes)
            {
                if (ch == '"')
                {
                    if (i + 1 < line.Length && line[i + 1] == '"') { sb.Append('"'); i++; }
                    else inQuotes = false;
                }
                else sb.Append(ch);
            }
            else
            {
                if (ch == '"') inQuotes = true;
                else if (ch == sep) { result.Add(sb.ToString()); sb.Clear(); }
                else sb.Append(ch);
            }
        }
        result.Add(sb.ToString());
        return result;
    }

    // ── Валидация значений ────────────────────────────────────────────────────

    private static bool TryConvertValue(string raw, DictionaryFieldType type, out JsonNode? node, out string error)
    {
        raw = raw.Trim();
        error = string.Empty;
        switch (type)
        {
            case DictionaryFieldType.Text:
            case DictionaryFieldType.Select:
                node = JsonValue.Create(raw);
                return true;

            case DictionaryFieldType.Number:
                if (long.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var l)
                    || long.TryParse(raw, NumberStyles.Integer, CultureInfo.CurrentCulture, out l))
                { node = JsonValue.Create(l); return true; }
                node = null; error = $"ожидается целое число, получено «{raw}»"; return false;

            case DictionaryFieldType.Decimal:
                var normalized = raw.Replace(',', '.');
                if (decimal.TryParse(normalized, NumberStyles.Float, CultureInfo.InvariantCulture, out var d))
                { node = JsonValue.Create(d); return true; }
                node = null; error = $"ожидается число, получено «{raw}»"; return false;

            case DictionaryFieldType.Boolean:
                var lower = raw.ToLowerInvariant();
                if (lower is "true" or "1" or "да" or "yes" or "y") { node = JsonValue.Create(true); return true; }
                if (lower is "false" or "0" or "нет" or "no" or "n") { node = JsonValue.Create(false); return true; }
                node = null; error = $"ожидается true/false, получено «{raw}»"; return false;

            case DictionaryFieldType.Date:
                if (DateTime.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var date)
                    || DateTime.TryParse(raw, CultureInfo.CurrentCulture, DateTimeStyles.AssumeLocal, out date))
                { node = JsonValue.Create(date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)); return true; }
                node = null; error = $"ожидается дата, получено «{raw}»"; return false;

            case DictionaryFieldType.DateTime:
                if (DateTime.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var dt)
                    || DateTime.TryParse(raw, CultureInfo.CurrentCulture, DateTimeStyles.AssumeLocal, out dt))
                { node = JsonValue.Create(dt.ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture)); return true; }
                node = null; error = $"ожидается дата/время, получено «{raw}»"; return false;

            default:
                node = JsonValue.Create(raw); return true;
        }
    }

    // ── Сборка файлов экспорта ────────────────────────────────────────────────

    private static byte[] BuildXlsx(List<DictionaryFieldEntity> fields, List<DictionaryRecordEntity> records, string sheetName)
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add(SafeSheetName(sheetName));

        for (var c = 0; c < fields.Count; c++)
        {
            ws.Cell(1, c + 1).Value = fields[c].DisplayName;
            ws.Cell(1, c + 1).Style.Font.Bold = true;
        }

        for (var r = 0; r < records.Count; r++)
        {
            JsonDocument? doc = null;
            try { doc = JsonDocument.Parse(records[r].Data); } catch { /* битый JSON — оставим строку пустой */ }

            for (var c = 0; c < fields.Count; c++)
            {
                var key = fields[c].FieldKey;
                if (doc != null && doc.RootElement.TryGetProperty(key, out var v))
                    WriteXlsxCell(ws.Cell(r + 2, c + 1), v, fields[c].FieldType);
            }
            doc?.Dispose();
        }

        ws.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    private static void WriteXlsxCell(IXLCell cell, JsonElement v, DictionaryFieldType type)
    {
        if (v.ValueKind == JsonValueKind.Null) return;
        switch (type)
        {
            case DictionaryFieldType.Number:
                if (v.ValueKind == JsonValueKind.Number && v.TryGetInt64(out var l)) cell.Value = l;
                else cell.Value = v.ToString();
                break;
            case DictionaryFieldType.Decimal:
                if (v.ValueKind == JsonValueKind.Number && v.TryGetDecimal(out var d)) cell.Value = d;
                else cell.Value = v.ToString();
                break;
            case DictionaryFieldType.Boolean:
                if (v.ValueKind is JsonValueKind.True or JsonValueKind.False) cell.Value = v.GetBoolean();
                else cell.Value = v.ToString();
                break;
            case DictionaryFieldType.Date:
            case DictionaryFieldType.DateTime:
                if (v.ValueKind == JsonValueKind.String && DateTime.TryParse(v.GetString(), CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var dt))
                {
                    cell.Value = dt;
                    cell.Style.DateFormat.Format = type == DictionaryFieldType.Date ? "yyyy-mm-dd" : "yyyy-mm-dd hh:mm:ss";
                }
                else cell.Value = v.ToString();
                break;
            default:
                cell.Value = v.ValueKind == JsonValueKind.String ? v.GetString() : v.ToString();
                break;
        }
    }

    private static string SafeSheetName(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return "Sheet1";
        var invalid = new[] { '\\', '/', '?', '*', '[', ']', ':' };
        var safe = new string(name.Select(c => invalid.Contains(c) ? '_' : c).ToArray());
        if (safe.Length > 31) safe = safe[..31];
        return safe;
    }

    private static byte[] BuildCsv(List<DictionaryFieldEntity> fields, List<DictionaryRecordEntity> records)
    {
        var sb = new StringBuilder();
        sb.Append('﻿'); // BOM для корректного открытия в Excel
        sb.AppendLine(string.Join(",", fields.Select(f => CsvEscape(f.DisplayName))));

        foreach (var rec in records)
        {
            JsonDocument? doc = null;
            try { doc = JsonDocument.Parse(rec.Data); } catch { }

            var cells = new List<string>(fields.Count);
            foreach (var f in fields)
            {
                if (doc != null && doc.RootElement.TryGetProperty(f.FieldKey, out var v) && v.ValueKind != JsonValueKind.Null)
                    cells.Add(CsvEscape(v.ValueKind == JsonValueKind.String ? v.GetString() ?? string.Empty : v.ToString()));
                else
                    cells.Add(string.Empty);
            }
            sb.AppendLine(string.Join(",", cells));
            doc?.Dispose();
        }
        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    private static string CsvEscape(string s)
    {
        if (string.IsNullOrEmpty(s)) return string.Empty;
        if (s.IndexOfAny(new[] { ',', '"', '\n', '\r' }) >= 0)
            return "\"" + s.Replace("\"", "\"\"") + "\"";
        return s;
    }
}
