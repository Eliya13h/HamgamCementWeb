const ATTENDANCE_BASE = '/api/attendance'
const SALARY_BASE = '/api/salary-payments'

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
          ? 'سرویس یافت نشد. سرور را ری‌استارت کنید.'
          : typeof data === 'string'
            ? data
            : 'خطایی رخ داد. لطفاً دوباره تلاش کنید.')
    throw new Error(message)
  }

  return data
}

function toQuery(params) {
  const search = new URLSearchParams()
  Object.entries(params).forEach(([key, value]) => {
    if (value === undefined || value === null || value === '') return
    search.set(key, String(value))
  })
  const text = search.toString()
  return text ? `?${text}` : ''
}

export async function fetchAttendanceRange(from, to) {
  const response = await fetch(
    `${ATTENDANCE_BASE}${toQuery({ from, to })}`,
    { credentials: 'include' },
  )
  return parseResponse(response)
}

export async function upsertAttendance(payload) {
  const response = await fetch(ATTENDANCE_BASE, {
    method: 'PUT',
    headers: { 'Content-Type': 'application/json' },
    credentials: 'include',
    body: JSON.stringify(payload),
  })
  return parseResponse(response)
}

export async function upsertAttendanceDay(payload) {
  const response = await fetch(`${ATTENDANCE_BASE}/day`, {
    method: 'PUT',
    headers: { 'Content-Type': 'application/json' },
    credentials: 'include',
    body: JSON.stringify(payload),
  })
  return parseResponse(response)
}

export async function fetchSalaryPayments({ year, month } = {}) {
  const response = await fetch(
    `${SALARY_BASE}${toQuery({ year, month })}`,
    { credentials: 'include' },
  )
  return parseResponse(response)
}

export async function fetchSalaryPreview({ employeeId, year, month, from, to }) {
  const response = await fetch(
    `${SALARY_BASE}/preview${toQuery({ employeeId, year, month, from, to })}`,
    { credentials: 'include' },
  )
  return parseResponse(response)
}

export async function createSalaryPayment(payload) {
  const response = await fetch(SALARY_BASE, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    credentials: 'include',
    body: JSON.stringify(payload),
  })
  return parseResponse(response)
}

export async function deleteSalaryPayment(id) {
  const response = await fetch(`${SALARY_BASE}/${id}`, {
    method: 'DELETE',
    credentials: 'include',
  })
  return parseResponse(response)
}
