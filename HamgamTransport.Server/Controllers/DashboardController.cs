using HamgamTransport.Server.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HamgamTransport.Server.Controllers;

[ApiController]
[Route("api/dashboard")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly IDashboardReadService _dashboard;

    public DashboardController(IDashboardReadService dashboard)
    {
        _dashboard = dashboard;
    }

    // کارت‌های خلاصه: سفرها، درآمد حمل و ناوگان فعال
    [HttpGet("summary")]
    public async Task<IActionResult> Summary(CancellationToken cancellationToken = default)
    {
        var result = await _dashboard.GetSummaryAsync(cancellationToken);
        return Ok(result);
    }

    // سری زمانی ماهانه درآمد/هزینه حمل و سایر عواید و مصارف
    [HttpGet("performance")]
    public async Task<IActionResult> Performance(
        [FromQuery] int months = 1,
        CancellationToken cancellationToken = default)
    {
        var result = await _dashboard.GetPerformanceAsync(months, cancellationToken);
        return Ok(result);
    }

    // آخرین سفرها، عواید و مصارف
    [HttpGet("recent-operations")]
    public async Task<IActionResult> RecentOperations(
        [FromQuery] int take = 15,
        CancellationToken cancellationToken = default)
    {
        var rows = await _dashboard.GetRecentOperationsAsync(take, cancellationToken);
        return Ok(rows);
    }

    // اعلان‌های سفر: در انتظار، بدون ثبت درآمد، تحویل‌شده
    [HttpGet("notifications")]
    public async Task<IActionResult> Notifications(CancellationToken cancellationToken = default)
    {
        var result = await _dashboard.GetNotificationsAsync(cancellationToken);
        return Ok(result);
    }
}
