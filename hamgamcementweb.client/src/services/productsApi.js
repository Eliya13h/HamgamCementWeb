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
}

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
export const fetchProductOptions = () => request('/api/products/list')
export const fetchNextProductCodePreview = () => request('/api/products/next-code-preview')
