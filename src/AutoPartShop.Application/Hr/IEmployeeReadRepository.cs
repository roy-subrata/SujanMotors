using AutoPartShop.Application.Hr.Dtos;

namespace AutoPartShop.Application.Hr
{
    public interface IEmployeeReadRepository
    {
        Task<(IReadOnlyCollection<EmployeeResponse> responses, int totalCount)> FindAllQuery(EmployeeQuery query, CancellationToken cancellationToken);

        /// <summary>
        /// Staff login accounts (non-customer) available to link to an employee record —
        /// excludes users already linked to another employee.
        /// </summary>
        Task<IReadOnlyCollection<LinkableUserResponse>> GetLinkableUsers(Guid? currentEmployeeId, CancellationToken cancellationToken);
    }

    public class LinkableUserResponse
    {
        public Guid Id { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }
}
