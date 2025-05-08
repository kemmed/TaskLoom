using System.Net.Mail;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using diplom.Models;
using diplom.Data;
using Microsoft.CodeAnalysis;
using DocumentFormat.OpenXml.Spreadsheet;
using Project = diplom.Models.Project;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace diplom.Services
{
    public class SortUsersService
    {
        private readonly diplomContext _context;

        public SortUsersService(diplomContext context)
        {
            this._context = context;
        }
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
