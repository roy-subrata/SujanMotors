using AutoPartShop.Application.Technecians.Dtos;

namespace AutoPartShop.Application.Technecians
{
    public interface ITechnecianReadRepository
    {
        public Task<(IReadOnlyCollection<TechnicianResponse> responses, int totalCount)> FindAllQuery(TechnecianQuery query, CancellationToken cancellationToken);
    }


}
