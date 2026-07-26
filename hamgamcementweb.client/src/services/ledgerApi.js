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
                ? 'سرویس موردنظر یافت نشد. سرور را ری‌استارت کنید.'
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

const ACCOUNTS_BASE = '/api/finance/accounts'
const JOURNAL_BASE = '/api/finance/journal'
const CASH_BASE = '/api/finance/cash-boxes'
const STATEMENTS_BASE = '/api/finance/statements'

export async function fetchAccountTree() {
  return request(`${ACCOUNTS_BASE}/tree`)
}

export const journalEntriesApi = {
  createDataTableAjax: (onError) => createDataTableAjax(JOURNAL_BASE, onError),
  get: (id) => request(`${JOURNAL_BASE}/${id}`),
}

export const cashBoxesApi = {
  create: (payload) =>
    request(CASH_BASE, { method: 'POST', body: JSON.stringify(payload) }),
  update: (id, payload) =>
    request(`${CASH_BASE}/${id}`, { method: 'PUT', body: JSON.stringify(payload) }),
  remove: () => Promise.reject(new Error('حذف صندوق از این صفحه پشتیبانی نمی‌شود.')),
  get: (id) => request(`${CASH_BASE}/${id}`),
  createDataTableAjax: (onError) => createDataTableAjax(CASH_BASE, onError),
}

export const cashShiftsApi = {
  createDataTableAjax: (onError) =>
    createDataTableAjax(`${CASH_BASE}/shifts`, onError),
}

export const fetchCashBoxOptions = () => request(`${CASH_BASE}/options`)

export const fetchCashBoxUserOptions = () =>
  request(`${CASH_BASE}/user-options`)

export const fetchCashBoxBalances = (id) =>
  request(`${CASH_BASE}/${id}/balances`)

export const fetchCashBoxesOverview = () => request(`${CASH_BASE}/overview`)

export function fetchProfitAndLoss({ dateFrom, dateTo } = {}) {
  const params = new URLSearchParams()
  if (dateFrom) params.set('dateFrom', dateFrom)
  if (dateTo) params.set('dateTo', dateTo)
  const query = params.toString()
  return request(`${STATEMENTS_BASE}/profit-loss${query ? `?${query}` : ''}`)
}

export function fetchBalanceSheet({ asOf } = {}) {
  const params = new URLSearchParams()
  if (asOf) params.set('asOf', asOf)
  const query = params.toString()
  return request(`${STATEMENTS_BASE}/balance-sheet${query ? `?${query}` : ''}`)
}

export const openCashShift = (payload) =>
  request(`${CASH_BASE}/shifts/open`, {
    method: 'POST',
    body: JSON.stringify(payload),
  })

export const closeCashShift = (id, payload) =>
  request(`${CASH_BASE}/shifts/${id}/close`, {
    method: 'POST',
    body: JSON.stringify(payload),
  })
