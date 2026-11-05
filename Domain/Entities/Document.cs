using MediDropProject.Domain.Enums;
namespace MediDropProject.Domain.Entities;

public class Document
{
    public long Id { get; set; }

    public DocumentType Type { get; set; }

    public string FileName { get; set; } = null!;
    public string ContentType { get; set; } = null!;
    public string StorageKey { get; set; } = null!;

    public long Size { get; set; }

    public int UploadedByUserId { get; set; }
    
    // Restricts patient documents to their owner/care relationship.
    public int? PatientId { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public ApplicationUser UploadedByUser { get; set; } = null!;
}
