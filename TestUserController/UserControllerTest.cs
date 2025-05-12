using System;
using System.Collections.Generic;
using System.Linq;
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
using Microsoft.Extensions.Caching.Memory;
using Xunit;
using System.IO;
using System.Text;
using System.Security.Cryptography;
using DocumentFormat.OpenXml.Spreadsheet;
using System.Net.Mail;
using System.Net;
using System.Text.Json;
using System.Reflection;
using Microsoft.DotNet.Scaffolding.Shared;

[Collection("DatabaseCollection")]
public class UsersControllerTests
{
    private readonly taskloomContext _context;
    private readonly Mock<MailService> _mockMailService;
    private readonly Mock<TokenService> _mockTokenService;
    private readonly Mock<taskloom.Services.PasswordService> _mockPasswordService;
    private readonly Mock<taskloom.Services.UserProjectService> _mockUserService;
    private readonly UsersController _controller;

    public UsersControllerTests(DatabaseFixture fixture)
    {
        _context = fixture.CreateContext();
        _mockMailService = new Mock<MailService>();
        _mockTokenService = new Mock<TokenService>();
        _mockPasswordService = new Mock<taskloom.Services.PasswordService>();
        _mockUserService = new Mock<taskloom.Services.UserProjectService>(_context);

        _mockUserService.Setup(x => x.GetCurrentUser(It.IsAny<HttpContext>())).ReturnsAsync((User?)null);

        var httpContext = new DefaultHttpContext();
        httpContext.Session = new TestSession();
        _controller = new UsersController(
            _context,
            _mockMailService.Object,
            _mockTokenService.Object,
            _mockPasswordService.Object,
            _mockUserService.Object
        )
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            }
        };

        _mockPasswordService.Setup(x => x.HashPassword(It.IsAny<string>())).Returns((string s) => s);
        _mockPasswordService.Setup(x => x.IsPasswordValid(It.IsAny<string>())).Returns(true);
        _mockTokenService.Setup(x => x.GenerateToken()).Returns("generated_token");
    }

    [Fact]
    public void Login_ReturnsView()
    {
        var result = _controller.Login() as ViewResult;
        Assert.NotNull(result);
    }

    [Fact]
    public void Registration_ReturnsView()
    {
        var result = _controller.Registration() as ViewResult;
        Assert.NotNull(result);
    }

    [Fact]
    public async Task UserProfile_UserNotFound_ReturnsNotFound()
    {
        _mockUserService.Setup(x => x.GetCurrentUser(It.IsAny<HttpContext>())).ReturnsAsync((User?)null);
        var result = await _controller.UserProfile() as NotFoundResult;
        Assert.NotNull(result);
    }

    [Fact]
    public async Task UserProfile_UserFound_ReturnsView()
    {
        var user = new User { ID = 1, Email = "test@example.com", HashPass = "hashed_password", IsActive = true, FName = "Иван", LName = "Иванов" };
        _mockUserService.Setup(x => x.GetCurrentUser(It.IsAny<HttpContext>())).ReturnsAsync(user);
        var result = await _controller.UserProfile() as ViewResult;
        Assert.NotNull(result);
        Assert.Equal(user, result.Model);
    }

    [Fact]
    public async Task Create_DuplicateUser_RedirectsToRegError()
    {
        var user = new User { Email = "test@example.com", HashPass = "StrongPass1!", FName = "Existing", LName = "User" };
        var result = await _controller.Create(user) as RedirectResult;
        Assert.NotNull(result);
        Assert.Equal("/Users/RegError?view=Registration", result.Url);
    }

    [Fact]
    public async Task PasswordRecoveryRequest_InvalidEmail_RedirectsToPasswordRecoveryError()
    {
        var user = new User
        {
            Email = "nonexistent@example.com",
            HashPass = "StrongPass1!"
        };
        var result = await _controller.PasswordRecoveryRequest(user) as RedirectResult;
        Assert.NotNull(result);
        Assert.Equal("/Users/PasswordRecoveryError?error=wrongemail", result.Url);
    }

    [Fact]
    public async Task ActivationUser_ValidToken_ActivatesUser()
    {
        var token = "generated_token";
        var user = _context.User.FirstOrDefault(u => u.ID == 2);
        if (user != null)
        {
            user.RegToken = token;
            user.RegTokenDate = DateTime.Now;
            user.IsActive = false;
            _context.Entry(user).State = EntityState.Modified;
            _context.SaveChanges();
        }
        var result = _controller.ActivationUser(token) as ViewResult;
        Assert.NotNull(result);
        Assert.True(_context.User.Find(user.ID)?.IsActive);
        Assert.Equal("Ваш аккаунт успешно активирован.", result.ViewData["message"]);
        Assert.Equal(true, result.ViewData["success"]);
    }

    [Fact]
    public async Task ActivationUser_ExpiredToken_ShowsErrorMessage()
    {
        var token = "generated_token";
        var user = _context.User.FirstOrDefault(u => u.ID == 2);
        if (user != null)
        {
            user.RegToken = token;
            user.RegTokenDate = DateTime.Now.AddHours(-1);
            user.IsActive = false;
            _context.Entry(user).State = EntityState.Modified;
            _context.SaveChanges();
        }
        var result = _controller.ActivationUser(token) as ViewResult;
        Assert.NotNull(result);
        Assert.False(_context.User.Find(user.ID)?.IsActive);
        Assert.Equal("Время активации аккаунта истекло.<br/>Попробуйте снова.", result.ViewData["message"]);
        Assert.Equal(false, result.ViewData["success"]);
    }

    [Fact]
    public void ActivationUser_UserNotFound_ReturnsErrorMessage()
    {
        var token = "nonexistent_token";
        var result = _controller.ActivationUser(token) as ViewResult;
        Assert.NotNull(result);
        Assert.Equal("Ошибка активации аккаунта.", result.ViewData["message"]);
        Assert.Equal(false, result.ViewData["success"]);
    }

    [Fact]
    public void ActivationUser_UserAlreadyActive_ReturnsSuccessMessage()
    {
        var token = "generated_token";
        var user = new User
        {
            ID = 3,
            Email = "alreadyactive@example.com",
            HashPass = "hashed_password",
            IsActive = true,
            FName = "Alice",
            LName = "Johnson",
            RegToken = token,
            RegTokenDate = DateTime.Now
        };
        _context.User.Add(user);
        _context.SaveChanges();

        var result = _controller.ActivationUser(token) as ViewResult;
        Assert.NotNull(result);
        Assert.Equal("Ваш аккаунт уже активирован.", result.ViewData["message"]);
        Assert.Equal(true, result.ViewData["success"]);
    }
    [Fact]
    public void AwaitingConfirmation_ReturnsViewWithCorrectEmail()
    {
        var email = "test@example.com";
        var result = _controller.AwaitingConfirmation(email) as ViewResult;
        Assert.NotNull(result);
        Assert.Equal(email, result.ViewData["email"]);
    }

    [Fact]
    public void WrongEmailReg_ReturnsViewWithCorrectEmail()
    {
        var email = "test@example.com";
        var result = _controller.WrongEmailReg(email) as ViewResult;
        Assert.NotNull(result);
        Assert.Equal(email, result.ViewData["email"]);
    }

    [Fact]
    public void PasswordRecoveryRequest_ReturnsView()
    {
        var result = _controller.PasswordRecoveryRequest() as ViewResult;
        Assert.NotNull(result);
    }



    [Fact]
    public void PasswordRecovery_ValidToken_ReturnsView()
    {
        var token = "valid_token";
        var user = new User
        {
            ID = 4,
            Email = "test@example.com",
            HashPass = "hashed_password",
            PassRecoveryToken = token,
            PassRecoveryTokenDate = DateTime.Now
        };
        _context.User.Add(user);
        _context.SaveChanges();

        var result = _controller.PasswordRecovery(token) as ViewResult;
        Assert.NotNull(result);
        Assert.Equal(token, _controller.ViewBag.token);
    }

    [Fact]
    public void PasswordRecovery_InvalidToken_RedirectsToPasswordRecoveryMessage()
    {
        var token = "invalid_token";

        var result = _controller.PasswordRecovery(token) as RedirectResult;
        Assert.NotNull(result);
        Assert.Equal($"PasswordRecoveryMessage?message=Ошибка восстановления пароля.&success=False", result.Url);
    }

    [Fact]
    public void PasswordRecovery_ExpiredToken_RedirectsToPasswordRecoveryMessage()
    {
        var token = "expired_token";
        var user = new User
        {
            ID = 5,
            Email = "test@example.com",
            HashPass = "hashed_password",
            PassRecoveryToken = token,
            PassRecoveryTokenDate = DateTime.Now.AddHours(-1)
        };
        _context.User.Add(user);
        _context.SaveChanges();

        var result = _controller.PasswordRecovery(token) as RedirectResult;
        Assert.NotNull(result);
        Assert.Equal($"PasswordRecoveryMessage?message=Время восстановления пароля истекло.<br/>Попробуйте снова.&success=False", result.Url);
    }

    [Fact]
    public void PasswordRecoveryMessage_ReturnsViewWithCorrectMessageAndSuccess()
    {
        var message = "Пароль успешно восстановлен.";
        var success = true;

        var result = _controller.PasswordRecoveryMessage(message, success) as ViewResult;
        Assert.NotNull(result);
        Assert.Equal(message, _controller.ViewBag.message);
        Assert.Equal(success, _controller.ViewBag.success);
    }

    [Fact]
    public async Task Login_ValidCredentials_RedirectsToAllProjects()
    {
        var user = new User { Email = "test@example.com", HashPass = "hashed_password" };
        _mockPasswordService.Setup(x => x.HashPassword(user.HashPass)).Returns(user.HashPass);
        var result = await _controller.Login(user) as RedirectResult;
        Assert.NotNull(result);
        Assert.Equal("/Projects/AllProjects", result.Url);
        Assert.Equal(1, _controller.HttpContext.Session.GetInt32("UserID"));
        Assert.Equal("Иван", _controller.HttpContext.Session.GetString("UserName"));
    }

    [Fact]
    public async Task Login_InvalidCredentials_RedirectsToAuthError()
    {
        var user = new User { Email = "nonexistent@example.com", HashPass = "hashed_password" };
        _mockPasswordService.Setup(x => x.HashPassword(user.HashPass)).Returns(user.HashPass);
        var result = await _controller.Login(user) as RedirectResult;
        Assert.NotNull(result);
        Assert.Equal("/Users/AuthError?error=wrongemail", result.Url);
    }

    [Fact]
    public void Logout_ClearsSessionAndRedirectsToHome()
    {
        _controller.HttpContext.Session.SetInt32("UserID", 1);
        _controller.HttpContext.Session.SetString("UserName", "Иван");

        var result = _controller.Logout() as RedirectResult;
        Assert.NotNull(result);
        Assert.Equal("/", result.Url);
        Assert.Null(_controller.HttpContext.Session.GetInt32("UserID"));
        Assert.Null(_controller.HttpContext.Session.GetString("UserName"));
    }

    [Fact]
    public void AuthError_WrongEmail_ReturnsLoginViewWithError()
    {
        var error = "wrongemail";

        var result = _controller.AuthError(error) as ViewResult;
        Assert.NotNull(result);
        Assert.Equal("Неправильный email или пароль.", _controller.ViewBag.error);
    }

    [Fact]
    public void PasswordRecoveryError_WrongEmail_ReturnsPasswordRecoveryRequestViewWithError()
    {
        var error = "wrongemail";
        var token = "some_token";

        var result = _controller.PasswordRecoveryError(error, token) as ViewResult;
        Assert.NotNull(result);
        Assert.Equal("Аккаунта с таким email не существует.", _controller.ViewBag.error);
    }

    [Fact]
    public void PasswordRecoveryError_WrongPass_ReturnsPasswordRecoveryViewWithError()
    {
        var error = "wrongpass";
        var token = "some_token";

        var result = _controller.PasswordRecoveryError(error, token) as ViewResult;
        Assert.NotNull(result);
        Assert.Equal("Пароли не совпадают.", _controller.ViewBag.error);
        Assert.Equal(token, _controller.ViewBag.token);
    }
    [Fact]
    public void AwaitingPasswordRecovery_ReturnsViewWithCorrectEmail()
    {
        var email = "test@example.com";
        var result = _controller.AwaitingPasswordRecovery(email) as ViewResult;
        Assert.NotNull(result);
        Assert.Equal(email, _controller.ViewBag.email);
    }

    [Fact]
    public void RegError_PasswordError_ReturnsViewWithCorrectErrorMessage()
    {
        var view = "Registration";
        var errorType = "password";
        var expectedErrorMessage = "Пароль должен содержать не менее 8 символов, включая заглавные, строчные буквы, цифры и специальные символы.";
        var result = _controller.RegError(view, errorType) as ViewResult;
        Assert.NotNull(result);
        Assert.Equal(expectedErrorMessage, _controller.ViewBag.error);
        Assert.Equal(view, result.ViewName);
    }

    [Fact]
    public void RegError_EmailExistsError_ReturnsViewWithCorrectErrorMessage()
    {
        var view = "Registration";
        var errorType = "email";
        var expectedErrorMessage = "На этот email уже зарегистрирован аккаунт.";
        var result = _controller.RegError(view, errorType) as ViewResult;
        Assert.NotNull(result);
        Assert.Equal(expectedErrorMessage, _controller.ViewBag.error);
        Assert.Equal(view, result.ViewName);
    }

}

public class PasswordService
{
    private static readonly Random _rnd = new Random();

    public virtual string GenerateToken()
    {
        string token = "";
        for (int i = 0; i < 16; i++)
        {
            token += (char)('a' + _rnd.Next(0, 28));
        }
        return token;
    }

    public virtual bool IsPasswordValid(string password)
    {
        if (string.IsNullOrEmpty(password) || password.Length < 8)
            return false;

        bool hasUpper = password.Any(char.IsUpper);
        bool hasLower = password.Any(char.IsLower);
        bool hasDigit = password.Any(char.IsDigit);
        bool hasSymbol = password.Any(c => !char.IsLetterOrDigit(c));

        return hasUpper && hasLower && hasDigit && hasSymbol;
    }

    public virtual string HashPassword(string password)
    {
        byte[] passwordBytes = Encoding.UTF8.GetBytes(password);
        using (SHA256 sha256 = SHA256.Create())
        {
            byte[] hashBytes = sha256.ComputeHash(passwordBytes);

            StringBuilder hashString = new StringBuilder();
            foreach (byte b in hashBytes)
            {
                hashString.Append(b.ToString("x2"));
            }

            return hashString.ToString();
        }
    }
}

public class UserProjectService
{
    private readonly taskloomContext _context;

    public UserProjectService(taskloomContext context)
    {
        _context = context;
    }

    public virtual async Task<User?> GetCurrentUser(HttpContext httpContext)
    {
        int? userSessionID = httpContext.Session.GetInt32("UserID");
        if (userSessionID == null)
            return null;

        User? user = await _context.User.FindAsync(userSessionID);
        return user;
    }
}