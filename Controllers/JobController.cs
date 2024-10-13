using JobPortal.Data;
using JobPortal.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;


[Route("api/[controller]")]
[ApiController]
public class JobController : ControllerBase
{
    private readonly JobPortalContext _context;
    private readonly IConfiguration _configuration; 
    public JobController(JobPortalContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;  
    }

    // GET: api/Job
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Job>>> GetJobs()
    {
        return await _context.Jobs.ToListAsync();
    }

    // GET: api/Job/5
    [HttpGet("{id}")]
    public async Task<ActionResult<Job>> GetJob(int id)
    {
        var job = await _context.Jobs.FindAsync(id);

        if (job == null)
        {
            return NotFound();
        }
        job.ViewCount++;
        await _context.SaveChangesAsync();

        return job;
    }



    [Authorize(Roles = "Employer, Admin")]
    [HttpPost]
    public async Task<ActionResult<Job>> PostJob(Job job)
    {
        // JWT'den e-posta adresini alıyoruz
        var email = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;

        if (string.IsNullOrEmpty(email))
        {
            return BadRequest("Kullanıcının e-posta adresi bulunamadı.");
        }

        // E-posta adresine göre veritabanından kullanıcıyı buluyoruz
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

        if (user == null)
        {
            return BadRequest("Kullanıcı bulunamadı.");
        }

        // İş ilanını oluşturan kullanıcıyı iş ilanına ekliyoruz
        job.CreatedByUserId = user.Id;

        // İş ilanını veritabanına kaydediyoruz
        _context.Jobs.Add(job);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetJob), new { id = job.Id }, job);
    }





    // PUT: api/Job/5
    [HttpPut("{id}")]
    public async Task<IActionResult> PutJob(int id, Job job)
    {
        if (id != job.Id)
        {
            return BadRequest();
        }

        _context.Entry(job).State = EntityState.Modified;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!JobExists(id))
            {
                return NotFound();
            }
            else
            {
                throw;
            }
        }

        return NoContent();
    }

    // DELETE: api/Job/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteJob(int id)
    {
        var job = await _context.Jobs.FindAsync(id);
        if (job == null)
        {
            return NotFound();
        }

        _context.Jobs.Remove(job);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    private bool JobExists(int id)
    {
        return _context.Jobs.Any(e => e.Id == id);
    }



    [HttpGet("search")]
    public async Task<ActionResult<IEnumerable<Job>>> GetFilteredJobs(
    string? title = null,
    string? companyName = null,
    string? location = null,
    int? minApplicationCount = null,
    int? maxApplicationCount = null,
    int? minFavoriteCount = null,
    int? maxFavoriteCount = null,
    int? minViewCount = null,
    int? maxViewCount = null)
    {
        var jobs = _context.Jobs.AsQueryable();

        // Başlık ile filtreleme
        if (!string.IsNullOrWhiteSpace(title))
        {
            jobs = jobs.Where(j => j.Title.Contains(title));
        }

        // Şirket adı ile filtreleme
        if (!string.IsNullOrWhiteSpace(companyName))
        {
            jobs = jobs.Where(j => j.CompanyName.Contains(companyName));
        }

        // Konum ile filtreleme
        if (!string.IsNullOrWhiteSpace(location))
        {
            jobs = jobs.Where(j => j.Location.Contains(location));
        }

        // Başvuru sayısına göre filtreleme
        if (minApplicationCount.HasValue)
        {
            jobs = jobs.Where(j => j.ApplicationCount >= minApplicationCount.Value);
        }
        if (maxApplicationCount.HasValue)
        {
            jobs = jobs.Where(j => j.ApplicationCount <= maxApplicationCount.Value);
        }

        // Favori sayısına göre filtreleme
        if (minFavoriteCount.HasValue)
        {
            jobs = jobs.Where(j => j.FavoriteCount >= minFavoriteCount.Value);
        }
        if (maxFavoriteCount.HasValue)
        {
            jobs = jobs.Where(j => j.FavoriteCount <= maxFavoriteCount.Value);
        }

        // Görüntülenme sayısına göre filtreleme
        if (minViewCount.HasValue)
        {
            jobs = jobs.Where(j => j.ViewCount >= minViewCount.Value);
        }
        if (maxViewCount.HasValue)
        {
            jobs = jobs.Where(j => j.ViewCount <= maxViewCount.Value);
        }

        // Herhangi bir ilan bulunamazsa
        if (!jobs.Any())
        {
            return NotFound("Aradığınız kriterlere uygun iş ilanı bulunamadı.");
        }

        return await jobs.ToListAsync();
    }




    // GET: api/Job/paginated
    [HttpGet("paginated")]
    public async Task<ActionResult<IEnumerable<Job>>> GetPaginatedJobs(int pageNumber = 1, int pageSize = 10)
    {
        var jobs = await _context.Jobs
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return jobs;
    }



    [Authorize(Roles = "Employee, Employer, Admin")]
    [HttpPost("{jobId}/apply")]
    public async Task<IActionResult> ApplyToJob(int jobId, [FromForm] IFormFile cvFile)
    {
        var job = await _context.Jobs.FindAsync(jobId);

        if (job == null)
        {
            return NotFound("İş ilanı bulunamadı.");
        }

        job.ApplicationCount++;

        var username = User.Identity.Name;
        var userEmail = await _context.Users
            .Where(u => u.Username == username)
            .Select(u => u.Email)
            .FirstOrDefaultAsync();

        if (string.IsNullOrEmpty(userEmail))
        {
            return BadRequest("Kullanıcının e-posta adresi bulunamadı.");
        }

        // Dosya yükleme işlemi
        string filePath = null;
        string fileName = null;
        if (cvFile != null && cvFile.Length > 0)
        {
            fileName = $"{Guid.NewGuid()}_{cvFile.FileName}";
            var uploadsFolderPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");

            if (!Directory.Exists(uploadsFolderPath))
            {
                Directory.CreateDirectory(uploadsFolderPath);
            }

            filePath = Path.Combine(uploadsFolderPath, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await cvFile.CopyToAsync(stream);
            }
        }

        // Başvuruyu iş ilanı için kaydediyoruz
        var application = new Application
        {
            JobId = jobId,
            ApplicantUsername = username,
            ApplicantEmail = userEmail,
            CvFileName = fileName,
            CvFilePath = filePath
        };

        _context.Applications.Add(application);
        await _context.SaveChangesAsync();

        // İş ilanını oluşturan kullanıcının e-posta adresini almak için CreatedByUserId'yi kullanarak veritabanında sorgulama yapıyoruz
        var jobOwner = await _context.Users.FirstOrDefaultAsync(u => u.Id == job.CreatedByUserId);

        if (jobOwner == null || string.IsNullOrEmpty(jobOwner.Email))
        {
            return BadRequest("İş ilanı sahibinin e-posta adresi bulunamadı.");
        }

        // İşveren ve başvuran kişiye e-posta gönderme
        var emailService = new EmailService(_configuration);

        // İş ilanını oluşturan kişiye e-posta gönderiyoruz
        await emailService.SendEmailAsync(jobOwner.Email, "Yeni Başvuru", $"{username} adlı kullanıcı {job.Title} ilanına başvurdu.");

        // Başvuru yapan kişiye e-posta gönderiyoruz 
        await emailService.SendEmailAsync(userEmail, "Başvurunuz Alındı", $"Başvurunuz başarıyla alındı: {job.Title} ilanı.");

        // İlan sahibine bildirim gönderme
        await SendNewApplicationNotification(jobOwner, job.Title, username);

        return Ok("Başvurunuz başarıyla kaydedildi.");
    }

    // Yeni başvuru yapıldığında ilan sahibine bildirim gönderme
    private async Task SendNewApplicationNotification(User jobOwner, string jobTitle, string applicantUsername)
    {
        var notification = new Notification
        {
            UserId = jobOwner.Id,
            Message = $"{jobTitle} ilanınıza {applicantUsername} kullanıcısı tarafından başvuru yapıldı.",
            CreatedAt = DateTime.UtcNow
        };

        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();
    }









    [Authorize(Roles = "Employer, Admin")]
    [HttpPut("{jobId}/applications/{applicationId}")]
    public async Task<IActionResult> UpdateApplicationStatus(int jobId, int applicationId, [FromBody] string newStatus)
    {
        var job = await _context.Jobs.FindAsync(jobId);
        if (job == null)
        {
            return NotFound("İş ilanı bulunamadı.");
        }

        var application = await _context.Applications.FindAsync(applicationId);
        if (application == null || application.JobId != jobId)
        {
            return NotFound("Başvuru bulunamadı.");
        }

        application.Status = newStatus;
        await _context.SaveChangesAsync();

        // Başvuru sahibi için bildirim gönderme
        await SendStatusChangeNotification(application, newStatus);

        // E-posta gönderme işlemi - Başvuru sahibine
        var emailService = new EmailService(_configuration);
        await emailService.SendEmailAsync(application.ApplicantEmail, "Başvuru Durumu Güncellendi", $"Başvurunuzun durumu: {newStatus}");

        return Ok("Başvuru durumu başarıyla güncellendi.");
    }

    // Başvuru durumu güncellendiğinde bildirim gönderme
    private async Task SendStatusChangeNotification(Application application, string newStatus)
    {
        // ApplicantUsername kullanılarak User tablosundan UserId alınıyor
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == application.ApplicantUsername);

        if (user == null)
        {
            // Kullanıcı bulunamazsa bir hata dönebilir veya loglanabilir
            throw new Exception("Başvuru sahibi bulunamadı.");
        }

        var notification = new Notification
        {
            UserId = user.Id,  // Bulunan kullanıcının ID'si
            Message = $"Başvurunuzun durumu güncellendi: {newStatus}"
        };

        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();
    }




    [Authorize(Roles = "Employee, Employer, Admin")]
    [HttpPost("{jobId}/favorite")]
    public async Task<IActionResult> AddToFavorites(int jobId)
    {
        var job = await _context.Jobs.FindAsync(jobId);
        if (job == null)
        {
            return NotFound("İş ilanı bulunamadı.");
        }

        job.FavoriteCount++;

        var username = User.Identity.Name;  // Kullanıcının adı
        var favorite = new Favorite
        {
            Username = username,
            JobId = jobId
        };

        _context.Favorites.Add(favorite);
        await _context.SaveChangesAsync();

        return Ok("İş ilanı favorilere eklendi.");
    }

    [Authorize(Roles = "Employee, Employer, Admin")]
    [HttpGet("favorites")]
    public async Task<ActionResult<IEnumerable<Job>>> GetFavorites()
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


    [Authorize(Roles = "Employee, Employer, Admin")]
    [HttpDelete("{jobId}/favorite")]
    public async Task<IActionResult> RemoveFromFavorites(int jobId)
    {
        var username = User.Identity.Name;
        var favorite = await _context.Favorites
            .FirstOrDefaultAsync(f => f.JobId == jobId && f.Username == username);

        if (favorite == null)
        {
            return NotFound("Favori iş ilanı bulunamadı.");
        }

        _context.Favorites.Remove(favorite);
        await _context.SaveChangesAsync();

        return Ok("İş ilanı favorilerden kaldırıldı.");
    }

    [Authorize(Roles = "Employee, Employer, Admin")]
    [HttpPost("{jobId}/comment")]
    public async Task<IActionResult> AddComment(int jobId, [FromBody] string content)
    {
        var job = await _context.Jobs.FindAsync(jobId);
        if (job == null)
        {
            return NotFound("İş ilanı bulunamadı.");
        }

        var username = User.Identity.Name;
        var comment = new Comment
        {
            Username = username,
            JobId = jobId,
            Content = content
        };

        _context.Comments.Add(comment);
        await _context.SaveChangesAsync();

        // Yorumun tüm bilgilerini geri döndürme
        return Ok(comment);
    }




    [AllowAnonymous]
    [HttpGet("{jobId}/comments")]
    public async Task<ActionResult<IEnumerable<Comment>>> GetComments(int jobId)
    {
        var job = await _context.Jobs.FindAsync(jobId);
        if (job == null)
        {
            return NotFound("İş ilanı bulunamadı.");
        }

        var comments = await _context.Comments
            .Where(c => c.JobId == jobId)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();

        return Ok(comments);
    }


    [Authorize(Roles = "Employee, Employer, Admin")]
    [HttpDelete("comment/{commentId}")]
    public async Task<IActionResult> DeleteComment(int commentId)
    {
        var comment = await _context.Comments.FindAsync(commentId);
        if (comment == null)
        {
            return NotFound("Yorum bulunamadı.");
        }

        var username = User.Identity.Name;  
        var userRole = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value; 

        if (comment.Username != username && userRole != "Employer")
        {
            return Forbid("Sadece yorum sahibi veya işveren yorumu silebilir.");
        }

        // Yorum siliniyor
        _context.Comments.Remove(comment);
        await _context.SaveChangesAsync();

        return Ok("Yorum başarıyla silindi.");
    }





    [Authorize(Roles = "Employer, Admin")]
    [HttpGet("statistics/{jobId}")]
    public async Task<ActionResult> GetJobStatistics(int jobId)
    {
        var job = await _context.Jobs.FindAsync(jobId);
        if (job == null)
        {
            return NotFound("İş ilanı bulunamadı.");
        }

        var statistics = new
        {
            JobId = job.Id,
            JobTitle = job.Title,
            ApplicationCount = job.ApplicationCount,
            FavoriteCount = job.FavoriteCount,
            ViewCount = job.ViewCount
        };

        return Ok(statistics);
    }



    [Authorize(Roles = "Admin")]
    [HttpGet("applications/{applicationId}/download-cv")]
    public async Task<IActionResult> DownloadCv(int applicationId)
    {
        var application = await _context.Applications.FindAsync(applicationId);
        if (application == null || string.IsNullOrEmpty(application.CvFilePath))
        {
            return NotFound("Başvuru veya CV dosyası bulunamadı.");
        }

        var username = User.Identity.Name;
        var userRole = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;

        var job = await _context.Jobs.FindAsync(application.JobId);
        if (job == null)
        {
            return NotFound("İş ilanı bulunamadı.");
        }

        if (userRole == "Employer" && job.CreatedByUserId != _context.Users.FirstOrDefault(u => u.Username == username)?.Id)
        {
            return Forbid("Bu başvuruya erişim yetkiniz yok.");
        }

        var filePath = application.CvFilePath;
        var fileName = application.CvFileName;
        var mimeType = "application/octet-stream";

        byte[] fileBytes = await System.IO.File.ReadAllBytesAsync(filePath);
        return File(fileBytes, mimeType, fileName);
    }





    [Authorize(Roles = "Employer, Admin")]
    [HttpGet("employer/applications")]
    public async Task<ActionResult<IEnumerable<Application>>> GetEmployerApplications()
    {
        var username = User.Identity.Name;
        var employer = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
        if (employer == null)
        {
            return BadRequest("İşveren bulunamadı.");
        }

        var jobs = await _context.Jobs
            .Where(j => j.CreatedByUserId == employer.Id)
            .ToListAsync();

        var jobIds = jobs.Select(j => j.Id).ToList();

        var applications = await _context.Applications
            .Where(a => jobIds.Contains(a.JobId))
            .ToListAsync();

        return Ok(applications);
    }


    [Authorize(Roles = "Employer")]
    [HttpGet("employer/post")]
    public async Task<ActionResult<IEnumerable<Application>>> GetEmployerPost()
    {
        var username = User.Identity.Name;
        var employer = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
        if (employer == null)
        {
            return BadRequest("İşveren bulunamadı.");
        }

        var jobs = await _context.Jobs
            .Where(j => j.CreatedByUserId == employer.Id)
            .ToListAsync();

        return Ok(jobs);
    }




    [Authorize(Roles = "Employer, Admin")]
    [HttpGet("employer/applications/{applicationId}/download-cv")]
    public async Task<IActionResult> DownloadEmployerCv(int applicationId)
    {
        var username = User.Identity.Name;
        var employer = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
        if (employer == null)
        {
            return BadRequest("İşveren bulunamadı.");
        }

        var application = await _context.Applications.FindAsync(applicationId);
        if (application == null)
        {
            return NotFound("Başvuru bulunamadı.");
        }

        var job = await _context.Jobs.FindAsync(application.JobId);
        if (job == null || job.CreatedByUserId != employer.Id)
        {
            return Forbid("Bu başvuruya erişim yetkiniz yok.");
        }

        var filePath = application.CvFilePath;
        var fileName = application.CvFileName;
        var mimeType = "application/octet-stream";

        byte[] fileBytes = await System.IO.File.ReadAllBytesAsync(filePath);
        return File(fileBytes, mimeType, fileName);
    }








}
