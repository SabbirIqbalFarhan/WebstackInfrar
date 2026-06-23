using ClosedXML.Excel;
using WebstackInfrar.ViewModels;

namespace WebstackInfrar.Services
{
    public interface IExcelExportService
    {
        byte[] GenerateWorkLogExcel(WorkLogFilterViewModel model, string reportTitle);
    }

    public class ExcelExportService : IExcelExportService
    {
        public byte[] GenerateWorkLogExcel(WorkLogFilterViewModel model, string reportTitle)
        {
            using var workbook = new XLWorkbook();
            var sheet = workbook.Worksheets.Add("Work Logs");

            // Title
            sheet.Cell(1, 1).Value = "Webstack Infrar";
            sheet.Cell(1, 1).Style.Font.Bold = true;
            sheet.Cell(1, 1).Style.Font.FontSize = 16;
            sheet.Cell(1, 1).Style.Font.FontColor = XLColor.FromHtml("#6C3FC5");

            sheet.Cell(2, 1).Value = reportTitle;
            sheet.Cell(2, 1).Style.Font.FontSize = 12;

            sheet.Cell(3, 1).Value = $"Generated: {DateTime.Now:dd MMM yyyy, hh:mm tt}";
            sheet.Cell(3, 1).Style.Font.FontSize = 9;
            sheet.Cell(3, 1).Style.Font.FontColor = XLColor.Gray;

            // Header row
            int headerRow = 5;
            string[] headers = { "#", "Employee", "Designation", "Clock In", "Clock Out", "Duration" };
            for (int c = 0; c < headers.Length; c++)
            {
                var cell = sheet.Cell(headerRow, c + 1);
                cell.Value = headers[c];
                cell.Style.Font.Bold = true;
                cell.Style.Font.FontColor = XLColor.White;
                cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#6C3FC5");
            }

            // Data rows
            int row = headerRow + 1;
            int i = 1;
            foreach (var log in model.Logs)
            {
                sheet.Cell(row, 1).Value = i;
                sheet.Cell(row, 2).Value = log.EmployeeName;
                sheet.Cell(row, 3).Value = log.Designation;
                sheet.Cell(row, 4).Value = log.ClockIn.ToString("dd/MM/yyyy hh:mm tt");
                sheet.Cell(row, 5).Value = log.ClockOut.HasValue
                    ? log.ClockOut.Value.ToString("dd/MM/yyyy hh:mm tt") : "-";
                sheet.Cell(row, 6).Value = log.Duration.HasValue
                    ? $"{(int)log.Duration.Value.TotalHours}h {log.Duration.Value.Minutes}m" : "-";

                if (i % 2 == 0)
                {
                    for (int c = 1; c <= 6; c++)
                        sheet.Cell(row, c).Style.Fill.BackgroundColor = XLColor.FromHtml("#F3EEFF");
                }

                row++;
                i++;
            }

            // Total
            sheet.Cell(row + 1, 1).Value = $"Total Records: {model.Logs.Count}";
            sheet.Cell(row + 1, 1).Style.Font.Bold = true;
            sheet.Cell(row + 1, 1).Style.Font.FontColor = XLColor.FromHtml("#6C3FC5");

            sheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }
    }
}