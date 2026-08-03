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

export const productsApi = {
  ...makeResource('/api/products'),
  getById: (id) => request(`/api/products/${id}`),
  convert: (payload) =>
    request('/api/products/convert', { method: 'POST', body: JSON.stringify(payload) }),
  suggestedPurchasePrice: (id, warehouseId) => {
    const qs =
      warehouseId != null && warehouseId !== ''
        ? `?warehouseId=${encodeURIComponent(warehouseId)}`
        : ''
    return request(`/api/products/${id}/suggested-purchase-price${qs}`)
  },
}

export const fetchSuggestedPurchasePrice = (productId, warehouseId) =>
  productsApi.suggestedPurchasePrice(productId, warehouseId)

export const categoriesApi = makeResource('/api/products/categories')
export const meaurmentsApi = makeResource('/api/products/meaurments')

export const fetchBaseMeaurmentOptions = () =>
  request('/api/products/meaurments/list/base-units')
export const fetchCategoryOptions = () => request('/api/products/categories/list')
export const fetchMeaurmentOptions = (baseMeaurmentId) =>
  request(
    baseMeaurmentId
      ? `/api/products/meaurments/list?baseMeaurmentId=${baseMeaurmentId}`
      : '/api/products/meaurments/list',
  )
export const fetchMeaurmentRatios = (baseMeaurmentId) =>
  request(
    baseMeaurmentId
      ? `/api/products/meaurments/ratios?baseMeaurmentId=${baseMeaurmentId}`
      : '/api/products/meaurments/ratios',
  )
export const fetchProductOptions = ({ kinds } = {}) => {
  const params = new URLSearchParams()
  if (Array.isArray(kinds) && kinds.length > 0) {
    params.set('kinds', kinds.join(','))
  }
  const qs = params.toString()
  return request(qs ? `/api/products/list?${qs}` : '/api/products/list')
}
export const fetchNextProductCodePreview = () => request('/api/products/next-code-preview')

export const PRODUCT_KIND = {
  Raw: 1,
  SemiFinished: 2,
  Processed: 3,
}

export const PRODUCT_SALE_PRICE_MODE = {
  Fixed: 1,
  ProfitPercent: 2,
}

export const PRODUCT_KIND_OPTIONS = [
  { value: PRODUCT_KIND.Processed, label: 'پروسس شده' },
  { value: PRODUCT_KIND.SemiFinished, label: 'نیمه پروسس' },
  { value: PRODUCT_KIND.Raw, label: 'خام' },
]

export const PRODUCT_SALE_PRICE_MODE_OPTIONS = [
  { value: PRODUCT_SALE_PRICE_MODE.Fixed, label: 'ثابت' },
  { value: PRODUCT_SALE_PRICE_MODE.ProfitPercent, label: 'متغیر بر اساس درصد سود' },
]
