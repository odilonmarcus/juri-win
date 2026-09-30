namespace ConselhoJuridicoIA.Models;

public sealed class DocumentAttachment
{
    public required string FileName { get; init; }
    public required string FilePath { get; init; }
    public required string Content { get; init; }
    public required long FileSizeBytes { get; init; }
    public bool IsTruncated { get; init; }

    public string DisplayMeta
    {
        get
        {
            var size = FileSizeBytes < 1024 * 1024
                ? $"{FileSizeBytes / 1024d:0.#} KB"
                : $"{FileSizeBytes / 1024d / 1024d:0.##} MB";
            var truncated = IsTruncated ? " · conteúdo truncado para análise" : string.Empty;
            return $"{size} · {Content.Length:N0} caracteres{truncated}";
        }
    }
}
