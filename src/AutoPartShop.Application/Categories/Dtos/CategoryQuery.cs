using AutoPartShop.Application.Common;

namespace AutoPartShop.Application.Categories.Dtos;

public class CategoryQuery : BaseQuery
{
    public bool? IsActive { get; set; }
}