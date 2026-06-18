using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebstackInfrar.Data;
using WebstackInfrar.Models;
using WebstackInfrar.Services;
using WebstackInfrar.ViewModels;

namespace WebstackInfrar.Controllers
{
    [Authorize]
    public class EmployeeController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _users;
        private readonly IPermissionService _perms;

        public EmployeeController(ApplicationDbContext db,
                                  UserManager<ApplicationUser> users,
                                  IPermissionService perms)
        {
            _db = db;
            _users = users;
            _perms = perms;
        }

        public async Task<IActionResult> Dashboard()
        {
            var user = await _users.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            var today = DateTime.Today;
            var todayLog = await _db.WorkLogs
                .Where(w => w.UserId == user.Id && w.ClockIn.Date == today)
                .OrderByDescending(w => w.ClockIn)
                .FirstOrDefaultAsync();

            var recentLogs = await _db.WorkLogs
                .Where(w => w.UserId == user.Id)
                .OrderByDescending(w => w.ClockIn)
                .Take(7)
                .Select(w => new WorkLogViewModel
                {
                    Id = w.Id,
                    ClockIn = w.ClockIn,
                    ClockOut = w.ClockOut,
                    Duration = w.ClockOut.HasValue ? w.ClockOut.Value - w.ClockIn : null,
                    Notes = w.Notes
                }).ToListAsync();

            var currentSalary = await _db.Salaries
                .FirstOrDefaultAsync(s => s.UserId == user.Id
                    && s.Month == DateTime.Now.Month
                    && s.Year == DateTime.Now.Year);

            var model = new EmployeeDashboardViewModel
            {
                FullName = user.FullName,
                Designation = user.Designation,
                TodaysLog = todayLog,
                IsClockedIn = todayLog != null && !todayLog.ClockOut.HasValue,
                Permissions = await _perms.GetUserPermissionsAsync(user.Id),
                RecentLogs = recentLogs,
                CurrentMonthSalary = currentSalary
            };

            return View(model);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ClockIn()
        {
            var user = await _users.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var existing = await _db.WorkLogs
                .FirstOrDefaultAsync(w => w.UserId == user.Id
                    && w.ClockIn.Date == DateTime.Today
                    && w.ClockOut == null);

            if (existing != null)
            {
                TempData["Warning"] = "You are already clocked in.";
                return RedirectToAction(nameof(Dashboard));
            }

            _db.WorkLogs.Add(new WorkLog
            {
                UserId = user.Id,
                ClockIn = DateTime.Now
            });
            await _db.SaveChangesAsync();
            TempData["Success"] = $"Clocked in at {DateTime.Now:hh:mm tt}";
            return RedirectToAction(nameof(Dashboard));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ClockOut()
        {
            var user = await _users.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var log = await _db.WorkLogs
                .FirstOrDefaultAsync(w => w.UserId == user.Id
                    && w.ClockIn.Date == DateTime.Today
                    && w.ClockOut == null);

            if (log == null)
            {
                TempData["Warning"] = "No active clock-in found.";
                return RedirectToAction(nameof(Dashboard));
            }

            log.ClockOut = DateTime.Now;
            await _db.SaveChangesAsync();
            var duration = log.ClockOut.Value - log.ClockIn;
            TempData["Success"] = $"Clocked out at {DateTime.Now:hh:mm tt} — {(int)duration.TotalHours}h {duration.Minutes}m worked";
            return RedirectToAction(nameof(Dashboard));
        }

        public async Task<IActionResult> MyWorkHistory()
        {
            var user = await _users.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var logs = await _db.WorkLogs
                .Where(w => w.UserId == user.Id)
                .OrderByDescending(w => w.ClockIn)
                .Select(w => new WorkLogViewModel
                {
                    Id = w.Id,
                    ClockIn = w.ClockIn,
                    ClockOut = w.ClockOut,
                    Duration = w.ClockOut.HasValue ? w.ClockOut.Value - w.ClockIn : null,
                    Notes = w.Notes
                }).ToListAsync();

            return View(logs);
        }

        public async Task<IActionResult> MySalary()
        {
            var user = await _users.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var salaries = await _db.Salaries
                .Where(s => s.UserId == user.Id)
                .OrderByDescending(s => s.Year)
                .ThenByDescending(s => s.Month)
                .ToListAsync();

            return View(salaries);
        }
    }
}