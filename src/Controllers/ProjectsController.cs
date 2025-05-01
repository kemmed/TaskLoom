using diplom.Data;
using diplom.Models;
using diplom.Services;
using DocumentFormat.OpenXml.Bibliography;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.CodeAnalysis;
using Microsoft.EntityFrameworkCore;

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
            //var allProjects = _context.Project.Where(x => x.CreatorID == userLoggedInID ||
            //_context.UserProject.Any(ub => ub.ProjectID == x.ID
            //&& ub.UserID == userLoggedInID
            //&& ub.IsActive))
            //.Include(p => p.UserProjects)
            //.ToList();

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

            return View(filteredProjects.ToList());
        }

        [HttpGet("/Projects/Project/{projectID}")]
        public async Task<IActionResult> Project(int projectID, string filter = "all")
        {
            if (_context.Project.FirstOrDefault(x => x.ID == projectID) == null ||
                HttpContext.Session.GetInt32("UserID") == null ||
                _context.UserProject.FirstOrDefault(x => x.UserID == HttpContext.Session.GetInt32("UserID") && x.ProjectID == projectID) == null)
                     return NotFound();

            int? userSessionID = HttpContext.Session.GetInt32("UserID");

            var project = await _context.Project
                .Include(p => p.Issues)
                    .ThenInclude(i => i.Performer)
                .Include(p => p.UserProjects)
                    .ThenInclude(up => up.User)
                .FirstOrDefaultAsync(p => p.ID == projectID);

            if (project == null)
                return NotFound();

            project.PriorityTypes ??= new List<string>();
            project.StatusTypes ??= new List<string>();
            project.CategoryTypes ??= new List<string>();

            var allIssues = project.Issues.Where(i => !i.IsDelete).ToList();

            List<Models.Issue> filteredIssues = filter switch
            {
                "myTasks" => allIssues.Where(i => i.PerformerID == userSessionID).ToList(),
                "myCategory" => allIssues.Where(i => i.Category != null && project.CategoryTypes.Contains(i.Category)).ToList(),
                _ => allIssues
            };

            ViewBag.ProjectName = project.Name;
            ViewBag.ProjectID = project.ID;

            var reponsibilities = project.UserProjects?
                .Select(c => new SelectListItem
                {
                    Value = c.UserID.ToString(),
                    Text = $"{c.User.LName} {c.User.FName}"
                }).ToList() ?? new List<SelectListItem>();

            var categories = project.CategoryTypes?
                .Select(c => new SelectListItem { Value = c, Text = c })
                .ToList() ?? new List<SelectListItem>();

            var priorities = project.PriorityTypes?
                .Select(c => new SelectListItem { Value = c, Text = c })
                .ToList() ?? new List<SelectListItem>();

            var statuses = project.StatusTypes?
                .Select(c => new SelectListItem { Value = c, Text = c })
                .ToList() ?? new List<SelectListItem>();

            ViewBag.Responsibilities = reponsibilities;
            ViewBag.Categories = categories;
            ViewBag.Priorities = priorities;
            ViewBag.Statuses = statuses;


            var userProject = project.UserProjects?.FirstOrDefault(x => x.UserID == userSessionID);
            ViewBag.userRole = userProject?.UserRole switch
            {
                UserRoles.Admin => 1,
                UserRoles.Manager => 2,
                _ => 3
            };

            ViewBag.UserID = userSessionID;
            ViewBag.StatusTypes = project.StatusTypes;

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
            var userID = HttpContext.Session.GetInt32("UserID");
            if (_context.Project.Any(x => x.ID == projectID) == false ||
                userID == null ||
                _context.UserProject.Any(x => x.UserID == userID && x.ProjectID == projectID) == false)
                return NotFound();

            var project = await _context.Project
                .Include(p => p.UserProjects)
                    .ThenInclude(up => up.User)
                .FirstOrDefaultAsync(x => x.ID == projectID);

            if (project == null)
                return NotFound();

            project.CategoryTypes ??= new List<string>();
            project.PriorityTypes ??= new List<string>();
            project.StatusTypes ??= new List<string>();

            project.UserProjects = project.UserProjects
                .Where(up => up.IsActive)
                .OrderBy(up => up.UserRole)
                .ToList();

            var currUserProj = project.UserProjects.FirstOrDefault(up => up.UserID == userID);
            if (currUserProj == null)
                return NotFound();

            ViewBag.ProjectID = projectID;
            ViewBag.UserRole = currUserProj.UserRole;

            ViewBag.CategoryTypes = project.CategoryTypes;
            ViewBag.PriorityTypes = project.PriorityTypes;
            ViewBag.StatusTypes = project.StatusTypes;

            return View(project);
        }

        [HttpGet("/Projects/DeletedIssueArchive/{projectID}")]
        public async Task<IActionResult> DeletedIssueArchive(int projectID)
        {
            var userID = HttpContext.Session.GetInt32("UserID");

            if (_context.Project.Any(x => x.ID == projectID) == false ||
                userID == null ||
                _context.UserProject.Any(x => x.UserID == userID && x.ProjectID == projectID) == false)
                return NotFound();

            var deletedIssues = await _context.Issue
                .Where(x => x.ProjectID == projectID && x.IsDelete)
                .Include(x => x.Performer)
                .ToListAsync();

            if (deletedIssues == null || !deletedIssues.Any())
                return NotFound();

            return View(deletedIssues);
        }


        //Создание проекта
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateProject([Bind("ID,Name,Description,DeadlineDate")] Models.Project project)
        {
            int? userSessionID = HttpContext.Session.GetInt32("UserID");

            User creator = _context.User.FirstOrDefault(x => x.ID == userSessionID);
            if (creator == null)
                return NotFound();

            project.DeadlineDate = project.DeadlineDate?.Date < DateTime.Now.Date ? DateTime.Now.Date : project.DeadlineDate;
            project.Status = ProjectStatus.InProcess;
            project.CreateDate = DateTime.Now.Date;
            project.StatusTypes = new List<string>
            {
                "В процессе",
                "На проверке",
                "Отложенные",
                "Завершенные"
            };
            project.PriorityTypes = new List<string>
            {
                "Низкий",
                "Средний",
                "Высокий"
            };
            project.CategoryTypes = new List<string>();

            _context.Add(project);
            await _context.SaveChangesAsync();

            UserProject userProject = new UserProject
            {
                ProjectID = project.ID,
                Project = project,
                UserID = creator.ID,
                User = creator,
                UserRole = UserRoles.Admin,
                IsActive = true,
                IsCreator = true,
                Categories = new List<string>()
            };

            _context.Add(userProject);


            // Логируем действие
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
            UserProject userProject = _context.UserProject.FirstOrDefault(x => x.ID == userProjectID);
            int projectID = userProject.ProjectID;
            _context.UserProject.Remove(userProject);

            int? userSessionID = HttpContext.Session.GetInt32("UserID");
            User remover = _context.User.FirstOrDefault(x => x.ID == userSessionID);

            // Добавление записи в лог
            _logService.LogAction(projectID, $"исключил пользователя {userProject.User.FName} {userProject.User.LName} из проекта.", userSessionID);

            _context.SaveChanges();
            return Redirect($"/Projects/ProjectSettings/{projectID}");
        }

        //Назначение категорий пользователю
        [HttpPost]
        public async Task<IActionResult> AssignCategoriesToUser(int userProjectID, int projectID, List<string> selectedCategories)
        {
            var userProject = _context.UserProject
                .Include(up => up.User)
                .FirstOrDefault(x => x.ID == userProjectID);

            if (userProject == null)
                return NotFound();

            userProject.Categories ??= new List<string>();

            userProject.Categories.Clear();

            if (selectedCategories != null && selectedCategories.Any())
            {
                userProject.Categories.AddRange(selectedCategories);
            }

            // Добавление записи в лог
            int? userSessionID = HttpContext.Session.GetInt32("UserID");
            var currentUser = _context.User.FirstOrDefault(u => u.ID == userSessionID);

            _logService.LogAction(projectID,$"обновил категории пользователя {userProject.User.FName} {userProject.User.LName}", userSessionID);

            _context.Update(userProject);
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
        public async Task<IActionResult> AddCategory(string categoryName, int projectID)
        {
            var currProject = await _context.Project.FindAsync(projectID);
            if (currProject == null)
                return NotFound();

            //if (string.IsNullOrWhiteSpace(categoryName))
            //    return BadRequest("Название категории не может быть пустым");

            if (currProject.CategoryTypes == null)
                currProject.CategoryTypes = new List<string>();

            if (currProject.CategoryTypes.Contains(categoryName))
                return Redirect($"/Projects/ProjectSettings/{currProject.ID}");

            currProject.CategoryTypes.Add(categoryName);

            int? userSessionID = HttpContext.Session.GetInt32("UserID");
            User creator = _context.User.FirstOrDefault(x => x.ID == userSessionID);

            // Добавление записи в лог
            _logService.LogAction(currProject.ID, $"добавил категорию \"{categoryName}\"", userSessionID);

            _context.Update(currProject);
            await _context.SaveChangesAsync();

            return Redirect($"/Projects/ProjectSettings/{currProject.ID}");
        }

        //Редактирование категории
        [HttpPost]
        public async Task<IActionResult> EditCategory(int projectID, string oldName, string newName)
        {
            var project = await _context.Project.FirstOrDefaultAsync(p => p.ID == projectID);
            if (project == null)
                return NotFound();

            project.CategoryTypes ??= new List<string>();

            if (!project.CategoryTypes.Contains(oldName))
                return BadRequest("Категория не найдена");

            if (project.CategoryTypes.Contains(newName))
                return Redirect($"/Projects/ProjectSettings/{projectID}");

            int index = project.CategoryTypes.IndexOf(oldName);
            project.CategoryTypes[index] = newName;

            // Добавление записи в лог
            int? userSessionID = HttpContext.Session.GetInt32("UserID");
            _logService.LogAction(projectID,$"изменил название категории с \"{oldName}\" на \"{newName}\"", userSessionID);

            _context.Update(project);
            await _context.SaveChangesAsync();

            return Redirect($"/Projects/ProjectSettings/{projectID}");
        }
        //Удаление категории
        [HttpPost]
        public async Task<IActionResult> DeleteCategory(int projectID, string categoryName)
        {
            var project = await _context.Project.FirstOrDefaultAsync(p => p.ID == projectID);
            if (project == null)
                return NotFound();

            project.CategoryTypes ??= new List<string>();

            if (!project.CategoryTypes.Contains(categoryName))
                return BadRequest("Категория не найдена");

            project.CategoryTypes.Remove(categoryName);

            if (project.Issues != null && project.Issues.Any())
            {
                foreach (var issue in project.Issues.Where(i => i.Category == categoryName))
                {
                    issue.Category = null;
                }
            }

            // Добавление записи в лог
            int? userSessionID = HttpContext.Session.GetInt32("UserID");
            _logService.LogAction(projectID,$"удалил категорию \"{categoryName}\"", userSessionID);

            _context.Update(project);
            await _context.SaveChangesAsync();

            return Redirect($"/Projects/ProjectSettings/{projectID}");
        }


        //Добавление задачи
        [HttpPost]
        public IActionResult CreateIssue([Bind("Name,Description,DeadlineDate,PerformerID,Priority,Category")] Models.Issue issue,int projectID)
        {
            int? userSessionID = HttpContext.Session.GetInt32("UserID");

            var project = _context.Project.FirstOrDefault(x => x.ID == projectID);

            if (project == null)
                return NotFound();

            project.PriorityTypes ??= new List<string>();
            project.CategoryTypes ??= new List<string>();
            project.StatusTypes ??= new List<string>();

            Models.Issue newIssue = new Models.Issue
            {
                Name = issue.Name,
                Description = issue.Description ?? "",
                CreateDate = DateTime.Now,
                DeadlineDate = issue.DeadlineDate?.Date < DateTime.Now.Date ? DateTime.Now.Date : issue.DeadlineDate?.Date,
                CreatorID = (int)userSessionID,
                PerformerID = issue.PerformerID,
                ProjectID = projectID,

                Priority = issue.Priority,
                Category = issue.Category,
                Status = project.StatusTypes.FirstOrDefault(st => st == "В процессе") ?? project.StatusTypes.FirstOrDefault(),

                IsDelete = false
            };

            _context.Add(newIssue);

            // Добавление записи в лог
            _logService.LogAction(projectID, $"создал задачу \"{newIssue.Name}\"", userSessionID);

            _context.SaveChanges();

            return Redirect($"/Projects/Project/{projectID}");
        }
        //Редактирование задачи
        //[HttpPost]
        //public IActionResult UpdateIssue([Bind("ID, Name, Description, DeadlineDate, PerformerID, PriorityTypeID, StatusTypeID, CategoryTypeID")] Models.Issue issue, int issueID)
        //{
        //    Models.Issue currIssue = _context.Issue.FirstOrDefault(x => x.ID == issueID);

        //    string oldName = currIssue.Name;
        //    string oldDescription = currIssue.Description;
        //    DateTime? oldDeadlineDate = currIssue.DeadlineDate;
        //    int? oldPerformerID = currIssue.PerformerID;
        //    int? oldPriorityTypeID = currIssue.PriorityTypeID;
        //    int? oldStatusTypeID = currIssue.StatusTypeID;
        //    int? oldCategoryTypeID = currIssue.CategoryTypeID;

        //    currIssue.Name = issue.Name;
        //    currIssue.Description = issue.Description ?? "";

        //    if (currIssue.DeadlineDate.Value.Date >= DateTime.Now.Date || currIssue.DeadlineDate.Value.Date > currIssue.DeadlineDate.Value.Date)
        //        currIssue.DeadlineDate = currIssue.DeadlineDate.Value.Date;

        //    currIssue.PerformerID = issue.PerformerID;
        //    currIssue.PriorityTypeID = issue.PriorityTypeID;
        //    currIssue.StatusTypeID = issue.StatusTypeID;
        //    currIssue.CategoryTypeID = issue.CategoryTypeID;
        //    currIssue.IsDelete = false;

        //    List<string> changes = new List<string>();
        //    if (oldName != currIssue.Name)
        //        changes.Add($"Название изменено с \"{oldName}\" на \"{currIssue.Name}\";");
        //    if (oldDescription != currIssue.Description)
        //        changes.Add($"Описание изменено;");
        //    if (oldDeadlineDate != currIssue.DeadlineDate)
        //        changes.Add($"Срок изменен с \"{oldDeadlineDate?.ToString("dd.MM.yyyy")}\" на \"{currIssue.DeadlineDate?.ToString("dd.MM.yyyy")}\";");
        //    if (oldPerformerID != currIssue.PerformerID)
        //    {
        //        var oldPerformer = _context.User.FirstOrDefault(u => u.ID == oldPerformerID);
        //        var newPerformer = _context.User.FirstOrDefault(u => u.ID == currIssue.PerformerID);
        //        changes.Add($"Ответственный изменен с \"{oldPerformer?.FName} {oldPerformer?.LName}\" на \"{newPerformer?.FName} {newPerformer?.LName}\";");
        //    }
        //    if (oldPriorityTypeID != currIssue.PriorityTypeID)
        //    {
        //        var oldPriority = _context.PriorityType.FirstOrDefault(p => p.ID == oldPriorityTypeID);
        //        var newPriority = _context.PriorityType.FirstOrDefault(p => p.ID == currIssue.PriorityTypeID);
        //        changes.Add($"Приоритет изменен с \"{oldPriority?.Name}\" на \"{newPriority?.Name}\";");
        //    }
        //    if (oldStatusTypeID != currIssue.StatusTypeID)
        //    {
        //        var oldStatus = _context.StatusType.FirstOrDefault(s => s.ID == oldStatusTypeID);
        //        var newStatus = _context.StatusType.FirstOrDefault(s => s.ID == currIssue.StatusTypeID);
        //        changes.Add($"Статус изменен с \"{oldStatus?.Name}\" на \"{newStatus?.Name}\";");
        //    }
        //    if (oldCategoryTypeID != currIssue.CategoryTypeID)
        //    {
        //        var oldCategory = _context.CategoryType.FirstOrDefault(c => c.ID == oldCategoryTypeID);
        //        var newCategory = _context.CategoryType.FirstOrDefault(c => c.ID == currIssue.CategoryTypeID);
        //        changes.Add($"Категория изменена с \"{oldCategory?.Name}\" на \"{newCategory?.Name}\"");
        //    }
        //    _context.SaveChanges();


        //    int? userSessionID = HttpContext.Session.GetInt32("UserID");
        //    if (changes.Any())
        //    {
        //        // Добавление записи в лог
        //        string changeMessage = string.Join(" ", changes);
        //        _logService.LogAction(currIssue.ProjectID, $"отредактировал задачу \"{oldName}\". Изменения: {changeMessage}", userSessionID);
        //    }

        //    return Redirect($"/Projects/Project/{currIssue.ProjectID}");
        //}

        ////Удаление задачи
        //[HttpPost]
        //public async Task<IActionResult> DeleteIssue(int issueID)
        //{
        //    Models.Issue currIssue = _context.Issue.FirstOrDefault(x => x.ID == issueID);
        //    currIssue.IsDelete = true;
        //    currIssue.DeleteDate = DateTime.Now;

        //    int? userSessionID = HttpContext.Session.GetInt32("UserID");
        //    User deleter = _context.User.FirstOrDefault(x => x.ID == userSessionID);

        //    // Добавление записи в лог
        //    _logService.LogAction(currIssue.ProjectID, $"удалил задачу \"{currIssue.Name}\"", userSessionID);

        //    await _context.SaveChangesAsync();
        //    return Redirect($"/Projects/Project/{currIssue.ProjectID}");
        //}
        //Восстановление задачи
        [HttpPost]
        public async Task<IActionResult> RestoreIssue(int issueID)
        {
            Models.Issue currIssue = _context.Issue.FirstOrDefault(x => x.ID == issueID);
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
        public IActionResult UpdateIssueStatus(int currIssueID, string newStatus)
        {
            var issue = _context.Issue
                .Include(i => i.Project)
                .FirstOrDefault(x => x.ID == currIssueID);

            if (issue == null)
                return NotFound();

            if (string.IsNullOrWhiteSpace(newStatus))
                return BadRequest("Новый статус не указан");

            if (issue.Status == newStatus)
                return Redirect($"/Projects/Project/{issue.ProjectID}");

            var oldStatusName = issue.Status;

            //if (issue.Project?.StatusTypes == null || !issue.Project.StatusTypes.Contains(newStatus))
            //    return BadRequest("Недопустимый статус");

            issue.Status = newStatus;

            int? userSessionID = HttpContext.Session.GetInt32("UserID");

            // Добавление записи в лог
            _logService.LogAction(issue.ProjectID, $"изменил статус задачи \"{issue.Name}\" с \"{oldStatusName}\" на \"{newStatus}\"", userSessionID);

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

        //Возвращение проекта
        [HttpGet("/Projects/GetProjectInfo/{projectID}")]
        public async Task<IActionResult> GetProjectInfo(int projectID)
        {
            bool isAdmin;
            int? userSessionID = HttpContext.Session.GetInt32("UserID");
            var project = await _context.Project.Include(p => p.UserProjects).FirstOrDefaultAsync(x => x.ID == projectID);

            if (project.UserProjects.FirstOrDefault(x => x.UserID == userSessionID).UserRole==UserRoles.Admin)
                isAdmin = true;
            else
                isAdmin = false;
            var currProject = new
            {
                project,
                isAdmin
            };
            return Ok(currProject);
        }
        //Возвращение задачи
        [HttpGet("/Projects/GetIssueInfo/{issueID}")]
        public async Task<IActionResult> GetIssueInfo(int issueID)
        {
            bool isCreator;
            int? userSessionID = HttpContext.Session.GetInt32("UserID");
            Models.Issue issue = await _context.Issue
                    .Include(i => i.Project)
                    .Include(i => i.Creator)
                    .FirstOrDefaultAsync(x => x.ID == issueID);
            if (issue.Creator.ID == userSessionID)
                isCreator = true;
            else
                isCreator = false;

            var currIssue = new
            {
                issue,
                isCreator
            };
            return Ok(currIssue);
        }
        //Возвращение пользователя в проекте
        [HttpGet("/Projects/GetUserProjectInfo/{userProjID}")]
        public async Task<IActionResult> GetUserProjectInfo(int userProjID)
        {
            var userProject = await _context.UserProject
                .Include(up => up.User)
                .Include(up => up.Project)
                .FirstOrDefaultAsync(x => x.ID == userProjID);

            if (userProject == null)
                return NotFound();

            var result = new
            {
                id = userProject.ID,
                user = new
                {
                    fName = userProject.User.FName,
                    lName = userProject.User.LName
                },
                project = new
                {
                    id = userProject.Project.ID
                }
            };

            return Ok(result);
        }
    }
}
