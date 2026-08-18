using System.Data.Common;
using Dapper;

namespace HamgamCementWeb.Server.Services;

public interface IAttendanceReadService
{
    Task<IReadOnlyList<AttendanceEmployeeRow>> ListActiveEmployeesAsync(
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AttendanceMonthRow>> ListMonthAsync(
        int year,
        int month,
        CancellationToken cancellationToken = default);

    Task<AttendanceMonthRow?> GetEmployeeMonthAsync(
        int employeeId,
        int year,
        int month,
        CancellationToken cancellationToken = default);
}

public sealed class AttendanceReadService : IAttendanceReadService
{
    private readonly ISqlConnectionFactory _sql;

    public AttendanceReadService(ISqlConnectionFactory sql)
    {
        _sql = sql;
    }

    public async Task<IReadOnlyList<AttendanceEmployeeRow>> ListActiveEmployeesAsync(
        CancellationToken cancellationToken = default)
    {
        await using var connection = (DbConnection)await _sql.OpenAsync(cancellationToken);

        const string sql = """
            SELECT
                e.EmployeeID AS EmployeeId,
                LTRIM(RTRIM(CONCAT(e.Name, N' ', e.Family))) AS FullName,
                ISNULL(d.Name, N'') AS DepartmentName,
                e.Sallary AS BaseSalary
            FROM dbo.Employees e
            LEFT JOIN dbo.Departments d ON d.DepartmentID = e.DepartmentId
            WHERE ISNULL(e.IsDeleted, 0) = 0
              AND ISNULL(e.IsActive, 1) = 1
            ORDER BY e.Family, e.Name
            """;

        var rows = await connection.QueryAsync<AttendanceEmployeeRow>(
            new CommandDefinition(sql, cancellationToken: cancellationToken));
        return rows.AsList();
    }

    public async Task<IReadOnlyList<AttendanceMonthRow>> ListMonthAsync(
        int year,
        int month,
        CancellationToken cancellationToken = default)
    {
        await using var connection = (DbConnection)await _sql.OpenAsync(cancellationToken);

        const string sql = """
            SELECT
                a.AttendanceID,
                a.EmployeeId,
                a.Year,
                a.Month,
                a.PresentDays,
                a.AbsentDays,
                a.LeavePaidDays,
                a.LeaveUnpaidDays,
                a.HolidayPaidDays,
                a.HolidayUnpaidDays,
                a.LateHours,
                a.EarlyLeaveHours,
                a.OvertimeHours,
                a.OvertimeCoefficient,
                a.Note
            FROM dbo.Attendances a
            WHERE ISNULL(a.IsDeleted, 0) = 0
              AND a.Year = @Year
              AND a.Month = @Month
            """;

        var rows = await connection.QueryAsync<AttendanceMonthRow>(
            new CommandDefinition(
                sql,
                new { Year = year, Month = month },
                cancellationToken: cancellationToken));
        return rows.AsList();
    }

    public async Task<AttendanceMonthRow?> GetEmployeeMonthAsync(
        int employeeId,
        int year,
        int month,
        CancellationToken cancellationToken = default)
    {
        await using var connection = (DbConnection)await _sql.OpenAsync(cancellationToken);

        const string sql = """
            SELECT TOP (1)
                a.AttendanceID,
                a.EmployeeId,
                a.Year,
                a.Month,
                a.PresentDays,
                a.AbsentDays,
                a.LeavePaidDays,
                a.LeaveUnpaidDays,
                a.HolidayPaidDays,
                a.HolidayUnpaidDays,
                a.LateHours,
                a.EarlyLeaveHours,
                a.OvertimeHours,
                a.OvertimeCoefficient,
                a.Note
            FROM dbo.Attendances a
            WHERE ISNULL(a.IsDeleted, 0) = 0
              AND a.EmployeeId = @EmployeeId
              AND a.Year = @Year
              AND a.Month = @Month
            """;

        return await connection.QuerySingleOrDefaultAsync<AttendanceMonthRow>(
            new CommandDefinition(
                sql,
                new { EmployeeId = employeeId, Year = year, Month = month },
                cancellationToken: cancellationToken));
    }
}

public sealed class AttendanceEmployeeRow
{
    public int EmployeeId { get; set; }
    public string FullName { get; set; } = "";
    public string DepartmentName { get; set; } = "";
    public decimal BaseSalary { get; set; }
}

public sealed class AttendanceMonthRow
{
    public int AttendanceID { get; set; }
    public int EmployeeId { get; set; }
    public int Year { get; set; }
    public int Month { get; set; }
    public int PresentDays { get; set; }
    public int AbsentDays { get; set; }
    public int LeavePaidDays { get; set; }
    public int LeaveUnpaidDays { get; set; }
    public int HolidayPaidDays { get; set; }
    public int HolidayUnpaidDays { get; set; }
    public decimal LateHours { get; set; }
    public decimal EarlyLeaveHours { get; set; }
    public decimal OvertimeHours { get; set; }
    public decimal OvertimeCoefficient { get; set; }
    public string? Note { get; set; }
}
