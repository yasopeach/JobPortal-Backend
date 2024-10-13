using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using JobPortal.Data;
using JobPortal.Models;
using Microsoft.EntityFrameworkCore;

namespace JobPortal.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")]  
    public class AdminController : ControllerBase
    {
        private readonly JobPortalContext _context;

        public AdminController(JobPortalContext context)
        {
            _context = context;
        }

        // 1. İlanları listeleme
        [HttpGet("jobs")]
        public async Task<ActionResult<IEnumerable<Job>>> GetJobs()
        {
            var jobs = await _context.Jobs.ToListAsync();
            return Ok(jobs);
        }

        // 2. İlan silme
        [HttpDelete("jobs/{id}")]
        public async Task<IActionResult> DeleteJob(int id)
        {
            var job = await _context.Jobs.FindAsync(id);
            if (job == null)
            {
                return NotFound("İş ilanı bulunamadı.");
            }

            _context.Jobs.Remove(job);
            await _context.SaveChangesAsync();
            return Ok("İş ilanı başarıyla silindi.");
        }

        // 3. Kullanıcıları listeleme
        [HttpGet("users")]
        public async Task<ActionResult<IEnumerable<User>>> GetUsers()
        {
            var users = await _context.Users.ToListAsync();
            return Ok(users);
        }

        // 4. Kullanıcı silme
        [HttpDelete("users/{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound("Kullanıcı bulunamadı.");
            }

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
            return Ok("Kullanıcı başarıyla silindi.");
        }

        // 5. Başvuruları listeleme
        [HttpGet("applications")]
        public async Task<ActionResult<IEnumerable<Application>>> GetApplications()
        {
            var applications = await _context.Applications.ToListAsync();
            return Ok(applications);
        }

        // 6. Başvuru durumu güncelleme
        [HttpPut("applications/{id}")]
        public async Task<IActionResult> UpdateApplicationStatus(int id, [FromBody] string status)
        {
            var application = await _context.Applications.FindAsync(id);
            if (application == null)
            {
                return NotFound("Başvuru bulunamadı.");
            }

            application.Status = status;
            await _context.SaveChangesAsync();
            return Ok("Başvuru durumu başarıyla güncellendi.");
        }


        //[Authorize(Roles = "Admin")]
        [HttpGet("applications/{id}/download-cv")]
        public async Task<IActionResult> DownloadCv(int id)
        {
            var application = await _context.Applications.FindAsync(id);
            if (application == null || string.IsNullOrEmpty(application.CvFilePath))
            {
                return NotFound("Başvuru veya CV dosyası bulunamadı.");
            }

            var filePath = application.CvFilePath;
            var fileName = application.CvFileName;
            var mimeType = "application/octet-stream";

            byte[] fileBytes = await System.IO.File.ReadAllBytesAsync(filePath);
            return File(fileBytes, mimeType, fileName);
        }

    }
}
