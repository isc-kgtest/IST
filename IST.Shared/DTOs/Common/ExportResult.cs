using MemoryPack;

namespace IST.Shared.DTOs.Common;

/// <summary>Результат экспорта: байты файла + MIME-тип + имя файла для скачивания.</summary>
[MemoryPackable]
public partial class ExportResult
{
    [MemoryPackOrder(0)] public byte[] FileContent  { get; set; } = Array.Empty<byte>();
    [MemoryPackOrder(1)] public string ContentType  { get; set; } = "application/octet-stream";
    [MemoryPackOrder(2)] public string FileName     { get; set; } = "export.xlsx";
}
