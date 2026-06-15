using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebstackInfrar.Models
{
    public class ApplicationUser : IdentityUser
    {
        [Required, MaxLength(100)]
        public string FullName { get; set; } = string.Empty;

        [MaxLength(100)]
        public string Designation { get; set; } = string.Empty;

        public EmployeeType EmployeeType { get; set; } = EmployeeType.Current;

        public bool IsActive { get; set; } = true;

        public DateTime JoinDate { get; set; } = DateTime.UtcNow;

        public DateTime? EndDate { get; set; }

        [MaxLength(300)]
        public string? ProfileImageUrl { get; set; }

        [MaxLength(500)]
        public string? Address { get; set; }

        public ICollection<UserPermission> UserPermissions { get; set; } = new List<UserPermission>();
        public ICollection<WorkLog> WorkLogs { get; set; } = new List<WorkLog>();
        public ICollection<Salary> Salaries { get; set; } = new List<Salary>();
    }

    public enum EmployeeType { Current, Ex, Intern }

    public class Permission
    {
        public int Id { get; set; }

        [Required, MaxLength(100)]
        public string FeatureName { get; set; } = string.Empty;

        [MaxLength(300)]
        public string Description { get; set; } = string.Empty;

        public ICollection<UserPermission> UserPermissions { get; set; } = new List<UserPermission>();
    }

    public class UserPermission
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        public int PermissionId { get; set; }

        [ForeignKey("UserId")]
        public ApplicationUser User { get; set; } = null!;

        [ForeignKey("PermissionId")]
        public Permission Permission { get; set; } = null!;
    }

    public class WorkLog
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        public DateTime ClockIn { get; set; }

        public DateTime? ClockOut { get; set; }

        public string? Notes { get; set; }

        [ForeignKey("UserId")]
        public ApplicationUser User { get; set; } = null!;

        [NotMapped]
        public TimeSpan? Duration => ClockOut.HasValue ? ClockOut.Value - ClockIn : null;
    }

    public class SaasProduct
    {
        public int Id { get; set; }

        [Required, MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(2000)]
        public string Description { get; set; } = string.Empty;

        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }

        [MaxLength(500)]
        public string? ImageUrl { get; set; }

        [MaxLength(500)]
        public string? Features { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<CustomerRecord> CustomerRecords { get; set; } = new List<CustomerRecord>();
    }

    public class Project
    {
        public int Id { get; set; }

        [Required, MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(2000)]
        public string Description { get; set; } = string.Empty;

        public ProjectStatus Status { get; set; } = ProjectStatus.Ongoing;

        [MaxLength(500)]
        public string? ImageUrl { get; set; }

        [MaxLength(200)]
        public string? ClientName { get; set; }

        [MaxLength(300)]
        public string? TechStack { get; set; }

        public DateTime StartDate { get; set; }

        public DateTime? CompletionDate { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<CustomerRecord> CustomerRecords { get; set; } = new List<CustomerRecord>();
    }

    public enum ProjectStatus { Ongoing, Delivered }

    public class CustomerRecord
    {
        public int Id { get; set; }

        [Required, MaxLength(200)]
        public string CustomerName { get; set; } = string.Empty;

        [MaxLength(200)]
        public string Email { get; set; } = string.Empty;

        [MaxLength(30)]
        public string Phone { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Address { get; set; }

        public int? SaasProductId { get; set; }

        public int? ProjectId { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal PaidAmount { get; set; }

        [NotMapped]
        public decimal DueAmount => TotalAmount - PaidAmount;

        public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Pending;

        [MaxLength(1000)]
        public string? Notes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey("SaasProductId")]
        public SaasProduct? SaasProduct { get; set; }

        [ForeignKey("ProjectId")]
        public Project? Project { get; set; }
    }

    public enum PaymentStatus { Pending, Partial, Paid }

    public class Salary
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        public int Month { get; set; }

        public int Year { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Due { get; set; }

        public bool IsPaid { get; set; } = false;

        public DateTime? PaidDate { get; set; }

        [MaxLength(500)]
        public string? Notes { get; set; }

        [ForeignKey("UserId")]
        public ApplicationUser User { get; set; } = null!;
    }

    public class SocialLink
    {
        public int Id { get; set; }

        [Required, MaxLength(100)]
        public string Platform { get; set; } = string.Empty;

        [MaxLength(10)]
        public string Emoji { get; set; } = string.Empty;

        [Required, MaxLength(500)]
        public string Url { get; set; } = string.Empty;

        public int SortOrder { get; set; } = 0;

        public bool IsActive { get; set; } = true;
    }
}