const BASE = '/api/suppliers'

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
          ? 'سرویس تأمین‌کنندگان یافت نشد. سرور را ری‌استارت کنید.'
          : typeof data === 'string'
            ? data
            : 'خطایی رخ داد. لطفاً دوباره تلاش کنید.')
    throw new Error(message)
  }

  return data
}

export async function fetchSupplier(supplierId) {
  const response = await fetch(`${BASE}/${supplierId}`, {
    credentials: 'include',
  })
  return parseResponse(response)
}

export async function createSupplier(payload) {
  const response = await fetch(BASE, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    credentials: 'include',
    body: JSON.stringify(payload),
  })

  return parseResponse(response)
}

export async function updateSupplier(supplierId, payload) {
  const response = await fetch(`${BASE}/${supplierId}`, {
    method: 'PUT',
    headers: { 'Content-Type': 'application/json' },
    credentials: 'include',
    body: JSON.stringify(payload),
  })

  return parseResponse(response)
}

export async function deleteSupplier(supplierId) {
  const response = await fetch(`${BASE}/${supplierId}`, {
    method: 'DELETE',
    credentials: 'include',
  })

  return parseResponse(response)
}

function createDataTableAjax(url, onError, onLoaded) {
  return (data, callback) => {
    fetch(url, {
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
                ? 'سرویس تأمین‌کنندگان یافت نشد. سرور را ری‌استارت کنید.'
                : 'بارگذاری داده‌ها با خطا مواجه شد.')
          throw new Error(message)
        }
        return response.json()
      })
      .then((json) => {
        onError?.('')
        onLoaded?.(json)
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

export function createSuppliersDataTableAjax(onError, onLoaded) {
  return createDataTableAjax(`${BASE}/datatable`, onError, onLoaded)
}

export function createSupplierInvoicesDataTableAjax(supplierId, onError, onLoaded) {
  return createDataTableAjax(
    `${BASE}/${supplierId}/purchase-invoices/datatable`,
    onError,
    onLoaded,
  )
}
