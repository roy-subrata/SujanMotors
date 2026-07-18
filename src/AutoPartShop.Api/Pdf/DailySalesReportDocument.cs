using AutoPartShop.Api.Pdf.Components;
using AutoPartShop.Api.Pdf.Design;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace AutoPartShop.Api.Pdf;

public record ZReportPaymentRow(string Method, int Count, decimal Amount);
public record ZReportCategoryRow(string Category, decimal Amount);

public record DailySalesReportData(
    string ReportNumber,
    DateTime BusinessDate,
    string BusinessDayLabel,
    string TerminalLabel,
    decimal GrossSales,
    decimal ReturnsAmount,
    decimal DiscountsAmount,
    decimal NetSales,
    decimal VatCollected,
    int ReceiptCount,
    IReadOnlyList<ZReportPaymentRow> Payments,
    IReadOnlyList<ZReportCategoryRow> Categories,
    string Note);

/// <summary>
/// Daily Sales (Z) Report — document 10 of design_handoff_pos_documents.
/// KPI card row, gross→net reconciliation, By Payment Method + By Category split, closing note.
/// </summary>
public class DailySalesReportDocument : IDocument
{
    private readonly DailySalesReportData _data;
    private readonly ShopProfile _shop;
    private readonly DocTheme _theme;

    public DailySalesReportDocument(DailySalesReportData data, ShopProfile shop, DocTheme? theme = null)
    {
        _data = data;
        _shop = shop;
        _theme = (theme ?? DocTheme.Default) with { CurrencySymbol = shop.CurrencySymbol };
    }

    public DocumentMetadata GetMetadata() => new()
    {
        Title = $"Daily Sales Report {_data.ReportNumber}",
        Author = _shop.Name,
        Subject = $"Z report for {_data.BusinessDate:dd MMM yyyy}",
        CreationDate = DateTime.UtcNow
    };

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            _theme.ApplyPage(page);

            page.Header().Element(ComposeHeader);
            page.Content().Element(ComposeContent);
            page.Footer().Element(ComposeFooter);
        });
    }

    private void ComposeHeader(IContainer container) =>
        new DocHeader(_theme, _shop, "Daily Sales Report",
        [
            new MetaField("No.", _data.ReportNumber),
            new MetaField("Date", _data.BusinessDate.ToString("dd MMM yyyy")),
            new MetaField("Business Day", _data.BusinessDayLabel),
            new MetaField("Terminal", _data.TerminalLabel),
        ]).Compose(container);

    private void ComposeContent(IContainer container)
    {
        container.PaddingTop(DocTheme.Px(18)).Column(col =>
        {
            col.Item().Element(ComposeKpis);
            col.Item().PaddingTop(DocTheme.Px(18)).Element(ComposeReconciliation);

            col.Item().PaddingTop(DocTheme.Px(22)).Row(row =>
            {
                row.RelativeItem().Element(ComposePaymentMethods);
                row.ConstantItem(DocTheme.Px(24));
                row.RelativeItem().Element(ComposeCategories);
            });

            col.Item().PaddingTop(DocTheme.Px(20)).ShowEntire().Element(ComposeNote);

            col.Item().ShowEntire().Element(c =>
                new SignRow("Head Cashier", "Accountant", "Manager").Compose(c));
        });
    }

    // ── KPI cards (4-up) ───────────────────────────────────────────────────────
    private void ComposeKpis(IContainer container)
    {
        container.Row(row =>
        {
            Kpi(row, "Gross Sales", _theme.Money(_data.GrossSales));
            row.ConstantItem(DocTheme.Px(12));
            Kpi(row, "Net Sales", _theme.Money(_data.NetSales));
            row.ConstantItem(DocTheme.Px(12));
            Kpi(row, "VAT Collected", _theme.Money(_data.VatCollected));
            row.ConstantItem(DocTheme.Px(12));
            Kpi(row, "Receipts", _data.ReceiptCount.ToString());
        });
    }

    private static void Kpi(RowDescriptor row, string label, string value)
    {
        row.RelativeItem()
            .Border(DocTheme.RuleHairline).BorderColor(DocTheme.Divider)
            .PaddingVertical(DocTheme.Px(12)).PaddingHorizontal(DocTheme.Px(14))
            .Column(col =>
            {
                col.Item().Text(label.ToUpperInvariant())
                    .FontSize(DocTheme.Px(8.5f)).SemiBold().FontColor(DocTheme.Label)
                    .LetterSpacing(1.2f / 8.5f);
                col.Item().PaddingTop(DocTheme.Px(4)).Text(value)
                    .Style(DocTheme.MonoText).FontSize(DocTheme.Px(15)).Bold().FontColor(DocTheme.Ink);
            });
    }

    // ── Gross → Net reconciliation ─────────────────────────────────────────────
    private void ComposeReconciliation(IContainer container)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(c =>
            {
                c.RelativeColumn();
                c.ConstantColumn(DocTheme.Px(140));
            });

            ReconRow(table, "Gross Sales", DocTheme.Amount(_data.GrossSales), false);
            ReconRow(table, "Less: Returns (Credit Notes)", $"- {DocTheme.Amount(_data.ReturnsAmount)}", false);
            ReconRow(table, "Less: Discounts", $"- {DocTheme.Amount(_data.DiscountsAmount)}", false);
            ReconRow(table, "Net Sales", DocTheme.Amount(_data.NetSales), true);
        });
    }

    private static void ReconRow(TableDescriptor table, string label, string value, bool total)
    {
        var labelCell = table.Cell().PaddingVertical(DocTheme.Px(total ? 8 : 7)).PaddingHorizontal(DocTheme.Px(8));
        var valueCell = table.Cell().PaddingVertical(DocTheme.Px(total ? 8 : 7)).PaddingHorizontal(DocTheme.Px(8)).AlignRight();

        if (total)
        {
            labelCell = labelCell.BorderBottom(DocTheme.RuleMedium).BorderColor(DocTheme.Ink);
            valueCell = valueCell.BorderBottom(DocTheme.RuleMedium).BorderColor(DocTheme.Ink);
        }
        else
        {
            labelCell = labelCell.BorderBottom(DocTheme.RuleHairline).BorderColor(DocTheme.Hairline);
            valueCell = valueCell.BorderBottom(DocTheme.RuleHairline).BorderColor(DocTheme.Hairline);
        }

        var labelSpan = labelCell.Text(label).FontSize(DocTheme.TableCell)
            .FontColor(total ? DocTheme.Ink : DocTheme.Label);
        if (total) labelSpan.Bold();

        var valueSpan = valueCell.Text(value).Style(DocTheme.MonoText)
            .FontSize(DocTheme.TableCell).FontColor(DocTheme.Ink);
        if (total) valueSpan.Bold();
    }

    // ── By payment method ──────────────────────────────────────────────────────
    private void ComposePaymentMethods(IContainer container)
    {
        container.Column(col =>
        {
            col.Item().PaddingBottom(DocTheme.Px(6)).Element(c => SectionLabel(c, "By Payment Method"));

            col.Item().Table(table =>
            {
                table.ColumnsDefinition(c =>
                {
                    c.RelativeColumn();
                    c.ConstantColumn(DocTheme.Px(50));
                    c.ConstantColumn(DocTheme.Px(95));
                });

                table.Header(header =>
                {
                    SubHead(header.Cell(), "Method");
                    SubHead(header.Cell(), "Txns", right: true);
                    SubHead(header.Cell(), $"Amount ({_theme.CurrencySymbol})", right: true);
                });

                foreach (var p in _data.Payments)
                {
                    SubCell(table.Cell(), p.Method);
                    SubCell(table.Cell(), p.Count.ToString(), mono: true, right: true);
                    SubCell(table.Cell(), DocTheme.Amount(p.Amount), mono: true, right: true);
                }
            });
        });
    }

    // ── By category ────────────────────────────────────────────────────────────
    private void ComposeCategories(IContainer container)
    {
        container.Column(col =>
        {
            col.Item().PaddingBottom(DocTheme.Px(6)).Element(c => SectionLabel(c, "By Category"));

            col.Item().Table(table =>
            {
                table.ColumnsDefinition(c =>
                {
                    c.RelativeColumn();
                    c.ConstantColumn(DocTheme.Px(95));
                });

                table.Header(header =>
                {
                    SubHead(header.Cell(), "Category");
                    SubHead(header.Cell(), $"Amount ({_theme.CurrencySymbol})", right: true);
                });

                foreach (var cat in _data.Categories)
                {
                    SubCell(table.Cell(), cat.Category);
                    SubCell(table.Cell(), DocTheme.Amount(cat.Amount), mono: true, right: true);
                }
            });
        });
    }

    private void ComposeNote(IContainer container)
    {
        var note = !string.IsNullOrWhiteSpace(_data.Note)
            ? _data.Note
            : $"Average sale {_theme.Money(_data.ReceiptCount == 0 ? 0 : _data.NetSales / _data.ReceiptCount)} " +
              $"across {_data.ReceiptCount} receipts.";

        container.Text(note)
            .FontSize(DocTheme.Px(10)).FontColor(DocTheme.Muted).LineHeight(1.7f);
    }

    private void ComposeFooter(IContainer container)
    {
        container.PaddingTop(DocTheme.Px(10)).Row(row =>
        {
            row.RelativeItem().Text(_shop.FooterText)
                .FontSize(DocTheme.AddressSize).FontColor(DocTheme.Label);
            row.AutoItem().Text(txt =>
            {
                txt.DefaultTextStyle(DocTheme.MonoText.FontSize(DocTheme.AddressSize).FontColor(DocTheme.Label));
                txt.CurrentPageNumber();
                txt.Span(" / ");
                txt.TotalPages();
            });
        });
    }

    // ── Helpers ────────────────────────────────────────────────────────────────
    private static void SubHead(IContainer cell, string text, bool right = false)
    {
        var c = cell
            .BorderBottom(DocTheme.RuleMedium).BorderColor(DocTheme.Ink)
            .PaddingVertical(DocTheme.Px(6)).PaddingHorizontal(DocTheme.Px(8));

        if (right) c = c.AlignRight();

        c.Text(text.ToUpperInvariant())
            .FontSize(DocTheme.Px(8.5f)).SemiBold().FontColor(DocTheme.Secondary)
            .LetterSpacing(1f / 8.5f);
    }

    private static void SubCell(IContainer cell, string text, bool mono = false, bool right = false)
    {
        var c = cell
            .BorderBottom(DocTheme.RuleHairline).BorderColor(DocTheme.Hairline)
            .PaddingVertical(DocTheme.Px(6)).PaddingHorizontal(DocTheme.Px(8));

        if (right) c = c.AlignRight();

        var span = c.Text(text).FontSize(DocTheme.Px(10.5f)).FontColor(DocTheme.Ink);
        if (mono) span.Style(DocTheme.MonoText).FontSize(DocTheme.Px(10.5f)).FontColor(DocTheme.Ink);
    }

    private static void SectionLabel(IContainer c, string text) =>
        c.Text(text.ToUpperInvariant())
            .FontSize(DocTheme.SectionLabel).SemiBold().FontColor(DocTheme.Label)
            .LetterSpacing(1.2f / DocTheme.SectionLabel);
}
