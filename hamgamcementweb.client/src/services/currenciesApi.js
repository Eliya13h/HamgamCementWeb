const BASE = '/api/currencies'

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
          ? 'سرویس ارزها یافت نشد. سرور را ری‌استارت کنید.'
          : typeof data === 'string'
            ? data
            : 'خطایی رخ داد. لطفاً دوباره تلاش کنید.')
    throw new Error(message)
  }

  return data
}

export async function fetchCurrenciesList() {
  const response = await fetch(`${BASE}/list`, {
    credentials: 'include',
  })
  return parseResponse(response)
}

export async function fetchBaseCurrency() {
  const response = await fetch(`${BASE}/base`, {
    credentials: 'include',
  })
  if (response.status === 404) {
    return null
  }
  return parseResponse(response)
}

export async function createCurrency(payload) {
  const response = await fetch(BASE, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    credentials: 'include',
    body: JSON.stringify(payload),
  })
  return parseResponse(response)
}

export async function updateCurrency(currencyId, payload) {
  const response = await fetch(`${BASE}/${currencyId}`, {
    method: 'PUT',
    headers: { 'Content-Type': 'application/json' },
    credentials: 'include',
    body: JSON.stringify(payload),
  })
  return parseResponse(response)
}

export async function setBaseCurrency(currencyId) {
  const response = await fetch(`${BASE}/${currencyId}/set-base`, {
    method: 'PUT',
    credentials: 'include',
  })
  return parseResponse(response)
}

export async function updateExchangeRate(currencyId, payload) {
  const response = await fetch(`${BASE}/${currencyId}/exchange-rate`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    credentials: 'include',
    body: JSON.stringify(payload),
  })
  return parseResponse(response)
}

export async function deleteCurrency(currencyId) {
  const response = await fetch(`${BASE}/${currencyId}`, {
    method: 'DELETE',
    credentials: 'include',
  })
  return parseResponse(response)
}

function createDataTableAjax(url, onError, extraBody = {}) {
  return (data, callback) => {
    fetch(url, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      credentials: 'include',
      body: JSON.stringify({ ...data, ...extraBody }),
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
                ? 'سرویس ارزها یافت نشد. سرور را ری‌استارت کنید.'
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

export function createCurrenciesDataTableAjax(onError) {
  return createDataTableAjax(`${BASE}/datatable`, onError)
}

export function createExchangeHistoryDataTableAjax(onError, currencyId) {
  return createDataTableAjax(`${BASE}/exchange-history/datatable`, onError, {
    currencyId: currencyId || null,
  })
}
