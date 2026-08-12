async function parseResponse(response) {
  const contentType = response.headers.get('content-type') ?? ''
  const hasJson = contentType.includes('application/json')
  const data = hasJson ? await response.json() : null
  if (!response.ok) {
    throw new Error(data?.message ?? 'خطایی رخ داد.')
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
          const body = response.headers.get('content-type')?.includes('application/json')
            ? await response.json()
            : null
          throw new Error(body?.message ?? 'بارگذاری داده‌ها با خطا مواجه شد.')
        }
        return response.json()
      })
      .then((json) => {
        onError?.('')
        callback(json)
      })
      .catch((error) => {
        onError?.(error.message)
        callback({ draw: data.draw, recordsTotal: 0, recordsFiltered: 0, data: [] })
      })
  }
}

const BASE = '/api/transport'

export const vehicleTypesApi = {
  createDataTableAjax: (onError) => createDataTableAjax(`${BASE}/vehicle-types`, onError),
  create: (payload) => request(`${BASE}/vehicle-types`, { method: 'POST', body: JSON.stringify(payload) }),
  update: (id, payload) => request(`${BASE}/vehicle-types/${id}`, { method: 'PUT', body: JSON.stringify(payload) }),
  remove: (id) => request(`${BASE}/vehicle-types/${id}`, { method: 'DELETE' }),
  options: () => request(`${BASE}/vehicle-types/options`),
}

export const vehicleOwnersApi = {
  createDataTableAjax: (onError) => createDataTableAjax(`${BASE}/vehicle-owners`, onError),
  create: (payload) => request(`${BASE}/vehicle-owners`, { method: 'POST', body: JSON.stringify(payload) }),
  update: (id, payload) => request(`${BASE}/vehicle-owners/${id}`, { method: 'PUT', body: JSON.stringify(payload) }),
  remove: (id) => request(`${BASE}/vehicle-owners/${id}`, { method: 'DELETE' }),
  options: () => request(`${BASE}/vehicle-owners/options`),
}

export const driversApi = {
  createDataTableAjax: (onError) => createDataTableAjax(`${BASE}/drivers`, onError),
  create: (payload) => request(`${BASE}/drivers`, { method: 'POST', body: JSON.stringify(payload) }),
  update: (id, payload) => request(`${BASE}/drivers/${id}`, { method: 'PUT', body: JSON.stringify(payload) }),
  remove: (id) => request(`${BASE}/drivers/${id}`, { method: 'DELETE' }),
  options: () => request(`${BASE}/drivers/options`),
}

export const vehiclesApi = {
  createDataTableAjax: (onError) => createDataTableAjax(`${BASE}/vehicles`, onError),
  create: (payload) => request(`${BASE}/vehicles`, { method: 'POST', body: JSON.stringify(payload) }),
  update: (id, payload) => request(`${BASE}/vehicles/${id}`, { method: 'PUT', body: JSON.stringify(payload) }),
  remove: (id) => request(`${BASE}/vehicles/${id}`, { method: 'DELETE' }),
  options: (role) => request(`${BASE}/vehicles/options${role != null ? `?role=${role}` : ''}`),
}

export const vehiclePairsApi = {
  createDataTableAjax: (onError) => createDataTableAjax(`${BASE}/vehicle-pairs`, onError),
  create: (payload) => request(`${BASE}/vehicle-pairs`, { method: 'POST', body: JSON.stringify(payload) }),
  update: (id, payload) => request(`${BASE}/vehicle-pairs/${id}`, { method: 'PUT', body: JSON.stringify(payload) }),
  remove: (id) => request(`${BASE}/vehicle-pairs/${id}`, { method: 'DELETE' }),
  options: () => request(`${BASE}/vehicle-pairs/options`),
  addShareAgreement: (id, payload) =>
    request(`${BASE}/vehicle-pairs/${id}/share-agreements`, { method: 'POST', body: JSON.stringify(payload) }),
}

export const tripsApi = {
  createDataTableAjax: (onError) => createDataTableAjax(`${BASE}/trips`, onError),
  get: (id) => request(`${BASE}/trips/${id}`),
  create: (payload) => request(`${BASE}/trips`, { method: 'POST', body: JSON.stringify(payload) }),
  update: (id, payload) => request(`${BASE}/trips/${id}`, { method: 'PUT', body: JSON.stringify(payload) }),
  remove: (id) => request(`${BASE}/trips/${id}`, { method: 'DELETE' }),
  updateStatus: (id, status) =>
    request(`${BASE}/trips/${id}/status`, { method: 'POST', body: JSON.stringify({ status }) }),
  postRevenue: (id) => request(`${BASE}/trips/${id}/post-revenue`, { method: 'POST' }),
  settle: (id) => request(`${BASE}/trips/${id}/settle`, { method: 'POST' }),
  addExpense: (id, payload) =>
    request(`${BASE}/trips/${id}/expenses`, { method: 'POST', body: JSON.stringify(payload) }),
  postExpense: (expenseId) => request(`${BASE}/trips/expenses/${expenseId}/post`, { method: 'POST' }),
}

export const tripExpenseCategoriesApi = {
  createDataTableAjax: (onError) => createDataTableAjax(`${BASE}/trip-expense-categories`, onError),
  create: (payload) =>
    request(`${BASE}/trip-expense-categories`, { method: 'POST', body: JSON.stringify(payload) }),
  update: (id, payload) =>
    request(`${BASE}/trip-expense-categories/${id}`, { method: 'PUT', body: JSON.stringify(payload) }),
  remove: (id) => request(`${BASE}/trip-expense-categories/${id}`, { method: 'DELETE' }),
  options: () => request(`${BASE}/trip-expense-categories/options`),
}

export const fleetReportsApi = {
  vehiclePl: (from, to) => {
    const q = new URLSearchParams()
    if (from) q.set('from', from)
    if (to) q.set('to', to)
    const suffix = q.toString() ? `?${q}` : ''
    return request(`${BASE}/reports/vehicle-pl${suffix}`)
  },
  ownerBalances: () => request(`${BASE}/reports/owner-balances`),
  customerAr: () => request(`${BASE}/reports/customer-ar`),
}

export async function fetchCustomersOptions() {
  const items = await request('/api/customers/list')
  return (items ?? []).map((c) => ({
    value: c.customerId ?? c.customerID ?? c.id,
    label: c.name,
  }))
}

export async function fetchCurrenciesOptions() {
  const items = await request('/api/currencies/list')
  return (items ?? []).map((c) => ({
    value: c.currencyId ?? c.currencyID,
    label: c.name ?? c.currencyCode,
  }))
}

export async function fetchCashBoxOptions() {
  const items = await request('/api/finance/cash-boxes/options')
  return items ?? []
}
