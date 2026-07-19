namespace WebApi.Features.CommonFiles;

public record CommonFileItemResponse(
    Guid Id,
    string DisplayName,
    string StoredFileName,
    string FileType,
    DateTime CreatedAt);

public record CommonFileDownload(
    string DisplayName,
    string StoredFileName,
    string ContentType,
    byte[] Bytes);
