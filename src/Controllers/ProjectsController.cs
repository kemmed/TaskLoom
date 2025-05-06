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
using DocumentFormat.OpenXml.Spreadsheet;
using DocumentFormat.OpenXml.Bibliography;
using Issue = diplom.Models.Issue;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.Xml.Linq;

namespace diplom.Controllers
{
    public class ProjectsController : Controller
    {
        private readonly diplomContext _context;
        private readonly MailService _mailService;
        private readonly TokenService _tokenService;
        private readonly LogService _logService;
        private readonly UserService _userService;


        public ProjectsController(diplomContext context, MailService mailService, TokenService tokenService, LogService logService, UserService userService)
        {
            _context = context;
            _mailService = mailService;
            _tokenService = tokenService;
            _logService = logService;
            _userService = userService;
        }
        public IActionResult InviteUserMessage(string message, int projectID)
        {
            ViewBag.message = message;
            ViewBag.projectID = projectID;
            return View();
        }
        [HttpGet("/Projects/ProjectStatistics/{projectID}")]
        public async Task<IActionResult> ProjectStatistics(int projectID, string startDate = null, string endDate = null, int[] selectedCategories = null)
        {
            User? currentUser = await _userService.GetCurrentUser(HttpContext);
            Models.Project? currentProject = await _userService.GetProjectByID(projectID);
            if (currentProject == null || currentUser == null)
                return NotFound();

            var issues = currentProject.Issues.Where(issue => !issue.IsDelete).ToList();

            var completedIssues = issues.Count(i => i.StatusType.Name == "Завершенные");
            decimal completionPercentage = issues.Count() > 0 ? (decimal)completedIssues / issues.Count() * 100 : 0;

            var allStatuses = await _context.StatusType
                .Where(st => st.ProjectID == currentProject.ID)
                .ToListAsync();

            var statusCounts = new Dictionary<string, int>();
            foreach (var status in allStatuses)
            {
                if (status.Name != "Завершенные")
                    statusCounts[status.Name] = 0;
            }

            foreach (var issue in issues)
            {
                if (issue.StatusType.Name != "Завершенные")
                {
                    if (statusCounts.ContainsKey(issue.StatusType.Name))
                        statusCounts[issue.StatusType.Name]++;
                }
            }

            var priorityCounts = new Dictionary<string, int>
                {
                    { "Низкий", issues.Count(i => i.PriorityType.Name == "Низкий") },
                    { "Средний", issues.Count(i => i.PriorityType.Name == "Средний") },
                    { "Высокий", issues.Count(i => i.PriorityType.Name == "Высокий") }
                };

            var userStatistics = currentProject.UserProjects
                .Select(up => up.User)
                .Where(user => user != null)
                .Select(user => new
                {
                    UserName = $"{user.LName} {user.FName}",
                    CompletedOnTime = issues
                        .Count(i => i.PerformerID == user.ID && 
                        i.EndDate != null && 
                        i.EndDate.Value.Date <= i.DeadlineDate.Value.Date),
                    CompletedLater = issues
                        .Count(i => i.PerformerID == user.ID  && 
                        (i.EndDate == null && DateTime.Now.Date >= i.DeadlineDate.Value.Date || 
                        (i.EndDate != null && i.EndDate.Value.Date > i.DeadlineDate.Value.Date)))
                })
                .ToList();

            if (string.IsNullOrEmpty(startDate) || string.IsNullOrEmpty(endDate))
            {
                var today = DateTime.Today;
                var startOfWeek = today.AddDays(-(int)today.DayOfWeek + (int)DayOfWeek.Monday);
                var endOfWeek = startOfWeek.AddDays(6);
                startDate = startOfWeek.ToString("yyyy-MM-dd");
                endDate = endOfWeek.ToString("yyyy-MM-dd");
            }
            DateTime start = DateTime.Parse(startDate);
            DateTime end = DateTime.Parse(endDate);

            issues = issues.Where(i => i.CreateDate.Date >= start && i.CreateDate.Date <= end).ToList();

            if (selectedCategories != null && selectedCategories.Length > 0)
                issues = issues
                    .Where(i => i.CategoryTypeID.HasValue && selectedCategories
                    .Contains(i.CategoryTypeID.Value))
                    .ToList();

            var groupedIssuesCreate = issues
                .GroupBy(i => i.CreateDate.Date)
                .OrderBy(g => g.Key)
                .ToDictionary(g => g.Key, g => g.Count());

            var groupedIssuesDone = issues
                .Where(i => i.EndDate!=null)
                .GroupBy(i => i.EndDate.Value.Date)
                .OrderBy(g => g.Key)
                .ToDictionary(g => g.Key, g => g.Count());

            string chartDataCreate = "";
            string chartDataDone= "";

            Dictionary<DateTime, int> dateIssue = new Dictionary<DateTime, int>();

            for (DateTime date = start; date <= end; date = date.AddDays(1))
            {
                chartDataDone += "{";
                chartDataDone += $"x: '{date.Date.ToString("dd.MM.yy")}', y: {(groupedIssuesDone.ContainsKey(date) ? groupedIssuesDone[date] : 0)}";
                chartDataDone += "}, ";

                chartDataCreate += "{";
                chartDataCreate += $"x: '{date.Date.ToString("dd.MM.yy")}', y: {(groupedIssuesCreate.ContainsKey(date) ? groupedIssuesCreate[date] : 0)}";
                chartDataCreate += "}, ";
            }

            ViewBag.StartDate = startDate;
            ViewBag.EndDate = endDate;

            ViewBag.CompletedIssues = completedIssues;
            ViewBag.StatusCounts = statusCounts;

            ViewBag.CompletionPercentage = completionPercentage;

            ViewBag.PriorityCounts = priorityCounts;

            ViewBag.UserStatistics = userStatistics;
            ViewBag.ProjectID = projectID;
            ViewBag.ChartDataCreate = chartDataCreate;
            ViewBag.ChartDataDone = chartDataDone;

            ViewBag.CategoryTypes = _context.CategoryType.Where(x => x.ProjectID == currentProject.ID).ToList();
            ViewBag.SelectedCategories = selectedCategories?.ToList() ?? new List<int>();

            return View();
        }
        public async Task<IActionResult> AllProjects(string? statusFilter = null)
        {
            User? currentUser = await _userService.GetCurrentUser(HttpContext);
            if (currentUser == null)
                return Redirect("/Users/Authorization");

            var allProjects = _context.Project
                .Where(x => x.UserProjects
                .Any(ub => ub.ProjectID == x.ID
                    && ub.UserID == currentUser.ID
                    && ub.IsActive))
                .Include(p => p.UserProjects)
                .ToList();

            var userProject = _context.UserProject.FirstOrDefault(x => x.UserID == currentUser.ID);

            var statusCounts = new Dictionary<ProjectStatus, int>
            {
                { ProjectStatus.InProcess, allProjects.Count(p => p.Status == ProjectStatus.InProcess) },
                { ProjectStatus.Completed, allProjects.Count(p => p.Status == ProjectStatus.Completed) },
                { ProjectStatus.Frozen, allProjects.Count(p => p.Status == ProjectStatus.Frozen) }
            };

            var filteredProjects = allProjects.AsQueryable();
            if (!string.IsNullOrEmpty(statusFilter))
            {
                if (Enum.TryParse(statusFilter, out ProjectStatus filter))
                    filteredProjects = filteredProjects.Where(p => p.Status == filter);
            }

            ViewBag.StatusCounts = statusCounts;
            ViewBag.StatusFilter = statusFilter;
            ViewBag.UserID = currentUser.ID;
            ViewBag.UserRole = userProject?.UserRole;

            return View(filteredProjects.ToList());
        }

        //Сортировка списка ответсвенных с количеством их задач
        public List<SelectListItem> GetSortedResponsibilities(int projectID)
        {
            return _context.UserProject
                .Where(up => up.ProjectID == projectID)
                .Select(up => new
                {
                    up.UserID,
                    UserName = up.User.LName + " " + up.User.FName,
                    TaskCount = _context.Issue
                        .Count(t => t.ProjectID == projectID
                            && !t.IsDelete
                            && t.PerformerID == up.UserID),
                    NearestDeadline = _context.Issue
                        .Where(t => t.ProjectID == projectID
                            && !t.IsDelete
                            && t.PerformerID == up.UserID
                            && t.DeadlineDate.HasValue)
                        .Min(t => t.DeadlineDate)
                })
                .AsEnumerable()
                .OrderBy(u => u.TaskCount)
                .ThenBy(u => u.NearestDeadline)
                .Select(u => new SelectListItem
                {
                    Value = u.UserID.ToString(),
                    Text = $"{u.UserName} ({u.TaskCount} {GetTaskWord(u.TaskCount)})"
                })
                .ToList();
        }

        //Метод корректного отображения слова задача
        public static string GetTaskWord(int count)
        {
            count = Math.Abs(count) % 100;
            var lastDigit = count % 10;

            if (count >= 11 && count <= 14)
                return "задач";

            switch (lastDigit)
            {
                case 1:
                    return "задача";
                case 2:
                case 3:
                case 4:
                    return "задачи";
                default:
                    return "задач";
            }
        }

        //Изменение списка ответственных в зависимости от выбранной категории
        [HttpGet("/Projects/GetPerformersByCategory")]
        public async Task<IActionResult> GetPerformersByCategory(int projectID, int? categoryTypeID)
        {
            User? currentUser = await _userService.GetCurrentUser(HttpContext);
            Models.Project? currentProject = await _userService.GetProjectByID(projectID);
            if (currentProject == null ||
                currentUser == null ||
                !await _userService.UserIsInProject(currentProject.ID, currentUser.ID))
                return NotFound();

            var users = await _context.UserProject
                .Where(up => up.ProjectID == projectID)
                .Include(up => up.User)
                .Include(up => up.CategoryTypes)
                .ToListAsync();

            if (categoryTypeID.HasValue)
            {
                users = users
                    .Where(u => u.CategoryTypes != null &&
                        u.CategoryTypes.Any(ct => ct.ID == categoryTypeID.Value))
                    .ToList();
            }

            var performers = users.Select(u => new
            {
                u.UserID,
                UserName = u.User.LName + " " + u.User.FName,

                TaskCount = _context.Issue
                    .Count(t => t.ProjectID == projectID &&
                        !t.IsDelete &&
                        t.PerformerID == u.UserID),

                NearestDeadline = _context.Issue
                    .Where(t => t.ProjectID == projectID &&
                        !t.IsDelete &&
                        t.PerformerID == u.UserID &&
                        t.DeadlineDate.HasValue)
                    .Min(t => t.DeadlineDate)
            })
            .AsEnumerable()
            .OrderBy(u => u.TaskCount)
            .ThenBy(u => u.NearestDeadline)
            .Select(u => new
            {
                ID = u.UserID,
                Name = $"{u.UserName} ({u.TaskCount} {GetTaskWord(u.TaskCount)})",
            }).ToList();

            return Json(performers);
        }


        [HttpGet("/Projects/Project/{projectID}")]
        public async Task<IActionResult> Project(int projectID, string filter = "all")
        {

            User? currentUser = await _userService.GetCurrentUser(HttpContext);
            Models.Project? currentProject = await _userService.GetProjectByID(projectID);
            if (currentProject == null || currentUser == null)
                return NotFound();

            UserProject? currentUserProject = await _userService.GetUserProjectByID(currentProject.ID, currentProject.ID);
            if (currentUserProject == null)
                return NotFound();

            var allIssues = _context.Issue
                 .Where(i => i.ProjectID == currentUser.ID && !i.IsDelete)
                 .Include(i => i.Project)
                     .ThenInclude(p => p.StatusTypes)
                 .Include(i => i.Project)
                     .ThenInclude(p => p.PriorityTypes)
                 .Include(i => i.Project)
                    .ThenInclude(i=>i.CategoryTypes)
                 .Include(i => i.Performer)
                 .ToList();

            List<Issue> filteredIssues = [];

            if (filter == "myTasks")
                filteredIssues = allIssues.Where(i => i.PerformerID == currentUser.ID).ToList();

            else if (filter == "myCategory")
            {
                var userCategoryIds = _context.CategoryType
                    .Where(ct => ct.ProjectID == currentUser.ID &&
                                 ct.UserProjects.Any(up => up.UserID == currentUser.ID))
                    .Select(ct => ct.ID)
                    .ToList();

                filteredIssues = allIssues
                    .Where(i => i.CategoryTypeID.HasValue && userCategoryIds.Contains(i.CategoryTypeID.Value))
                    .ToList();
            }
            else
                filteredIssues = allIssues;

            var columns = _context.StatusType.Where(x => x.ProjectID == currentUser.ID);

            ViewBag.StatusTypes = columns;

            ViewBag.ProjectName = currentProject.Name;
            ViewBag.ProjectID = currentProject.ID;

            var reponsibilities = GetSortedResponsibilities(currentUser.ID);

            var categories = _context.CategoryType.Where(x => x.ProjectID == currentUser.ID)
                .Select(c => new SelectListItem
                {
                    Value = c.ID.ToString(),
                    Text = c.Name
                }).ToList();

            var priorities = _context.PriorityType.Where(x => x.ProjectID == currentUser.ID)
                .Select(c => new SelectListItem
                {
                    Value = c.ID.ToString(),
                    Text = c.Name
                }).ToList();

            var statuses = _context.StatusType.Where(x => x.ProjectID == currentUser.ID)
                .Select(c => new SelectListItem
                {
                    Value = c.ID.ToString(),
                    Text = c.Name
                }).ToList();

            ViewBag.Responsibilities = reponsibilities;
            ViewBag.Categories = categories;
            ViewBag.Priorities = priorities;
            ViewBag.Statuses = statuses;


            UserRoles userRole = currentUserProject.UserRole;
            if (userRole == UserRoles.Admin)
                ViewBag.userRole = 1;
            else if (userRole == UserRoles.Manager)
                ViewBag.userRole = 2;
            else
                ViewBag.userRole = 3;


            ViewBag.UserID = currentUser.ID;
            return View(filteredIssues);
        }

        [HttpGet("/Projects/ProjectHistory/{projectID}")]
        public async Task<IActionResult> ProjectHistory(int projectID)
        {
            User? currentUser = await _userService.GetCurrentUser(HttpContext);
            Models.Project? currentProject = await _userService.GetProjectByID(projectID);
            if (currentProject == null ||
                currentUser == null ||
                !await _userService.UserIsInProject(currentProject.ID, currentUser.ID))
                return NotFound();

            var log = _context.Log
                .Where(x => x.ProjectID == currentProject.ID)
                .OrderByDescending(x => x.DateTime)
                .ToList();

            ViewBag.ProjectID = currentProject.ID;
            return View(log);
        }

        [HttpGet("/Projects/ProjectSettings/{projectID}")]
        public async Task<IActionResult> ProjectSettings(int projectID)
        {
            User? currentUser = await _userService.GetCurrentUser(HttpContext);
            if (currentUser == null)
                return NotFound();

            Models.Project? currentProject = await _context.Project
                .Include(p => p.UserProjects.Where(up => up.IsActive))
                    .ThenInclude(up => up.User)
                .Include(p => p.UserProjects.Where(up => up.IsActive))
                    .ThenInclude(up => up.CategoryTypes)
                .Include(p => p.CategoryTypes)
                .Include(p => p.PriorityTypes)
                .Include(p => p.StatusTypes)
                .FirstOrDefaultAsync(x => x.ID == projectID);

            if (currentProject == null)
                return NotFound();

            UserProject? currUserProject = await _userService.GetUserProjectByID(currentProject.ID, currentProject.ID);

            if (currentProject == null || currUserProject == null)
                return NotFound();

            currentProject.UserProjects = currentProject.UserProjects
                .OrderBy(up => up.UserRole)
                .ToList();

            var userIdsInProject = currentProject.UserProjects.ToList();

            var expirationTime = DateTime.Now.AddHours(-24);

            var pendingUsers = await _context.UserProject
                .Where(up => up.ProjectID == currentProject.ID 
                    && !up.IsActive 
                    && up.InviteTokenDate > expirationTime)
                .Include(up => up.User)
                .Select(up => up.User)
                .ToListAsync();

            ViewBag.PendingUsers = pendingUsers;
            ViewBag.ProjectID = currentProject.ID;
            ViewBag.UserRole = currUserProject.UserRole;

            return View(currentProject);
        }
        [HttpGet("/Projects/DeletedIssueArchive/{projectID}")]
        public async Task<IActionResult> DeletedIssueArchive(int projectID)
        {
            User? currentUser = await _userService.GetCurrentUser(HttpContext);
            Models.Project? currentProject = await _userService.GetProjectByID(projectID);
            if (currentProject == null ||
                currentUser == null ||
                !await _userService.UserIsInProject(currentProject.ID, currentUser.ID))
                return NotFound();

            var issue = _context.Issue
                .Where(x => x.ProjectID == currentProject.ID && x.IsDelete)
                .Include(x => x.CategoryType)
                .Include(x => x.Performer)
                .ToList();

            if (issue == null)
                return NotFound();

            return View(issue);
        }

        //Создание проекта
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateProject([Bind("ID,Name,Description,DeadlineDate")] Models.Project project)
        {
            User? currentUser = await _userService.GetCurrentUser(HttpContext);
            if (currentUser == null)
                return NotFound();

            if (project.DeadlineDate.HasValue && project.DeadlineDate.Value.Date < DateTime.Now.Date)
                project.DeadlineDate = DateTime.Now.Date;

            else if (!project.DeadlineDate.HasValue)
                project.DeadlineDate = DateTime.Now.Date;

            project.DeadlineDate = project.DeadlineDate;
            project.Status = ProjectStatus.InProcess;
            project.CreateDate = DateTime.Now.Date;

            _context.Add(project);

            await _context.SaveChangesAsync();

            UserProject userProject = new()
            {
                ProjectID = project.ID,
                Project = project,
                UserID = currentUser.ID,
                User = currentUser,
                UserRole = UserRoles.Admin,
                IsActive = true,
                IsCreator = true
            };

            _context.Add(userProject);

            await _context.SaveChangesAsync();

            var statusTypes = new[] 
            {
                new StatusType { Name = "В процессе", ProjectID = project.ID },
                new StatusType { Name = "На проверке", ProjectID = project.ID },
                new StatusType { Name = "Отложенные", ProjectID = project.ID },
                new StatusType { Name = "Завершенные", ProjectID = project.ID }
            };
            var priorityTypes = new[]
            {
                new PriorityType { Name = "Низкий", ProjectID = project.ID },
                new PriorityType { Name = "Средний", ProjectID = project.ID },
                new PriorityType { Name = "Высокий", ProjectID = project.ID }
            };

            _context.AddRange(statusTypes);
            _context.AddRange(priorityTypes);

            // Добавление записи в лог
            _logService.LogAction(project.ID, 
                $"создал проект \"{project.Name}\"", 
                currentUser.ID);

            await _context.SaveChangesAsync();

            return Redirect("AllProjects");
        }

        //Редактирование проекта
        [HttpPost]
        public async Task<IActionResult> EditProject([Bind("ID,Name,Description,Status,DeadlineDate")] Models.Project project, IFormCollection formData, int projectID, string? returnUrl = null)
        {
            User? currentUser = await _userService.GetCurrentUser(HttpContext);
            Models.Project? currentProject = await _userService.GetProjectByID(projectID);
            if (currentProject == null ||
                currentUser == null ||
                !await _userService.UserIsInProject(currentProject.ID, currentUser.ID))
                return NotFound();

            string oldName = currentProject.Name;
            string? oldDescription = currentProject.Description;

            ProjectStatus oldStatus = currentProject.Status;
            DateTime? oldDeadlineDate = currentProject.DeadlineDate;

            currentProject.Name = project.Name;
            currentProject.Description = project.Description;
            currentProject.Status = project.Status;

            if (project.DeadlineDate.HasValue)
            {
                if (project.DeadlineDate.Value.Date >= DateTime.Now.Date ||
                    (currentProject.DeadlineDate.HasValue && project.DeadlineDate.Value.Date > currentProject.DeadlineDate.Value.Date))
                    currentProject.DeadlineDate = project.DeadlineDate.Value.Date;
            }
            else
                currentProject.DeadlineDate = null;

            if (currentProject.Status == ProjectStatus.Completed)
                currentProject.EndDate = DateTime.Now.Date;
            else
                currentProject.EndDate = null;

            List<string> changes = [];
            if (oldName != currentProject.Name)
                changes.Add($"Название изменено с \"{oldName}\" на \"{currentProject.Name}\".");
            if (oldDescription != currentProject.Description)
                changes.Add($"Описание изменено.");
            if (oldStatus != currentProject.Status)
                changes.Add($"Статус изменен с \"{currentProject.ConvertStatus(oldStatus)}\" на \"{currentProject.ConvertStatus(currentProject.Status)}\".");
            if (oldDeadlineDate != currentProject.DeadlineDate)
                changes.Add($"Срок изменен с \"{oldDeadlineDate?.ToString("dd.MM.yyyy")}\" на \"{currentProject.DeadlineDate?.ToString("dd.MM.yyyy")}\".");

            if (changes.Count != 0)
            {
                string changeMessage = string.Join(" ", changes);
                // Добавление записи в лог
                _logService.LogAction(currentProject.ID, 
                    $"отредактировал проект \"{oldName}\". Изменения: {changeMessage}", 
                    currentUser.ID);
            }

            await _context.SaveChangesAsync();
            return Redirect(string.IsNullOrEmpty(returnUrl) ? "/Projects/AllProjects" : returnUrl);
        }

        //Отправка приглашения на присоединение к проектку
        [HttpPost]
        public async Task<IActionResult> InviteUser([Bind("Email")] User user, int projectID)
        {
            User? currentUser = await _userService.GetCurrentUser(HttpContext);
            Models.Project? currentProject = await _userService.GetProjectByID(projectID);
            if (currentProject == null ||
                currentUser == null ||
                !await _userService.UserIsInProject(currentProject.ID, currentUser.ID))
                return NotFound();

            string message;
            var invitedUser = await _context.User.FirstOrDefaultAsync(m => m.Email == user.Email);

            if (invitedUser == null || !invitedUser.IsActive)
            {
                message = "Пользователя с таким email не существует в системе.";
                return RedirectToAction("InviteUserMessage", new { message, currentProject.ID });
            }
            var existingUserProject = await _userService.GetUserProjectByID(currentProject.ID, invitedUser.ID);

            if (existingUserProject != null)
            {
                message = "Этот пользователь уже присоединен к проекту.";
                return RedirectToAction("InviteUserMessage", new { message, currentProject.ID });
            }

            string inviteToken = _tokenService.GenerateToken();

            var userProject = new UserProject
            {
                UserID = invitedUser.ID,
                ProjectID = currentProject.ID,
                UserRole = UserRoles.Employee,
                InviteToken = inviteToken,
                InviteTokenDate = DateTime.Now,
                IsActive = false
            };

            _context.UserProject.Add(userProject);

            string inviteLink = $"https://localhost:7297/Projects/AcceptInvite?token={inviteToken}&projectID={currentProject.ID}";

            string templatePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "emailMessages", "inviteUser.html");
            string emailTemplate = System.IO.File.ReadAllText(templatePath);

            string emailBody = emailTemplate.Replace("{inviteLink}", inviteLink);
            emailBody = emailBody.Replace("{FName}", invitedUser.FName);
            emailBody = emailBody.Replace("{LName}", invitedUser.LName);

            _mailService.SendEmail(emailBody, "Приглашение в проект", invitedUser.Email);

            // Добавление записи в лог
            _logService.LogAction(currentProject.ID, 
                $"отправил приглашение пользователю {invitedUser.FName} {invitedUser.LName} на присоединение к проекту", 
                currentUser.ID);

            await _context.SaveChangesAsync();

            message = "Приглашение успешно отправлено. Дождитесь, пока пользователь примет ваше приглашение.";
            return RedirectToAction("InviteUserMessage", new { message, currentProject.ID });
        }

        //Принятие приглашения
        [HttpGet]
        public async Task<IActionResult> AcceptInviteAsync(string token)
        {
            UserProject? userProject = _context.UserProject.FirstOrDefault(u => u.InviteToken == token);
           
            if (userProject == null)
            {
                ViewBag.message = "Ошибка присоединения к проекту.";
                ViewBag.success = false;
                return View();
            }
            Models.Project? currentProject = await _userService.GetProjectByID(userProject.ProjectID);
            User? currentUser = await _userService.GetUserByID(userProject.UserID);

            if (currentProject == null ||
                currentUser == null ||
                !await _userService.UserIsInProject(currentProject.ID, currentUser.ID))
            {
                ViewBag.message = "Ошибка присоединения к проекту.";
                ViewBag.success = false;
                return View();
            }

            if (!userProject.InviteTokenDate.HasValue)
            {
                ViewBag.message = "Приглашение недействительно.";
                ViewBag.success = false;
            }
            else if ((DateTime.Now - userProject.InviteTokenDate.Value).TotalHours >= 24)
            {
                ViewBag.message = "Время действия приглашения истекло.<br/>Попробуйте снова.";
                ViewBag.success = false;
            }
            else if (!userProject.IsActive)
            {
                ViewBag.message = "Вы успешно присоеденены к проекту.";
                userProject.IsActive = true;
                ViewBag.success = true;

                // Добавление записи в лог
                _logService.LogAction(currentProject.ID, 
                    $"присоединился к проекту \"{currentProject.Name}\"", 
                    currentUser.ID);
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
            UserProject? userProject = await _userService.GetUserProjectByID(userProjectID);
            if (userProject == null)
                return NotFound();

            User? currentUser = await _userService.GetCurrentUser(HttpContext);
            Models.Project? currentProject = await _userService.GetProjectByID(userProject.ProjectID);
            if (currentProject == null ||
                currentUser == null ||
                !await _userService.UserIsInProject(currentProject.ID, currentUser.ID))
                return NotFound();

            // Добавление записи в лог
            _logService.LogAction(currentProject.ID,
                $"исключил пользователя {userProject.User.FName} {userProject.User.LName} из проекта.", 
                currentUser.ID);

            _context.UserProject.Remove(userProject);
            _context.SaveChanges();
            return Redirect($"/Projects/ProjectSettings/{currentProject.ID}#users-section");
        }

        //Назначение категорий пользователю
        [HttpPost]
        public async Task<IActionResult> AssignCategoriesToUser(int userProjectID, int projectID, List<int> selectedCategories)
        {
            User? currentUser = await _userService.GetCurrentUser(HttpContext);
            Models.Project? currentProject = await _userService.GetProjectByID(projectID);
            UserProject? userProject = _context.UserProject
                .Include(x => x.User)
                .Include(up => up.CategoryTypes)
                .FirstOrDefault(x => x.ID == userProjectID);

            if (currentProject == null || 
                currentUser == null || 
                userProject == null || 
                !await _userService.UserIsInProject(currentProject.ID, currentUser.ID))
                return NotFound();

            var allCategories = _context.CategoryType.Where(c => c.ProjectID == projectID).ToList();

            userProject.CategoryTypes.Clear();

            if (selectedCategories != null && selectedCategories.Count != 0)
            {
                foreach (var categoryId in selectedCategories)
                {
                    var category = allCategories.FirstOrDefault(c => c.ID == categoryId);
                    if (category != null)
                        userProject.CategoryTypes.Add(category);
                }
            };

            _logService.LogAction(projectID, 
                $"обновил категории пользователя {userProject.User.FName} {userProject.User.LName}", 
                currentUser.ID);

            await _context.SaveChangesAsync();
            return Redirect($"/Projects/ProjectSettings/{userProject.ProjectID}#users-section");
        }

        //Выход из проекта
        [HttpPost]
        public async Task<IActionResult> LeaveProject(int projectID)
        {
            User? currentUser = await _userService.GetCurrentUser(HttpContext);
            Models.Project? currentProject = await _userService.GetProjectByID(projectID);
            if (currentUser == null || currentProject == null)
                return NotFound();

            UserProject? userProject = await _userService.GetUserProjectByID(projectID, currentUser.ID);

            if (userProject == null)
                return NotFound();
            _context.UserProject.Remove(userProject);

            // Добавление записи в лог
            _logService.LogAction(projectID, 
                $"покинул проект", 
                currentUser.ID);

            await _context.SaveChangesAsync();
            return Redirect("/Projects/AllProjects");
        }
        //Изменение роли пользователя
        [HttpPost]
        public async Task<IActionResult> EditUserRole(int userProjectID, UserRoles newRole)
        {
            User? currentUser = await _userService.GetCurrentUser(HttpContext);
            UserProject? userProject = _context.UserProject
                .Include(x => x.User)
                .FirstOrDefault(x => x.ID == userProjectID);

            if (userProject == null ||
                currentUser == null ||
                !await _userService.UserIsInProject(userProject.ProjectID, currentUser.ID))
                return NotFound();

            UserProject? userProjectEditor = await _userService.GetUserProjectByID(userProject.ProjectID, currentUser.ID);
            if (userProjectEditor == null)
                return NotFound();

            if (newRole == UserRoles.Admin)
            {
                userProject.UserRole = newRole;
                userProjectEditor.UserRole = UserRoles.Employee;

                // Добавление записи в лог
                _logService.LogAction(userProject.ProjectID, 
                    $"передал права на проект пользователю {userProject.User.FName}", 
                    currentUser.ID);
            }
            else
            {
                string oldRole = userProject.ConvertRoles(userProject.UserRole);
                userProject.UserRole = newRole;

                // Добавление записи в лог
                _logService.LogAction(userProject.ProjectID, 
                    $"изменил роль пользователя {userProject.User.FName} {userProject.User.LName} с \"{oldRole}\" на \"{userProject.ConvertRoles(newRole)}\"", 
                    currentUser.ID);
            }
            _context.SaveChanges();
            return Redirect($"/Projects/ProjectSettings/{userProject.ProjectID}#users-section");
        }

        //Добавление категории
        [HttpPost]
        public async Task<IActionResult> AddCategory([Bind("ID,Name")] CategoryType categoryType, IFormCollection formData, int projectID)
        {
            User? currentUser = await _userService.GetCurrentUser(HttpContext);
            Models.Project? currentProject = await _userService.GetProjectByID(projectID);
            if (currentProject == null ||
                currentUser == null ||
                !await _userService.UserIsInProject(currentProject.ID, currentUser.ID))
                return NotFound();

            categoryType.Project = currentProject;
            categoryType.ProjectID = currentProject.ID;

            _context.Add(categoryType);

            // Добавление записи в лог
            _logService.LogAction(currentProject.ID, 
                $"добавил категорию \"{categoryType.Name}\"", 
                currentUser.ID);

            _context.SaveChanges();

            return Redirect($"/Projects/ProjectSettings/{currentProject.ID}#categories-section");
        }
        //Редактирование категории
        [HttpPost]
        public async Task<IActionResult> EditCategory([Bind("ID,Name")] CategoryType categoryType, int categoryID)
        {
            User? currentUser = await _userService.GetCurrentUser(HttpContext);
            Models.Project? currentProject = await _userService.GetProjectByID(categoryType.ProjectID);
            if (currentProject == null ||
                currentUser == null ||
                !await _userService.UserIsInProject(currentProject.ID, currentUser.ID))
                return NotFound();

            CategoryType? category = _context.CategoryType.FirstOrDefault(x => x.ID == categoryID);
            if (category == null)
                return NotFound();

            string oldName = category.Name;
            category.Name = categoryType.Name;

            // Добавление записи в лог
            _logService.LogAction(category.ProjectID, 
                $"изменил название категории с \"{oldName}\" на \"{category.Name}\"", 
                currentUser.ID);

            await _context.SaveChangesAsync();
            return Redirect($"/Projects/ProjectSettings/{currentProject.ID}#categories-section");
        }
        //Удаление категории
        [HttpPost]
        public async Task<IActionResult> DeleteCategory(int categoryID)
        {
            User? currentUser = await _userService.GetCurrentUser(HttpContext);

            CategoryType? category = _context.CategoryType.FirstOrDefault(x => x.ID == categoryID);
            if (category == null)
                return NotFound();
            Models.Project? currentProject = await _userService.GetProjectByID(category.ProjectID);

            if (currentProject == null ||
                currentUser == null ||
                !await _userService.UserIsInProject(currentProject.ID, currentUser.ID))
                return NotFound();

            if (category.Project.Issues != null)
            {
                foreach (Issue issue in category.Project.Issues)
                {
                    issue.CategoryType = null;
                    issue.CategoryTypeID = null;
                }
            }
            _context.SaveChanges();

            // Добавление записи в лог
            _logService.LogAction(currentProject.ID, 
                $"удалил категорию \"{category.Name}\"", 
                currentUser.ID);

            _context.CategoryType.Remove(category);
            _context.SaveChanges();
            return Redirect($"/Projects/ProjectSettings/{currentProject.ID}#categories-section");
        }


        //Добавление задачи
        [HttpPost]
        public async Task<IActionResult> CreateIssueAsync([Bind("Name, Description, DeadlineDate, PerformerID, PriorityTypeID, CategoryTypeID")] Issue issue, int projectID)
        {
            User? currentUser = await _userService.GetCurrentUser(HttpContext);
            Models.Project? currentProject = await _userService.GetProjectByID(projectID);
            if (currentProject == null ||
                currentUser == null ||
                !await _userService.UserIsInProject(currentProject.ID, currentUser.ID))
                return NotFound();

            StatusType? statusType = currentProject.StatusTypes.FirstOrDefault(x => x.Name == "В процессе");
            if (statusType == null)
                return NotFound();

            Issue newIssue = new()
            {
            Name = issue.Name,
            Description = issue.Description ?? "",
            CreateDate = DateTime.Now,
            DeadlineDate = issue.DeadlineDate?.Date < DateTime.Now.Date ? DateTime.Now.Date : issue.DeadlineDate?.Date,
            CreatorID = currentUser.ID,
            PerformerID = issue.PerformerID,
            ProjectID = projectID,
            PriorityTypeID = issue.PriorityTypeID,
            StatusTypeID = statusType.ID,
            CategoryTypeID = issue.CategoryTypeID,
            IsDelete = false
            };

            _context.Add(newIssue);

            // Добавление записи в лог
            _logService.LogAction(currentProject.ID, 
                $"создал задачу \"{newIssue.Name}\"", 
                currentUser.ID);

            _context.SaveChanges();

            return Redirect($"/Projects/Project/{currentProject.ID}");
        }
        //Редактирование задачи
        [HttpPost]
        public async Task<IActionResult> UpdateIssueAsync([Bind("ID, Name, Description, DeadlineDate, PerformerID, PriorityTypeID, StatusTypeID, CategoryTypeID")] Issue issue, int issueID)
        {
            User? currentUser = await _userService.GetCurrentUser(HttpContext);
            Issue? currIssue = _context.Issue.FirstOrDefault(x => x.ID == issueID);
            if (currIssue == null ||
                currentUser == null ||
                !await _userService.UserIsInProject(currIssue.ProjectID, currentUser.ID))
                return NotFound();


            string oldName = currIssue.Name;
            string oldDescription = currIssue.Description ?? "";
            DateTime? oldDeadlineDate = currIssue.DeadlineDate;
            int? oldPerformerID = currIssue.PerformerID;
            int? oldPriorityTypeID = currIssue.PriorityTypeID;
            int? oldStatusTypeID = currIssue.StatusTypeID;
            int? oldCategoryTypeID = currIssue.CategoryTypeID;

            currIssue.Name = issue.Name;
            currIssue.Description = issue.Description ?? "";

            if (issue.DeadlineDate.HasValue)
            {
                if (issue.DeadlineDate.Value.Date >= DateTime.Now.Date)
                    currIssue.DeadlineDate = issue.DeadlineDate.Value.Date;
            }
            else
                currIssue.DeadlineDate = null;

            if (issue.StatusTypeID == _context.StatusType.FirstOrDefault(x => x.Name == "Завершенные" && x.ProjectID == currIssue.ProjectID).ID)
                currIssue.EndDate = DateTime.Now.Date;
            else if (currIssue.StatusType == _context.StatusType.FirstOrDefault(x => x.Name == "Завершенные" && x.ProjectID == currIssue.ProjectID))
                currIssue.EndDate = null;

            currIssue.PerformerID = issue.PerformerID;
            currIssue.PriorityTypeID = issue.PriorityTypeID;
            currIssue.StatusTypeID = issue.StatusTypeID;
            currIssue.CategoryTypeID = issue.CategoryTypeID;
            currIssue.IsDelete = false;
            _context.SaveChanges();

            List<string> changes = [];
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

            if (changes.Count != 0)
            {
                // Добавление записи в лог
                string changeMessage = string.Join(" ", changes);
                _logService.LogAction(currIssue.ProjectID, 
                    $"отредактировал задачу \"{oldName}\". Изменения: {changeMessage}", 
                    currentUser.ID);
            }

            return Redirect($"/Projects/Project/{currIssue.ProjectID}");
        }

        //Удаление задачи
        [HttpPost]
        public async Task<IActionResult> DeleteIssue(int issueID)
        {
            User? currentUser = await _userService.GetCurrentUser(HttpContext);
            Issue? currIssue = _context.Issue.FirstOrDefault(x => x.ID == issueID);
            if (currIssue == null ||
                currentUser == null ||
                !await _userService.UserIsInProject(currIssue.ProjectID, currentUser.ID))
                return NotFound();

            currIssue.IsDelete = true;
            currIssue.DeleteDate = DateTime.Now;

            // Добавление записи в лог
            _logService.LogAction(currIssue.ProjectID, 
                $"удалил задачу \"{currIssue.Name}\"", 
                currentUser.ID);

            await _context.SaveChangesAsync();
            return Redirect($"/Projects/Project/{currIssue.ProjectID}");
        }
        //Восстановление задачи
        [HttpPost]
        public async Task<IActionResult> RestoreIssue(int issueID)
        {
            User? currentUser = await _userService.GetCurrentUser(HttpContext);
            Issue? currIssue = _context.Issue.FirstOrDefault(x => x.ID == issueID);
            if (currIssue == null ||
                currentUser == null ||
                !await _userService.UserIsInProject(currIssue.ProjectID, currentUser.ID))
                return NotFound();

            currIssue.IsDelete = false;
            currIssue.DeleteDate = null;

            // Добавление записи в лог
            _logService.LogAction(currIssue.ProjectID, 
                $"восстановил задачу \"{currIssue.Name}\"", 
                currentUser.ID);

            await _context.SaveChangesAsync();
            return Redirect($"/Projects/DeletedIssueArchive/{currIssue.ProjectID}");
        }

        //Перетаскивание задачи
        [HttpGet]
        public async Task<IActionResult> UpdateIssueStatusAsync(int currIssueID, int currStatusID)
        {
            User? currentUser = await _userService.GetCurrentUser(HttpContext);
            Issue? currIssue = _context.Issue.Include(x=>x.StatusType).FirstOrDefault(x => x.ID == currIssueID);
            StatusType? newStatus = _context.StatusType.FirstOrDefault(x => x.ID == currStatusID);
            if (currIssue == null ||
                currentUser == null ||
                newStatus == null ||
                !await _userService.UserIsInProject(currIssue.ProjectID, currentUser.ID))
                return NotFound();

            if (currIssue.StatusTypeID == currStatusID)
                return Redirect($"/Projects/Project/{currIssue.ProjectID}");

            else if (currStatusID == _context.StatusType.FirstOrDefault(x => x.Name == "Завершенные" && x.ProjectID == currIssue.ProjectID).ID)
                    currIssue.EndDate = DateTime.Now.Date;
            else if (currIssue.StatusType == _context.StatusType.FirstOrDefault(x => x.Name == "Завершенные" && x.ProjectID == currIssue.ProjectID))
                currIssue.EndDate = null;

            string? oldStatusName = currIssue.StatusType.Name;
            currIssue.StatusTypeID = currStatusID;

            // Добавление записи в лог
            _logService.LogAction(currIssue.ProjectID, 
                $"изменил статус задачи \"{currIssue.Name}\" с \"{oldStatusName}\" на \"{newStatus.Name}\"",
                currentUser.ID);

            _context.SaveChanges();
            return Redirect($"/Projects/Project/{currIssue.ProjectID}");
        }

        //Сохранение лога
        [HttpGet("/Projects/DownloadHistory/{projectID}")]
        public async Task<IActionResult> DownloadHistoryAsync(int projectID)
        {
            User? currentUser = await _userService.GetCurrentUser(HttpContext);
            Models.Project? currentProject = await _userService.GetProjectByID(projectID);
            if (currentProject == null ||
                currentUser == null ||
                !await _userService.UserIsInProject(currentProject.ID, currentUser.ID))
                return NotFound();

            var logs = _context.Log
                .Where(x => x.ProjectID == currentProject.ID)
                .OrderByDescending(x => x.DateTime)
                .ToList();

            using var workbook = new ClosedXML.Excel.XLWorkbook();
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

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            var content = stream.ToArray();

            return File(
                content,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"ProjectHistory_{currentProject.Name}.xlsx"
            );
        }

    }
}
