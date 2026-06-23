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
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _users;
        private readonly IPdfExportService _pdf;
        private readonly IExcelExportService _excel;
        private readonly IWebHostEnvironment _env;

        public AdminController(ApplicationDbContext db,
                               UserManager<ApplicationUser> users,
                               IPdfExportService pdf,
                               IExcelExportService excel,
                               IWebHostEnvironment env)
        {
            _db = db;
            _users = users;
            _pdf = pdf;
            _excel = excel;
            _env = env;
        }

        // ── DASHBOARD
        public async Task<IActionResult> Dashboard()
        {
            var today = DateTime.Today;
            var model = new AdminDashboardViewModel
            {
                TotalEmployees = await _db.Users.CountAsync(u => u.EmployeeType == EmployeeType.Current && u.IsActive),
                TotalInterns = await _db.Users.CountAsync(u => u.EmployeeType == EmployeeType.Intern && u.IsActive),
                TotalCustomers = await _db.CustomerRecords.CountAsync(),
                TotalProjects = await _db.Projects.CountAsync(),
                OngoingProjects = await _db.Projects.CountAsync(p => p.Status == ProjectStatus.Ongoing),
                DeliveredProjects = await _db.Projects.CountAsync(p => p.Status == ProjectStatus.Delivered),
                TotalRevenue = await _db.CustomerRecords.SumAsync(c => (decimal?)c.PaidAmount) ?? 0,
                PendingDues = await _db.CustomerRecords.SumAsync(c => (decimal?)(c.TotalAmount - c.PaidAmount)) ?? 0,
                TodaysLogs = await _db.WorkLogs
                    .Where(w => w.ClockIn.Date == today)
                    .Include(w => w.User)
                    .Select(w => new WorkLogViewModel
                    {
                        Id = w.Id,
                        EmployeeName = w.User.FullName,
                        Designation = w.User.Designation,
                        ClockIn = w.ClockIn,
                        ClockOut = w.ClockOut,
                        Duration = w.ClockOut.HasValue ? w.ClockOut.Value - w.ClockIn : null
                    }).ToListAsync()
            };
            return View(model);
        }

        // ── EMPLOYEES
        public async Task<IActionResult> Employees()
        {
            var employees = await _db.Users
                .OrderBy(u => u.EmployeeType).ThenBy(u => u.FullName)
                .ToListAsync();
            return View(employees);
        }

        [HttpGet]
        public async Task<IActionResult> RegisterEmployee()
        {
            var model = new RegisterEmployeeViewModel
            {
                AvailablePermissions = await GetPermissionCheckboxes(new List<int>())
            };
            return View(model);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> RegisterEmployee(RegisterEmployeeViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.AvailablePermissions = await GetPermissionCheckboxes(model.SelectedPermissionIds);
                return View(model);
            }

            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                FullName = model.FullName,
                Designation = model.Designation,
                EmployeeType = model.EmployeeType,
                JoinDate = model.JoinDate,
                Address = model.Address,
                PhoneNumber = model.PhoneNumber,
                IsActive = true,
                EmailConfirmed = true
            };

            if (model.ProfilePhoto != null && model.ProfilePhoto.Length > 0)
            {
                user.ProfileImageUrl = await SaveProfilePhoto(model.ProfilePhoto);
            }

            var result = await _users.CreateAsync(user, model.Password);
            if (!result.Succeeded)
            {
                foreach (var e in result.Errors)
                    ModelState.AddModelError("", e.Description);
                model.AvailablePermissions = await GetPermissionCheckboxes(model.SelectedPermissionIds);
                return View(model);
            }

            await _users.AddToRoleAsync(user, "Employee");
            foreach (var permId in model.SelectedPermissionIds)
                _db.UserPermissions.Add(new UserPermission { UserId = user.Id, PermissionId = permId });
            await _db.SaveChangesAsync();
            TempData["Success"] = $"{model.FullName} registered successfully.";
            return RedirectToAction(nameof(Employees));
        }

        [HttpGet]
        public async Task<IActionResult> EditEmployee(string id)
        {
            var user = await _db.Users
                .Include(u => u.UserPermissions)
                .FirstOrDefaultAsync(u => u.Id == id);
            if (user == null) return NotFound();
            var selectedIds = user.UserPermissions.Select(up => up.PermissionId).ToList();
            return View(new EditEmployeeViewModel
            {
                Id = user.Id,
                FullName = user.FullName,
                Designation = user.Designation,
                EmployeeType = user.EmployeeType,
                IsActive = user.IsActive,
                JoinDate = user.JoinDate,
                EndDate = user.EndDate,
                Address = user.Address,
                PhoneNumber = user.PhoneNumber,
                ExistingPhotoUrl = user.ProfileImageUrl,
                SelectedPermissionIds = selectedIds,
                AvailablePermissions = await GetPermissionCheckboxes(selectedIds)
            });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> EditEmployee(EditEmployeeViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.AvailablePermissions = await GetPermissionCheckboxes(model.SelectedPermissionIds);
                return View(model);
            }

            var user = await _db.Users.FindAsync(model.Id);
            if (user == null) return NotFound();

            user.FullName = model.FullName;
            user.Designation = model.Designation;
            user.EmployeeType = model.EmployeeType;
            user.IsActive = model.IsActive;
            user.JoinDate = model.JoinDate;
            user.EndDate = model.EndDate;
            user.Address = model.Address;
            user.PhoneNumber = model.PhoneNumber;

            if (model.ProfilePhoto != null && model.ProfilePhoto.Length > 0)
            {
                user.ProfileImageUrl = await SaveProfilePhoto(model.ProfilePhoto);
            }

            await _users.UpdateAsync(user);

            var existing = _db.UserPermissions.Where(up => up.UserId == model.Id);
            _db.UserPermissions.RemoveRange(existing);
            foreach (var permId in model.SelectedPermissionIds)
                _db.UserPermissions.Add(new UserPermission { UserId = model.Id, PermissionId = permId });

            await _db.SaveChangesAsync();
            TempData["Success"] = "Employee updated successfully.";
            return RedirectToAction(nameof(Employees));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteEmployee(string id)
        {
            var user = await _db.Users.FindAsync(id);
            if (user == null) return NotFound();
            await _users.DeleteAsync(user);
            TempData["Success"] = "Employee and all related records deleted.";
            return RedirectToAction(nameof(Employees));
        }

        // ── WORK LOGS
        public async Task<IActionResult> WorkLogs(WorkLogFilterViewModel filter)
        {
            filter.Logs = await GetFilteredLogs(filter);
            filter.Employees = await _db.Users
                .Where(u => u.IsActive)
                .OrderBy(u => u.FullName)
                .ToListAsync();
            return View(filter);
        }

        public async Task<IActionResult> ExportWorkLogsPdf(WorkLogFilterViewModel filter)
        {
            filter.Logs = await GetFilteredLogs(filter);
            string title = GetReportTitle(filter);
            var pdfBytes = _pdf.GenerateWorkLogReport(filter, title);
            return File(pdfBytes, "application/pdf", $"WorkLog_{filter.ReportType}_{DateTime.Now:yyyyMMdd}.pdf");
        }

        public async Task<IActionResult> ExportWorkLogsExcel(WorkLogFilterViewModel filter)
        {
            filter.Logs = await GetFilteredLogs(filter);
            string title = GetReportTitle(filter);
            var excelBytes = _excel.GenerateWorkLogExcel(filter, title);
            return File(excelBytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"WorkLog_{filter.ReportType}_{DateTime.Now:yyyyMMdd}.xlsx");
        }

        private async Task<List<WorkLogViewModel>> GetFilteredLogs(WorkLogFilterViewModel filter)
        {
            var query = _db.WorkLogs.Include(w => w.User).AsQueryable();

            if (!string.IsNullOrEmpty(filter.EmployeeId))
                query = query.Where(w => w.UserId == filter.EmployeeId);

            if (filter.ReportType == "daily" && filter.FromDate.HasValue)
            {
                query = query.Where(w => w.ClockIn.Date == filter.FromDate.Value.Date);
            }
            else if (filter.ReportType == "weekly" && filter.FromDate.HasValue)
            {
                var weekStart = filter.FromDate.Value.Date;
                var weekEnd = weekStart.AddDays(7);
                query = query.Where(w => w.ClockIn.Date >= weekStart && w.ClockIn.Date < weekEnd);
            }
            else if (filter.ReportType == "monthly" && filter.Month.HasValue && filter.Year.HasValue)
            {
                query = query.Where(w => w.ClockIn.Month == filter.Month && w.ClockIn.Year == filter.Year);
            }
            else if (filter.ReportType == "yearly" && filter.Year.HasValue)
            {
                query = query.Where(w => w.ClockIn.Year == filter.Year);
            }
            else
            {
                if (filter.FromDate.HasValue) query = query.Where(w => w.ClockIn.Date >= filter.FromDate.Value.Date);
                if (filter.ToDate.HasValue) query = query.Where(w => w.ClockIn.Date <= filter.ToDate.Value.Date);
            }

            return await query.OrderByDescending(w => w.ClockIn)
                .Select(w => new WorkLogViewModel
                {
                    Id = w.Id,
                    EmployeeName = w.User.FullName,
                    Designation = w.User.Designation,
                    ClockIn = w.ClockIn,
                    ClockOut = w.ClockOut,
                    Duration = w.ClockOut.HasValue ? w.ClockOut.Value - w.ClockIn : null,
                    Notes = w.Notes
                }).ToListAsync();
        }

        private string GetReportTitle(WorkLogFilterViewModel filter)
        {
            return filter.ReportType switch
            {
                "daily" => $"Daily Work Log - {filter.FromDate:dd MMM yyyy}",
                "weekly" => $"Weekly Work Log - {filter.FromDate:dd MMM yyyy} to {filter.FromDate?.AddDays(6):dd MMM yyyy}",
                "monthly" => $"Monthly Work Log - {new DateTime(filter.Year ?? DateTime.Now.Year, filter.Month ?? 1, 1):MMMM yyyy}",
                "yearly" => $"Yearly Work Log - {filter.Year}",
                _ => "Work Log Report"
            };
        }

        // ── PRODUCTS
        public async Task<IActionResult> Products() =>
            View(await _db.SaasProducts.OrderByDescending(p => p.CreatedAt).ToListAsync());

        [HttpGet]
        public IActionResult CreateProduct() => View(new SaasProductViewModel());

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateProduct(SaasProductViewModel model)
        {
            if (!ModelState.IsValid) return View(model);
            _db.SaasProducts.Add(new SaasProduct
            {
                Name = model.Name,
                Description = model.Description,
                Price = model.Price,
                ImageUrl = model.ImageUrl,
                Features = model.Features,
                IsActive = model.IsActive
            });
            await _db.SaveChangesAsync();
            TempData["Success"] = "Product added.";
            return RedirectToAction(nameof(Products));
        }

        [HttpGet]
        public async Task<IActionResult> EditProduct(int id)
        {
            var p = await _db.SaasProducts.FindAsync(id);
            if (p == null) return NotFound();
            return View(new SaasProductViewModel
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
                Price = p.Price,
                ImageUrl = p.ImageUrl,
                Features = p.Features,
                IsActive = p.IsActive
            });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProduct(SaasProductViewModel model)
        {
            if (!ModelState.IsValid) return View(model);
            var p = await _db.SaasProducts.FindAsync(model.Id);
            if (p == null) return NotFound();
            p.Name = model.Name;
            p.Description = model.Description;
            p.Price = model.Price;
            p.ImageUrl = model.ImageUrl;
            p.Features = model.Features;
            p.IsActive = model.IsActive;
            await _db.SaveChangesAsync();
            TempData["Success"] = "Product updated.";
            return RedirectToAction(nameof(Products));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            var p = await _db.SaasProducts.FindAsync(id);
            if (p == null) return NotFound();
            _db.SaasProducts.Remove(p);
            await _db.SaveChangesAsync();
            TempData["Success"] = "Product deleted.";
            return RedirectToAction(nameof(Products));
        }

        // ── PROJECTS
        public async Task<IActionResult> Projects() =>
            View(await _db.Projects.OrderByDescending(p => p.CreatedAt).ToListAsync());

        [HttpGet]
        public IActionResult CreateProject() => View(new ProjectViewModel { StartDate = DateTime.Today });

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateProject(ProjectViewModel model)
        {
            if (!ModelState.IsValid) return View(model);
            _db.Projects.Add(new Project
            {
                Title = model.Title,
                Description = model.Description,
                Status = model.Status,
                ImageUrl = model.ImageUrl,
                ClientName = model.ClientName,
                TechStack = model.TechStack,
                StartDate = model.StartDate,
                CompletionDate = model.CompletionDate
            });
            await _db.SaveChangesAsync();
            TempData["Success"] = "Project added.";
            return RedirectToAction(nameof(Projects));
        }

        [HttpGet]
        public async Task<IActionResult> EditProject(int id)
        {
            var p = await _db.Projects.FindAsync(id);
            if (p == null) return NotFound();
            return View(new ProjectViewModel
            {
                Id = p.Id,
                Title = p.Title,
                Description = p.Description,
                Status = p.Status,
                ImageUrl = p.ImageUrl,
                ClientName = p.ClientName,
                TechStack = p.TechStack,
                StartDate = p.StartDate,
                CompletionDate = p.CompletionDate
            });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProject(ProjectViewModel model)
        {
            if (!ModelState.IsValid) return View(model);
            var p = await _db.Projects.FindAsync(model.Id);
            if (p == null) return NotFound();
            p.Title = model.Title;
            p.Description = model.Description;
            p.Status = model.Status;
            p.ImageUrl = model.ImageUrl;
            p.ClientName = model.ClientName;
            p.TechStack = model.TechStack;
            p.StartDate = model.StartDate;
            p.CompletionDate = model.CompletionDate;
            await _db.SaveChangesAsync();
            TempData["Success"] = "Project updated.";
            return RedirectToAction(nameof(Projects));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteProject(int id)
        {
            var p = await _db.Projects.FindAsync(id);
            if (p == null) return NotFound();
            _db.Projects.Remove(p);
            await _db.SaveChangesAsync();
            TempData["Success"] = "Project deleted.";
            return RedirectToAction(nameof(Projects));
        }

        // ── CUSTOMERS
        public async Task<IActionResult> Customers() =>
            View(await _db.CustomerRecords
                .Include(c => c.SaasProduct)
                .Include(c => c.Project)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync());

        [HttpGet]
        public async Task<IActionResult> CreateCustomer()
        {
            return View(new CustomerViewModel
            {
                SaasProducts = await _db.SaasProducts.Where(p => p.IsActive).ToListAsync(),
                Projects = await _db.Projects.ToListAsync()
            });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateCustomer(CustomerViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.SaasProducts = await _db.SaasProducts.Where(p => p.IsActive).ToListAsync();
                model.Projects = await _db.Projects.ToListAsync();
                return View(model);
            }
            _db.CustomerRecords.Add(new CustomerRecord
            {
                CustomerName = model.CustomerName,
                Email = model.Email,
                Phone = model.Phone,
                Address = model.Address,
                SaasProductId = model.SaasProductId,
                ProjectId = model.ProjectId,
                TotalAmount = model.TotalAmount,
                PaidAmount = model.PaidAmount,
                PaymentStatus = model.PaymentStatus,
                Notes = model.Notes
            });
            await _db.SaveChangesAsync();
            TempData["Success"] = "Customer added.";
            return RedirectToAction(nameof(Customers));
        }

        [HttpGet]
        public async Task<IActionResult> EditCustomer(int id)
        {
            var c = await _db.CustomerRecords.FindAsync(id);
            if (c == null) return NotFound();
            return View(new CustomerViewModel
            {
                Id = c.Id,
                CustomerName = c.CustomerName,
                Email = c.Email,
                Phone = c.Phone,
                Address = c.Address,
                SaasProductId = c.SaasProductId,
                ProjectId = c.ProjectId,
                TotalAmount = c.TotalAmount,
                PaidAmount = c.PaidAmount,
                PaymentStatus = c.PaymentStatus,
                Notes = c.Notes,
                SaasProducts = await _db.SaasProducts.Where(p => p.IsActive).ToListAsync(),
                Projects = await _db.Projects.ToListAsync()
            });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> EditCustomer(CustomerViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.SaasProducts = await _db.SaasProducts.Where(p => p.IsActive).ToListAsync();
                model.Projects = await _db.Projects.ToListAsync();
                return View(model);
            }
            var c = await _db.CustomerRecords.FindAsync(model.Id);
            if (c == null) return NotFound();
            c.CustomerName = model.CustomerName;
            c.Email = model.Email;
            c.Phone = model.Phone;
            c.Address = model.Address;
            c.SaasProductId = model.SaasProductId;
            c.ProjectId = model.ProjectId;
            c.TotalAmount = model.TotalAmount;
            c.PaidAmount = model.PaidAmount;
            c.PaymentStatus = model.PaymentStatus;
            c.Notes = model.Notes;
            await _db.SaveChangesAsync();
            TempData["Success"] = "Customer updated.";
            return RedirectToAction(nameof(Customers));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCustomer(int id)
        {
            var c = await _db.CustomerRecords.FindAsync(id);
            if (c == null) return NotFound();
            _db.CustomerRecords.Remove(c);
            await _db.SaveChangesAsync();
            TempData["Success"] = "Customer deleted.";
            return RedirectToAction(nameof(Customers));
        }

        // ── SALARIES
        public async Task<IActionResult> Salaries() =>
            View(await _db.Salaries
                .Include(s => s.User)
                .OrderByDescending(s => s.Year)
                .ThenByDescending(s => s.Month)
                .ToListAsync());

        [HttpGet]
        public async Task<IActionResult> CreateSalary()
        {
            return View(new SalaryViewModel
            {
                Employees = await _db.Users
                    .Where(u => u.IsActive)
                    .OrderBy(u => u.FullName)
                    .ToListAsync()
            });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateSalary(SalaryViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.Employees = await _db.Users.Where(u => u.IsActive).OrderBy(u => u.FullName).ToListAsync();
                return View(model);
            }
            _db.Salaries.Add(new Salary
            {
                UserId = model.UserId,
                Month = model.Month,
                Year = model.Year,
                Amount = model.Amount,
                Due = model.Due,
                IsPaid = model.IsPaid,
                PaidDate = model.PaidDate,
                Notes = model.Notes
            });
            await _db.SaveChangesAsync();
            TempData["Success"] = "Salary record added.";
            return RedirectToAction(nameof(Salaries));
        }

        [HttpGet]
        public async Task<IActionResult> EditSalary(int id)
        {
            var s = await _db.Salaries
                .Include(x => x.User)
                .FirstOrDefaultAsync(x => x.Id == id);
            if (s == null) return NotFound();
            return View(new SalaryViewModel
            {
                Id = s.Id,
                UserId = s.UserId,
                EmployeeName = s.User.FullName,
                Month = s.Month,
                Year = s.Year,
                Amount = s.Amount,
                Due = s.Due,
                IsPaid = s.IsPaid,
                PaidDate = s.PaidDate,
                Notes = s.Notes,
                Employees = await _db.Users.Where(u => u.IsActive).OrderBy(u => u.FullName).ToListAsync()
            });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> EditSalary(SalaryViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.Employees = await _db.Users.Where(u => u.IsActive).OrderBy(u => u.FullName).ToListAsync();
                return View(model);
            }
            var s = await _db.Salaries.FindAsync(model.Id);
            if (s == null) return NotFound();
            s.UserId = model.UserId;
            s.Month = model.Month;
            s.Year = model.Year;
            s.Amount = model.Amount;
            s.Due = model.Due;
            s.IsPaid = model.IsPaid;
            s.PaidDate = model.PaidDate;
            s.Notes = model.Notes;
            await _db.SaveChangesAsync();
            TempData["Success"] = "Salary updated.";
            return RedirectToAction(nameof(Salaries));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteSalary(int id)
        {
            var s = await _db.Salaries.FindAsync(id);
            if (s == null) return NotFound();
            _db.Salaries.Remove(s);
            await _db.SaveChangesAsync();
            TempData["Success"] = "Salary record deleted.";
            return RedirectToAction(nameof(Salaries));
        }

        // ── SOCIAL LINKS
        public async Task<IActionResult> SocialLinks() =>
            View(await _db.SocialLinks.OrderBy(s => s.SortOrder).ToListAsync());

        [HttpGet]
        public IActionResult CreateSocialLink() => View(new SocialLinkViewModel());

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateSocialLink(SocialLinkViewModel model)
        {
            if (!ModelState.IsValid) return View(model);
            _db.SocialLinks.Add(new SocialLink
            {
                Platform = model.Platform,
                Emoji = model.Emoji,
                Url = model.Url,
                SortOrder = model.SortOrder,
                IsActive = model.IsActive
            });
            await _db.SaveChangesAsync();
            TempData["Success"] = "Social link added.";
            return RedirectToAction(nameof(SocialLinks));
        }

        [HttpGet]
        public async Task<IActionResult> EditSocialLink(int id)
        {
            var s = await _db.SocialLinks.FindAsync(id);
            if (s == null) return NotFound();
            return View(new SocialLinkViewModel
            {
                Id = s.Id,
                Platform = s.Platform,
                Emoji = s.Emoji,
                Url = s.Url,
                SortOrder = s.SortOrder,
                IsActive = s.IsActive
            });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> EditSocialLink(SocialLinkViewModel model)
        {
            if (!ModelState.IsValid) return View(model);
            var s = await _db.SocialLinks.FindAsync(model.Id);
            if (s == null) return NotFound();
            s.Platform = model.Platform;
            s.Emoji = model.Emoji;
            s.Url = model.Url;
            s.SortOrder = model.SortOrder;
            s.IsActive = model.IsActive;
            await _db.SaveChangesAsync();
            TempData["Success"] = "Social link updated.";
            return RedirectToAction(nameof(SocialLinks));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteSocialLink(int id)
        {
            var s = await _db.SocialLinks.FindAsync(id);
            if (s == null) return NotFound();
            _db.SocialLinks.Remove(s);
            await _db.SaveChangesAsync();
            TempData["Success"] = "Social link deleted.";
            return RedirectToAction(nameof(SocialLinks));
        }

        // ── HELPERS
        private async Task<List<PermissionCheckboxViewModel>> GetPermissionCheckboxes(List<int> selectedIds)
        {
            var permissions = await _db.Permissions.ToListAsync();
            return permissions.Select(p => new PermissionCheckboxViewModel
            {
                Id = p.Id,
                FeatureName = p.FeatureName,
                Description = p.Description,
                IsSelected = selectedIds.Contains(p.Id)
            }).ToList();
        }

        private async Task<string> SaveProfilePhoto(IFormFile photo)
        {
            var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", "profiles");
            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);

            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(photo.FileName)}";
            var filePath = Path.Combine(uploadsFolder, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await photo.CopyToAsync(stream);
            }

            return $"/uploads/profiles/{fileName}";
        }
    }
}