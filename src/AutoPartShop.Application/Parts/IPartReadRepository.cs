using AutoPartShop.Application.Parts.Dtos;

namespace AutoPartShop.Application.Parts
{
    public interface IPartReadRepository
    {
        Task<(IEnumerable<PartResponse> Parts, int TotalCount)> FindAllAsync(PartQuery query, CancellationToken cancellationToken = default);
    }
}
