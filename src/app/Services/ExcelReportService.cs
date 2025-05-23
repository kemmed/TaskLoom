﻿using System.Net.Mail;
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
    public class ExcelReportService
    {
        /// <summary>
        /// Создает Excel-файл с логами проекта.
        /// </summary>
        /// <param name="logs">Список логов для записи в Excel.</param>
        /// <param name="projectName">Название проекта, используемое в названии листа.</param>
        /// <returns>Массив байтов, представляющий созданный Excel-файл.</returns>
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
