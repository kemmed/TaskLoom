using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.InMemory;
using Moq;
using taskloom.Controllers;
using taskloom.Data;
using taskloom.Models;
using taskloom.Services;
using Microsoft.Extensions.Logging;
using Xunit;
using System.IO;

[Collection("DatabaseCollection")]
public class ProjectsControllerTests
{
    private readonly taskloomContext _context;
    private readonly Mock<MailService> _mockMailService;
    private readonly Mock<TokenService> _mockTokenService;
    private readonly Mock<LogService> _mockLogService;
    private readonly Mock<taskloom.Services.UserProjectService> _mockUserProjectService;
    private readonly ChartService _chartService;
    private readonly Mock<SortUsersService> _mockSortUsersService;
    private readonly Mock<ExcelReportService> _mockExcelReportService;
    private readonly ProjectsController _controller;

    public ProjectsControllerTests(DatabaseFixture fixture)
    {
        _context = fixture.CreateContext();
        _mockMailService = new Mock<MailService>();
        _mockTokenService = new Mock<TokenService>();
        _mockLogService = new Mock<LogService>(_context);
        _mockUserProjectService = new Mock<taskloom.Services.UserProjectService>(_context);
        _chartService = new ChartService(_context);
        _mockSortUsersService = new Mock<SortUsersService>(_context);
        _mockExcelReportService = new Mock<ExcelReportService>();
        _mockUserProjectService.Setup(x => x.GetCurrentUser(It.IsAny<HttpContext>()))
            .ReturnsAsync(() => new User
            {
                ID = 1,
                Email = "test@example.com",
                HashPass = "hashed_password",
                IsActive = true,
                FName = "Иван",
                LName = "Иванов"
            });

        _mockLogService.Setup(x => x.LogAction(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<int?>()));

        var httpContext = new DefaultHttpContext();
        httpContext.Session = new TestSession();
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
        {
            new Claim(ClaimTypes.NameIdentifier, "1"),
            new Claim(ClaimTypes.Email, "test@example.com")
        }, "test"));

        _controller = new ProjectsController(
            _context,
            _mockMailService.Object,
            _mockTokenService.Object,
            _mockLogService.Object,
            _mockUserProjectService.Object,
            _chartService,
            _mockSortUsersService.Object,
            _mockExcelReportService.Object
        )
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            }
        };
        _mockTokenService.Setup(x => x.GenerateToken()).Returns("generated_token");
    }

    [Fact]
    public async Task AllProjects_ReturnsViewWithProjects()
    {
        var project = new taskloom.Models.Project
        {
            ID = 1,
            Name = "Test Project",
            Description = "This is a test project.",
            CreateDate = DateTime.Now,
            DeadlineDate = DateTime.Now.AddDays(10),
            Status = ProjectStatus.InProcess
        };
        _context.Project.Add(project);
        var userProject = new UserProject
        {
            UserID = 1,
            ProjectID = 1,
            UserRole = UserRoles.Admin,
            IsActive = true,
            IsCreator = true
        };
        _context.UserProject.Add(userProject);
        _context.SaveChanges();

        var result = await _controller.AllProjects() as ViewResult;
        Assert.NotNull(result);
        var model = result.Model as List<Project>;
        Assert.NotNull(model);
        Assert.Single(model);
        Assert.Equal("Test Project", model.First().Name);
    }

    [Fact]
    public async Task InviteUser_InvalidEmail_ReturnsErrorMessage()
    {
        var projectID = 1;
        var project = new Project
        {
            ID = projectID,
            Name = "Test Project",
            Description = "This is a test project.",
            CreateDate = DateTime.Now,
            DeadlineDate = DateTime.Now.AddDays(10),
            Status = ProjectStatus.InProcess
        };
        _context.Project.Add(project);
        var userProject = new UserProject
        {
            UserID = 1,
            ProjectID = projectID,
            UserRole = UserRoles.Admin,
            IsActive = true,
            IsCreator = true
        };
        _context.UserProject.Add(userProject);
        _context.SaveChanges();

        var result = await _controller.InviteUser(new User { Email = "nonexistent@example.com", HashPass = "hash_pass", FName="Иван",LName="Иванов" }, projectID) as RedirectToActionResult;
        Assert.NotNull(result);
        Assert.Equal("InviteUserMessage", result.ActionName);
        Assert.Equal(projectID, result.RouteValues["projectID"]);

        _mockMailService.Verify(x => x.SendEmail(It.IsAny<string>(), It.IsAny<string>(), "nonexistent@example.com"), Times.Never);
    }

    [Fact]
    public async Task AcceptInvite_ValidToken_AcceptsInvite()
    {
        var projectID = 1;
        var project = new Project
        {
            ID = projectID,
            Name = "Test Project",
            Description = "This is a test project.",
            CreateDate = DateTime.Now,
            DeadlineDate = DateTime.Now.AddDays(10),
            Status = ProjectStatus.InProcess
        };
        _context.Project.Add(project);
        var userProject = new UserProject
        {
            UserID = 1,
            ProjectID = projectID,
            UserRole = UserRoles.Admin,
            IsActive = true,
            IsCreator = true
        };
        _context.UserProject.Add(userProject);
        _context.SaveChanges();

        var invitedUser = new User
        {
            ID = 3,
            Email = "invited@example.com",
            HashPass = "hashed_password",
            IsActive = true,
            FName = "Alice",
            LName = "Johnson"
        };
        _context.User.Add(invitedUser);
        _context.SaveChanges();

        var inviteToken = "generated_token";
        var userProj = new UserProject
        {
            UserID = 3,
            ProjectID = projectID,
            UserRole = UserRoles.Employee,
            InviteToken = inviteToken,
            InviteTokenDate = DateTime.Now,
            IsActive = false
        };
        _context.UserProject.Add(userProj);
        _context.SaveChanges();

        var result = await _controller.AcceptInviteAsync(inviteToken) as ViewResult;
        Assert.NotNull(result);
        Assert.Equal("Вы успешно присоеденены к проекту.", result.ViewData["message"]);
        Assert.Equal(true, result.ViewData["success"]);

        var updatedUserProj = _context.UserProject.FirstOrDefault(up => up.UserID == 3 && up.ProjectID == projectID);
        Assert.NotNull(updatedUserProj);
        Assert.True(updatedUserProj.IsActive);
    }

    [Fact]
    public async Task AcceptInvite_ExpiredToken_ReturnsErrorMessage()
    {
        var projectID = 1;
        var project = new Project
        {
            ID = projectID,
            Name = "Test Project",
            Description = "This is a test project.",
            CreateDate = DateTime.Now,
            DeadlineDate = DateTime.Now.AddDays(10),
            Status = ProjectStatus.InProcess
        };
        _context.Project.Add(project);
        var userProject = new UserProject
        {
            UserID = 1,
            ProjectID = projectID,
            UserRole = UserRoles.Admin,
            IsActive = true,
            IsCreator = true
        };
        _context.UserProject.Add(userProject);
        _context.SaveChanges();

        var invitedUser = new User
        {
            ID = 3,
            Email = "invited@example.com",
            HashPass = "hashed_password",
            IsActive = true,
            FName = "Alice",
            LName = "Johnson"
        };
        _context.User.Add(invitedUser);
        _context.SaveChanges();

        var inviteToken = "generated_token";
        var userProj = new UserProject
        {
            UserID = 3,
            ProjectID = projectID,
            UserRole = UserRoles.Employee,
            InviteToken = inviteToken,
            InviteTokenDate = DateTime.Now.AddHours(-25),
            IsActive = false
        };
        _context.UserProject.Add(userProj);
        _context.SaveChanges();

        var result = await _controller.AcceptInviteAsync(inviteToken) as ViewResult;
        Assert.NotNull(result);
        Assert.Equal("Время действия приглашения истекло.<br/>Попробуйте снова.", result.ViewData["message"]);
        Assert.Equal(false, result.ViewData["success"]);

        var updatedUserProj = _context.UserProject.FirstOrDefault(up => up.UserID == 3 && up.ProjectID == projectID);
        Assert.NotNull(updatedUserProj);
        Assert.False(updatedUserProj.IsActive);
    }

    [Fact]
    public async Task RemoveUserFromProject_ValidUser_RemovesUser()
    {
        var projectID = 1;
        var project = new Project
        {
            ID = projectID,
            Name = "Test Project",
            Description = "This is a test project.",
            CreateDate = DateTime.Now,
            DeadlineDate = DateTime.Now.AddDays(10),
            Status = ProjectStatus.InProcess
        };
        _context.Project.Add(project);
        var userProject = new UserProject
        {
            UserID = 1,
            ProjectID = projectID,
            UserRole = UserRoles.Admin,
            IsActive = true,
            IsCreator = true
        };
        _context.UserProject.Add(userProject);
        _context.SaveChanges();

        var invitedUser = new User
        {
            ID = 3,
            Email = "invited@example.com",
            HashPass = "hashed_password",
            IsActive = true,
            FName = "Alice",
            LName = "Johnson"
        };
        _context.User.Add(invitedUser);
        _context.SaveChanges();

        var userProj = new UserProject
        {
            UserID = 3,
            ProjectID = projectID,
            UserRole = UserRoles.Employee,
            InviteToken = "generated_token",
            InviteTokenDate = DateTime.Now,
            IsActive = true
        };
        _context.UserProject.Add(userProj);
        _context.SaveChanges();

        var result = await _controller.RemoveUserFromProject(userProj.ID) as RedirectResult;
        Assert.NotNull(result);
        Assert.Equal($"/Projects/ProjectSettings/{projectID}#users-section", result.Url);

        var removedUserProj = _context.UserProject.FirstOrDefault(up => up.UserID == 3 && up.ProjectID == projectID);
        Assert.Null(removedUserProj);
    }

    [Fact]
    public async Task EditUserRole_ValidUser_ChangesRole()
    {
        var projectID = 1;
        var project = new Project
        {
            ID = projectID,
            Name = "Test Project",
            Description = "This is a test project.",
            CreateDate = DateTime.Now,
            DeadlineDate = DateTime.Now.AddDays(10),
            Status = ProjectStatus.InProcess
        };
        _context.Project.Add(project);
        var userProject = new UserProject
        {
            UserID = 1,
            ProjectID = projectID,
            UserRole = UserRoles.Admin,
            IsActive = true,
            IsCreator = true
        };
        _context.UserProject.Add(userProject);
        _context.SaveChanges();

        var invitedUser = new User
        {
            ID = 3,
            Email = "invited@example.com",
            HashPass = "hashed_password",
            IsActive = true,
            FName = "Alice",
            LName = "Johnson"
        };
        _context.User.Add(invitedUser);
        _context.SaveChanges();

        var userProj = new UserProject
        {
            UserID = 3,
            ProjectID = projectID,
            UserRole = UserRoles.Employee,
            InviteToken = "generated_token",
            InviteTokenDate = DateTime.Now,
            IsActive = true
        };
        _context.UserProject.Add(userProj);
        _context.SaveChanges();

        var result = await _controller.EditUserRole(userProj.ID, UserRoles.Manager) as RedirectResult;
        Assert.NotNull(result);
        Assert.Equal($"/Projects/ProjectSettings/{projectID}#users-section", result.Url);

        var updatedUserProj = _context.UserProject.FirstOrDefault(up => up.UserID == 3 && up.ProjectID == projectID);
        Assert.NotNull(updatedUserProj);
        Assert.Equal(UserRoles.Manager, updatedUserProj.UserRole);
    }

    [Fact]
    public async Task AddCategory_ValidCategory_AddsCategory()
    {
        var projectID = 1;
        var project = new Project
        {
            ID = projectID,
            Name = "Тестовый Проект",
            Description = "Это тестовый проект.",
            CreateDate = DateTime.Now,
            DeadlineDate = DateTime.Now.AddDays(10),
            Status = ProjectStatus.InProcess
        };
        _context.Project.Add(project);
        var userProject = new UserProject
        {
            UserID = 1,
            ProjectID = projectID,
            UserRole = UserRoles.Admin,
            IsActive = true,
            IsCreator = true
        };
        _context.UserProject.Add(userProject);
        _context.SaveChanges();

        var categoryType = new CategoryType
        {
            Name = "Новая Категория"
        };
        var result = await _controller.AddCategory(categoryType, null, projectID) as RedirectResult;

        Assert.NotNull(result);
        Assert.Equal($"/Projects/ProjectSettings/{projectID}#categories-section", result.Url);

        var addedCategory = _context.CategoryType.FirstOrDefault(ct => ct.Name == "Новая Категория" && ct.ProjectID == projectID);
        Assert.NotNull(addedCategory);
        Assert.Equal("Новая Категория", addedCategory.Name);

        _mockLogService.Verify(x => x.LogAction(projectID, It.Is<string>(s => s.Contains("добавил категорию")), 1), Times.Once);
    }
    [Fact]
    public async Task EditCategory_ValidCategory_EditsCategory()
    {
        var projectID = 1;
        var project = new Project
        {
            ID = projectID,
            Name = "Test Project",
            Description = "This is a test project.",
            CreateDate = DateTime.Now,
            DeadlineDate = DateTime.Now.AddDays(10),
            Status = ProjectStatus.InProcess
        };
        _context.Project.Add(project);
        var userProject = new UserProject
        {
            UserID = 1,
            ProjectID = projectID,
            UserRole = UserRoles.Admin,
            IsActive = true,
            IsCreator = true
        };
        _context.UserProject.Add(userProject);
        _context.SaveChanges();

        var categoryType = new CategoryType
        {
            ID = 1,
            Name = "Old Category",
            ProjectID = projectID
        };
        _context.CategoryType.Add(categoryType);
        _context.SaveChanges();

        var updatedCategoryType = new CategoryType
        {
            ID = 1,
            Name = "Updated Category",
            ProjectID = projectID
        };

        var result = await _controller.EditCategory(updatedCategoryType, categoryType.ID) as RedirectResult;
        Assert.NotNull(result);
        Assert.Equal($"/Projects/ProjectSettings/{projectID}#categories-section", result.Url);

        var editedCategory = _context.CategoryType.FirstOrDefault(ct => ct.ID == 1);
        Assert.NotNull(editedCategory);
        Assert.Equal("Updated Category", editedCategory.Name);
        _mockLogService.Verify(x => x.LogAction(projectID, It.Is<string>(s => s.Contains("изменил название категории с \"Old Category\" на \"Updated Category\"")), 1), Times.Once);
    }

    [Fact]
    public async Task DeleteCategory_ValidCategory_DeletesCategory()
    {
        var projectID = 1;
        var project = new Project
        {
            ID = projectID,
            Name = "Test Project",
            Description = "This is a test project.",
            CreateDate = DateTime.Now,
            DeadlineDate = DateTime.Now.AddDays(10),
            Status = ProjectStatus.InProcess
        };
        _context.Project.Add(project);
        var userProject = new UserProject
        {
            UserID = 1,
            ProjectID = projectID,
            UserRole = UserRoles.Admin,
            IsActive = true,
            IsCreator = true
        };
        _context.UserProject.Add(userProject);
        _context.SaveChanges();

        var categoryType = new CategoryType
        {
            ID = 1,
            Name = "Category to Delete",
            ProjectID = projectID
        };
        _context.CategoryType.Add(categoryType);
        _context.SaveChanges();

        var result = await _controller.DeleteCategory(categoryType.ID) as RedirectResult;
        Assert.NotNull(result);
        Assert.Equal($"/Projects/ProjectSettings/{projectID}#categories-section", result.Url);

        var deletedCategory = _context.CategoryType.FirstOrDefault(ct => ct.ID == 1);
        Assert.Null(deletedCategory);
    }

    
    [Fact]
    public async Task UpdateIssue_ValidIssue_UpdatesIssue()
    {
        var projectID = 1;
        var project = new Project
        {
            ID = projectID,
            Name = "Test Project",
            Description = "This is a test project.",
            CreateDate = DateTime.Now,
            DeadlineDate = DateTime.Now.AddDays(10),
            Status = ProjectStatus.InProcess
        };
        _context.Project.Add(project);
        var userProject = new UserProject
        {
            UserID = 1,
            ProjectID = projectID,
            UserRole = UserRoles.Admin,
            IsActive = true,
            IsCreator = true
        };
        _context.UserProject.Add(userProject);
        _context.SaveChanges();

        var issue = new Issue
        {
            ID = 1,
            Name = "Old Issue",
            Description = "Old description.",
            CreateDate = DateTime.Now,
            DeadlineDate = DateTime.Now.AddDays(5),
            CreatorID = 1,
            PerformerID = 1,
            ProjectID = projectID,
            PriorityTypeID = 1,
            StatusTypeID = 1,
            CategoryTypeID = 1,
            IsDelete = false
        };
        _context.Issue.Add(issue);
        _context.SaveChanges();

        var updatedIssue = new Issue
        {
            ID = 1,
            Name = "Updated Issue",
            Description = "Updated description.",
            DeadlineDate = DateTime.Now.AddDays(10),
            PerformerID = 1,
            PriorityTypeID = 1,
            StatusTypeID = 1,
            CategoryTypeID = 1
        };

        var result = await _controller.UpdateIssueAsync(updatedIssue, issue.ID) as RedirectResult;
        Assert.NotNull(result);
        Assert.Equal($"/Projects/Project/{projectID}", result.Url);

        var editedIssue = _context.Issue.FirstOrDefault(i => i.ID == 1);
        Assert.NotNull(editedIssue);
        Assert.Equal("Updated Issue", editedIssue.Name);
        Assert.Equal("Updated description.", editedIssue.Description);
        Assert.Equal(DateTime.Now.AddDays(10).Date, editedIssue.DeadlineDate.Value.Date);
    }

    [Fact]
    public async Task DeleteIssue_ValidIssue_DeletesIssue()
    {
        var projectID = 1;
        var project = new Project
        {
            ID = projectID,
            Name = "Test Project",
            Description = "This is a test project.",
            CreateDate = DateTime.Now,
            DeadlineDate = DateTime.Now.AddDays(10),
            Status = ProjectStatus.InProcess
        };
        _context.Project.Add(project);
        var userProject = new UserProject
        {
            UserID = 1,
            ProjectID = projectID,
            UserRole = UserRoles.Admin,
            IsActive = true,
            IsCreator = true
        };
        _context.UserProject.Add(userProject);
        _context.SaveChanges();

        var issue = new Issue
        {
            ID = 1,
            Name = "Issue to Delete",
            Description = "Description of issue to delete.",
            CreateDate = DateTime.Now,
            DeadlineDate = DateTime.Now.AddDays(5),
            CreatorID = 1,
            PerformerID = 1,
            ProjectID = projectID,
            PriorityTypeID = 1,
            StatusTypeID = 1,
            CategoryTypeID = 1,
            IsDelete = false
        };
        _context.Issue.Add(issue);
        _context.SaveChanges();

        var result = await _controller.DeleteIssue(issue.ID) as RedirectResult;
        Assert.NotNull(result);
        Assert.Equal($"/Projects/Project/{projectID}", result.Url);

        var deletedIssue = _context.Issue.FirstOrDefault(i => i.ID == 1);
        Assert.NotNull(deletedIssue);
        Assert.True(deletedIssue.IsDelete);
    }

    [Fact]
    public async Task RestoreIssue_ValidIssue_RestoresIssue()
    {
        var projectID = 1;
        var project = new Project
        {
            ID = projectID,
            Name = "Test Project",
            Description = "This is a test project.",
            CreateDate = DateTime.Now,
            DeadlineDate = DateTime.Now.AddDays(10),
            Status = ProjectStatus.InProcess
        };
        _context.Project.Add(project);
        var userProject = new UserProject
        {
            UserID = 1,
            ProjectID = projectID,
            UserRole = UserRoles.Admin,
            IsActive = true,
            IsCreator = true
        };
        _context.UserProject.Add(userProject);
        _context.SaveChanges();

        var issue = new Issue
        {
            ID = 1,
            Name = "Issue to Restore",
            Description = "Description of issue to restore.",
            CreateDate = DateTime.Now,
            DeadlineDate = DateTime.Now.AddDays(5),
            CreatorID = 1,
            PerformerID = 1,
            ProjectID = projectID,
            PriorityTypeID = 1,
            StatusTypeID = 1,
            CategoryTypeID = 1,
            IsDelete = true,
            DeleteDate = DateTime.Now
        };
        _context.Issue.Add(issue);
        _context.SaveChanges();

        var result = await _controller.RestoreIssue(issue.ID) as RedirectResult;
        Assert.NotNull(result);
        Assert.Equal($"/Projects/DeletedIssueArchive/{projectID}", result.Url);

        var restoredIssue = _context.Issue.FirstOrDefault(i => i.ID == 1);
        Assert.NotNull(restoredIssue);
        Assert.False(restoredIssue.IsDelete);
        Assert.Null(restoredIssue.DeleteDate);
    }
    [Fact]
    public void InviteUserMessage_ReturnsViewWithMessageAndProjectID()
    {
        var message = "Test Message";
        var projectID = 1;

        var result = _controller.InviteUserMessage(message, projectID) as ViewResult;

        Assert.NotNull(result);
        Assert.Equal(message, result.ViewData["message"]);
        Assert.Equal(projectID, result.ViewData["projectID"]);
    }

}