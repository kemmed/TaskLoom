using System.Net.Mail;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using taskloom.Models;
using taskloom.Data;

namespace taskloom.Services
{
    public class LogService
    {
        private readonly taskloomContext _context;

        public LogService(taskloomContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Записывает и сохраняет действие пользователя в лог проекта.
        /// </summary>
        /// <param name="projectID">Идентификатор проекта, к которому относится действие.</param>
        /// <param name="action">Описание действия, которое было выполнено.</param>
        /// <param name="userID">Идентификатор пользователя, выполнившего действие. Если не указан, используется значение null.</param>
        public virtual void LogAction(int projectID, string action, int? userID = null)
        {
            User? user = userID.HasValue ? _context.User.FirstOrDefault(u => u.ID == userID.Value) : null;
            string userName = user != null ? $"{user.FName} {user.LName}" : " ";

            Log log = new Log
            {
                DateTime = DateTime.Now,
                Event = $"Пользователь {userName} {action}.",
                ProjectID = projectID
            };

            _context.Log.Add(log);
            _context.SaveChanges();
        }
    }
}
