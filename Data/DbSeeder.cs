using Microsoft.AspNetCore.Identity;
using WebstackInfrar.Models;

namespace WebstackInfrar.Data
{
    public static class DbSeeder
    {
        public static async Task SeedAsync(IServiceProvider services)
        {
            var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
            var db = services.GetRequiredService<ApplicationDbContext>();

            string[] roles = { "Admin", "Employee" };
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                    await roleManager.CreateAsync(new IdentityRole(role));
            }

            const string adminEmail = "admin@webstackinfrar.com";
            if (await userManager.FindByEmailAsync(adminEmail) == null)
            {
                var admin = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    FullName = "Super Admin",
                    Designation = "Administrator",
                    EmployeeType = EmployeeType.Current,
                    IsActive = true,
                    EmailConfirmed = true,
                    JoinDate = DateTime.UtcNow
                };

                var result = await userManager.CreateAsync(admin, "Admin@123456");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(admin, "Admin");

                    var allPermissions = db.Permissions.ToList();
                    foreach (var perm in allPermissions)
                    {
                        db.UserPermissions.Add(new UserPermission
                        {
                            UserId = admin.Id,
                            PermissionId = perm.Id
                        });
                    }
                    await db.SaveChangesAsync();
                }
            }
        }
    }
}