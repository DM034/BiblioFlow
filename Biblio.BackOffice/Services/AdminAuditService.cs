using Biblio.BackOffice.Data;

namespace Biblio.BackOffice.Services;

public interface IAdminAuditService
{
    Task LogAsync(string action, string entityType, string? entityId = null, string? details = null);
}

public class AdminAuditService : IAdminAuditService
{
    private readonly LibraryDbContext _db;
    private readonly IHttpContextAccessor _http;

    public AdminAuditService(LibraryDbContext db, IHttpContextAccessor http)
    {
        _db = db;
        _http = http;
    }

    public async Task LogAsync(string action, string entityType, string? entityId = null, string? details = null)
    {
        try
        {
            var ctx = _http.HttpContext;
            var adminEmail = ctx?.Session.GetString("admin_email") ?? "unknown";
            var ipAddress = ctx?.Connection.RemoteIpAddress?.ToString();

            _db.AdminAuditEvents.Add(new AdminAuditEvent
            {
                AdminEmail = adminEmail,
                Action = action,
                EntityType = entityType,
                EntityId = entityId,
                Details = details,
                IpAddress = ipAddress,
                CreatedAt = DateTime.UtcNow
            });

            await _db.SaveChangesAsync();
        }
        catch
        {
        }
    }
}
