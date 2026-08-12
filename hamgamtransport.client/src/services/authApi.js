const AUTH_BASE = '/api/auth'

async function parseResponse(response) {
  const contentType = response.headers.get('content-type') ?? ''
  const hasJson = contentType.includes('application/json')
  const data = hasJson ? await response.json() : null

  if (!response.ok) {
    const message =
      data?.message ??
      data?.title ??
      (typeof data === 'string' ? data : 'خطایی رخ داد. لطفاً دوباره تلاش کنید.')
    throw new Error(message)
  }

  return data
}

export async function login(userName, password) {
  const response = await fetch(`${AUTH_BASE}/login`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    credentials: 'include',
    body: JSON.stringify({ userName, password }),
  })

  return parseResponse(response)
}

export async function logout() {
  const response = await fetch(`${AUTH_BASE}/logout`, {
    method: 'POST',
    credentials: 'include',
  })

  return parseResponse(response)
}

export async function fetchCurrentUser() {
  const response = await fetch(`${AUTH_BASE}/me`, {
    credentials: 'include',
  })

  if (response.status === 401) {
    return null
  }

  return parseResponse(response)
}
