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
}
