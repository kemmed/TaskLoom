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
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.Blazor;

namespace diplom.Controllers
{
    public class ProjectsController : Controller
    {
        private readonly diplomContext _context;
        private readonly MailService _mailService;
        private readonly TokenService _tokenService;
        private readonly LogService _logService;


        public ProjectsController(diplomContext context, MailService mailService, TokenService tokenService, LogService logService)
        {
            _context = context;
            _mailService = mailService;
            _tokenService = tokenService;
            _logService = logService;
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

            var allProjects = _context.Project.Where(x => x.UserProjects.Any(ub => ub.ProjectID == x.ID
                && ub.UserID == userLoggedInID
                && ub.IsActive))
                .Include(p => p.UserProjects)
                .ToList();

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
            ViewBag.UserID = userLoggedInID;
            var userProject = _context.UserProject.FirstOrDefault(x => x.UserID == userLoggedInID);

            ViewBag.UserRole = userProject?.UserRole;

            return View(filteredProjects.ToList());
        }

        [HttpGet("/Projects/Project/{projectID}")]
        public async Task<IActionResult> Project(int projectID, string filter = "all")
        {
            if (_context.Project.FirstOrDefault(x => x.ID == projectID) == null ||
                HttpContext.Session.GetInt32("UserID") == null ||
                _context.UserProject.FirstOrDefault(x => x.UserID == HttpContext.Session.GetInt32("UserID")) == null)
            {
                return NotFound();
            }
            int? userSessionID = HttpContext.Session.GetInt32("UserID");

            var allIssues = _context.Issue
                 .Where(i => i.ProjectID == projectID && !i.IsDelete)
                 .Include(i => i.Project)
                     .ThenInclude(p => p.StatusTypes)
                 .Include(i => i.Project)
                     .ThenInclude(p => p.PriorityTypes)
                 .Include(i => i.Project)
                    .ThenInclude(i=>i.CategoryTypes)
                 .Include(i => i.Performer)
                 .ToList();

            List<Issue> filteredIssues = new List<Issue>();

            if (filter == "myTasks")
            {
                filteredIssues = allIssues.Where(i => i.PerformerID == userSessionID).ToList();
            }
            else if (filter == "myCategory")
            {
                var userCategoryIds = _context.CategoryType
                    .Where(ct => ct.ProjectID == projectID &&
                                 ct.UserProjects.Any(up => up.UserID == userSessionID))
                    .Select(ct => ct.ID)
                    .ToList();

                filteredIssues = allIssues
                    .Where(i => i.CategoryTypeID.HasValue && userCategoryIds.Contains(i.CategoryTypeID.Value))
                    .ToList();
            }
            else
            {
                filteredIssues = allIssues;
            }

            var columns = _context.StatusType.Where(x => x.ProjectID == projectID);

            ViewBag.StatusTypes = columns;

            ViewBag.ProjectName = _context.Project.FirstOrDefault(x => x.ID == projectID).Name;
            ViewBag.ProjectID = _context.Project.FirstOrDefault(x => x.ID == projectID).ID;

            var reponsibilities = _context.UserProject.Where(x => x.ProjectID == projectID)
                .Select(c => new SelectListItem
                {
                    Value = c.UserID.ToString(),
                    Text = c.User.LName + " " + c.User.FName
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
            else if (userRole == UserRoles.Manager)
                ViewBag.userRole = 2;
            else
                ViewBag.userRole = 3;


            ViewBag.UserID = userSessionID;
            return View(filteredIssues);
        }

        [HttpGet("/Projects/ProjectHistory/{projectID}")]
        public async Task<IActionResult> ProjectHistory(int projectID)
        {
            if (_context.Project.FirstOrDefault(x => x.ID == projectID) == null ||
              HttpContext.Session.GetInt32("UserID") == null ||
              _context.UserProject.FirstOrDefault(x => x.UserID == HttpContext.Session.GetInt32("UserID")) == null)
            {
                return NotFound();
            }
            var log = _context.Log.Where(x => x.ProjectID == projectID).OrderByDescending(x => x.DateTime).ToList();

            ViewBag.ProjectID = projectID;
            return View(log);
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

            UserProject currUserProj = _context.UserProject.FirstOrDefault(x => x.UserID == HttpContext.Session.GetInt32("UserID") && x.ProjectID == projectID);

            var project = await _context.Project
                .Include(p => p.UserProjects.Where(up => up.IsActive))
                .ThenInclude(up => up.User)
                .Include(p => p.UserProjects.Where(up => up.IsActive))
                .ThenInclude(up => up.CategoryTypes)
                .Include(p => p.CategoryTypes)
                .Include(p => p.PriorityTypes)
                .Include(p => p.StatusTypes)
                .FirstOrDefaultAsync(x => x.ID == projectID);

            if (project == null)
            {
                return NotFound();
            }
            project.UserProjects = project.UserProjects
                .OrderBy(up => up.UserRole)
                .ToList();

            var userIdsInProject = project.UserProjects.ToList();

            var expirationTime = DateTime.Now.AddHours(-24);

            var pendingUsers = await _context.UserProject
                .Where(up => up.ProjectID == projectID && !up.IsActive && up.InviteTokenDate > expirationTime)
                .Include(up => up.User)
                .Select(up => up.User)
                .ToListAsync();

            ViewBag.PendingUsers = pendingUsers;
            ViewBag.ProjectID = projectID;
            ViewBag.UserRole = currUserProj.UserRole;

            return View(project);
        }
        [HttpGet("/Projects/DeletedIssueArchive/{projectID}")]
        public async Task<IActionResult> DeletedIssueArchive(int projectID)
        {
            if (_context.Project.FirstOrDefault(x => x.ID == projectID) == null ||
                HttpContext.Session.GetInt32("UserID") == null ||
                _context.UserProject.FirstOrDefault(x => x.UserID == HttpContext.Session.GetInt32("UserID")) == null)
            {
                return NotFound();
            }

            var issue = _context.Issue.Where(x => x.ProjectID == projectID && x.IsDelete)
                .Include(x => x.CategoryType)
                .Include(x => x.Performer)
                .ToList();

            if (issue == null)
            {
                return NotFound();
            }
            return View(issue);
        }


        //Создание проекта
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateProject([Bind("ID,Name,Description,DeadlineDate")] Models.Project project)
        {
            int? userSessionID = HttpContext.Session.GetInt32("UserID");

            User creator = _context.User.FirstOrDefault(x => x.ID == userSessionID);
            project.DeadlineDate = project.DeadlineDate.Value.Date < DateTime.Now.Date ? DateTime.Now.Date : project.DeadlineDate;
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
            userProject.IsCreator = true;
            _context.Add(userProject);

            await _context.SaveChangesAsync();

            var statusTypes = new List<StatusType>
            {
                new StatusType { Name = "В процессе", ProjectID = project.ID },
                new StatusType { Name = "На проверке", ProjectID = project.ID },
                new StatusType { Name = "Отложенные", ProjectID = project.ID },
                new StatusType { Name = "Завершенные", ProjectID = project.ID }
            };
            _context.AddRange(statusTypes);
            var priorityTypes = new List<PriorityType>
            {
                new PriorityType { Name = "Низкий", ProjectID = project.ID },
                new PriorityType { Name = "Средний", ProjectID = project.ID },
                new PriorityType { Name = "Высокий", ProjectID = project.ID }
            };
            _context.AddRange(priorityTypes);

            // Добавление записи в лог
            _logService.LogAction(project.ID, $"создал проект \"{project.Name}\"", creator.ID);

            await _context.SaveChangesAsync();

            return Redirect("AllProjects");
        }

        //Редактирование проекта
        [HttpPost]
        public async Task<IActionResult> EditProject([Bind("ID,Name,Description,Status,DeadlineDate")] Models.Project project, IFormCollection formData, int projectID, string returnUrl = null)
        {
            var currProject = await _context.Project.FindAsync(int.Parse(formData["projectID"]));

            string oldName = currProject.Name;
            string oldDescription = currProject.Description;
            ProjectStatus oldStatus = currProject.Status;
            DateTime? oldDeadlineDate = currProject.DeadlineDate;


            currProject.Name = project.Name;
            currProject.Description = project.Description;
            currProject.Status = project.Status;
            if (project.DeadlineDate.Value.Date >= DateTime.Now.Date || project.DeadlineDate.Value.Date > currProject.DeadlineDate.Value.Date)
                currProject.DeadlineDate = project.DeadlineDate.Value.Date;
            if (currProject.Status == ProjectStatus.Completed)
                currProject.EndDate = DateTime.Now.Date;
            else
                currProject.EndDate = null;

            List<string> changes = new List<string>();
            if (oldName != currProject.Name)
                changes.Add($"Название изменено с \"{oldName}\" на \"{currProject.Name}\".");
            if (oldDescription != currProject.Description)
                changes.Add($"Описание изменено.");
            if (oldStatus != currProject.Status)
                changes.Add($"Статус изменен с \"{currProject.ConvertStatus(oldStatus)}\" на \"{currProject.ConvertStatus(currProject.Status)}\".");
            if (oldDeadlineDate != currProject.DeadlineDate)
                changes.Add($"Срок изменен с \"{oldDeadlineDate?.ToString("dd.MM.yyyy")}\" на \"{currProject.DeadlineDate?.ToString("dd.MM.yyyy")}\".");

            int? userSessionID = HttpContext.Session.GetInt32("UserID");
            if (changes.Any())
            {
                string changeMessage = string.Join(" ", changes);
                // Добавление записи в лог
                _logService.LogAction(currProject.ID, $"отредактировал проект \"{oldName}\". Изменения: {changeMessage}", userSessionID);
            }

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

            int? userSessionID = HttpContext.Session.GetInt32("UserID");
            User inviter = _context.User.FirstOrDefault(x => x.ID == userSessionID);

            // Добавление записи в лог
            _logService.LogAction(projectID, $"отправил приглашение пользователю {invitedUser.FName} {invitedUser.LName} на присоединение к проекту", userSessionID);

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

                var project = _context.Project.FirstOrDefault(p => p.ID == userProject.ProjectID);
                var user = _context.User.FirstOrDefault(u => u.ID == userProject.UserID);

                // Добавление записи в лог
                _logService.LogAction(project.ID, $"присоединился к проекту \"{project.Name}\"", user.ID);

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
            UserProject userProject = _context.UserProject.Include(up=>up.User).FirstOrDefault(x => x.ID == userProjectID);
            int projectID = userProject.ProjectID;

            int? userSessionID = HttpContext.Session.GetInt32("UserID");
            User remover = _context.User.FirstOrDefault(x => x.ID == userSessionID);

            // Добавление записи в лог
            _logService.LogAction(projectID, $"исключил пользователя {userProject.User.FName} {userProject.User.LName} из проекта.", userSessionID);

            _context.UserProject.Remove(userProject);
            _context.SaveChanges();
            return Redirect($"/Projects/ProjectSettings/{projectID}");
        }

        //Назначение категорий пользователю
        [HttpPost]
        public async Task<IActionResult> AssignCategoriesToUser(int userProjectID, int projectID, List<int> selectedCategories)
        {
            UserProject userProject = _context.UserProject
                .Include(x => x.User)
                .Include(up => up.CategoryTypes)
                .FirstOrDefault(x => x.ID == userProjectID);

            var allCategories = _context.CategoryType.Where(c => c.ProjectID == projectID).ToList();

            userProject.CategoryTypes.Clear();

            if (selectedCategories != null && selectedCategories.Any())
            {
                foreach (var categoryId in selectedCategories)
                {
                    var category = allCategories.FirstOrDefault(c => c.ID == categoryId);
                    if (category != null)
                        userProject.CategoryTypes.Add(category);
                }
            }

            int? userSessionID = HttpContext.Session.GetInt32("UserID");
            User currentUser = _context.User.FirstOrDefault(x => x.ID == userSessionID);

            _logService.LogAction(projectID, $"обновил категории пользователя {userProject.User.FName} {userProject.User.LName}", userSessionID);

            await _context.SaveChangesAsync();
            return Redirect($"/Projects/ProjectSettings/{projectID}");
        }

        //Выход из проекта
        [HttpPost]
        public async Task<IActionResult> LeaveProject(int projectID)
        {
            int? userSessionID = HttpContext.Session.GetInt32("UserID");
            User leaver = _context.User.FirstOrDefault(x => x.ID == userSessionID);
            var userProject = await _context.UserProject.FirstOrDefaultAsync(up => up.ProjectID == projectID && up.UserID == userSessionID);
            _context.UserProject.Remove(userProject);

            // Добавление записи в лог
            _logService.LogAction(projectID, $"покинул проект", userSessionID);

            await _context.SaveChangesAsync();
            return Redirect("/Projects/AllProjects");
        }
        //Изменение роли пользователя
        [HttpPost]
        public async Task<IActionResult> EditUserRole(int userProjectID, UserRoles newRole)
        {
            UserProject userProject = _context.UserProject.Include(x => x.User).FirstOrDefault(x => x.ID == userProjectID);

            int? userSessionID = HttpContext.Session.GetInt32("UserID");
            User editor = _context.User.FirstOrDefault(x => x.ID == userSessionID);
            if (newRole == UserRoles.Admin)
            {
                userProject.UserRole = newRole;

                UserProject userProjectEditor = _context.UserProject.Include(x => x.User).FirstOrDefault(x => x.UserID == editor.ID && x.ProjectID == userProject.ProjectID);
                userProjectEditor.UserRole = UserRoles.Employee;

                // Добавление записи в лог
                _logService.LogAction(userProject.ProjectID, $"передал права на проект пользователю {userProject.User.FName}", userSessionID);
            }
            else
            {
                string oldRole = userProject.ConvertRoles(userProject.UserRole);
                userProject.UserRole = newRole;

                // Добавление записи в лог
                _logService.LogAction(userProject.ProjectID, $"изменил роль пользователя {userProject.User.FName} {userProject.User.LName} с \"{oldRole}\" на \"{userProject.ConvertRoles(newRole)}\"", userSessionID);
            }
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

            int? userSessionID = HttpContext.Session.GetInt32("UserID");
            User creator = _context.User.FirstOrDefault(x => x.ID == userSessionID);

            _context.Add(categoryType);

            // Добавление записи в лог
            _logService.LogAction(currProject.ID, $"добавил категорию \"{categoryType.Name}\"", userSessionID);

            _context.SaveChanges();
            var c = _context.CategoryType.ToList();
            return Redirect($"/Projects/ProjectSettings/{currProject.ID}");
        }
        //Редактирование категории
        [HttpPost]
        public async Task<IActionResult> EditCategory([Bind("ID,Name")] CategoryType categoryType, int categoryID)
        {
            CategoryType category = _context.CategoryType.FirstOrDefault(x => x.ID == categoryID);
            string oldName = category.Name;
            category.Name = categoryType.Name;

            int? userSessionID = HttpContext.Session.GetInt32("UserID");
            User editor = _context.User.FirstOrDefault(x => x.ID == userSessionID);

            // Добавление записи в лог
            _logService.LogAction(category.ProjectID, $"изменил название категории с \"{oldName}\" на \"{category.Name}\"", userSessionID);

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

            int? userSessionID = HttpContext.Session.GetInt32("UserID");
            User deleter = _context.User.FirstOrDefault(x => x.ID == userSessionID);

            // Добавление записи в лог
            _logService.LogAction(projectID, $"удалил категорию \"{category.Name}\"", userSessionID);

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
            User creator = _context.User.FirstOrDefault(x => x.ID == userSessionID);

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

            // Добавление записи в лог
            _logService.LogAction(projectID, $"создал задачу \"{newIssue.Name}\"", userSessionID);

            _context.SaveChanges();

            return Redirect($"/Projects/Project/{projectID}");
        }
        //Редактирование задачи
        [HttpPost]
        public IActionResult UpdateIssue([Bind("ID, Name, Description, DeadlineDate, PerformerID, PriorityTypeID, StatusTypeID, CategoryTypeID")] Issue issue, int issueID)
        {
            Issue currIssue = _context.Issue.FirstOrDefault(x => x.ID == issueID);

            string oldName = currIssue.Name;
            string oldDescription = currIssue.Description;
            DateTime? oldDeadlineDate = currIssue.DeadlineDate;
            int? oldPerformerID = currIssue.PerformerID;
            int? oldPriorityTypeID = currIssue.PriorityTypeID;
            int? oldStatusTypeID = currIssue.StatusTypeID;
            int? oldCategoryTypeID = currIssue.CategoryTypeID;

            currIssue.Name = issue.Name;
            currIssue.Description = issue.Description ?? "";

            if (currIssue.DeadlineDate.Value.Date >= DateTime.Now.Date || currIssue.DeadlineDate.Value.Date > currIssue.DeadlineDate.Value.Date)
                currIssue.DeadlineDate = currIssue.DeadlineDate.Value.Date;

            currIssue.PerformerID = issue.PerformerID;
            currIssue.PriorityTypeID = issue.PriorityTypeID;
            currIssue.StatusTypeID = issue.StatusTypeID;
            currIssue.CategoryTypeID = issue.CategoryTypeID;
            currIssue.IsDelete = false;

            List<string> changes = new List<string>();
            if (oldName != currIssue.Name)
                changes.Add($"Название изменено с \"{oldName}\" на \"{currIssue.Name}\";");
            if (oldDescription != currIssue.Description)
                changes.Add($"Описание изменено;");
            if (oldDeadlineDate != currIssue.DeadlineDate)
                changes.Add($"Срок изменен с \"{oldDeadlineDate?.ToString("dd.MM.yyyy")}\" на \"{currIssue.DeadlineDate?.ToString("dd.MM.yyyy")}\";");
            if (oldPerformerID != currIssue.PerformerID)
            {
                var oldPerformer = _context.User.FirstOrDefault(u => u.ID == oldPerformerID);
                var newPerformer = _context.User.FirstOrDefault(u => u.ID == currIssue.PerformerID);
                changes.Add($"Ответственный изменен с \"{oldPerformer?.FName} {oldPerformer?.LName}\" на \"{newPerformer?.FName} {newPerformer?.LName}\";");
            }
            if (oldPriorityTypeID != currIssue.PriorityTypeID)
            {
                var oldPriority = _context.PriorityType.FirstOrDefault(p => p.ID == oldPriorityTypeID);
                var newPriority = _context.PriorityType.FirstOrDefault(p => p.ID == currIssue.PriorityTypeID);
                changes.Add($"Приоритет изменен с \"{oldPriority?.Name}\" на \"{newPriority?.Name}\";");
            }
            if (oldStatusTypeID != currIssue.StatusTypeID)
            {
                var oldStatus = _context.StatusType.FirstOrDefault(s => s.ID == oldStatusTypeID);
                var newStatus = _context.StatusType.FirstOrDefault(s => s.ID == currIssue.StatusTypeID);
                changes.Add($"Статус изменен с \"{oldStatus?.Name}\" на \"{newStatus?.Name}\";");
            }
            if (oldCategoryTypeID != currIssue.CategoryTypeID)
            {
                var oldCategory = _context.CategoryType.FirstOrDefault(c => c.ID == oldCategoryTypeID);
                var newCategory = _context.CategoryType.FirstOrDefault(c => c.ID == currIssue.CategoryTypeID);
                changes.Add($"Категория изменена с \"{oldCategory?.Name}\" на \"{newCategory?.Name}\"");
            }
            _context.SaveChanges();


            int? userSessionID = HttpContext.Session.GetInt32("UserID");
            if (changes.Any())
            {
                // Добавление записи в лог
                string changeMessage = string.Join(" ", changes);
                _logService.LogAction(currIssue.ProjectID, $"отредактировал задачу \"{oldName}\". Изменения: {changeMessage}", userSessionID);
            }

            return Redirect($"/Projects/Project/{currIssue.ProjectID}");
        }

        //Удаление задачи
        [HttpPost]
        public async Task<IActionResult> DeleteIssue(int issueID)
        {
            Issue currIssue = _context.Issue.FirstOrDefault(x => x.ID == issueID);
            currIssue.IsDelete = true;
            currIssue.DeleteDate = DateTime.Now;

            int? userSessionID = HttpContext.Session.GetInt32("UserID");
            User deleter = _context.User.FirstOrDefault(x => x.ID == userSessionID);

            // Добавление записи в лог
            _logService.LogAction(currIssue.ProjectID, $"удалил задачу \"{currIssue.Name}\"", userSessionID);

            await _context.SaveChangesAsync();
            return Redirect($"/Projects/Project/{currIssue.ProjectID}");
        }
        //Восстановление задачи
        [HttpPost]
        public async Task<IActionResult> RestoreIssue(int issueID)
        {
            Issue currIssue = _context.Issue.FirstOrDefault(x => x.ID == issueID);
            currIssue.IsDelete = false;
            currIssue.DeleteDate = null;

            int? userSessionID = HttpContext.Session.GetInt32("UserID");
            User restorer = _context.User.FirstOrDefault(x => x.ID == userSessionID);

            // Добавление записи в лог
            _logService.LogAction(currIssue.ProjectID, $"восстановил задачу \"{currIssue.Name}\"", userSessionID);

            await _context.SaveChangesAsync();
            return Redirect($"/Projects/DeletedIssueArchive/{currIssue.ProjectID}");
        }

        //Перетаскивание задачи
        [HttpGet]
        public IActionResult UpdateIssueStatus(int currIssueID, int currStatusID)
        {
            Issue issue = _context.Issue.Include(i => i.StatusType).FirstOrDefault(x => x.ID == currIssueID);

            StatusType newStatus = _context.StatusType.FirstOrDefault(x => x.ID == currStatusID);

            if (issue.StatusTypeID == currStatusID)
            {
                return Redirect($"/Projects/Project/{issue.ProjectID}");
            }
            string oldStatusName = issue.StatusType?.Name;
            issue.StatusTypeID = currStatusID;

            int? userSessionID = HttpContext.Session.GetInt32("UserID");
            User editor = _context.User.FirstOrDefault(x => x.ID == userSessionID);

            // Добавление записи в лог
            _logService.LogAction(issue.ProjectID, $"изменил статус задачи \"{issue.Name}\" с \"{oldStatusName}\" на \"{newStatus.Name}\"", userSessionID);

            _context.SaveChanges();
            return Redirect($"/Projects/Project/{issue.ProjectID}");
        }

        //Сохранение лога
        [HttpGet("/Projects/DownloadHistory/{projectID}")]
        public IActionResult DownloadHistory(int projectID)
        {
            var logs = _context.Log.Where(x => x.ProjectID == projectID).OrderByDescending(x => x.DateTime).ToList();
            Models.Project project = _context.Project.FirstOrDefault(x => x.ID == projectID);

            using (var workbook = new ClosedXML.Excel.XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Project History");
                worksheet.Cell(1, 1).Value = "ДАТА";
                worksheet.Cell(1, 2).Value = "ДЕЙСТВИЕ";

                var headerRange = worksheet.Range("A1:B1");
                headerRange.Style.Font.Bold = true;
                headerRange.Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Center;

                int row = 2;
                foreach (var log in logs)
                {
                    worksheet.Cell(row, 1).Value = log.DateTime.ToString("dd.MM.yyyy HH:mm");
                    worksheet.Cell(row, 2).Value = log.Event;
                    row++;
                }
                worksheet.Columns().AdjustToContents();

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    var content = stream.ToArray();

                    return File(
                        content,
                        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                        $"ProjectHistory_{project.Name}.xlsx"
                    );
                }
            }
        }

        [HttpGet("/Projects/GetIssueInfo/{issueID}")]
        public async Task<IActionResult> GetIssueInfo(int issueID)
        {
            int? userSessionID = HttpContext.Session.GetInt32("UserID");

            var issue = await _context.Issue
                .Include(i => i.Project)
                .Include(i => i.Creator)
                .Include(i => i.Performer)
                .FirstOrDefaultAsync(x => x.ID == issueID);

            if (issue == null)
                return NotFound();

            var result = new
            {
                issue = new
                {
                    issue.ID,
                    issue.Name,
                    issue.Description,
                    issue.PriorityTypeID,
                    issue.StatusTypeID,
                    issue.CategoryTypeID,
                    Performer = issue.Performer != null ? new { ID = issue.Performer.ID } : null,
                    DeadlineDate = issue.DeadlineDate?.ToString("yyyy-MM-dd")
                },
                isCreator = issue.Creator?.ID == userSessionID
            };

            return Ok(result);
        }

    }
}
