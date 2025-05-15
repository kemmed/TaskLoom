using System.Net.Mail;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using taskloom.Models;
using taskloom.Data;
using Microsoft.CodeAnalysis;
using DocumentFormat.OpenXml.Spreadsheet;
using Project = taskloom.Models.Project;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace taskloom.Services
{
    public class SortUsersService
    {
        private readonly taskloomContext _context;

        public SortUsersService(taskloomContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Возвращает отсортированный список ответственных пользователей за проект.
        /// Сортировка осуществляется по количеству задач и ближайшему сроку выполнения задач.
        /// </summary>
        /// <param name="projectID">Идентификатор проекта.</param>
        /// <returns>Список, содержащий информацию о пользователях и их задачах.</returns>
        public List<SelectListItem> GetSortedResponsibilities(int projectID)
        {
            return _context.UserProject
                .Where(up => up.ProjectID == projectID && up.IsActive)
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

        /// <summary>
        /// Возвращает правильное склонение слова "задача" в зависимости от их количества.
        /// </summary>
        /// <param name="count">Количество задач.</param>
        /// <returns>Строка с правильным склонением слова "задача".</returns>
        public string GetTaskWord(int count)
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
    }
}
