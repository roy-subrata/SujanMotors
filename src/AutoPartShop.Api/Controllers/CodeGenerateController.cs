using Microsoft.AspNetCore.Mvc;

namespace AutoPartsShop.Api.Controllers
{
    [Route("api/code-generate")]
    [ApiController]
    [Produces("application/json")]
    public class CodeGenerateController(ICodeGenerateService _codeGenerator) : ControllerBase
    {
        [HttpGet("unit")]
        public async Task<IActionResult> GenerateUnitCode(CancellationToken cancellationToken = default)
        {
            var code = await _codeGenerator.GenerateAsync("UNI", cancellationToken);
            return Ok(code);
        }

        [HttpGet("category")]
        public async Task<IActionResult> GenerateCategoryCode(CancellationToken cancellationToken = default)
        {
            var code = await _codeGenerator.GenerateAsync("CAT", cancellationToken);
            return Ok(code);
        }

        [HttpGet("brand")]
        public async Task<IActionResult> GenerateBrandCode(CancellationToken cancellationToken = default)
        {
            var code = await _codeGenerator.GenerateAsync("BRD", cancellationToken);
            return Ok(code);
        }

        [HttpGet("part")]
        public async Task<IActionResult> GeneratePartCode(CancellationToken cancellationToken = default)
        {
            var code = await _codeGenerator.GenerateAsync("SKU", cancellationToken);
            return Ok(code);
        }

        [HttpGet("warehouse")]
        public async Task<IActionResult> GenerateWarehouseCode(CancellationToken cancellationToken = default)
        {
            var code = await _codeGenerator.GenerateAsync("WH", cancellationToken);
            return Ok(code);
        }

        [HttpGet("invoice")]
        public async Task<IActionResult> GenerateInvoiceNumber(CancellationToken cancellationToken = default)
        {
            var invoiceNumber = await _codeGenerator.GenerateAsync("INV", cancellationToken);
            return Ok(new { invoiceNumber });
        }

        [HttpGet("sales-order")]
        public async Task<IActionResult> GenerateSalesOrderNumber(CancellationToken cancellationToken = default)
        {
            var salesOrderNumber = await _codeGenerator.GenerateAsync("SO", cancellationToken);
            return Ok(new { salesOrderNumber });
        }

        [HttpGet("customer")]
        public async Task<IActionResult> GenerateCustomerCode(CancellationToken cancellationToken = default)
        {
            var customerCode = await _codeGenerator.GenerateAsync("CUST", cancellationToken);
            return Ok(new { customerCode });
        }

        [HttpGet("supplier")]
        public async Task<IActionResult> GenerateSupplierCode(CancellationToken cancellationToken = default)
        {
            var supplierCode = await _codeGenerator.GenerateAsync("SUP", cancellationToken);
            return Ok(new { supplierCode });
        }

        [HttpGet("purchase-order")]
        public async Task<IActionResult> GeneratePurchaseOrderNumber(CancellationToken cancellationToken = default)
        {
            var purchaseOrderNumber = await _codeGenerator.GenerateAsync("PO", cancellationToken);
            return Ok(new { purchaseOrderNumber });
        }

        [HttpGet("goods-receipt")]
        public async Task<IActionResult> GenerateGoodsReceiptNumber(CancellationToken cancellationToken = default)
        {
            var goodsReceiptNumber = await _codeGenerator.GenerateAsync("GRN", cancellationToken);
            return Ok(new { goodsReceiptNumber });
        }   

        [HttpGet("sales-return")]
        public async Task<IActionResult> GenerateSalesReturnNumber(CancellationToken cancellationToken = default)
        {
            var salesReturnNumber = await _codeGenerator.GenerateAsync("SR", cancellationToken);
            return Ok(new { salesReturnNumber });
        }
        
        [HttpGet("technician")]
        public async Task<IActionResult> GenerateTechnicianCode(CancellationToken cancellationToken = default)
        {
            var technicianCode = await _codeGenerator.GenerateAsync("TECH", cancellationToken);
            return Ok(new { technicianCode });
        }
    }
}
