using AutoPartShop.Application.Hr.Dtos;

namespace AutoPartShop.Application.Hr
{
    public interface IAttendanceReadRepository
    {
        /// <summary>All active employees with their attendance mark (if any) for the given day.</summary>
        Task<IReadOnlyCollection<DailyAttendanceRow>> GetDailySheet(DateTime date, CancellationToken cancellationToken);

        /// <summary>Per-employee status counts for the given month (active employees only).</summary>
        Task<IReadOnlyCollection<MonthlyAttendanceSummaryRow>> GetMonthlySummary(int year, int month, CancellationToken cancellationToken);
    }

    public interface ILeaveRequestReadRepository
    {
        Task<(IReadOnlyCollection<LeaveRequestResponse> responses, int totalCount)> FindAllQuery(LeaveRequestQuery query, CancellationToken cancellationToken);
    }

    public interface ISalaryAdvanceReadRepository
    {
        Task<(IReadOnlyCollection<SalaryAdvanceResponse> responses, int totalCount)> FindAllQuery(SalaryAdvanceQuery query, CancellationToken cancellationToken);
    }

    public interface IHrSalesReadRepository
    {
        /// <summary>
        /// Total sales (grand total, non-cancelled/non-returned orders) per employee for the
        /// given month — matched via the employee's linked login account (SalesOrder.CreatedBy
        /// stores the username). The base for payslip commission.
        /// </summary>
        Task<IReadOnlyDictionary<Guid, decimal>> GetMonthlySalesTotalsByEmployee(int year, int month, CancellationToken cancellationToken);
    }
}
