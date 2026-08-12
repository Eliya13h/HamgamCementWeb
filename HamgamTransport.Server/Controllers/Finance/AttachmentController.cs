using HamgamTransport.Server.Authorization;
using HamgamTransport.Server.Data;
using HamgamTransport.Server.Data.Models.Finance;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HamgamTransport.Server.Controllers.Finance;

[ApiController]
[Route("api/finance/attachments")]
[Authorize]
public class AttachmentController : FinanceControllerBase
{
    private readonly IWebHostEnvironment _env;

    public AttachmentController(AppDbContext db, IWebHostEnvironment env) : base(db)
    {
        _env = env;
    }

    [HttpGet]
    [HasPermission("accounting.expenses.view")]
    public async Task<IActionResult> List(
        [FromQuery] string entityType,
        [FromQuery] int entityId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(entityType))
        {
            return BadRequest(new { message = "نوع موجودیت الزامی است." });
        }

        var rows = await Db.Attachments.AsNoTracking()
            .Where(a => a.IsDeleted != true
                        && a.EntityType == entityType
                        && a.EntityId == entityId)
            .OrderByDescending(a => a.AttachmentID)
            .Select(a => new
            {
                attachmentId = a.AttachmentID,
                fileName = a.FileName,
                relativePath = a.RelativePath,
                contentType = a.ContentType,
                sizeBytes = a.SizeBytes,
                createdAt = a.CreatedAt,
            })
            .ToListAsync(cancellationToken);

        return Ok(rows);
    }

    [HttpPost("upload")]
    [HasPermission("accounting.expenses.create")]
    [RequestSizeLimit(20_000_000)]
    public async Task<IActionResult> Upload(
        [FromForm] string entityType,
        [FromForm] int entityId,
        IFormFile file,
        CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest(new { message = "فایل الزامی است." });
        }

        if (!string.Equals(entityType, "JournalEntry", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new { message = "فعلاً فقط پیوست سند دفتر پشتیبانی می‌شود." });
        }

        var entryExists = await Db.JournalEntries.AnyAsync(
            e => e.JournalEntryID == entityId && e.IsDeleted != true, cancellationToken);
        if (!entryExists)
        {
            return NotFound(new { message = "سند یافت نشد." });
        }

        var webRoot = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");
        var folder = Path.Combine(webRoot, "uploads", "attachments");
        Directory.CreateDirectory(folder);

        var ext = Path.GetExtension(file.FileName);
        var stored = $"{Guid.NewGuid():N}{ext}";
        var fullPath = Path.Combine(folder, stored);
        await using (var stream = System.IO.File.Create(fullPath))
        {
            await file.CopyToAsync(stream, cancellationToken);
        }

        var relative = $"/uploads/attachments/{stored}";
        var entity = new Attachment
        {
            EntityType = "JournalEntry",
            EntityId = entityId,
            FileName = Path.GetFileName(file.FileName),
            StoredFileName = stored,
            RelativePath = relative,
            ContentType = file.ContentType,
            SizeBytes = file.Length,
            IsActive = true,
            IsDeleted = false,
            CreatedAt = DateTime.Now,
            CreatedBy = ResolveCurrentUserId(),
        };
        Db.Attachments.Add(entity);
        await Db.SaveChangesAsync(cancellationToken);

        return Ok(new
        {
            message = "پیوست ذخیره شد.",
            attachmentId = entity.AttachmentID,
            relativePath = entity.RelativePath,
        });
    }

    [HttpDelete("{id:int}")]
    [HasPermission("accounting.expenses.delete")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var entity = await Db.Attachments.FirstOrDefaultAsync(
            a => a.AttachmentID == id && a.IsDeleted != true, cancellationToken);
        if (entity is null) return NotFound(new { message = "پیوست یافت نشد." });

        entity.IsDeleted = true;
        entity.IsActive = false;
        entity.DeletedAt = DateTime.Now;
        entity.DeletedBy = ResolveCurrentUserId();
        await Db.SaveChangesAsync(cancellationToken);
        return Ok(new { message = "پیوست حذف شد." });
    }
}
