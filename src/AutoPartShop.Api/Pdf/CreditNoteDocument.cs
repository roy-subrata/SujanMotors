using AutoPartShop.Api.Pdf.Components;
using AutoPartShop.Api.Pdf.Design;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace AutoPartShop.Api.Pdf;

public record CreditNoteLine(
    int SlNo,
    string PartNumber,
    string DisplayName,
    string? LocalName,
    int Quantity,
    string UnitSymbol,
    decimal UnitPrice,
    decimal LineTotal);

public record CreditNoteDocumentData(
    string CreditNoteNumber,
    DateTime IssueDate,
    string RefInvoiceNumber,
    string CustomerName,
    string CustomerAddress,
    string CustomerPhone,
    string Reason,
    List<CreditNoteLine> Lines,
    decimal TotalCredit,
    string Notes,
    /// <summary>
    /// When true, renders as a Debit Note — the handoff's own copy says "Debit Notes use the same
    /// layout" as the Credit Note, so this flips the title, section label, and totals label rather
    /// than duplicating the class. Defaults to false so existing Credit Note callers are unaffected.
    /// </summary>
    bool IsDebit = false);

/// <summary>
/// Credit Note / Debit Note — document 7 of design_handoff_pos_documents (both directions share
/// this one layout, per the handoff's own note). Customer-facing.
///
/// The credit direction is backed by <c>CustomerCreditNote</c>, not the <c>CreditNote</c> entity —
/// the latter is a *supplier* credit (credit we receive against a purchase return), which runs in
/// the opposite direction and is not this document. The debit direction is backed by
/// <c>CustomerDebitNote</c>.
/// </summary>
public class CreditNoteDocument : IDocument
{
    private readonly CreditNoteDocumentData _data;
    private readonly ShopProfile _shop;
    private readonly DocTheme _theme;

    public CreditNoteDocument(CreditNoteDocumentData data, ShopProfile shop, DocTheme? theme = null)
    {
        _data = data;
        _shop = shop;
        _theme = (theme ?? DocTheme.Default) with { CurrencySymbol = shop.CurrencySymbol };
    }

    private string DocTitle => _data.IsDebit ? _theme.T("creditNote.debitTitle") : _theme.T("creditNote.title");

    public DocumentMetadata GetMetadata() => new()
    {
        Title = $"{DocTitle} {_data.CreditNoteNumber}",
        Author = _shop.Name,
        Subject = $"{DocTitle} for {_data.CustomerName}",
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
        new DocHeader(_theme, _shop, DocTitle,
        [
            new MetaField(_theme.T("common.no"), _data.CreditNoteNumber),
            new MetaField(_theme.T("common.date"), _data.IssueDate.ToString("dd MMM yyyy")),
            new MetaField(_theme.T("common.refInvoice"), _data.RefInvoiceNumber),
        ]).Compose(container);

    private void ComposeContent(IContainer container)
    {
        container.PaddingTop(DocTheme.Px(18)).Column(col =>
        {
            col.Item().Row(row =>
            {
                row.RelativeItem().Element(ComposeIssuedTo);
                row.ConstantItem(DocTheme.Px(24));
                row.RelativeItem().Element(ComposeReason);
            });

            col.Item().PaddingTop(DocTheme.Px(22)).Element(ComposeItems);
            col.Item().PaddingTop(DocTheme.Px(20)).ShowEntire().Element(ComposeNote);

            col.Item().ShowEntire().Element(c =>
                new SignRow(_theme.T("common.preparedBy"), _theme.T("common.customerAcknowledgement"), _theme.T("common.authorizedSignatory")).Compose(c));
        });
    }

    private void ComposeIssuedTo(IContainer container)
    {
        container.Column(col =>
        {
            col.Item().Element(c => SectionLabel(c, _theme.T("creditNote.issuedTo")));

            col.Item().PaddingTop(DocTheme.Px(6)).Text(_data.CustomerName)
                .FontSize(DocTheme.Px(13)).SemiBold().FontColor(DocTheme.Ink);

            var lines = new List<string>();
            if (!string.IsNullOrWhiteSpace(_data.CustomerAddress)) lines.Add(_data.CustomerAddress);
            if (!string.IsNullOrWhiteSpace(_data.CustomerPhone)) lines.Add(_data.CustomerPhone);

            if (lines.Count > 0)
                col.Item().PaddingTop(DocTheme.Px(4)).Column(c =>
                {
                    foreach (var line in lines)
                        c.Item().Text(line)
                            .FontSize(DocTheme.Body).FontColor(DocTheme.Secondary).LineHeight(1.55f);
                });
        });
    }

    private void ComposeReason(IContainer container)
    {
        container.Column(col =>
        {
            col.Item().Element(c => SectionLabel(c, _data.IsDebit ? _theme.T("creditNote.reasonForDebit") : _theme.T("creditNote.reasonForCredit")));
            col.Item().PaddingTop(DocTheme.Px(6)).Text(
                    string.IsNullOrWhiteSpace(_data.Reason) ? "—" : _data.Reason)
                .FontSize(DocTheme.TableCell).FontColor(DocTheme.Ink).LineHeight(1.6f);
        });
    }

    private void ComposeItems(IContainer container)
    {
        var items = _data.Lines.Select(l => new ItemRow(
            Sn: l.SlNo,
            Code: l.PartNumber,
            Name: string.IsNullOrWhiteSpace(l.LocalName) ? l.DisplayName : $"{l.DisplayName}\n{l.LocalName}",
            Qty: string.IsNullOrWhiteSpace(l.UnitSymbol) ? l.Quantity.ToString() : $"{l.Quantity} {l.UnitSymbol}",
            Rate: DocTheme.Amount(l.UnitPrice),
            Amount: DocTheme.Amount(l.LineTotal))).ToList();

        // Scoped to the corrected items only, so the sole total is the credit/debit itself.
        new ItemsTable(
            _theme, items, totals: [],
            grandLabel: _data.IsDebit ? _theme.T("common.totalDebit") : _theme.T("common.totalCredit"),
            grandValue: DocTheme.Amount(_data.TotalCredit),
            words: AmountInWords.Convert(_data.TotalCredit)).Compose(container);
    }

    private void ComposeNote(IContainer container)
    {
        var note = !string.IsNullOrWhiteSpace(_data.Notes)
            ? _data.Notes
            : _data.IsDebit
                ? _theme.T("creditNote.standingNoteDebit")
                : _theme.T("creditNote.standingNoteCredit");

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
