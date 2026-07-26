using HamgamCementWeb.Server.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HamgamCementWeb.Server.Controllers;

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

    // کارت‌های خلاصه: تولید امروز، فروش/خرید امروز و ماه جاری
    [HttpGet("summary")]
    public async Task<IActionResult> Summary(CancellationToken cancellationToken = default)
    {
        var result = await _dashboard.GetSummaryAsync(cancellationToken);
        return Ok(result);
    }

    // سری زمانی ماهانه خرید، فروش، درآمد و مصرف برای نمودار داشبورد
    [HttpGet("performance")]
    public async Task<IActionResult> Performance(
        [FromQuery] int months = 1,
        CancellationToken cancellationToken = default)
    {
        var result = await _dashboard.GetPerformanceAsync(months, cancellationToken);
        return Ok(result);
    }

    // آخرین عملیات تولید، خرید و فروش
    [HttpGet("recent-operations")]
    public async Task<IActionResult> RecentOperations(
        [FromQuery] int take = 15,
        CancellationToken cancellationToken = default)
    {
        var rows = await _dashboard.GetRecentOperationsAsync(take, cancellationToken);
        return Ok(rows);
    }

    // اعلان‌ها: کمبود محصول، پر شدن انبار، خالی بودن کمتر از ۲۰٪
    [HttpGet("notifications")]
    public async Task<IActionResult> Notifications(CancellationToken cancellationToken = default)
    {
        var result = await _dashboard.GetNotificationsAsync(cancellationToken);
        return Ok(result);
    }
}
