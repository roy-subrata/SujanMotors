using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AutoPartsShop.Api.Controllers;

/// <summary>
/// Returns a preview of what the next document number / code will look like.
///
/// IMPORTANT: These endpoints do NOT consume a sequence number.
/// The actual code is generated server-side at entity creation time and
/// returned in the create response.  Use these only for UI display hints
/// (e.g., showing "next SO number" in a form header before the user submits).
/// </summary>
[Route("api/code-generate")]
[Route("api/v1/code-generate")]
[ApiController]
[Authorize]
[Produces("application/json")]
public class CodeGenerateController(ICodeGenerateService _codeGenerator) : ControllerBase
{
    // ── Inventory ─────────────────────────────────────────────────────────────

    [HttpGet("part")]
    public async Task<IActionResult> PreviewPartCode(CancellationToken ct) =>
        Ok(new { code = await _codeGenerator.PeekAsync("SKU", ct) });

    [HttpGet("warehouse")]
    public async Task<IActionResult> PreviewWarehouseCode(CancellationToken ct) =>
        Ok(new { code = await _codeGenerator.PeekAsync("WH", ct) });

    // ── Sales ─────────────────────────────────────────────────────────────────

    [HttpGet("invoice")]
    public async Task<IActionResult> PreviewInvoiceNumber(CancellationToken ct) =>
        Ok(new { code = await _codeGenerator.PeekAsync("INV", ct) });

    [HttpGet("sales-order")]
    public async Task<IActionResult> PreviewSalesOrderNumber(CancellationToken ct) =>
        Ok(new { code = await _codeGenerator.PeekAsync("SO", ct) });

    [HttpGet("sales-return")]
    public async Task<IActionResult> PreviewSalesReturnNumber(CancellationToken ct) =>
        Ok(new { code = await _codeGenerator.PeekAsync("SR", ct) });

    // ── Procurement ───────────────────────────────────────────────────────────

    [HttpGet("purchase-order")]
    public async Task<IActionResult> PreviewPurchaseOrderNumber(CancellationToken ct) =>
        Ok(new { code = await _codeGenerator.PeekAsync("PO", ct) });

    [HttpGet("goods-receipt")]
    public async Task<IActionResult> PreviewGoodsReceiptNumber(CancellationToken ct) =>
        Ok(new { code = await _codeGenerator.PeekAsync("GRN", ct) });

    // ── Customers / Suppliers ─────────────────────────────────────────────────

    [HttpGet("customer")]
    public async Task<IActionResult> PreviewCustomerCode(CancellationToken ct) =>
        Ok(new { code = await _codeGenerator.PeekAsync("CUST", ct) });

    [HttpGet("supplier")]
    public async Task<IActionResult> PreviewSupplierCode(CancellationToken ct) =>
        Ok(new { code = await _codeGenerator.PeekAsync("SUP", ct) });

    [HttpGet("technician")]
    public async Task<IActionResult> PreviewTechnicianCode(CancellationToken ct) =>
        Ok(new { code = await _codeGenerator.PeekAsync("TECH", ct) });

    // ── HR ────────────────────────────────────────────────────────────────────

    [HttpGet("employee")]
    public async Task<IActionResult> PreviewEmployeeCode(CancellationToken ct) =>
        Ok(new { code = await _codeGenerator.PeekAsync("EMP", ct) });
}
