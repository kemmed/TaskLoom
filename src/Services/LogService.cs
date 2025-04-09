using System.Net.Mail;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using diplom.Models;
using diplom.Data;

namespace diplom.Services
{
    public class LogService
    {
        private readonly diplomContext _context;

        public LogService(diplomContext context)
        {
            _context = context;
        }

        public void LogAction(int projectID, string action, int? userID = null)
        {
            var user = userID.HasValue ? _context.User.FirstOrDefault(u => u.ID == userID) : null;

            Log log = new Log
            {
                DateTime = DateTime.Now,
                Event = $"Пользователь {user.FName} {user.LName} {action}.",
                ProjectID = projectID
            };

            _context.Log.Add(log);
            _context.SaveChanges();
        }
    }
}
