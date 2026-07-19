using ClosedXML.Excel;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace AutoPartShop.Api.Services;

/// <summary>How a report column is rendered in exports (number format, alignment).</summary>
public enum ReportColumnFormat
{
    Text,
    Integer,
    Money,
    /// <summary>Percentage expressed 0–100 (not 0–1).</summary>
    Percent,
    Date,
    DateTime
}

/// <summary>One column of a tabular report export: header text, value selector, render format.</summary>
public sealed record ReportColumn<T>(string Header, Func<T, object?> Value, ReportColumnFormat Format = ReportColumnFormat.Text);

/// <summary>Title block stamped on every export (worksheet header / PDF page header).</summary>
/// <param name="Title">Report display name, e.g. "Sales by Product".</param>
/// <param name="FilterSummary">Human-readable filter line, e.g. "01 Jun 2026 – 30 Jun 2026".</param>
public sealed record ReportExportMeta(string Title, string? FilterSummary);

public interface IReportExportService
{
    byte[] ToXlsx<T>(ReportExportMeta meta, IReadOnlyList<T> rows, IReadOnlyList<ReportColumn<T>> columns);
    byte[] ToPdf<T>(ReportExportMeta meta, IReadOnlyList<T> rows, IReadOnlyList<ReportColumn<T>> columns);
}

/// <summary>
/// Renders any report's rows + column map to a downloadable file, so each report endpoint
/// needs only a ReportColumnMaps entry rather than bespoke export code. Excel via ClosedXML,
/// PDF via QuestPDF (license configured once in Program.cs). Amounts are printed as plain
/// numbers (base currency, BDT) — no currency symbol, matching the on-screen tables.
/// </summary>
public class ReportExportService : IReportExportService
{
    public byte[] ToXlsx<T>(ReportExportMeta meta, IReadOnlyList<T> rows, IReadOnlyList<ReportColumn<T>> columns)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add(ToSheetName(meta.Title));

        sheet.Cell(1, 1).Value = meta.Title;
        sheet.Cell(1, 1).Style.Font.SetBold().Font.SetFontSize(14);

        var headerRow = 2;
        if (!string.IsNullOrWhiteSpace(meta.FilterSummary))
        {
            sheet.Cell(2, 1).Value = meta.FilterSummary;
            sheet.Cell(2, 1).Style.Font.SetFontColor(XLColor.Gray);
            headerRow = 4;
        }

        for (var c = 0; c < columns.Count; c++)
        {
            var cell = sheet.Cell(headerRow, c + 1);
            cell.Value = columns[c].Header;
            cell.Style.Font.SetBold();
            cell.Style.Fill.SetBackgroundColor(XLColor.FromHtml("#e5e7eb"));
        }

        for (var r = 0; r < rows.Count; r++)
        {
            for (var c = 0; c < columns.Count; c++)
            {
                var cell = sheet.Cell(headerRow + 1 + r, c + 1);
                SetCellValue(cell, columns[c].Value(rows[r]), columns[c].Format);
            }
        }

        sheet.SheetView.FreezeRows(headerRow);
        sheet.RangeUsed()?.SetAutoFilter();
        sheet.Columns(1, columns.Count).AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public byte[] ToPdf<T>(ReportExportMeta meta, IReadOnlyList<T> rows, IReadOnlyList<ReportColumn<T>> columns)
    {
        // Brand accent, aligned with the POS document design system (design_handoff_pos_documents).
        const string headerBg = "#B0392E";
        const string gray200 = "#e7e5e4";
        const string gray500 = "#78716c";

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.MarginHorizontal(28);
                page.MarginVertical(26);
                page.DefaultTextStyle(x => x.FontSize(8));

                page.Header().Column(col =>
                {
                    col.Item().Text(meta.Title).Bold().FontSize(14).FontColor(headerBg);
                    if (!string.IsNullOrWhiteSpace(meta.FilterSummary))
                        col.Item().PaddingTop(2).Text(meta.FilterSummary).FontSize(8).FontColor(gray500);
                    col.Item().PaddingTop(6).LineHorizontal(1f).LineColor(headerBg);
                });

                page.Content().PaddingTop(10).Table(table =>
                {
                    table.ColumnsDefinition(def =>
                    {
                        foreach (var _ in columns)
                            def.RelativeColumn();
                    });

                    table.Header(header =>
                    {
                        foreach (var column in columns)
                        {
                            var cell = header.Cell().Background(headerBg).Padding(4);
                            (IsNumeric(column.Format) ? cell.AlignRight() : cell)
                                .Text(column.Header).Bold().FontColor("#ffffff");
                        }
                    });

                    var alt = false;
                    foreach (var row in rows)
                    {
                        var bg = alt ? "#f9fafb" : "#ffffff";
                        alt = !alt;
                        foreach (var column in columns)
                        {
                            var cell = table.Cell().Background(bg)
                                .BorderBottom(0.5f).BorderColor(gray200).Padding(4);
                            (IsNumeric(column.Format) ? cell.AlignRight() : cell)
                                .Text(FormatValue(column.Value(row), column.Format));
                        }
                    }
                });

                page.Footer().Row(row =>
                {
                    row.RelativeItem()
                        .Text($"Generated {DateTime.UtcNow:dd MMM yyyy, HH:mm} UTC")
                        .FontSize(7).FontColor(gray500);
                    row.ConstantItem(70).AlignRight().Text(txt =>
                    {
                        txt.Span("Page ").FontSize(7).FontColor(gray500);
                        txt.CurrentPageNumber().Format(n => (n ?? 0).ToString()).FontSize(7);
                        txt.Span(" / ").FontSize(7).FontColor(gray500);
                        txt.TotalPages().Format(n => (n ?? 0).ToString()).FontSize(7);
                    });
                });
            });
        });

        return document.GeneratePdf();
    }

    private static void SetCellValue(IXLCell cell, object? value, ReportColumnFormat format)
    {
        if (value is null)
        {
            cell.Value = Blank.Value;
            return;
        }

        switch (format)
        {
            case ReportColumnFormat.Integer:
                cell.Value = Convert.ToDouble(value);
                cell.Style.NumberFormat.SetFormat("#,##0");
                break;
            case ReportColumnFormat.Money:
                cell.Value = Convert.ToDouble(value);
                cell.Style.NumberFormat.SetFormat("#,##0.00");
                break;
            case ReportColumnFormat.Percent:
                cell.Value = Convert.ToDouble(value);
                cell.Style.NumberFormat.SetFormat("0.00\"%\"");
                break;
            case ReportColumnFormat.Date:
                cell.Value = (DateTime)value;
                cell.Style.DateFormat.SetFormat("dd mmm yyyy");
                break;
            case ReportColumnFormat.DateTime:
                cell.Value = (DateTime)value;
                cell.Style.DateFormat.SetFormat("dd mmm yyyy hh:mm");
                break;
            default:
                cell.Value = value.ToString();
                break;
        }
    }

    private static string FormatValue(object? value, ReportColumnFormat format)
    {
        if (value is null) return "";
        return format switch
        {
            ReportColumnFormat.Integer => Convert.ToDecimal(value).ToString("N0"),
            ReportColumnFormat.Money => Convert.ToDecimal(value).ToString("N2"),
            ReportColumnFormat.Percent => $"{Convert.ToDecimal(value):N2}%",
            ReportColumnFormat.Date => ((DateTime)value).ToString("dd MMM yyyy"),
            ReportColumnFormat.DateTime => ((DateTime)value).ToString("dd MMM yyyy HH:mm"),
            _ => value.ToString() ?? ""
        };
    }

    private static bool IsNumeric(ReportColumnFormat format)
        => format is ReportColumnFormat.Integer or ReportColumnFormat.Money or ReportColumnFormat.Percent;

    /// <summary>Excel worksheet names: max 31 chars, no \ / ? * [ ] :</summary>
    private static string ToSheetName(string title)
    {
        var cleaned = new string(title.Where(ch => ch is not ('\\' or '/' or '?' or '*' or '[' or ']' or ':')).ToArray());
        if (string.IsNullOrWhiteSpace(cleaned)) cleaned = "Report";
        return cleaned.Length <= 31 ? cleaned : cleaned[..31];
    }
}
