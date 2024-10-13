using JobPortal.Data;
using JobPortal.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[Route("api/[controller]")]
[ApiController]
[Authorize] 
public class NotificationController : ControllerBase
{
    private readonly JobPortalContext _context;

    public NotificationController(JobPortalContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Notification>>> GetUserNotifications()
    {
        var username = User.Identity.Name;

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);

        if (user == null)
        {
            return NotFound("Kullanıcı bulunamadı.");
        }

        var notifications = await _context.Notifications
            .Where(n => n.UserId == user.Id)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync();

        return Ok(notifications);
    }


    [HttpPut("{notificationId}/mark-as-read")]
    public async Task<IActionResult> MarkAsRead(int notificationId)
    {
        var notification = await _context.Notifications.FindAsync(notificationId);

        if (notification == null)
        {
            return NotFound("Bildirim bulunamadı.");
        }

        notification.IsRead = true;
        await _context.SaveChangesAsync();

        return Ok("Bildirim okundu olarak işaretlendi.");
    }

}
