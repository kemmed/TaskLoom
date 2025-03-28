using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using diplom.Data;
using diplom.Models;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

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
            var projects = _context.Project.Where(x => x.CreatorID == userLoggedInID || 
            _context.UserProject.Any(ub => ub.ProjectID == x.ID && ub.UserID == userLoggedInID)).ToList();

            return View(projects);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateProject([Bind("ID,Name,Description,DeadlineDate")] Project project)
        {
            int? userSessionID = HttpContext.Session.GetInt32("UserID");

            User creator = _context.User.FirstOrDefault(x => x.ID == userSessionID);
            project.CreatorUser = creator;
            project.CreatorID = creator.ID;
            if (project.DeadlineDate.Value.Date < DateTime.Now.Date)
                project.DeadlineDate = DateTime.Now.Date;
            else
                project.DeadlineDate = project.DeadlineDate;
            project.ProjectStatus = ProjectStatus.InProcess;
            project.CreateDate = DateTime.Now.Date;
            _context.Add(project);

            await _context.SaveChangesAsync();

            UserProject userProject = new UserProject();
            userProject.ProjectID = project.ID;
            userProject.Project = project;
            userProject.UserID = creator.ID;
            userProject.User = creator;
            userProject.UserRole = UserRoles.Admin;
            _context.Add(userProject);

            await _context.SaveChangesAsync();

            StatusType statusType1 = new StatusType();
            statusType1.Name = "Сделать";
            statusType1.Project = project;
            statusType1.ProjectID = project.ID;

             StatusType statusType2 = new StatusType();
            statusType2.Name = "В процессе";
            statusType2.Project = project;
            statusType2.ProjectID = project.ID;

            StatusType statusType3 = new StatusType();
            statusType3.Name = "На проветке";
            statusType3.Project = project;
            statusType3.ProjectID = project.ID;

            StatusType statusType4 = new StatusType();
            statusType4.Name = "Выполнена";
            statusType4.Project = project;
            statusType4.ProjectID = project.ID;

            _context.Add(statusType1);
            _context.Add(statusType2);
            _context.Add(statusType3);
            _context.Add(statusType4);

            PriorityType priorityType1 = new PriorityType();
            priorityType1.Name = "Низкий";
            priorityType1.Project = project;
            priorityType1.ProjectID = project.ID;

            PriorityType priorityType2 = new PriorityType();
            priorityType2.Name = "Средний";
            priorityType2.Project = project;
            priorityType2.ProjectID = project.ID;

            PriorityType priorityType3 = new PriorityType();
            priorityType3.Name = "Высокий";
            priorityType3.Project = project;
            priorityType3.ProjectID = project.ID;

            _context.Add(priorityType1);
            _context.Add(priorityType2);
            _context.Add(priorityType3);
            await _context.SaveChangesAsync();

            return Redirect("AllProjects");
        }
    }
}
