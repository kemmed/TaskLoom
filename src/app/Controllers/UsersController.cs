﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using taskloom.Data;
using taskloom.Models;
using taskloom.Services;
using System.Text;
using System.Security.Cryptography;
using NuGet.Common;
using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.CodeAnalysis;

namespace taskloom.Controllers
{
    public class UsersController : Controller
    {
        private readonly taskloomContext _context;
        private readonly MailService _mailService;
        private readonly TokenService _tokenService;
        private readonly PasswordService _passwordService;
        private readonly UserProjectService _userService;

        public UsersController(taskloomContext context, MailService mailService, TokenService tokenService, PasswordService passwordService, UserProjectService userService)
        {
            _context = context;
            _mailService = mailService;
            _tokenService = tokenService;
            _passwordService = passwordService;
            _userService = userService;
        }

        public IActionResult Login()
        {
            return View();
        }

        public IActionResult Registration()
        {
            return View();
        }
        public IActionResult AwaitingConfirmation(string email)
        {
            ViewBag.email = email;
            return View();
        }
        public IActionResult WrongEmailReg(string email)
        {
            ViewBag.email = email;
            return View();
        }
        public IActionResult PasswordRecoveryRequest()
        {
            return View();
        }
        public async Task<IActionResult> UserProfile()
        {
            User? currentUser = await _userService.GetCurrentUser(HttpContext);
            if (currentUser == null)
                return NotFound();

            return View(currentUser);
        }

        //Восстановление пароля
        public IActionResult PasswordRecovery(string token)
        {
            ViewBag.token = token;
            bool success = true;
            string message;

            User? recoveryUser = _context.User.FirstOrDefault(u => u.PassRecoveryToken == token);

            if (recoveryUser == null)
            {
                message = "Ошибка восстановления пароля.";
                success = false;
                return Redirect($"PasswordRecoveryMessage?message={message}&success={success}");
            }
            else if (!recoveryUser.PassRecoveryTokenDate.HasValue || (DateTime.Now - recoveryUser.PassRecoveryTokenDate.Value).TotalMinutes >= 30)
            {
                message = "Время восстановления пароля истекло.<br/>Попробуйте снова.";
                success = false;
                return Redirect($"PasswordRecoveryMessage?message={message}&success={success}");
            }

            return View();
        }
        //Результат восстановления пароля
        public IActionResult PasswordRecoveryMessage(string message, bool success)
        {
            ViewBag.message = message;
            ViewBag.success = success;

            return View();
        }
        //Ожидание восстановления пароля
        public IActionResult AwaitingPasswordRecovery(string email)
        {
            ViewBag.email = email;
            return View();
        }

        //Авторизация пользователя
        [HttpPost]
        public async Task<IActionResult> Login([Bind("ID,Email,HashPass")] User user)
        {
            List<User> users = await _context.User.ToListAsync();
            var logUser = await _context.User
                .FirstOrDefaultAsync(m => m.Email == user.Email && 
                m.HashPass == _passwordService.HashPassword(user.HashPass));

            if (logUser == null)
                return Redirect("/Users/AuthError?error=wrongemail");
            else
            {
                HttpContext.Session.SetInt32("UserID", logUser.ID);
                HttpContext.Session.SetString("UserName", logUser.FName ?? "Не найдено");
                return Redirect("/Projects/AllProjects");
            }
        }
        //Выход пользователя
        [HttpGet]
        public IActionResult Logout()
        {
            HttpContext.Session.Remove("UserID");
            HttpContext.Session.Remove("UserName");
            return Redirect("/");
        }
        //Запрос на восстановление пароля
        [HttpPost]
        public async Task<IActionResult> PasswordRecoveryRequest([Bind("ID,Email")] User user)
        {
            var recoveryUser = await _context.User
                .FirstOrDefaultAsync(m => m.Email == user.Email);

            if (recoveryUser == null || !recoveryUser.IsActive)
            {
                return Redirect("/Users/PasswordRecoveryError?error=wrongemail");
            }
            else
            {
                    recoveryUser.PassRecoveryToken = _tokenService.GenerateToken();
                    recoveryUser.PassRecoveryTokenDate = DateTime.Now;

                    string templatePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "emailMessages", "passRecovery.html");
                    string emailTemplate = System.IO.File.ReadAllText(templatePath);

                    string recoveryLink = $"https://localhost:7297/Users/PasswordRecovery?token={recoveryUser.PassRecoveryToken}";
                    string emailBody = emailTemplate.Replace("{recoveryLink}", recoveryLink);
                    emailBody = emailBody.Replace("{FName}", recoveryUser.FName);
                    emailBody = emailBody.Replace("{LName}", recoveryUser.LName);

                    _mailService.SendEmail(emailBody, "Восстановление пароля", recoveryUser.Email);

                    await _context.SaveChangesAsync();
                    return Redirect($"AwaitingPasswordRecovery?email={recoveryUser.Email}");
            }
        }
        //Восстановление пароля
        [HttpPost]
        public async Task<IActionResult> PasswordRecovery([Bind("ID,HashPass")] User user, string token, string HashPassRepeat)
        {
            bool success;
            string message;
            User? recoveryUser = _context.User.FirstOrDefault(u => u.PassRecoveryToken == token);

            if (recoveryUser == null ||
            !recoveryUser.PassRecoveryTokenDate.HasValue ||
            (DateTime.Now - recoveryUser.PassRecoveryTokenDate.Value).TotalMinutes >= 30)
            {
                message = "Ошибка восстановления пароля.";
                success = false;
                return Redirect($"PasswordRecoveryMessage?message={message}&success={success}");
            }

            recoveryUser.HashPass = _passwordService.HashPassword(user.HashPass);
            HashPassRepeat = _passwordService.HashPassword(HashPassRepeat);

            if (recoveryUser.HashPass != HashPassRepeat)
                return Redirect($"/Users/PasswordRecoveryError?error=wrongpass&token={token}");

            recoveryUser.PassRecoveryToken = null;
            recoveryUser.PassRecoveryTokenDate = null;

            await _context.SaveChangesAsync();

            message = "Пароль успешно восстановлен.";
            success = true;

            string successMessage = WebUtility.UrlEncode(message);
            return Redirect($"PasswordRecoveryMessage?message={successMessage}&success={success}");
        }

        // Регистрация пользователя
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ID,FName,LName,Email,HashPass")] User user)
        {
            var existingUser = await _context.User.FirstOrDefaultAsync(u => u.Email == user.Email);

            if (existingUser != null)
                return Redirect("/Users/RegError?view=Registration");
            if (!_passwordService.IsPasswordValid(user.HashPass))
                return Redirect("/Users/RegError?view=Registration&errorType=password");

            user.HashPass = _passwordService.HashPassword(user.HashPass);
            user.IsActive = false;
            _context.Add(user);

            HttpContext.Session.Remove("UserID");

            try
            {
                user.RegToken = _tokenService.GenerateToken();
                user.RegTokenDate = DateTime.Now;

                string templatePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "emailMessages", "аctivation.html");
                string emailTemplate = System.IO.File.ReadAllText(templatePath);
                string activationLink = $"https://localhost:7297/Users/ActivationUser?token={user.RegToken}";
                string emailBody = emailTemplate.Replace("{activationLink}", activationLink);
                emailBody = emailBody.Replace("{FName}", user.FName);
                emailBody = emailBody.Replace("{LName}", user.LName);

                _mailService.SendEmail(emailBody, "Активация аккаунта", user.Email);
            }
            catch (Exception)
            {
                return Redirect($"WrongEmailReg?email={user.Email}");
            }

            
            await _context.SaveChangesAsync();
            return Redirect($"AwaitingConfirmation?email={user.Email}");
        }

        //Активация аккаунта пользователя
        [HttpGet]
        public  IActionResult ActivationUser(string token)
        {
            User? existingUser = _context.User.FirstOrDefault(u => u.RegToken == token);
            if (existingUser == null)
            {
                ViewBag.message = "Ошибка активации аккаунта.";
                ViewBag.success = false;
            }
            else if (!existingUser.RegTokenDate.HasValue || (DateTime.Now - existingUser.RegTokenDate.Value).TotalMinutes >= 30)
            {
                ViewBag.message = "Время активации аккаунта истекло.<br/>Попробуйте снова.";
                ViewBag.success = false;
            }
            else if (!existingUser.IsActive)
            {
                ViewBag.message = "Ваш аккаунт успешно активирован.";
                existingUser.IsActive = true;
                ViewBag.success = true;
            }
            else
            {
                ViewBag.message = "Ваш аккаунт уже активирован.";
                ViewBag.success = true;
            }
            _context.SaveChanges();
            return View();
        }

        // Изменение имени пользователя
        [HttpPost]
        public async Task<IActionResult> UpdateProfileName([Bind("ID,FName,LName")] User user)
        {
            User? currentUser = await _userService.GetCurrentUser(HttpContext);
            if (currentUser == null)
                return NotFound();

            currentUser.LName = user.LName;
            currentUser.FName = user.FName;
            HttpContext.Session.SetString("UserName", user.FName ?? "Не найдено");

            await _context.SaveChangesAsync();
            return Redirect("UserProfile");

        }

        // Изменение пароля пользователя
        [HttpPost]
        public async Task<IActionResult> UpdateProfilePassword([Bind("ID,HashPass")] User user)
        {
            User? currentUser = await _userService.GetCurrentUser(HttpContext);
            if (currentUser == null)
                return NotFound();

            currentUser.HashPass = _passwordService.HashPassword(user.HashPass);
            await _context.SaveChangesAsync();
            return Redirect("UserProfile");
        }

        // Ошибка при регистрации
        public IActionResult RegError(string view, string errorType)
        {
            if (errorType == "password")
                ViewBag.error = "Пароль должен содержать не менее 8 символов, включая заглавные, строчные буквы, цифры и специальные символы.";
            else
                ViewBag.error = "На этот email уже зарегистрирован аккаунт.";

            return View($"{view}");
        }

        // Ошибка при авторизации
        public IActionResult AuthError(string error)
        {
            if (error == "wrongemail")
                ViewBag.error = "Неправильный email или пароль.";
            return View("Login");
        }
        // Ошибка при отправке запроса на подтверждение
        public IActionResult PasswordRecoveryError(string error, string token)
        {
            if (error == "wrongemail")
            {
                ViewBag.error = "Аккаунта с таким email не существует.";
                return View("PasswordRecoveryRequest");
            }
            else if (error == "wrongpass")
            {
                ViewBag.error = "Пароли не совпадают.";
                ViewBag.token = token;
                return View("PasswordRecovery");
            }

            return View("/");
        }
    }
}
