const SETTINGS_BASE = '/api/settings/general'

function extractErrorMessage(data, status) {
  if (data?.errors && typeof data.errors === 'object') {
    const messages = Object.values(data.errors)
      .flat()
      .filter((item) => typeof item === 'string' && item.trim())
    if (messages.length > 0) {
      return messages.join(' ')
    }
  }

  if (typeof data?.message === 'string' && data.message.trim()) {
    return data.message
  }

  if (status === 401) {
    return 'نشست شما منقضی شده است. لطفاً دوباره وارد شوید.'
  }

  if (status === 403) {
    return 'شما مجوز انجام این عملیات را ندارید.'
  }

  if (typeof data === 'string' && data.trim()) {
    return data
  }

  if (typeof data?.title === 'string' && data.title.trim() && !data.title.includes('validation errors')) {
    return data.title
  }

  return 'خطایی رخ داد. لطفاً دوباره تلاش کنید.'
}

async function parseResponse(response) {
  const contentType = response.headers.get('content-type') ?? ''
  const hasJson = contentType.includes('application/json')
  const data = hasJson ? await response.json() : null

  if (!response.ok) {
    throw new Error(extractErrorMessage(data, response.status))
  }

  return data
}

export async function fetchGeneralSettings() {
  const response = await fetch(SETTINGS_BASE, {
    credentials: 'include',
  })

  return parseResponse(response)
}

export async function updateGeneralSettings(payload) {
  const response = await fetch(SETTINGS_BASE, {
    method: 'PUT',
    headers: { 'Content-Type': 'application/json' },
    credentials: 'include',
    body: JSON.stringify(payload),
  })

  return parseResponse(response)
}

export async function uploadCompanyLogo(file) {
  const formData = new FormData()
  formData.append('file', file)

  const response = await fetch(`${SETTINGS_BASE}/company-logo`, {
    method: 'POST',
    credentials: 'include',
    body: formData,
  })

  return parseResponse(response)
}

const FISCAL_YEARS_BASE = '/api/finance/fiscal-years'

export async function fetchFiscalYears() {
  const response = await fetch(FISCAL_YEARS_BASE, {
    credentials: 'include',
  })

  return parseResponse(response)
}

export async function fetchFiscalYearClosingPreview(fiscalYearId) {
  const response = await fetch(`${FISCAL_YEARS_BASE}/${fiscalYearId}/closing-preview`, {
    credentials: 'include',
  })

  return parseResponse(response)
}

export async function closeFiscalYear(fiscalYearId, password) {
  const response = await fetch(`${FISCAL_YEARS_BASE}/${fiscalYearId}/close`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    credentials: 'include',
    body: JSON.stringify({ password }),
  })

  return parseResponse(response)
}

export async function reopenFiscalYear(fiscalYearId, password) {
  const response = await fetch(`${FISCAL_YEARS_BASE}/${fiscalYearId}/reopen`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    credentials: 'include',
    body: JSON.stringify({ password }),
  })

  return parseResponse(response)
}
