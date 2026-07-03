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

function makeResource(base) {
  return {
    create: (payload) =>
      request(base, { method: 'POST', body: JSON.stringify(payload) }),
    update: (id, payload) =>
      request(`${base}/${id}`, { method: 'PUT', body: JSON.stringify(payload) }),
    remove: (id) => request(`${base}/${id}`, { method: 'DELETE' }),
    createDataTableAjax: (onError) => createDataTableAjax(base, onError),
  }
}

export const vehicleTypesApi = makeResource('/api/transport/vehicle-types')
export const vehiclesApi = makeResource('/api/transport/vehicles')
export const routesApi = makeResource('/api/transport/routes')
export const tripsApi = makeResource('/api/transport/trips')
export const expenseCategoriesApi = makeResource('/api/transport/expense-categories')
export const invoicesApi = makeResource('/api/transport/invoices')
export const maintenancesApi = makeResource('/api/transport/maintenances')
export const partsApi = makeResource('/api/transport/parts')

// لیست‌ها برای دراپ‌داون‌ها
export const fetchVehicleTypeOptions = () =>
  request('/api/transport/vehicle-types/list')
export const fetchVehicleOptions = () => request('/api/transport/vehicles/list')
export const fetchRouteOptions = () => request('/api/transport/routes/list')
export const fetchTripOptions = () => request('/api/transport/trips/list')
export const fetchExpenseCategoryOptions = () =>
  request('/api/transport/expense-categories/list')
export const fetchDriverOptions = () => request('/api/drivers/list')
export const fetchVehicleOwnerOptions = () => request('/api/vehicle-owners/list')

export async function fetchCurrencyOptions() {
  const items = await request('/api/currencies/list')
  return (items ?? []).map((c) => ({
    value: c.currencyID,
    label: `${c.name} (${c.currencyCode})`,
    symbol: c.symbol ?? '',
    isBaseCurrency: c.isBaseCurrency,
  }))
}

// فاکتور مصارف
export const getInvoice = (invoiceId) =>
  request(`/api/transport/invoices/${invoiceId}`)
