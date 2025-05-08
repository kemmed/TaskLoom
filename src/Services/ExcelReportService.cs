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

namespace diplom.Services
{
    public class ExcelReportService
    {
        public byte[] CreateExcelLog(List<Log> logs, string projectName)
        {
            using var workbook = new ClosedXML.Excel.XLWorkbook();
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

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }
    }
}
