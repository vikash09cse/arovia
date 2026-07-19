namespace WebApi.Features.PatientDocuments;

public record PatientDocumentItemResponse(
    Guid Id,
    Guid PatientId,
    string DisplayName,
    string StoredFileName,
    string FileType,
    DateTime CreatedAt);

public record PatientDocumentDownload(
    string DisplayName,
    string StoredFileName,
    string ContentType,
    byte[] Bytes);
