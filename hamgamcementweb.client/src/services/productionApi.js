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

export const PRODUCTION_FORMULA_MODE = {
  Fixed: 1,
  Variable: 2,
}

export const PRODUCTION_FORMULA_MODE_OPTIONS = [
  { value: 1, label: 'ثابت' },
  { value: 2, label: 'متغیر' },
]

export const PRODUCTION_COST_TYPE = {
  DirectWage: 1,
  Overhead: 2,
  Ancillary: 3,
  Fixed: 4,
}

export const PRODUCTION_COST_TYPE_OPTIONS = [
  { value: 1, label: 'دستمزد مستقیم' },
  { value: 2, label: 'سربار تولید' },
  { value: 3, label: 'هزینه جانبی' },
  { value: 4, label: 'هزینه ثابت' },
]

export const PRODUCTION_COST_AMOUNT_MODE = {
  PerBase: 1,
  Flat: 2,
}

export const PRODUCTION_COST_AMOUNT_MODE_OPTIONS = [
  { value: 1, label: 'به ازای مقدار پایه' },
  { value: 2, label: 'مبلغ ثابت هر تولید' },
]

export const productionFormulasApi = {
  ...makeResource('/api/production/formulas'),
  getById: (id) => request(`/api/production/formulas/${id}`),
  getForProduction: (id) => request(`/api/production/formulas/${id}/for-production`),
  setDefault: (id) =>
    request(`/api/production/formulas/${id}/set-default`, { method: 'POST' }),
  fetchOptions: (productId) =>
    request(
      productId
        ? `/api/production/formulas/list?productId=${productId}`
        : '/api/production/formulas/list',
    ),
}

export const productionBatchesApi = {
  ...makeResource('/api/production/batches'),
  getById: (id) => request(`/api/production/batches/${id}`),
  post: (id) => request(`/api/production/batches/${id}/post`, { method: 'POST' }),
  unpost: (id) => request(`/api/production/batches/${id}/unpost`, { method: 'POST' }),
  trace: (id) => request(`/api/production/batches/${id}/trace`),
  previewPost: (id) => request(`/api/production/batches/${id}/preview-post`),
  fetchOptions: () => request('/api/production/batches/list'),
}

export const productionPlansApi = {
  ...makeResource('/api/production/plans'),
  getById: (id) => request(`/api/production/plans/${id}`),
  fetchOptions: (productId) =>
    request(
      productId
        ? `/api/production/plans/list?productId=${productId}`
        : '/api/production/plans/list',
    ),
}

export const PRODUCTION_BATCH_STATUS = {
  Draft: 1,
  Posted: 2,
}

export function buildProductionFormulaPayload(form) {
  return {
    name: form.name,
    productId: Number(form.productId),
    meaurmentId: Number(form.meaurmentId),
    baseQuantity: Number(form.baseQuantity),
    mode: Number(form.mode) || PRODUCTION_FORMULA_MODE.Fixed,
    isDefault: Boolean(form.isDefault),
    notes: form.notes || null,
    materialLines: form.materialLines.map((line) => ({
      productId: Number(line.productId),
      meaurmentId: Number(line.meaurmentId),
      quantity: Number(line.quantity),
      defaultWarehouseId: line.defaultWarehouseId
        ? Number(line.defaultWarehouseId)
        : null,
    })),
    costLines: (form.costLines || [])
      .filter((line) => line.costType && Number(line.amount) >= 0)
      .map((line) => ({
        costType: Number(line.costType),
        description: line.description || null,
        amountMode: Number(line.amountMode) || PRODUCTION_COST_AMOUNT_MODE.PerBase,
        amount: Number(line.amount) || 0,
        accountId: line.accountId ? Number(line.accountId) : null,
      })),
  }
}

export function buildProductionBatchPayload(form) {
  return {
    productionDate: form.productionDate,
    productionFormulaId: Number(form.productionFormulaId),
    productionPlanId: form.productionPlanId ? Number(form.productionPlanId) : null,
    outputWarehouseId: Number(form.outputWarehouseId),
    producedQuantity: Number(form.producedQuantity),
    description: form.description || null,
    inputLines: (form.inputLines || []).map((line) => ({
      warehouseId: Number(line.warehouseId),
      productId: Number(line.productId),
      meaurmentId: Number(line.meaurmentId),
      quantity: Number(line.quantity),
    })),
    costLines: (form.costLines || []).map((line) => ({
      costType: Number(line.costType),
      description: line.description || null,
      amount: Number(line.amount) || 0,
      accountId: line.accountId ? Number(line.accountId) : null,
    })),
  }
}

export function scaleFormulaForProduction(formula, producedQuantity) {
  const base = Number(formula.baseQuantity) || 1
  const qty = Number(producedQuantity) || 0
  const scale = base > 0 ? qty / base : 0

  return {
    inputLines: (formula.materialLines || []).map((line) => ({
      warehouseId: line.defaultWarehouseId ?? '',
      productId: line.productId,
      productName: line.productName,
      meaurmentId: line.meaurmentId,
      meaurmentName: line.meaurmentName,
      quantity: Number((Number(line.quantity) * scale).toFixed(6)),
    })),
    costLines: (formula.costLines || []).map((line) => ({
      costType: line.costType,
      description: line.description ?? '',
      amount:
        Number(line.amountMode) === PRODUCTION_COST_AMOUNT_MODE.Flat
          ? Number(line.amount) || 0
          : Number(((Number(line.amount) || 0) * scale).toFixed(4)),
      accountId: line.accountId ?? '',
    })),
    outputProductId: formula.productId,
    outputProductName: formula.productName,
    outputMeaurmentId: formula.meaurmentId,
    outputMeaurmentName: formula.meaurmentName,
    mode: formula.mode,
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
