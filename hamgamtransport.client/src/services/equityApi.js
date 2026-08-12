const BASE = '/api/finance/equity-txns'
const SHAREHOLDERS_BASE = '/api/shareholders'

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
          ? 'سرویس حقوق صاحبان سهام یافت نشد. سرور را ری‌استارت کنید.'
          : typeof data === 'string'
            ? data
            : 'خطایی رخ داد. لطفاً دوباره تلاش کنید.')
    throw new Error(message)
  }

  return data
}

async function request(url, options = {}) {
  const response = await fetch(url, {
    credentials: 'include',
    headers: options.body ? { 'Content-Type': 'application/json' } : undefined,
    ...options,
  })
  return parseResponse(response)
}

function createDataTableAjax(base, onError) {
  return (data, callback) => {
    fetch(`${base}/datatable`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      credentials: 'include',
      body: JSON.stringify(data),
    })
      .then(async (response) => {
        if (!response.ok) {
          const contentType = response.headers.get('content-type') ?? ''
          const hasJson = contentType.includes('application/json')
          const body = hasJson ? await response.json() : null
          const message =
            body?.message ??
            (response.status === 401
              ? 'نشست شما منقضی شده است. لطفاً دوباره وارد شوید.'
              : response.status === 404
                ? 'سرویس حقوق صاحبان سهام یافت نشد. سرور را ری‌استارت کنید.'
                : 'بارگذاری داده‌ها با خطا مواجه شد.')
          throw new Error(message)
        }
        return response.json()
      })
      .then((json) => {
        onError?.('')
        callback(json)
      })
      .catch((error) => {
        onError?.(error.message)
        callback({
          draw: data.draw,
          recordsTotal: 0,
          recordsFiltered: 0,
          data: [],
        })
      })
  }
}

export const equityTxnsApi = {
  create: (payload) =>
    request(BASE, { method: 'POST', body: JSON.stringify(payload) }),
  remove: (id) => request(`${BASE}/${id}`, { method: 'DELETE' }),
  createDataTableAjax: (onError) => createDataTableAjax(BASE, onError),
}

export async function fetchShareholderOptions() {
  const rows = await request(`${SHAREHOLDERS_BASE}/options`)
  return (rows ?? []).map((r) => ({
    value: r.value,
    label: r.label,
  }))
}

export async function fetchEquityCashBoxOptions() {
  const rows = await request(`${BASE}/cash-box-options`)
  return (rows ?? []).map((r) => ({
    value: r.value,
    label: r.label,
  }))
}

export async function fetchDistributable(shareholderId, asOf) {
  const params = new URLSearchParams({
    shareholderId: String(shareholderId),
  })
  if (asOf) {
    params.set('asOf', String(asOf).slice(0, 10))
  }
  return request(`${BASE}/distributable?${params.toString()}`)
}

export async function postShareholderOpeningBalance(shareholderId) {
  return request(`${SHAREHOLDERS_BASE}/${shareholderId}/opening-balance`, {
    method: 'POST',
    body: JSON.stringify({}),
  })
}
