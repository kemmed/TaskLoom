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
using Microsoft.CodeAnalysis;
using Microsoft.Build.Evaluation;
using diplom.Services;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages.Manage;
using System.Net;

namespace diplom.Controllers
{
    public class ProjectsController : Controller
    {
        private readonly diplomContext _context;
        private readonly MailService _mailService;
        private readonly TokenService _tokenService;


        public ProjectsController(diplomContext context, MailService mailService, TokenService tokenService)
        {
            _context = context;
            _mailService = mailService;
            _tokenService = tokenService;
        }
        public IActionResult InviteUserMessage(string message, int projectID)
        {
            ViewBag.message = message;
            ViewBag.projectID = projectID;
            return View();
        }
        public async Task<IActionResult> AllProjects(string statusFilter = null)
        {
            int? userLoggedInID = HttpContext.Session.GetInt32("UserID");
            if (userLoggedInID == null)
                return Redirect("/Users/Authorization");

            var allProjects = _context.Project.Where(x => x.CreatorID == userLoggedInID ||
                _context.UserProject.Any(ub => ub.ProjectID == x.ID && ub.UserID == userLoggedInID)).ToList();

            var statusCounts = new Dictionary<ProjectStatus, int>
            {
                { ProjectStatus.InProcess, allProjects.Count(p => p.Status == ProjectStatus.InProcess) },
                { ProjectStatus.Completed, allProjects.Count(p => p.Status == ProjectStatus.Completed) },
                { ProjectStatus.Frozen, allProjects.Count(p => p.Status == ProjectStatus.Frozen) }
            };

            ViewBag.StatusCounts = statusCounts;
            var filteredProjects = allProjects.AsQueryable();
            if (!string.IsNullOrEmpty(statusFilter))
            {
                if (Enum.TryParse(statusFilter, out ProjectStatus filter))
                {
                    filteredProjects = filteredProjects.Where(p => p.Status == filter);
                }
            }

            ViewBag.StatusFilter = statusFilter;
            ViewBag.UserID = _context.User.FirstOrDefault(x => x.ID == HttpContext.Session.GetInt32("UserID")).ID;
            return View(filteredProjects.ToList());
        }

        [HttpGet("/Projects/Project/{projectID}")]
        public async Task<IActionResult> Project(int projectID)
        {
            if (_context.Project.FirstOrDefault(x => x.ID == projectID) == null ||
                HttpContext.Session.GetInt32("UserID") == null ||
                _context.UserProject.FirstOrDefault(x => x.UserID == HttpContext.Session.GetInt32("UserID")) == null)
            {
                return NotFound();
            }
            var columns = _context.StatusType.Where(x => x.ProjectID == projectID)
                .Include(x => x.Project)
                .ThenInclude(x => x.PriorityTypes)
                .Include(x => x.Issues.Where(y => !y.IsDelete))
                .ThenInclude(x => x.Performer).ToList();

            //var project = _context.Project.Where(x => x.ID == projectID)
            //    .Include(x => x.StatusTypes)
            //    .Include(x => x.PriorityTypes)
            //    .Include(x => x.Issues)
            //    .ThenInclude(x => x.Performer)

            ViewBag.ProjectName = _context.Project.FirstOrDefault(x => x.ID == projectID).Name;
            ViewBag.ProjectID = _context.Project.FirstOrDefault(x => x.ID == projectID).ID;

            var reponsibilities = _context.UserProject.Where(x => x.ProjectID == projectID)
                .Select(c => new SelectListItem
                {
                    Value = c.UserID.ToString(),
                    Text = c.User.LName +" "+ c.User.FName  
                }).ToList();
            var categories = _context.CategoryType.Where(x => x.ProjectID == projectID)
                .Select(c => new SelectListItem
            {
                Value = c.ID.ToString(),
                Text = c.Name
            }).ToList();
            var priorities = _context.PriorityType.Where(x => x.ProjectID == projectID)
                .Select(c => new SelectListItem
            {
                Value = c.ID.ToString(),
                Text = c.Name
            }).ToList();
            var statuses = _context.StatusType.Where(x => x.ProjectID == projectID)
                .Select(c => new SelectListItem
            {
                Value = c.ID.ToString(),
                Text = c.Name
            }).ToList();

            ViewBag.Responsibilities = reponsibilities;
            ViewBag.Categories = categories;
            ViewBag.Priorities = priorities;
            ViewBag.Statuses = statuses;


            UserRoles userRole = _context.UserProject.FirstOrDefault(x => x.UserID == HttpContext.Session.GetInt32("UserID") && x.ProjectID == projectID).UserRole;
            if (userRole == UserRoles.Admin)
                ViewBag.userRole = 1;
            else if(userRole == UserRoles.Manager)
                ViewBag.userRole = 2;
            else 
                ViewBag.userRole = 3;

            List<Issue> issues = new List<Issue>();
            issues = _context.Issue.Where(x => x.ProjectID == projectID && !x.IsDelete).ToList();
            ViewBag.projectIssues = issues;
            ViewBag.UserID = _context.User.FirstOrDefault(x => x.ID == HttpContext.Session.GetInt32("UserID")).ID;
            return View(columns);
        }

        [HttpGet("/Projects/ProjectSettings/{projectID}")]
        public async Task<IActionResult> ProjectSettings(int projectID)
        {
            if (_context.Project.FirstOrDefault(x => x.ID == projectID) == null ||
                HttpContext.Session.GetInt32("UserID") == null ||
                _context.UserProject.FirstOrDefault(x => x.UserID == HttpContext.Session.GetInt32("UserID")) == null)
            {
                return NotFound();
            }

            var project = await _context.Project
                .Include(p => p.UserProjects.Where(up => up.IsActive))
                .ThenInclude(up => up.User)
                .Include(p => p.CategoryTypes)
                .Include(p => p.PriorityTypes)
                .Include(p => p.StatusTypes)
                .FirstOrDefaultAsync(x => x.ID == projectID);

            if (project == null)
            {
                return NotFound();
            }
            project.UserProjects = project.UserProjects
                .OrderByDescending(up => up.UserRole)
                .ToList();

            ViewBag.ProjectID = projectID;
            ViewBag.UserRole = _context.UserProject.FirstOrDefault(x => x.UserID == HttpContext.Session.GetInt32("UserID")).UserRole;
            return View(project);
        }

        //Создание проекта
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateProject([Bind("ID,Name,Description,DeadlineDate")] Models.Project project)
        {
            int? userSessionID = HttpContext.Session.GetInt32("UserID");

            User creator = _context.User.FirstOrDefault(x => x.ID == userSessionID);
            project.CreatorUser = creator;
            project.CreatorID = creator.ID;
            if (project.DeadlineDate.Value.Date < DateTime.Now.Date)
                project.DeadlineDate = DateTime.Now.Date;
            else
                project.DeadlineDate = project.DeadlineDate;
            project.Status = ProjectStatus.InProcess;
            project.CreateDate = DateTime.Now.Date;
            _context.Add(project);

            await _context.SaveChangesAsync();

            UserProject userProject = new UserProject();
            userProject.ProjectID = project.ID;
            userProject.Project = project;
            userProject.UserID = creator.ID;
            userProject.User = creator;
            userProject.UserRole = UserRoles.Admin;
            userProject.IsActive = true;
            _context.Add(userProject);

            await _context.SaveChangesAsync();

            StatusType statusType1 = new StatusType();
            statusType1.Name = "В процессе";
            statusType1.Project = project;
            statusType1.ProjectID = project.ID;

            StatusType statusType2 = new StatusType();
            statusType2.Name = "На проверке";
            statusType2.Project = project;
            statusType2.ProjectID = project.ID;

            StatusType statusType3 = new StatusType();
            statusType3.Name = "Отложенные";
            statusType3.Project = project;
            statusType3.ProjectID = project.ID;

            StatusType statusType4 = new StatusType();
            statusType4.Name = "Завершенные";
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

        //Редактирование проекта
        [HttpPost]
        public async Task<IActionResult> EditProject([Bind("ID,Name,Description,Status,DeadlineDate")] Models.Project project, IFormCollection formData, int projectID, string returnUrl = null)
        {
            var currProject = await _context.Project.FindAsync(int.Parse(formData["projectID"]));
            currProject.Name = project.Name;
            currProject.Description = project.Description;
            currProject.Status = project.Status;
            if (project.DeadlineDate.Value.Date >= DateTime.Now.Date || project.DeadlineDate.Value.Date > currProject.DeadlineDate.Value.Date)
                currProject.DeadlineDate = project.DeadlineDate.Value.Date;
            if (currProject.Status == ProjectStatus.Completed)
                currProject.EndDate = DateTime.Now.Date;
            else
                currProject.EndDate = null;
            await _context.SaveChangesAsync();
            return Redirect(string.IsNullOrEmpty(returnUrl) ? "/Projects/AllProjects" : returnUrl);
        }

        //Отправка приглашения на присоединение к проектку
        [HttpPost]
        public async Task<IActionResult> InviteUser([Bind("Email")] User user, int projectID)
        {
            string message;
            var invitedUser = await _context.User
                .FirstOrDefaultAsync(m => m.Email == user.Email);

            if (invitedUser == null || !invitedUser.IsActive)
            {
                message = "Пользователя с таким email не существует в системе.";
                return RedirectToAction("InviteUserMessage", new { message, projectID });
            }
            var existingUserProject = await _context.UserProject
                .FirstOrDefaultAsync(up => up.UserID == invitedUser.ID && up.ProjectID == projectID);

            if (existingUserProject != null)
            {
                message = "Этот пользователь уже присоединен к проекту.";
                return RedirectToAction("InviteUserMessage", new { message, projectID });
            }

            string inviteToken = _tokenService.GenerateToken();

            var userProject = new UserProject
            {
                UserID = invitedUser.ID,
                ProjectID = projectID,
                UserRole = UserRoles.Employee,
                InviteToken = inviteToken,
                InviteTokenDate = DateTime.Now,
                IsActive = false
            };

            _context.UserProject.Add(userProject);

            string inviteLink = $"https://localhost:7297/Projects/AcceptInvite?token={inviteToken}&projectID={projectID}";

            string templatePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "emailMessages", "inviteUser.html");
            string emailTemplate = System.IO.File.ReadAllText(templatePath);

            string emailBody = emailTemplate.Replace("{inviteLink}", inviteLink);
            emailBody = emailBody.Replace("{FName}", invitedUser.FName);
            emailBody = emailBody.Replace("{LName}", invitedUser.LName);

            _mailService.SendEmail(emailBody, "Приглашение в проект", invitedUser.Email);

            await _context.SaveChangesAsync();

            message = "Приглашение успешно отправлено. Дождитесь, пока пользователь примет ваше приглашение.";
            return RedirectToAction("InviteUserMessage", new { message, projectID });
        }
        //Принятие приглашения
        [HttpGet]
        public IActionResult AcceptInvite(string token)
        {
            UserProject userProject = _context.UserProject.FirstOrDefault(u => u.InviteToken == token);
            if (userProject == null)
            {
                ViewBag.message = "Ошибка присоединения к проекту.";
                ViewBag.success = false;
            }
            else if ((DateTime.Now - userProject.InviteTokenDate).Value.Hours >= 24)
            {
                ViewBag.message = "Время действия приглашения истекло.<br/>Попробуйте снова.";
                ViewBag.success = false;
            }
            else if (!userProject.IsActive)
            {
                ViewBag.message = "Вы успешно присоеденены к проекту.";
                userProject.IsActive = true;
                ViewBag.success = true;
            }
            else
            {
                ViewBag.message = "Вы уже состоите в проекте.";
                ViewBag.success = true;
            }
            _context.SaveChanges();
            return View();
        }
        //Удаление пользователя
        [HttpPost]
        public async Task<IActionResult> RemoveUserFromProject(int userProjectID)
        {
            UserProject userProject = _context.UserProject.FirstOrDefault(x => x.ID == userProjectID);
            int projectID = userProject.ProjectID;
            _context.UserProject.Remove(userProject);
            _context.SaveChanges();
            return Redirect($"/Projects/ProjectSettings/{projectID}");
        }
        //Выход из доски
        [HttpPost]
        public async Task<IActionResult> LeaveProject(int projectID)
        {
            int? userSessionID = HttpContext.Session.GetInt32("UserID");
            var userProject = await _context.UserProject.FirstOrDefaultAsync(up => up.ProjectID == projectID && up.UserID == userSessionID);
            _context.UserProject.Remove(userProject);
            await _context.SaveChangesAsync();
            return Redirect("/Projects/AllProjects");
        }
        //Изменение роли пользователя
        [HttpPost]
        public async Task<IActionResult> EditUserRole(int userProjectID, UserRoles newRole)
        {
            UserProject userProject = _context.UserProject.FirstOrDefault(x => x.ID == userProjectID);
            userProject.UserRole = newRole;

            _context.SaveChanges();
            return Redirect($"/Projects/ProjectSettings/{userProject.ProjectID}");
        }

        //Добавление категории
        [HttpPost]
        public async Task<IActionResult> AddCategory([Bind("ID,Name")] CategoryType categoryType, IFormCollection formData, int projectID)
        {
            var currProject = await _context.Project.FindAsync(int.Parse(formData["projectID"]));
            categoryType.Project = currProject;
            categoryType.ProjectID = currProject.ID;

            _context.Add(categoryType);
            _context.SaveChanges();
            var c = _context.CategoryType.ToList();
            return Redirect($"/Projects/ProjectSettings/{currProject.ID}");
        }
        //Редактирование категории
        [HttpPost]
        public async Task<IActionResult> EditCategory([Bind("ID,Name")] CategoryType categoryType, int categoryID)
        {
            CategoryType category = _context.CategoryType.FirstOrDefault(x => x.ID == categoryID);
            category.Name = categoryType.Name;

            _context.SaveChangesAsync();
            return Redirect($"/Projects/ProjectSettings/{category.ProjectID}");
        }
        //Удаление категории
        [HttpPost]
        public async Task<IActionResult> DeleteCategory(int categoryID)
        {
            CategoryType category = _context.CategoryType.FirstOrDefault(x => x.ID == categoryID);
            int projectID = category.ProjectID;
            if (category.Issues != null)
            {
                foreach (Issue issue in category.Issues)
                {
                    issue.CategoryType = null;
                    issue.CategoryTypeID = null;
                }
            }
            _context.SaveChanges();
            _context.CategoryType.Remove(category);
            _context.SaveChanges();
            return Redirect($"/Projects/ProjectSettings/{projectID}");
        }


        //Добавление задачи
        [HttpPost]
        public IActionResult CreateIssue([Bind("Name, Description, DeadlineDate, PerformerID, PriorityTypeID, CategoryTypeID")] Issue issue, int projectID)
        {
            int? userSessionID = HttpContext.Session.GetInt32("UserID");
            var project = _context.Project.Include(x => x.StatusTypes).FirstOrDefault(x => x.ID == projectID);

            Issue newIssue = new Issue();

            newIssue.Name = issue.Name;
            newIssue.Description = issue.Description ?? "";
            newIssue.CreateDate = DateTime.Now;
            newIssue.DeadlineDate = issue.DeadlineDate?.Date < DateTime.Now.Date ? DateTime.Now.Date : issue.DeadlineDate?.Date;
            newIssue.CreatorID = (int)userSessionID;
            newIssue.PerformerID = issue.PerformerID;
            newIssue.ProjectID = projectID;
            newIssue.PriorityTypeID = issue.PriorityTypeID;
            newIssue.StatusTypeID = project.StatusTypes.FirstOrDefault(x => x.Name == "В процессе").ID;
            newIssue.CategoryTypeID = issue.CategoryTypeID;
            newIssue.IsDelete = false;

            _context.Add(newIssue);
            _context.SaveChanges();

            return Redirect($"/Projects/Project/{projectID}");
        }
        //Редактирование задачи
        [HttpPost]
        public IActionResult UpdateIssue([Bind("ID, Name, Description, DeadlineDate, PerformerID, PriorityTypeID, StatusTypeID, CategoryTypeID")] Issue issue, int issueID)
        {
            Issue currIssue = _context.Issue.FirstOrDefault(x => x.ID == issueID);
            currIssue.Name = issue.Name;
            currIssue.Description = issue.Description ?? "";
            currIssue.CreateDate = DateTime.Now;
            currIssue.DeadlineDate = issue.DeadlineDate?.Date < DateTime.Now.Date ? DateTime.Now.Date : issue.DeadlineDate?.Date;

            currIssue.PerformerID = issue.PerformerID;
            currIssue.PriorityTypeID = issue.PriorityTypeID;
            currIssue.StatusTypeID = issue.StatusTypeID;
            currIssue.CategoryTypeID = issue.CategoryTypeID;
            currIssue.IsDelete = false;
            _context.SaveChanges();

            return Redirect($"/Projects/Project/{currIssue.ProjectID}");
        }

        //Удаление задачи
        [HttpPost]
        public async Task<IActionResult> DeleteIssue(int issueID)
        {
            Issue currIssue = _context.Issue.FirstOrDefault(x => x.ID == issueID);
            currIssue.IsDelete = true;

            await _context.SaveChangesAsync();
            return Redirect($"/Projects/Project/{currIssue.ProjectID}");
        }

        //Перетаскивание задачи
        [HttpGet]
        public IActionResult UpdateIssueStatus(int currIssueID, int currStatusID)
        {
            Issue issue = _context.Issue.FirstOrDefault(x => x.ID == currIssueID);
            StatusType status = _context.StatusType.FirstOrDefault(x => x.ID == currStatusID);

            issue.StatusType = status;
            issue.StatusTypeID = status.ID;
            _context.SaveChanges();
            return Redirect($"/Projects/Project/{issue.ProjectID}");
        }
    }
}
