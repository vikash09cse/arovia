namespace WebApi.Features.UserDocuments;

public record UserDocumentItemResponse(
    Guid Id,
    Guid UserId,
    byte DocumentType,
    string DocumentTypeLabel,
    string DisplayName,
    string StoredFileName,
    string FileType,
    DateTime CreatedAt);

public record UserDocumentDownload(
    string DisplayName,
    string StoredFileName,
    string ContentType,
    byte[] Bytes);
