using System.ComponentModel.DataAnnotations;
using WebstackInfrar.Models;

namespace WebstackInfrar.ViewModels
{
    public class LoginViewModel
    {
        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required, DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        public bool RememberMe { get; set; }
    }

    public class RegisterEmployeeViewModel
    {
        [Required, MaxLength(100)]
        [Display(Name = "Full Name")]
        public string FullName { get; set; } = string.Empty;

        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required, MinLength(8)]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        public string Designation { get; set; } = string.Empty;

        [Display(Name = "Employee Type")]
        public EmployeeType EmployeeType { get; set; } = EmployeeType.Current;

        [DataType(DataType.Date)]
        [Display(Name = "Join Date")]
        public DateTime JoinDate { get; set; } = DateTime.Today;

        [MaxLength(500)]
        public string? Address { get; set; }

        [Phone, Display(Name = "Phone Number")]
        public string? PhoneNumber { get; set; }

        [Display(Name = "Profile Photo")]
        public IFormFile? ProfilePhoto { get; set; }
        public List<int> SelectedPermissionIds { get; set; } = new();
        public List<PermissionCheckboxViewModel> AvailablePermissions { get; set; } = new();
    }

    public class PermissionCheckboxViewModel
    {
        public int Id { get; set; }
        public string FeatureName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsSelected { get; set; }
    }

    public class EditEmployeeViewModel
    {
        public string Id { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        [Display(Name = "Full Name")]
        public string FullName { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        public string Designation { get; set; } = string.Empty;

        [Display(Name = "Employee Type")]
        public EmployeeType EmployeeType { get; set; }

        public bool IsActive { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Join Date")]
        public DateTime JoinDate { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "End Date")]
        public DateTime? EndDate { get; set; }

        [MaxLength(500)]
        public string? Address { get; set; }

        [Phone]
        public string? PhoneNumber { get; set; }
        [Display(Name = "Profile Photo")]
        public IFormFile? ProfilePhoto { get; set; }

        public string? ExistingPhotoUrl { get; set; }
        public List<int> SelectedPermissionIds { get; set; } = new();
        public List<PermissionCheckboxViewModel> AvailablePermissions { get; set; } = new();
    }

    public class WorkLogViewModel
    {
        public int Id { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public string Designation { get; set; } = string.Empty;
        public DateTime ClockIn { get; set; }
        public DateTime? ClockOut { get; set; }
        public TimeSpan? Duration { get; set; }
        public string? Notes { get; set; }
    }

    public class WorkLogFilterViewModel
    {
        public string? EmployeeId { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string ReportType { get; set; } = "custom";
        public int? Month { get; set; }
        public int? Year { get; set; }
        public List<WorkLogViewModel> Logs { get; set; } = new();
        public List<ApplicationUser> Employees { get; set; } = new();
    }

    public class SaasProductViewModel
    {
        public int Id { get; set; }

        [Required, MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [Required, MaxLength(2000)]
        public string Description { get; set; } = string.Empty;

        [Range(0, double.MaxValue)]
        public decimal Price { get; set; }

        public string? ImageUrl { get; set; }

        [Display(Name = "Features (comma-separated)")]
        public string? Features { get; set; }

        public bool IsActive { get; set; } = true;
    }

    public class ProjectViewModel
    {
        public int Id { get; set; }

        [Required, MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required, MaxLength(2000)]
        public string Description { get; set; } = string.Empty;

        public ProjectStatus Status { get; set; } = ProjectStatus.Ongoing;

        public string? ImageUrl { get; set; }

        [Display(Name = "Client Name")]
        public string? ClientName { get; set; }

        [Display(Name = "Tech Stack")]
        public string? TechStack { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Start Date")]
        public DateTime StartDate { get; set; } = DateTime.Today;

        [DataType(DataType.Date)]
        [Display(Name = "Completion Date")]
        public DateTime? CompletionDate { get; set; }
    }

    public class CustomerViewModel
    {
        public int Id { get; set; }

        [Required, MaxLength(200)]
        [Display(Name = "Customer Name")]
        public string CustomerName { get; set; } = string.Empty;

        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Phone]
        public string Phone { get; set; } = string.Empty;

        public string? Address { get; set; }

        [Display(Name = "SaaS Product")]
        public int? SaasProductId { get; set; }

        [Display(Name = "Project")]
        public int? ProjectId { get; set; }

        [Range(0, double.MaxValue)]
        [Display(Name = "Total Amount")]
        public decimal TotalAmount { get; set; }

        [Range(0, double.MaxValue)]
        [Display(Name = "Paid Amount")]
        public decimal PaidAmount { get; set; }

        public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Pending;

        public string? Notes { get; set; }

        public decimal DueAmount => TotalAmount - PaidAmount;

        public List<SaasProduct> SaasProducts { get; set; } = new();
        public List<Project> Projects { get; set; } = new();
    }

    public class SalaryViewModel
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Employee")]
        public string UserId { get; set; } = string.Empty;

        public string EmployeeName { get; set; } = string.Empty;

        [Range(1, 12)]
        public int Month { get; set; } = DateTime.Now.Month;

        [Range(2000, 2100)]
        public int Year { get; set; } = DateTime.Now.Year;

        [Range(0, double.MaxValue)]
        public decimal Amount { get; set; }

        [Range(0, double.MaxValue)]
        public decimal Due { get; set; }

        public bool IsPaid { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Paid Date")]
        public DateTime? PaidDate { get; set; }

        public string? Notes { get; set; }

        public List<ApplicationUser> Employees { get; set; } = new();
    }

    public class SocialLinkViewModel
    {
        public int Id { get; set; }

        [Required, MaxLength(100)]
        public string Platform { get; set; } = string.Empty;

        [MaxLength(10)]
        public string Emoji { get; set; } = string.Empty;

        [Required, Url, MaxLength(500)]
        public string Url { get; set; } = string.Empty;

        [Display(Name = "Sort Order")]
        public int SortOrder { get; set; }

        public bool IsActive { get; set; } = true;
    }

    public class AdminDashboardViewModel
    {
        public int TotalEmployees { get; set; }
        public int TotalInterns { get; set; }
        public int TotalCustomers { get; set; }
        public int TotalProjects { get; set; }
        public int OngoingProjects { get; set; }
        public int DeliveredProjects { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal PendingDues { get; set; }
        public List<WorkLogViewModel> TodaysLogs { get; set; } = new();
    }

    public class EmployeeDashboardViewModel
    {
        public string FullName { get; set; } = string.Empty;
        public string Designation { get; set; } = string.Empty;
        public WorkLog? TodaysLog { get; set; }
        public bool IsClockedIn { get; set; }
        public List<string> Permissions { get; set; } = new();
        public List<WorkLogViewModel> RecentLogs { get; set; } = new();
        public Salary? CurrentMonthSalary { get; set; }
    }
    public class WorkLogExportRequest
    {
        public string? EmployeeId { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string ReportType { get; set; } = "custom";
        public int? Month { get; set; }
        public int? Year { get; set; }
        public string Format { get; set; } = "pdf"; // pdf or excel
    }
}