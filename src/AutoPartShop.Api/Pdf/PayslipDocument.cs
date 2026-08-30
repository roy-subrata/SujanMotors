using AutoPartShop.Api.Pdf.Components;
using AutoPartShop.Api.Pdf.Design;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace AutoPartShop.Api.Pdf;

public record PayslipDocumentData(
    string EmployeeCode,
    string EmployeeName,
    string Designation,
    string Department,
    string MonthName,
    string RunCode,
    string Currency,
    decimal MonthlySalary,
    decimal OvertimeAmount,
    decimal BonusAmount,
    decimal OtherAllowance,
    decimal CommissionAmount,
    decimal GrossPay,
    decimal AbsenceDeduction,
    int AbsentDays,
    int HalfDays,
    decimal AdvanceDeduction,
    decimal TaxDeduction,
    decimal OtherDeduction,
    decimal TotalDeduction,
    decimal NetPay,
    int PresentDays,
    int LateDays,
    int LeaveDays);

/// <summary>
/// Payslip — employee-facing HR document. Mirrors the earnings/deductions content of
/// PayrollController.BuildPayslipHtml (the emailed version), rendered with the shared
/// document design system used for the POS document set.
/// </summary>
public class PayslipDocument : IDocument
{
    private readonly PayslipDocumentData _data;
    private readonly ShopProfile _shop;
    private readonly DocTheme _theme;

    public PayslipDocument(PayslipDocumentData data, ShopProfile shop, DocTheme? theme = null)
    {
        _data = data;
        _shop = shop;
        _theme = theme ?? DocTheme.Default;
    }

    public DocumentMetadata GetMetadata() => new()
    {
        Title = $"Payslip — {_data.MonthName} ({_data.EmployeeCode})",
        Author = _shop.Name,
        Subject = $"Payslip for {_data.EmployeeName}",
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
        new DocHeader(_theme, _shop, _theme.T("payslip.title"),
        [
            new MetaField(_theme.T("common.no"), _data.EmployeeCode),
            new MetaField(_theme.T("payslip.month"), _data.MonthName),
            new MetaField(_theme.T("payslip.ref"), _data.RunCode),
        ]).Compose(container);

    private void ComposeContent(IContainer container)
    {
        container.PaddingTop(DocTheme.Px(18)).Column(col =>
        {
            col.Item().Element(ComposeEmployee);
            col.Item().PaddingTop(DocTheme.Px(22)).Element(ComposeEarningsAndDeductions);
            col.Item().PaddingTop(DocTheme.Px(12)).Element(ComposeNetPayBox);
            col.Item().PaddingTop(DocTheme.Px(16)).Element(ComposeAttendanceNote);

            col.Item().ShowEntire().Element(c =>
                new SignRow(_theme.T("common.preparedBy"), _theme.T("common.employeeSignature"), _theme.T("common.authorizedSignatory")).Compose(c));
        });
    }

    private void ComposeEmployee(IContainer container)
    {
        container.Column(col =>
        {
            col.Item().Text(_theme.T("payslip.employee").ToUpperInvariant())
                .FontSize(DocTheme.SectionLabel).SemiBold().FontColor(DocTheme.Label)
                .LetterSpacing(1.2f / DocTheme.SectionLabel);

            col.Item().PaddingTop(DocTheme.Px(5)).Text(_data.EmployeeName)
                .FontSize(DocTheme.Px(13)).SemiBold().FontColor(DocTheme.Ink);

            var lines = new List<string>();
            if (!string.IsNullOrWhiteSpace(_data.Designation)) lines.Add(_data.Designation);
            if (!string.IsNullOrWhiteSpace(_data.Department)) lines.Add(_data.Department);

            if (lines.Count > 0)
                col.Item().PaddingTop(DocTheme.Px(4)).Text(string.Join("  ·  ", lines))
                    .FontSize(DocTheme.Body).FontColor(DocTheme.Secondary).LineHeight(1.55f);
        });
    }

    // ── Earnings + deductions table (label/value, matches BuildPayslipHtml's rows) ────────────
    private void ComposeEarningsAndDeductions(IContainer container)
    {
        var rows = new List<(string Label, decimal Value, bool Bold)>
        {
            (_theme.T("payslip.basicSalary"), _data.MonthlySalary, false),
        };

        if (_data.OvertimeAmount > 0) rows.Add((_theme.T("payslip.overtime"), _data.OvertimeAmount, false));
        if (_data.BonusAmount > 0) rows.Add((_theme.T("payslip.bonus"), _data.BonusAmount, false));
        if (_data.OtherAllowance > 0) rows.Add((_theme.T("payslip.allowance"), _data.OtherAllowance, false));
        if (_data.CommissionAmount > 0) rows.Add((_theme.T("payslip.salesCommission"), _data.CommissionAmount, false));
        rows.Add((_theme.T("payslip.grossPay"), _data.GrossPay, true));

        if (_data.AbsenceDeduction > 0)
            rows.Add((_theme.T("payslip.absenceDeduction")
                .Replace("{{absent}}", _data.AbsentDays.ToString())
                .Replace("{{half}}", _data.HalfDays.ToString()), -_data.AbsenceDeduction, false));
        if (_data.AdvanceDeduction > 0) rows.Add((_theme.T("payslip.salaryAdvance"), -_data.AdvanceDeduction, false));
        if (_data.TaxDeduction > 0) rows.Add((_theme.T("payslip.tax"), -_data.TaxDeduction, false));
        if (_data.OtherDeduction > 0) rows.Add((_theme.T("payslip.otherDeduction"), -_data.OtherDeduction, false));

        container.Table(table =>
        {
            table.ColumnsDefinition(c =>
            {
                c.RelativeColumn();
                c.ConstantColumn(DocTheme.Px(140));
            });

            foreach (var (label, value, bold) in rows)
            {
                var labelCell = table.Cell()
                    .BorderBottom(DocTheme.RuleHairline).BorderColor(DocTheme.Hairline).Padding(DocTheme.Px(8))
                    .Text(label).FontSize(DocTheme.TableCell).FontColor(bold ? DocTheme.Ink : DocTheme.Secondary);
                if (bold) labelCell.SemiBold();

                var valueCell = table.Cell()
                    .BorderBottom(DocTheme.RuleHairline).BorderColor(DocTheme.Hairline).Padding(DocTheme.Px(8)).AlignRight()
                    .Text($"{FormatSigned(value)} {_data.Currency}")
                    .Style(DocTheme.MonoText).FontSize(DocTheme.TableCell).FontColor(bold ? DocTheme.Ink : DocTheme.Secondary);
                if (bold) valueCell.SemiBold();
            }
        });
    }

    private void ComposeNetPayBox(IContainer container)
    {
        container
            .Border(DocTheme.RuleMedium).BorderColor(DocTheme.Ink)
            .PaddingVertical(DocTheme.Px(18)).PaddingHorizontal(DocTheme.Px(22))
            .Row(row =>
            {
                row.RelativeItem().AlignMiddle()
                    .Text(_theme.T("payslip.netPay").ToUpperInvariant())
                    .FontSize(DocTheme.Px(10)).SemiBold().FontColor(DocTheme.Muted)
                    .LetterSpacing(1.5f / 10f);

                row.AutoItem().AlignRight().AlignMiddle()
                    .Text($"{DocTheme.Amount(_data.NetPay)} {_data.Currency}")
                    .Style(DocTheme.MonoText).FontSize(DocTheme.Px(26)).Bold().FontColor(_theme.Accent);
            });
    }

    private void ComposeAttendanceNote(IContainer container) =>
        container.Text(
                _theme.T("payslip.attendanceNote")
                    .Replace("{{present}}", _data.PresentDays.ToString())
                    .Replace("{{late}}", _data.LateDays.ToString())
                    .Replace("{{half}}", _data.HalfDays.ToString())
                    .Replace("{{absent}}", _data.AbsentDays.ToString())
                    .Replace("{{leave}}", _data.LeaveDays.ToString()))
            .FontSize(DocTheme.Px(10)).FontColor(DocTheme.Muted).LineHeight(1.7f);

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

    /// <summary>Deductions are passed in as negative values so the sign shows without a special case.</summary>
    private static string FormatSigned(decimal value) =>
        value < 0 ? $"-{DocTheme.Amount(-value)}" : DocTheme.Amount(value);
}
