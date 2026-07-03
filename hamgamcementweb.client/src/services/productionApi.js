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

export const productionBatchesApi = {
  ...makeResource('/api/production/batches'),
  getById: (id) => request(`/api/production/batches/${id}`),
  post: (id) => request(`/api/production/batches/${id}/post`, { method: 'POST' }),
  trace: (id) => request(`/api/production/batches/${id}/trace`),
  fetchOptions: (availableForSales = false) =>
    request(`/api/production/batches/list?availableForSales=${availableForSales}`),
}

export const productionPlansApi = makeResource('/api/production/plans')

export const PRODUCTION_BATCH_STATUS = {
  Draft: 1,
  Posted: 2,
}

export const PURCHASE_ENTRY_SOURCE = {
  Market: 1,
  Production: 2,
}

export const PURCHASE_ENTRY_SOURCE_OPTIONS = [
  { value: 1, label: 'خرید از بازار' },
  { value: 2, label: 'ورود از تولید' },
]

export function buildProductionBatchPayload(form) {
  return {
    productionDate: form.productionDate,
    outputWarehouseId: Number(form.outputWarehouseId),
    fixedCost: Number(form.fixedCost) || 0,
    variableCost: Number(form.variableCost) || 0,
    description: form.description || null,
    inputLines: form.inputLines.map((line) => ({
      warehouseId: Number(line.warehouseId),
      productId: Number(line.productId),
      meaurmentId: Number(line.meaurmentId),
      quantity: Number(line.quantity),
    })),
    outputLines: form.outputLines.map((line) => ({
      productId: Number(line.productId),
      meaurmentId: Number(line.meaurmentId),
      quantity: Number(line.quantity),
    })),
  }
}

export function buildProductionPlanPayload(form) {
  return {
    planDate: form.planDate,
    productId: Number(form.productId),
    meaurmentId: Number(form.meaurmentId),
    plannedQuantity: Number(form.plannedQuantity),
    notes: form.notes || null,
  }
}
