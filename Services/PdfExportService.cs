using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using WebstackInfrar.ViewModels;

namespace WebstackInfrar.Services
{
    public interface IPdfExportService
    {
        byte[] GenerateWorkLogReport(WorkLogFilterViewModel model, string reportTitle);
    }

    public class PdfExportService : IPdfExportService
    {
        public byte[] GenerateWorkLogReport(WorkLogFilterViewModel model, string reportTitle)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.DefaultTextStyle(x => x.FontSize(11));

                    page.Header().Column(col =>
                    {
                        col.Item().Text("Webstack Infrar").FontSize(22).Bold().FontColor("#6C3FC5");
                        col.Item().Text(reportTitle).FontSize(14).FontColor("#444444");
                        col.Item().Text($"Generated: {DateTime.Now:dd MMM yyyy, hh:mm tt}").FontSize(9).FontColor("#888888");
                        col.Item().PaddingTop(5).LineHorizontal(1).LineColor("#6C3FC5");
                    });

                    page.Content().PaddingVertical(10).Column(col =>
                    {
                        if (!model.Logs.Any())
                        {
                            col.Item().Text("No records found for this period.").FontColor("#888888").Italic();
                            return;
                        }

                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(cols =>
                            {
                                cols.ConstantColumn(30);
                                cols.RelativeColumn(2);
                                cols.RelativeColumn(1.5f);
                                cols.RelativeColumn(2);
                                cols.RelativeColumn(2);
                                cols.RelativeColumn(1);
                            });

                            static IContainer HeaderCell(IContainer c) =>
                                c.Background("#6C3FC5").Padding(5);

                            table.Header(header =>
                            {
                                header.Cell().Element(HeaderCell).Text("#").Bold().FontColor(Colors.White);
                                header.Cell().Element(HeaderCell).Text("Employee").Bold().FontColor(Colors.White);
                                header.Cell().Element(HeaderCell).Text("Designation").Bold().FontColor(Colors.White);
                                header.Cell().Element(HeaderCell).Text("Clock In").Bold().FontColor(Colors.White);
                                header.Cell().Element(HeaderCell).Text("Clock Out").Bold().FontColor(Colors.White);
                                header.Cell().Element(HeaderCell).Text("Duration").Bold().FontColor(Colors.White);
                            });

                            int i = 1;
                            foreach (var log in model.Logs)
                            {
                                string bg = i % 2 == 0 ? "#F3EEFF" : "#FFFFFF";

                                table.Cell().Background(bg).Padding(5).Text(i.ToString());
                                table.Cell().Background(bg).Padding(5).Text(log.EmployeeName);
                                table.Cell().Background(bg).Padding(5).Text(log.Designation);
                                table.Cell().Background(bg).Padding(5).Text(log.ClockIn.ToString("dd/MM/yy hh:mm tt"));
                                table.Cell().Background(bg).Padding(5).Text(log.ClockOut.HasValue
                                    ? log.ClockOut.Value.ToString("dd/MM/yy hh:mm tt") : "-");
                                table.Cell().Background(bg).Padding(5).Text(log.Duration.HasValue
                                    ? $"{(int)log.Duration.Value.TotalHours}h {log.Duration.Value.Minutes}m" : "-");
                                i++;
                            }
                        });

                        col.Item().PaddingTop(15).Text($"Total Records: {model.Logs.Count}").Bold().FontSize(11).FontColor("#6C3FC5");
                    });

                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.Span("Page ").FontSize(9).FontColor("#888888");
                        x.CurrentPageNumber().FontSize(9).FontColor("#888888");
                        x.Span(" of ").FontSize(9).FontColor("#888888");
                        x.TotalPages().FontSize(9).FontColor("#888888");
                    });
                });
            });

            return document.GeneratePdf();
        }
    }
}