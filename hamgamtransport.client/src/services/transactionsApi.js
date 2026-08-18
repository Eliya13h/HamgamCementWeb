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

export async function fetchSupplierOptions() {
  const items = await request('/api/suppliers/list')
  return (items ?? []).map((s) => ({ value: s.supplierId, label: s.name }))
}

export async function fetchCustomerOptions() {
  const items = await request('/api/customers/list')
  return (items ?? []).map((c) => ({ value: c.customerId, label: c.name }))
}

export async function fetchCurrencyRateAt(currencyId, date) {
  const params = new URLSearchParams({ currencyId: String(currencyId) })
  if (date) params.set('date', date)
  return request(`/api/currencies/rate-at?${params}`)
}

export function getCurrencyRateToBase(currencyId, baseCurrencyId, tableRates, fallbackRate) {
  if (!currencyId || String(currencyId) === String(baseCurrencyId)) return 1
  const tableRate = tableRates?.[String(currencyId)]
  if (tableRate != null && tableRate > 0) return Number(tableRate)
  const fallback = Number(fallbackRate)
  return fallback > 0 ? fallback : 1
}

export function convertAmountFromBase(amountInBase, currencyId, baseCurrencyId, rateToBase) {
  const base = Number(amountInBase)
  if (!Number.isFinite(base) || base === 0) return amountInBase
  if (!currencyId || String(currencyId) === String(baseCurrencyId)) return base
  const rate = Number(rateToBase) || 1
  if (rate <= 0) return base
  return base / rate
}

export function convertAmountToBase(amount, currencyId, baseCurrencyId, rateToBase) {
  const value = Number(amount)
  if (!Number.isFinite(value) || value === 0) return amount
  if (!currencyId || String(currencyId) === String(baseCurrencyId)) return value
  const rate = Number(rateToBase) || 1
  return value * rate
}

export function convertBetweenCurrencies(
  amount,
  fromCurrencyId,
  toCurrencyId,
  baseCurrencyId,
  fromRate,
  toRate,
) {
  const inBase = convertAmountToBase(amount, fromCurrencyId, baseCurrencyId, fromRate)
  return convertAmountFromBase(inBase, toCurrencyId, baseCurrencyId, toRate)
}
