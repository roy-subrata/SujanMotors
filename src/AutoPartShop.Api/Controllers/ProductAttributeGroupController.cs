using AutoPartShop.Domain.Entities;
using AutoPartShop.Api.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AutoPartShop.Api.Controllers;

[Route("api/attribute-groups")]
[Route("api/v1/attribute-groups")]
[ApiController]
[Produces("application/json")]
[HasPermission(Permissions.InventoryView)]
public class ProductAttributeGroupController : ControllerBase
{
    private readonly AutoPartDbContext _db;
    private readonly ILogger<ProductAttributeGroupController> _logger;

    public ProductAttributeGroupController(AutoPartDbContext db, ILogger<ProductAttributeGroupController> logger)
    {
        _db = db;
        _logger = logger;
    }

    // â”€â”€ Groups â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var groups = await _db.ProductAttributeGroups
            .OrderBy(g => g.SortOrder).ThenBy(g => g.Name)
            .Include(g => g.Attributes.OrderBy(a => a.Name))
                .ThenInclude(a => a.Options.OrderBy(o => o.SortOrder))
            .AsNoTracking()
            .ToListAsync(ct);

        return Ok(groups.Select(MapGroup));
    }

    [HttpPost("list")]
    public async Task<IActionResult> GetPaged([FromBody] AttributeGroupQuery query, CancellationToken ct)
    {
        var q = _db.ProductAttributeGroups
            .Include(g => g.Attributes.OrderBy(a => a.Name))
                .ThenInclude(a => a.Options.OrderBy(o => o.SortOrder))
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Search))
            q = q.Where(g => g.Name.Contains(query.Search));

        if (query.IsActive.HasValue)
            q = q.Where(g => g.IsActive == query.IsActive.Value);

        var total = await q.CountAsync(ct);

        var groups = await q
            .OrderBy(g => g.SortOrder).ThenBy(g => g.Name)
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(ct);

        return Ok(new
        {
            data = groups.Select(MapGroup),
            pagination = new
            {
                pageNumber = query.PageNumber,
                pageSize = query.PageSize,
                totalCount = total,
                totalPages = (int)Math.Ceiling(total / (double)query.PageSize)
            }
        });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var group = await _db.ProductAttributeGroups
            .Include(g => g.Attributes.OrderBy(a => a.Name))
                .ThenInclude(a => a.Options.OrderBy(o => o.SortOrder))
            .AsNoTracking()
            .FirstOrDefaultAsync(g => g.Id == id, ct);

        if (group is null) return NotFound();
        return Ok(MapGroup(group));
    }

    [HttpPost]
    [HasPermission(Permissions.InventoryCreate)]
    public async Task<IActionResult> CreateGroup([FromBody] CreateAttributeGroupRequest req, CancellationToken ct)
    {
        try
        {
            var group = ProductAttributeGroup.Create(req.Name, req.SortOrder);
            _db.ProductAttributeGroups.Add(group);
            await _db.SaveChangesAsync(ct);
            return CreatedAtAction(nameof(GetById), new { id = group.Id }, MapGroup(group));
        }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating attribute group");
            return StatusCode(500, "An error occurred");
        }
    }

    [HttpPut("{id:guid}")]
    [HasPermission(Permissions.InventoryEdit)]
    public async Task<IActionResult> UpdateGroup(Guid id, [FromBody] CreateAttributeGroupRequest req, CancellationToken ct)
    {
        try
        {
            var group = await _db.ProductAttributeGroups
                .Include(g => g.Attributes).ThenInclude(a => a.Options)
                .FirstOrDefaultAsync(g => g.Id == id, ct);
            if (group is null) return NotFound();

            group.Update(req.Name, req.SortOrder, req.IsActive);
            await _db.SaveChangesAsync(ct);
            return Ok(MapGroup(group));
        }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating attribute group");
            return StatusCode(500, "An error occurred");
        }
    }

    [HttpDelete("{id:guid}")]
    [HasPermission(Permissions.InventoryDelete)]
    public async Task<IActionResult> DeleteGroup(Guid id, CancellationToken ct)
    {
        var group = await _db.ProductAttributeGroups.FindAsync(new object[] { id }, ct);
        if (group is null) return NotFound();
        _db.ProductAttributeGroups.Remove(group);
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    // â”€â”€ Attributes â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    [HttpPost("{groupId:guid}/attributes")]
    [HasPermission(Permissions.InventoryEdit)]
    public async Task<IActionResult> AddAttribute(Guid groupId, [FromBody] CreateAttributeRequest req, CancellationToken ct)
    {
        try
        {
            if (!await _db.ProductAttributeGroups.AnyAsync(g => g.Id == groupId, ct))
                return NotFound(new { message = "Attribute group not found" });

            if (await _db.ProductAttributes.AnyAsync(a => a.Code == req.Code.Trim().ToUpperInvariant(), ct))
                return Conflict(new { message = $"Attribute code '{req.Code}' already exists" });

            var attr = ProductAttribute.Create(groupId, req.Name, req.Code, req.DataType, req.Unit ?? "");
            _db.ProductAttributes.Add(attr);
            await _db.SaveChangesAsync(ct);
            return Ok(MapAttribute(attr));
        }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding attribute");
            return StatusCode(500, "An error occurred");
        }
    }

    [HttpPut("{groupId:guid}/attributes/{attrId:guid}")]
    [HasPermission(Permissions.InventoryEdit)]
    public async Task<IActionResult> UpdateAttribute(Guid groupId, Guid attrId, [FromBody] CreateAttributeRequest req, CancellationToken ct)
    {
        try
        {
            var attr = await _db.ProductAttributes
                .Include(a => a.Options)
                .FirstOrDefaultAsync(a => a.Id == attrId && a.AttributeGroupId == groupId, ct);
            if (attr is null) return NotFound();

            attr.Update(req.Name, req.Code, req.DataType, req.Unit ?? "", req.IsActive);
            await _db.SaveChangesAsync(ct);
            return Ok(MapAttribute(attr));
        }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating attribute");
            return StatusCode(500, "An error occurred");
        }
    }

    [HttpDelete("{groupId:guid}/attributes/{attrId:guid}")]
    [HasPermission(Permissions.InventoryEdit)]
    public async Task<IActionResult> DeleteAttribute(Guid groupId, Guid attrId, CancellationToken ct)
    {
        var attr = await _db.ProductAttributes
            .FirstOrDefaultAsync(a => a.Id == attrId && a.AttributeGroupId == groupId, ct);
        if (attr is null) return NotFound();
        _db.ProductAttributes.Remove(attr);
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    // â”€â”€ Options â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    [HttpPost("{groupId:guid}/attributes/{attrId:guid}/options")]
    [HasPermission(Permissions.InventoryEdit)]
    public async Task<IActionResult> AddOption(Guid groupId, Guid attrId, [FromBody] CreateOptionRequest req, CancellationToken ct)
    {
        try
        {
            if (!await _db.ProductAttributes.AnyAsync(a => a.Id == attrId && a.AttributeGroupId == groupId, ct))
                return NotFound(new { message = "Attribute not found" });

            var option = ProductAttributeOption.Create(attrId, req.Value, req.SortOrder);
            _db.ProductAttributeOptions.Add(option);
            await _db.SaveChangesAsync(ct);
            return Ok(new { option.Id, option.AttributeId, option.Value, option.SortOrder });
        }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding option");
            return StatusCode(500, "An error occurred");
        }
    }

    [HttpPut("{groupId:guid}/attributes/{attrId:guid}/options/{optId:guid}")]
    [HasPermission(Permissions.InventoryEdit)]
    public async Task<IActionResult> UpdateOption(Guid groupId, Guid attrId, Guid optId, [FromBody] CreateOptionRequest req, CancellationToken ct)
    {
        try
        {
            var opt = await _db.ProductAttributeOptions
                .FirstOrDefaultAsync(o => o.Id == optId && o.AttributeId == attrId, ct);
            if (opt is null) return NotFound();

            opt.Update(req.Value, req.SortOrder);
            await _db.SaveChangesAsync(ct);
            return Ok(new { opt.Id, opt.AttributeId, opt.Value, opt.SortOrder });
        }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating option");
            return StatusCode(500, "An error occurred");
        }
    }

    [HttpDelete("{groupId:guid}/attributes/{attrId:guid}/options/{optId:guid}")]
    [HasPermission(Permissions.InventoryEdit)]
    public async Task<IActionResult> DeleteOption(Guid groupId, Guid attrId, Guid optId, CancellationToken ct)
    {
        var opt = await _db.ProductAttributeOptions
            .FirstOrDefaultAsync(o => o.Id == optId && o.AttributeId == attrId, ct);
        if (opt is null) return NotFound();
        _db.ProductAttributeOptions.Remove(opt);
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    // â”€â”€ Mappers â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    private static object MapGroup(ProductAttributeGroup g) => new
    {
        g.Id,
        g.Name,
        g.SortOrder,
        g.IsActive,
        attributes = g.Attributes.Select(MapAttribute)
    };

    private static object MapAttribute(ProductAttribute a) => new
    {
        a.Id,
        a.AttributeGroupId,
        a.Name,
        a.Code,
        a.DataType,
        a.Unit,
        a.IsActive,
        options = a.Options.OrderBy(o => o.SortOrder).Select(o => new { o.Id, o.AttributeId, o.Value, o.SortOrder })
    };
}

public record CreateAttributeGroupRequest(string Name, int SortOrder = 0, bool IsActive = true);
public record CreateAttributeRequest(string Name, string Code, string DataType = "option", string? Unit = null, bool IsActive = true);
public record CreateOptionRequest(string Value, int SortOrder = 0);
public record AttributeGroupQuery(string Search = "", bool? IsActive = null, int PageNumber = 1, int PageSize = 10);
