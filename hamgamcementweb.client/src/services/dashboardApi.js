async function parseResponse(response) {
  const contentType = response.headers.get('content-type') ?? ''
  const hasJson = contentType.includes('application/json')
  const data = hasJson ? await response.json() : null

  if (!response.ok) {
    const message =
      data?.message ??
      data?.title ??
      (response.status === 401
        ? 'نشست شما منقضی شده است. لطفاً دوباره وارد شوید.'
        : response.status === 404
          ? 'سرویس موردنظر یافت نشد. سرور را ری‌استارت کنید.'
          : typeof data === 'string'
            ? data
            : 'خطایی رخ داد. لطفاً دوباره تلاش کنید.')
    throw new Error(message)
  }

  return data
}

export function fetchDashboardSummary() {
  return fetch('/api/dashboard/summary', {
    credentials: 'include',
  }).then(parseResponse)
}

export function fetchDashboardPerformance(months = 1) {
  return fetch(`/api/dashboard/performance?months=${encodeURIComponent(months)}`, {
    credentials: 'include',
  }).then(parseResponse)
}

export function fetchDashboardRecentOperations(take = 15) {
  return fetch(`/api/dashboard/recent-operations?take=${encodeURIComponent(take)}`, {
    credentials: 'include',
  }).then(parseResponse)
}

export function fetchDashboardNotifications() {
  return fetch('/api/dashboard/notifications', {
    credentials: 'include',
  }).then(parseResponse)
}
