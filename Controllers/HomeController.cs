using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebstackInfrar.Data;

namespace WebstackInfrar.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _db;

        public HomeController(ApplicationDbContext db) => _db = db;

        public async Task<IActionResult> Index()
        {
            ViewBag.Products = await _db.SaasProducts.Where(p => p.IsActive).ToListAsync();
            ViewBag.Projects = await _db.Projects.OrderByDescending(p => p.CreatedAt).ToListAsync();
            ViewBag.Employees = await _db.Users.Where(u => u.IsActive).OrderBy(u => u.EmployeeType).ToListAsync();
            ViewBag.SocialLinks = await _db.SocialLinks.Where(s => s.IsActive).OrderBy(s => s.SortOrder).ToListAsync();
            return View();
        }

        public IActionResult Error()
        {
            return View();
        }
    }
}