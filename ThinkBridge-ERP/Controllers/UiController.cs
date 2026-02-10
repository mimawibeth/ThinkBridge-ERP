using Microsoft.AspNetCore.Mvc;

namespace ThinkBridge_ERP.Controllers
{
    public class UiController : Controller
    {
        // Helper method to set role from query parameter
        private void SetUserRole(string? role = null)
        {
            var userRole = role ?? Request.Query["role"].ToString();
            if (!string.IsNullOrEmpty(userRole))
            {
                ViewData["UserRole"] = userRole;
            }
        }

        // Company Admin Dashboard (existing)
        public IActionResult Dashboard()
        {
            ViewData["UserRole"] = "companyadmin";
            return View();
        }

        // Super Admin Dashboard
        public IActionResult SuperAdminDashboard()
        {
            ViewData["UserRole"] = "superadmin";
            return View();
        }

        // Super Admin - Companies Management
        public IActionResult Companies(string? role)
        {
            SetUserRole(role ?? "superadmin");
            return View();
        }

        // Super Admin - Subscriptions
        public IActionResult Subscriptions(string? role)
        {
            SetUserRole(role ?? "superadmin");
            return View();
        }

        // Super Admin - Payments
        public IActionResult Payments(string? role)
        {
            SetUserRole(role ?? "superadmin");
            return View();
        }

        // Project Manager Dashboard
        public IActionResult ProjectManagerDashboard()
        {
            ViewData["UserRole"] = "projectmanager";
            return View();
        }

        // Project Manager - Reports
        public IActionResult Reports(string? role)
        {
            SetUserRole(role ?? "projectmanager");
            return View();
        }

        // Team Member Dashboard
        public IActionResult TeamMemberDashboard()
        {
            ViewData["UserRole"] = "teammember";
            return View();
        }

        // Team Member - My Tasks
        public IActionResult MyTasks(string? role)
        {
            SetUserRole(role ?? "teammember");
            return View();
        }

        // Team Member - Documents
        public IActionResult Documents(string? role)
        {
            SetUserRole(role ?? "teammember");
            return View();
        }

        // Shared pages - role from query param
        public IActionResult Projects(string? role)
        {
            SetUserRole(role);
            return View();
        }

        public IActionResult Tasks(string? role)
        {
            SetUserRole(role);
            return View();
        }

        public IActionResult ProductLifecycle(string? role)
        {
            SetUserRole(role);
            return View();
        }

        public IActionResult Team(string? role)
        {
            SetUserRole(role);
            return View();
        }

        public IActionResult KnowledgeBase(string? role)
        {
            SetUserRole(role);
            return View();
        }

        public IActionResult Activity(string? role)
        {
            SetUserRole(role);
            return View();
        }

        public IActionResult AuditLog(string? role)
        {
            SetUserRole(role ?? "companyadmin");
            return View();
        }

        public IActionResult Settings(string? role)
        {
            SetUserRole(role);
            return View();
        }

        public IActionResult Login()
        {
            return View();
        }
    }
}
