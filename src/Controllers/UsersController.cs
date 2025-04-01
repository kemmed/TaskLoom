using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using diplom.Data;
using diplom.Models;
using diplom.Services;
using System.Text;
using System.Security.Cryptography;
using NuGet.Common;
using System.Net;

namespace diplom.Controllers
{
    public class UsersController : Controller
    {
        private readonly diplomContext _context;
        private readonly MailService _mailService;

        public UsersController(diplomContext context, MailService mailService)
        {
            _context = context;
            _mailService = mailService;
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
            int? userSessionID = HttpContext.Session.GetInt32("UserID");
            User currentUser = await _context.User.FindAsync(userSessionID);
            if (currentUser != null)
                return View(currentUser);
            else
                return NotFound();
        }

        //Восстановление пароля
        public IActionResult PasswordRecovery(string token)
        {
            ViewBag.token = token;
            bool success = true;
            string message;
            User recoveryUser = _context.User.FirstOrDefault(u => u.PassRecoveryToken == token);
            if (recoveryUser == null)
            {
                message = "Ошибка восстановления пароля.";
                success = false;
                return Redirect($"PasswordRecoveryMessage?message={message}&success={success}");
            }
            else if ((DateTime.Now - recoveryUser.PassRecoveryTokenDate).Value.Minutes >= 30)
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
                .FirstOrDefaultAsync(m => m.Email == user.Email && m.HashPass == HashPassword(user.HashPass));
            if (logUser == null)
            {
                return Redirect("/Users/AuthError?error=wrongemail");
            }
            else
            {
                HttpContext.Session.SetInt32("UserID", logUser.ID);
                return Redirect("/Projects/AllProjects");
            }
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
                    recoveryUser.PassRecoveryToken = GenerateToken();
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
            User recoveryUser = _context.User.FirstOrDefault(u => u.PassRecoveryToken == token);

            if (recoveryUser == null || (DateTime.Now - recoveryUser.PassRecoveryTokenDate).Value.Minutes >= 30)
            {
                message = "Ошибка восстановления пароля.";
                success = false;
                return Redirect($"PasswordRecoveryMessage?message={message}&success={success}");
            }


            recoveryUser.HashPass = HashPassword(user.HashPass);
            HashPassRepeat = HashPassword(HashPassRepeat);

            if (recoveryUser.HashPass != HashPassRepeat)
            {

                return Redirect($"/Users/PasswordRecoveryError?error=wrongpass&token={token}");
            }

            recoveryUser.PassRecoveryToken = null;
            recoveryUser.PassRecoveryTokenDate = null;

            await _context.SaveChangesAsync();

            message = "Пароль успешно восстановлен.";
            success = true;

            string successMessage = WebUtility.UrlEncode(message);
            return Redirect($"PasswordRecoveryMessage?message={successMessage}&success={success}");
        }

        //Хеширование пароля
        static string HashPassword(string password)
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
        // Регистрация пользователя
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ID,FName,LName,Email,HashPass")] User user)
        {
            ////_context.User.RemoveRange(_context.User);
            ////await _context.SaveChangesAsync();

            var existingUser = await _context.User.FirstOrDefaultAsync(u => u.Email == user.Email);

            if (existingUser != null)
            {
                return Redirect("/Users/RegError?view=Registration");
            }

            user.HashPass = HashPassword(user.HashPass);
            user.IsActive = false;
            _context.Add(user);

            HttpContext.Session.Remove("UserID");

            try
            {
                user.RegToken = GenerateToken();
                user.RegTokenDate = DateTime.Now;

                string templatePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "emailMessages", "аctivation.html");
                string emailTemplate = System.IO.File.ReadAllText(templatePath);
                string activationLink = $"https://localhost:7297/Users/ActivationUser?token={user.RegToken}";
                string emailBody = emailTemplate.Replace("{activationLink}", activationLink);
                emailBody = emailBody.Replace("{FName}", user.FName);
                emailBody = emailBody.Replace("{LName}", user.LName);

                _mailService.SendEmail(emailBody, "Активация аккаунта", user.Email);
            }
            catch (Exception e)
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
            User existingUser = _context.User.FirstOrDefault(u => u.RegToken == token);
            if (existingUser == null)
            {
                ViewBag.message = "Ошибка активации аккаунта.";
                ViewBag.success = false;
            }
            else if ((DateTime.Now - existingUser.RegTokenDate).Value.Minutes >= 30)
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
            int? userSessionID = HttpContext.Session.GetInt32("UserID");
            var currentUser = _context.User.FirstOrDefault(x => x.ID == userSessionID);
            currentUser.LName = user.LName;
            currentUser.FName = user.FName;

            await _context.SaveChangesAsync();
            return Redirect("UserProfile");
        }
        // Изменение email пользователя
        //[HttpPost]
        //public async Task<IActionResult> UpdateProfileEmail([Bind("ID,Email")] User user)
        //{
        //    int? userSessionID = HttpContext.Session.GetInt32("UserID");
        //    var currentUser = _context.User.FirstOrDefault(x => x.ID == userSessionID);
        //    if ((_context.User.FirstOrDefault(x => x.Email == user.Email) == null))
        //    {
        //        currentUser.Email = user.Email;
        //    }
        //    else if (currentUser.Email == user.Email)
        //    {
        //        return Redirect("UserProfile");
        //    }
        //    else
        //    {
        //        return Redirect("/Users/RegError?view=UserProfile");
        //    }
        //    await _context.SaveChangesAsync();
        //    return Redirect("UserProfile");
        //}

        // Изменение пароля пользователя
        [HttpPost]
        public async Task<IActionResult> UpdateProfilePassword([Bind("ID,HashPass")] User user)
        {
            int? userSessionID = HttpContext.Session.GetInt32("UserID");
            var currentUser = _context.User.FirstOrDefault(x => x.ID == userSessionID);

            currentUser.HashPass = HashPassword(user.HashPass);
            await _context.SaveChangesAsync();
            return Redirect("UserProfile");
        }
        // Генерация токена
        public string GenerateToken()
        {
            Random rnd = new Random();
            string token = "";
            for (int i=0; i<16; i++)
            {
                token += 'a' + rnd.Next(0, 28);
            }
            return token;
        }
        // Ошибка при регистрации на существующий в системе email
        public IActionResult RegError(string view)
        {
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
