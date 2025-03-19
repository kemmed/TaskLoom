using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using diplom.Data;
using diplom.Models;

namespace diplom.Controllers
{
    public class ProjectsController : Controller
    {
        private readonly diplomContext _context;

        public ProjectsController(diplomContext context)
        {
            _context = context;
        }
        public async Task<IActionResult> AllProjects()
        {
            int? userLoggedInID = HttpContext.Session.GetInt32("UserID");
            if (userLoggedInID == null)
                return Redirect("/Users/Authorization");
            var projects = _context.Project.Where(x => x.CreatorID == userLoggedInID || _context.UserProject.Any(ub => ub.ProjectID == x.ID && ub.UserID == userLoggedInID)).ToList();

            return View(projects);
        }
    }
}
