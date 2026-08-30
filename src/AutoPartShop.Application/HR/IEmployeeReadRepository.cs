using AutoPartShop.Application.HR.Dtos;

namespace AutoPartShop.Application.HR
{
    public interface IEmployeeReadRepository
    {
        Task<(IReadOnlyCollection<EmployeeResponse> responses, int totalCount)> FindAllQuery(EmployeeQuery query, CancellationToken cancellationToken);

        /// <summary>
        /// Staff login accounts (non-customer) available to link to an employee record —
        /// excludes users already linked to another employee.
        /// </summary>
        Task<IReadOnlyCollection<LinkableUserResponse>> GetLinkableUsers(Guid? currentEmployeeId, CancellationToken cancellationToken);

        /// <summary>Returns shift names for the given shift IDs.</summary>
        Task<Dictionary<Guid, string>> GetShiftNamesAsync(IEnumerable<Guid> shiftIds, CancellationToken cancellationToken);
    }

    public class LinkableUserResponse
    {
        public Guid Id { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }
}
