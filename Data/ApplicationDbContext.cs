using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using WebstackInfrar.Models;

namespace WebstackInfrar.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        public DbSet<Permission> Permissions { get; set; }
        public DbSet<UserPermission> UserPermissions { get; set; }
        public DbSet<WorkLog> WorkLogs { get; set; }
        public DbSet<SaasProduct> SaasProducts { get; set; }
        public DbSet<Project> Projects { get; set; }
        public DbSet<CustomerRecord> CustomerRecords { get; set; }
        public DbSet<Salary> Salaries { get; set; }
        public DbSet<SocialLink> SocialLinks { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<WorkLog>()
                .HasOne(w => w.User)
                .WithMany(u => u.WorkLogs)
                .HasForeignKey(w => w.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Salary>()
                .HasOne(s => s.User)
                .WithMany(u => u.Salaries)
                .HasForeignKey(s => s.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<UserPermission>()
                .HasOne(up => up.User)
                .WithMany(u => u.UserPermissions)
                .HasForeignKey(up => up.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<UserPermission>()
                .HasOne(up => up.Permission)
                .WithMany(p => p.UserPermissions)
                .HasForeignKey(up => up.PermissionId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<CustomerRecord>()
                .HasOne(c => c.SaasProduct)
                .WithMany(p => p.CustomerRecords)
                .HasForeignKey(c => c.SaasProductId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.Entity<CustomerRecord>()
                .HasOne(c => c.Project)
                .WithMany(p => p.CustomerRecords)
                .HasForeignKey(c => c.ProjectId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.Entity<Permission>().HasData(
                new Permission { Id = 1, FeatureName = "ManageEmployees", Description = "Register, edit, delete employees" },
                new Permission { Id = 2, FeatureName = "ManageCustomers", Description = "View and manage customer records" },
                new Permission { Id = 3, FeatureName = "ManageProjects", Description = "Add, edit, delete projects" },
                new Permission { Id = 4, FeatureName = "ManageProducts", Description = "Add, edit, delete SaaS products" },
                new Permission { Id = 5, FeatureName = "ViewWorkLogs", Description = "View all employee work logs" },
                new Permission { Id = 6, FeatureName = "ManageSalaries", Description = "Manage employee salaries and dues" },
                new Permission { Id = 7, FeatureName = "ExportReports", Description = "Export PDF reports" },
                new Permission { Id = 8, FeatureName = "ManageSocialLinks", Description = "Manage social media links" }
            );

            builder.Entity<SocialLink>().HasData(
                new SocialLink { Id = 1, Platform = "Facebook Page", Emoji = "🔵", Url = "https://facebook.com/webstackinfrar", SortOrder = 1, IsActive = true },
                new SocialLink { Id = 2, Platform = "Facebook Group", Emoji = "👥", Url = "https://facebook.com/groups/webstackinfrar", SortOrder = 2, IsActive = true },
                new SocialLink { Id = 3, Platform = "WhatsApp", Emoji = "💬", Url = "https://wa.me/8801XXXXXXXXX", SortOrder = 3, IsActive = true },
                new SocialLink { Id = 4, Platform = "LinkedIn", Emoji = "💼", Url = "https://linkedin.com/company/webstackinfrar", SortOrder = 4, IsActive = true }
            );
        }
    }
}