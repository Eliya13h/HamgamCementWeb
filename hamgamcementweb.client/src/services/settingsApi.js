const SETTINGS_BASE = '/api/settings/general'

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
        : response.status === 403
          ? 'شما مجوز انجام این عملیات را ندارید.'
          : typeof data === 'string'
            ? data
            : 'خطایی رخ داد. لطفاً دوباره تلاش کنید.')
    throw new Error(message)
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
