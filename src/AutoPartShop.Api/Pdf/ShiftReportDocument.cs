using AutoPartShop.Api.Pdf.Components;
using AutoPartShop.Api.Pdf.Design;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace AutoPartShop.Api.Pdf;

public record ShiftMethodCount(string Method, int Count);

public record ShiftReportDocumentData(
    string ReportNumber,
    DateTime ReportDate,
    string ShiftLabel,
    string ShiftHours,
    string TerminalLabel,
    string CashierName,
    DateTime SignedIn,
    DateTime? SignedOut,
    int ReceiptCount,
    int ReturnCount,
    int VoidCount,
    List<ShiftMethodCount> MethodCounts,
    decimal OpeningFloat,
    decimal CashSales,
    decimal CashRefunds,
    decimal CashDrops,
    decimal ExpectedInDrawer,
    decimal CountedAtClose,
    decimal OverShort,
    string Note);

/// <summary>
/// Shift Report — document 12 of design_handoff_pos_documents. Per-cashier till reconciliation:
/// header + Cashier/Transactions two-column + Cash Drawer Reconciliation table + closing note +
/// signature row. Cash-only reconciliation — non-cash methods are counted for the transaction
/// summary but excluded from the drawer math, matching the handoff's own closing note that non-cash
/// settlements reconcile against acquirer reports separately.
/// </summary>
public class ShiftReportDocument : IDocument
{
    private readonly ShiftReportDocumentData _data;
    private readonly ShopProfile _shop;
    private readonly DocTheme _theme;

    public ShiftReportDocument(ShiftReportDocumentData data, ShopProfile shop, DocTheme? theme = null)
    {
        _data = data;
        _shop = shop;
        _theme = (theme ?? DocTheme.Default) with { CurrencySymbol = shop.CurrencySymbol };
    }

    public DocumentMetadata GetMetadata() => new()
    {
        Title = $"Shift Report {_data.ReportNumber}",
        Author = _shop.Name,
        Subject = $"Shift report for {_data.CashierName}",
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
        // "Shift" and its hours are split into two short fields rather than one compound string
        // ("Shift A · 09:00 – 17:00") — that wraps to two lines in the meta column at this width.
        new DocHeader(_theme, _shop, "Shift Report",
        [
            new MetaField("No.", _data.ReportNumber),
            new MetaField("Date", _data.ReportDate.ToString("dd MMM yyyy")),
            new MetaField("Shift", _data.ShiftLabel),
            new MetaField("Hours", _data.ShiftHours),
            new MetaField("Terminal", _data.TerminalLabel),
        ]).Compose(container);

    private void ComposeContent(IContainer container)
    {
        container.PaddingTop(DocTheme.Px(18)).Column(col =>
        {
            col.Item().Row(row =>
            {
                row.RelativeItem().Element(ComposeCashier);
                row.ConstantItem(DocTheme.Px(24));
                row.RelativeItem().Element(ComposeTransactions);
            });

            col.Item().PaddingTop(DocTheme.Px(18)).Element(ComposeReconciliation);
            col.Item().PaddingTop(DocTheme.Px(20)).ShowEntire().Element(ComposeNote);

            col.Item().ShowEntire().Element(c =>
                new SignRow("Cashier", "Head Cashier", "Manager").Compose(c));
        });
    }

    private void ComposeCashier(IContainer container)
    {
        container.Column(col =>
        {
            col.Item().Element(c => SectionLabel(c, "Cashier"));

            col.Item().PaddingTop(DocTheme.Px(6)).Text(_data.CashierName)
                .FontSize(DocTheme.TableCell).Bold().FontColor(DocTheme.Ink).LineHeight(1.6f);

            var signOff = _data.SignedOut is { } so
                ? $"Signed in {_data.SignedIn:HH:mm} · Signed out {so:HH:mm}"
                : $"Signed in {_data.SignedIn:HH:mm} · Still open";

            col.Item().Text(signOff)
                .FontSize(DocTheme.TableCell).FontColor(DocTheme.Ink).LineHeight(1.6f);
        });
    }

    private void ComposeTransactions(IContainer container)
    {
        container.Column(col =>
        {
            col.Item().Element(c => SectionLabel(c, "Transactions"));

            var summary = $"{_data.ReceiptCount} receipt{(_data.ReceiptCount == 1 ? "" : "s")} · " +
                          $"{_data.ReturnCount} return{(_data.ReturnCount == 1 ? "" : "s")} · " +
                          $"{_data.VoidCount} void{(_data.VoidCount == 1 ? "" : "s")}";

            col.Item().PaddingTop(DocTheme.Px(6)).Text(summary)
                .FontSize(DocTheme.TableCell).FontColor(DocTheme.Ink).LineHeight(1.6f);

            var methods = _data.MethodCounts.Count > 0
                ? string.Join(" · ", _data.MethodCounts.Select(m => $"{m.Method} {m.Count}"))
                : "—";

            col.Item().Text(methods)
                .FontSize(DocTheme.TableCell).FontColor(DocTheme.Ink).LineHeight(1.6f);
        });
    }

    private void ComposeReconciliation(IContainer container)
    {
        container.Column(col =>
        {
            col.Item().PaddingBottom(DocTheme.Px(6)).Element(c => SectionLabel(c, "Cash Drawer Reconciliation"));

            col.Item().Table(table =>
            {
                table.ColumnsDefinition(c =>
                {
                    c.RelativeColumn();
                    c.ConstantColumn(DocTheme.Px(150));
                });

                Row(table, "Opening Float", DocTheme.Amount(_data.OpeningFloat));
                Row(table, "Cash Sales", $"+ {DocTheme.Amount(_data.CashSales)}");
                if (_data.CashRefunds > 0)
                    Row(table, "Cash Refunds", $"- {DocTheme.Amount(_data.CashRefunds)}");
                if (_data.CashDrops > 0)
                    Row(table, "Cash Drops to Safe", $"- {DocTheme.Amount(_data.CashDrops)}");
                Row(table, "Expected in Drawer", DocTheme.Amount(_data.ExpectedInDrawer));
                Row(table, "Counted at Close", DocTheme.Amount(_data.CountedAtClose));

                // Over/Short: bold, 2px top rule, accent-colored (the handoff shows the shortage in
                // the brand accent regardless of sign — it's a flag-for-attention color here, not a
                // red/green semantic).
                var label = table.Cell()
                    .BorderTop(DocTheme.RuleMedium).BorderColor(DocTheme.Ink)
                    .PaddingVertical(DocTheme.Px(9)).PaddingHorizontal(DocTheme.Px(8));
                label.Text("Over / Short").FontSize(DocTheme.TableCell).Bold().FontColor(DocTheme.Ink);

                var value = table.Cell()
                    .BorderTop(DocTheme.RuleMedium).BorderColor(DocTheme.Ink)
                    .PaddingVertical(DocTheme.Px(9)).PaddingHorizontal(DocTheme.Px(8)).AlignRight();
                var sign = _data.OverShort < 0 ? "- " : _data.OverShort > 0 ? "+ " : "";
                value.Text($"{sign}{DocTheme.Amount(Math.Abs(_data.OverShort))}")
                    .Style(DocTheme.MonoText).FontSize(DocTheme.TableCell).Bold().FontColor(_theme.Accent);
            });
        });
    }

    private static void Row(TableDescriptor table, string label, string value)
    {
        table.Cell()
            .BorderBottom(DocTheme.RuleHairline).BorderColor(DocTheme.Hairline)
            .PaddingVertical(DocTheme.Px(7)).PaddingHorizontal(DocTheme.Px(8))
            .Text(label).FontSize(DocTheme.TableCell).FontColor(DocTheme.Muted);

        table.Cell()
            .BorderBottom(DocTheme.RuleHairline).BorderColor(DocTheme.Hairline)
            .PaddingVertical(DocTheme.Px(7)).PaddingHorizontal(DocTheme.Px(8)).AlignRight()
            .Text(value).Style(DocTheme.MonoText).FontSize(DocTheme.TableCell).FontColor(DocTheme.Ink);
    }

    private void ComposeNote(IContainer container)
    {
        var note = !string.IsNullOrWhiteSpace(_data.Note)
            ? _data.Note
            : _data.OverShort == 0
                ? "Drawer balanced. Non-cash settlements reconcile with acquirer reports."
                : $"{(_data.OverShort < 0 ? "Shortage" : "Overage")} of {_theme.Money(Math.Abs(_data.OverShort))} noted and logged. Non-cash settlements reconcile with acquirer reports.";

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

    private static void SectionLabel(IContainer c, string text) =>
        c.Text(text.ToUpperInvariant())
            .FontSize(DocTheme.SectionLabel).SemiBold().FontColor(DocTheme.Label)
            .LetterSpacing(1.2f / DocTheme.SectionLabel);
}
