using AutoPartShop.Application.Common;

namespace AutoPartShop.Application.Brands.Dtos;

public class BrandQuery : BaseQuery
{
    public bool? IsActive { get; set; }
    public string? Country { get; set; }
}
