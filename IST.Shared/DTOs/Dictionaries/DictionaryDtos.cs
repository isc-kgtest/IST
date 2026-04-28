using System;

namespace IST.Shared.DTOs.Dictionaries;

public class DictionaryDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string FieldStructure { get; set; } = "[]";
    public DateTime CreatedAt { get; set; }
    public bool IsDeleted { get; set; }
}

public class UpsertDictionaryRequest
{
    public Guid? Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string FieldStructure { get; set; } = "[]";
}

public class DictionaryRecordDto
{
    public Guid Id { get; set; }
    public Guid DictionaryId { get; set; }
    public string Data { get; set; } = "{}";
    public DateTime CreatedAt { get; set; }
    public bool IsDeleted { get; set; }
}

public class UpsertDictionaryRecordRequest
{
    public Guid? Id { get; set; }
    public Guid DictionaryId { get; set; }
    public string Data { get; set; } = "{}";
}
