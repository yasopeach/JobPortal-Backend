using JobPortal.Data;
using JobPortal.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace JobPortal.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]  
    public class UserController : ControllerBase
    {
        private readonly JobPortalContext _context;

        public UserController(JobPortalContext context)
        {
            _context = context;
        }

        [HttpGet("profile")]
        public async Task<ActionResult> GetUserProfile()
        {
            var username = User.Identity.Name;
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);

            if (user == null)
            {
                return NotFound("Kullanıcı bulunamadı.");
            }

            var userProfile = new
            {
                user.Username,
                user.Email,
                user.Role,
                user.Name,
                user.Surname,
                user.Age,
                user.Residence
            };

            return Ok(userProfile);
        }


        // Kullanıcının yaptığı başvuruları listeler
        [HttpGet("applications")]
        public async Task<ActionResult<IEnumerable<object>>> GetUserApplications()
        {
            var username = User.Identity.Name;

            var applications = await _context.Applications
                .Where(a => a.ApplicantUsername == username)
                .Join(_context.Jobs, 
                      app => app.JobId,
                      job => job.Id,
                      (app, job) => new
                      {
                          app.Id,
                          app.Status,
                          app.CreatedAt, 
                          JobTitle = job.Title, 
                          ApplicantUsername = app.ApplicantUsername, 
                          app.JobId 
                      })
                .ToListAsync();

            return Ok(applications);
        }



        [HttpGet("favorites")]
        public async Task<ActionResult<IEnumerable<Job>>> GetUserFavorites()
        {
            var username = User.Identity.Name;

            var favoriteJobIds = await _context.Favorites
                .Where(f => f.Username == username)
                .Select(f => f.JobId)
                .ToListAsync();

            var favoriteJobs = await _context.Jobs
                .Where(j => favoriteJobIds.Contains(j.Id))
                .ToListAsync();

            return Ok(favoriteJobs);
        }

        [HttpPut("profile")]
        public async Task<IActionResult> UpdateUserProfile([FromBody] UserProfileUpdateModel updatedUser)
        {
            var username = User.Identity.Name;
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);

            if (user == null)
            {
                return NotFound("Kullanıcı bulunamadı.");
            }

            user.Email = updatedUser.Email ?? user.Email;
            user.Role = updatedUser.Role ?? user.Role;
            user.Name = updatedUser.Name ?? user.Name;
            user.Surname = updatedUser.Surname ?? user.Surname;
            user.Age = updatedUser.Age ?? user.Age;
            user.Residence = updatedUser.Residence ?? user.Residence;

            await _context.SaveChangesAsync();

            return Ok("Profil başarıyla güncellendi.");
        }



        [Authorize]
        [HttpPut("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordModel model)
        {
            var username = User.Identity.Name;

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);

            if (user == null)
            {
                return NotFound("Kullanıcı bulunamadı.");
            }

            if (user.Password != model.OldPassword)
            {
                return BadRequest("Eski parola yanlış.");
            }

            user.Password = model.NewPassword;
            await _context.SaveChangesAsync();

            return Ok("Parola başarıyla güncellendi.");
        }



        [Authorize]
        [HttpGet("notifications")]
        public async Task<ActionResult<IEnumerable<Notification>>> GetUserNotifications()
        {
            var username = User.Identity.Name;
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);

            if (user == null)
            {
                return NotFound("Kullanıcı bulunamadı.");
            }

            var notifications = await _context.Notifications
                .Where(n => n.UserId == user.Id && !n.IsRead)
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();

            return Ok(notifications);
        }

        [Authorize]
        [HttpPost("notifications/mark-as-read")]
        public async Task<IActionResult> MarkNotificationsAsRead()
        {
            var username = User.Identity.Name;
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);

            if (user == null)
            {
                return NotFound("Kullanıcı bulunamadı.");
            }

            var notifications = await _context.Notifications
                .Where(n => n.UserId == user.Id && !n.IsRead)
                .ToListAsync();

            foreach (var notification in notifications)
            {
                notification.IsRead = true;
            }

            await _context.SaveChangesAsync();

            return Ok("Bildirimler okundu olarak işaretlendi.");
        }




    }
}
