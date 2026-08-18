import { toLatinIsoDate } from '../lib/afghanSolarCalendar'

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
    headers:
      options.body && !(options.body instanceof FormData)
        ? { 'Content-Type': 'application/json' }
        : undefined,
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
const BANK_BASE = '/api/finance/bank-accounts'
const SETTLEMENTS_BASE = '/api/finance/settlements'
const STATEMENTS_BASE = '/api/finance/statements'
const INSTALLMENTS_BASE = '/api/finance/installments'
const COST_CENTERS_BASE = '/api/finance/cost-centers'
const FISCAL_PERIODS_BASE = '/api/finance/fiscal-periods'
const ATTACHMENTS_BASE = '/api/finance/attachments'
const DOUBTFUL_PROVISIONS_BASE = '/api/finance/doubtful-provisions'
const RECURRING_JOURNALS_BASE = '/api/finance/recurring-journals'

export async function fetchAccountTree() {
  return request(`${ACCOUNTS_BASE}/tree`)
}

export const journalEntriesApi = {
  createDataTableAjax: (onError) => createDataTableAjax(JOURNAL_BASE, onError),
  get: (id) => request(`${JOURNAL_BASE}/${id}`),
  create: (payload) =>
    request(JOURNAL_BASE, { method: 'POST', body: JSON.stringify(payload) }),
  remove: (id) => request(`${JOURNAL_BASE}/${id}`, { method: 'DELETE' }),
  reverse: (id, payload = {}) =>
    request(`${JOURNAL_BASE}/${id}/reverse`, {
      method: 'POST',
      body: JSON.stringify(payload),
    }),
  partyAccount: (partyType, partyId) =>
    request(
      `${JOURNAL_BASE}/party-account?partyType=${encodeURIComponent(partyType)}&partyId=${encodeURIComponent(partyId)}`,
    ),
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

export const rechargePettyCash = (id, payload) =>
  request(`${CASH_BASE}/${id}/recharge`, {
    method: 'POST',
    body: JSON.stringify(payload),
  })

export const invoiceInstallmentsApi = {
  list: (kind, invoiceId) =>
    request(`${INSTALLMENTS_BASE}?kind=${kind}&invoiceId=${invoiceId}`),
  generate: (payload) =>
    request(`${INSTALLMENTS_BASE}/generate`, {
      method: 'POST',
      body: JSON.stringify(payload),
    }),
}

export const costCentersApi = {
  createDataTableAjax: (onError) => createDataTableAjax(COST_CENTERS_BASE, onError),
  create: (payload) => request(COST_CENTERS_BASE, { method: 'POST', body: JSON.stringify(payload) }),
  update: (id, payload) => request(`${COST_CENTERS_BASE}/${id}`, { method: 'PUT', body: JSON.stringify(payload) }),
  remove: (id) => request(`${COST_CENTERS_BASE}/${id}`, { method: 'DELETE' }),
  options: () => request(`${COST_CENTERS_BASE}/options`),
}

export const fiscalPeriodsApi = {
  list: (solarYear) =>
    request(`${FISCAL_PERIODS_BASE}${solarYear ? `?solarYear=${solarYear}` : ''}`),
  create: (payload) => request(FISCAL_PERIODS_BASE, { method: 'POST', body: JSON.stringify(payload) }),
  close: (id) => request(`${FISCAL_PERIODS_BASE}/${id}/close`, { method: 'POST' }),
  reopen: (id) => request(`${FISCAL_PERIODS_BASE}/${id}/reopen`, { method: 'POST' }),
}

export const attachmentsApi = {
  list: (entityType, entityId) =>
    request(`${ATTACHMENTS_BASE}?entityType=${encodeURIComponent(entityType)}&entityId=${entityId}`),
  upload: (entityType, entityId, file) => {
    const body = new FormData()
    body.append('entityType', entityType)
    body.append('entityId', String(entityId))
    body.append('file', file)
    return request(`${ATTACHMENTS_BASE}/upload`, { method: 'POST', body })
  },
  remove: (id) => request(`${ATTACHMENTS_BASE}/${id}`, { method: 'DELETE' }),
}

export const doubtfulProvisionsApi = {
  list: () => request(DOUBTFUL_PROVISIONS_BASE),
  create: (payload) => request(DOUBTFUL_PROVISIONS_BASE, { method: 'POST', body: JSON.stringify(payload) }),
  remove: (id) => request(`${DOUBTFUL_PROVISIONS_BASE}/${id}`, { method: 'DELETE' }),
}

export const recurringJournalsApi = {
  list: () => request(RECURRING_JOURNALS_BASE),
  get: (id) => request(`${RECURRING_JOURNALS_BASE}/${id}`),
  create: (payload) => request(RECURRING_JOURNALS_BASE, { method: 'POST', body: JSON.stringify(payload) }),
  generate: (id, payload) => request(`${RECURRING_JOURNALS_BASE}/${id}/generate`, { method: 'POST', body: JSON.stringify(payload) }),
  remove: (id) => request(`${RECURRING_JOURNALS_BASE}/${id}`, { method: 'DELETE' }),
}

export const cashShiftsApi = {
  createDataTableAjax: (onError) =>
    createDataTableAjax(`${CASH_BASE}/shifts`, onError),
}

export const bankAccountsApi = {
  createDataTableAjax: (onError) => createDataTableAjax(BANK_BASE, onError),
  create: (payload) =>
    request(BANK_BASE, { method: 'POST', body: JSON.stringify(payload) }),
  update: (id, payload) =>
    request(`${BANK_BASE}/${id}`, { method: 'PUT', body: JSON.stringify(payload) }),
  get: (id) => request(`${BANK_BASE}/${id}`),
  options: () => request(`${BANK_BASE}/options`),
}

export const settlementsApi = {
  createDataTableAjax: (onError) =>
    createDataTableAjax(SETTLEMENTS_BASE, onError),
  create: (payload) =>
    request(SETTLEMENTS_BASE, { method: 'POST', body: JSON.stringify(payload) }),
  remove: (id) =>
    request(`${SETTLEMENTS_BASE}/${id}`, { method: 'DELETE' }),
}

const CURRENCY_EXCHANGES_BASE = '/api/finance/currency-exchanges'

export const currencyExchangesApi = {
  createDataTableAjax: (onError) =>
    createDataTableAjax(CURRENCY_EXCHANGES_BASE, onError),
  create: (payload) =>
    request(CURRENCY_EXCHANGES_BASE, {
      method: 'POST',
      body: JSON.stringify(payload),
    }),
  remove: (id) =>
    request(`${CURRENCY_EXCHANGES_BASE}/${id}`, { method: 'DELETE' }),
}

export const freeCashTransfer = (payload) =>
  request(`${CASH_BASE}/transfers`, {
    method: 'POST',
    body: JSON.stringify(payload),
  })

export const fetchCashBoxOptions = () => request(`${CASH_BASE}/options`)

export const fetchCashBoxUserOptions = () =>
  request(`${CASH_BASE}/user-options`)

export const fetchCashBoxBalances = (id) =>
  request(`${CASH_BASE}/${id}/balances`)

export const fetchCashBoxesOverview = () => request(`${CASH_BASE}/overview`)

export function fetchProfitAndLoss({ dateFrom, dateTo, compareFrom, compareTo } = {}) {
  const params = new URLSearchParams()
  if (dateFrom) params.set('dateFrom', dateFrom)
  if (dateTo) params.set('dateTo', dateTo)
  if (compareFrom) params.set('compareFrom', compareFrom)
  if (compareTo) params.set('compareTo', compareTo)
  const query = params.toString()
  return request(`${STATEMENTS_BASE}/profit-loss${query ? `?${query}` : ''}`)
}

export function fetchBalanceSheet({ asOf, compareAsOf } = {}) {
  const params = new URLSearchParams()
  if (asOf) params.set('asOf', asOf)
  if (compareAsOf) params.set('compareAsOf', compareAsOf)
  const query = params.toString()
  return request(`${STATEMENTS_BASE}/balance-sheet${query ? `?${query}` : ''}`)
}

export function fetchTrialBalance({ asOf } = {}) {
  const params = new URLSearchParams()
  if (asOf) params.set('asOf', asOf)
  const query = params.toString()
  return request(`${STATEMENTS_BASE}/trial-balance${query ? `?${query}` : ''}`)
}

export function fetchArAging({ asOf } = {}) {
  const params = new URLSearchParams()
  if (asOf) params.set('asOf', asOf)
  const query = params.toString()
  return request(`${STATEMENTS_BASE}/aging/ar${query ? `?${query}` : ''}`)
}

export function fetchApAging({ asOf } = {}) {
  const params = new URLSearchParams()
  if (asOf) params.set('asOf', asOf)
  const query = params.toString()
  return request(`${STATEMENTS_BASE}/aging/ap${query ? `?${query}` : ''}`)
}

export function fetchCashFlow({ dateFrom, dateTo } = {}) {
  const params = new URLSearchParams()
  if (dateFrom) params.set('dateFrom', dateFrom)
  if (dateTo) params.set('dateTo', dateTo)
  const query = params.toString()
  return request(`${STATEMENTS_BASE}/cash-flow${query ? `?${query}` : ''}`)
}

export function postInventoryOpening(payload) {
  return request('/api/inventory/opening', {
    method: 'POST',
    body: JSON.stringify(payload),
  })
}

export function fetchAccountLedger(accountId, { dateFrom, dateTo, partyId, costCenterId } = {}) {
  const params = new URLSearchParams()
  if (dateFrom) params.set('dateFrom', dateFrom)
  if (dateTo) params.set('dateTo', dateTo)
  if (partyId) params.set('partyId', String(partyId))
  if (costCenterId) params.set('costCenterId', String(costCenterId))
  const query = params.toString()
  return request(`${ACCOUNTS_BASE}/${accountId}/ledger${query ? `?${query}` : ''}`)
}

export function getAccountLedgerPrintUrl(
  accountId,
  { dateFrom, dateTo, partyId, costCenterId } = {},
) {
  const params = new URLSearchParams({ accountId: String(accountId) })
  const from = toLatinIsoDate(dateFrom)
  const to = toLatinIsoDate(dateTo)
  if (from) params.set('dateFrom', from)
  if (to) params.set('dateTo', to)
  if (partyId) params.set('partyId', String(partyId))
  if (costCenterId) params.set('costCenterId', String(costCenterId))
  return `/report-viewer/account-ledger?${params.toString()}`
}

export function getCostCenterReportUrl({ dateFrom, dateTo, costCenterId, accountId } = {}) {
  const params = new URLSearchParams()
  const from = toLatinIsoDate(dateFrom)
  const to = toLatinIsoDate(dateTo)
  if (from) params.set('dateFrom', from)
  if (to) params.set('dateTo', to)
  if (costCenterId) params.set('costCenterId', String(costCenterId))
  if (accountId) params.set('accountId', String(accountId))
  const query = params.toString()
  return `/report-viewer/cost-center${query ? `?${query}` : ''}`
}

export const accountsApi = {
  get: (id) => request(`${ACCOUNTS_BASE}/${id}`),
  create: (payload) =>
    request(ACCOUNTS_BASE, { method: 'POST', body: JSON.stringify(payload) }),
  update: (id, payload) =>
    request(`${ACCOUNTS_BASE}/${id}`, { method: 'PUT', body: JSON.stringify(payload) }),
  remove: (id) => request(`${ACCOUNTS_BASE}/${id}`, { method: 'DELETE' }),
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
