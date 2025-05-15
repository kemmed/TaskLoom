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

namespace taskloom.Services
{
    public class ChartService
    {
        private readonly taskloomContext _context;

        public ChartService(taskloomContext context)
        {
            this._context = context;
        }

        /// <summary>
        /// Получает данные для графиков создания и завершения задач.
        /// </summary>
        /// <param name="issues">Список задач для графика.</param>
        /// <param name="startDate">Начальная дата для фильтрации задач. Если не указана, используется начало текущей недели.</param>
        /// <param name="endDate">Конечная дата для фильтрации задач. Если не указана, используется конец текущей недели.</param>
        /// <param name="selectedCategories">Массив выбранных категорий задач для фильтрации. Если пустой, фильтрация по категориям не применяется.</param>
        /// <returns>Строковое представление данных для графика созранных и выполненных задач.</returns>
        public (string chartDataCreate, string chartDataDone) GetChartData(List<Models.Issue> issues, string? startDate, string? endDate, int[]? selectedCategories)
        {
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
            {
                issues = issues
                    .Where(i => i.CategoryTypeID.HasValue && selectedCategories.Contains(i.CategoryTypeID.Value))
                    .ToList();
            }

            var groupedIssuesCreate = issues
                .GroupBy(i => i.CreateDate.Date)
                .OrderBy(g => g.Key)
                .ToDictionary(g => g.Key, g => g.Count());

            var groupedIssuesDone = issues
                .Where(i => i.EndDate.HasValue)
                .GroupBy(i => i.EndDate.Value.Date)
                .OrderBy(g => g.Key)
                .ToDictionary(g => g.Key, g => g.Count());

            string chartDataCreate = "";
            string chartDataDone = "";

            for (DateTime date = start; date <= end; date = date.AddDays(1))
            {
                chartDataDone += "{";
                chartDataDone += $"x: '{date.Date.ToString("dd.MM.yy")}', y: {(groupedIssuesDone.ContainsKey(date) ? groupedIssuesDone[date] : 0)}";
                chartDataDone += "}, ";

                chartDataCreate += "{";
                chartDataCreate += $"x: '{date.Date.ToString("dd.MM.yy")}', y: {(groupedIssuesCreate.ContainsKey(date) ? groupedIssuesCreate[date] : 0)}";
                chartDataCreate += "}, ";
            }

            return (chartDataCreate, chartDataDone);
        }
    }
}
