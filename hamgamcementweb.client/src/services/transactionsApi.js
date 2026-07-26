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
    post: (id) => request(`${base}/${id}/post`, { method: 'POST' }),
    createDataTableAjax: (onError) => createDataTableAjax(base, onError),
    fetchReturnableLines: (id) => request(`${base}/${id}/returnable-lines`),
    fetchReturns: (id) => request(`${base}/${id}/returns`),
    createReturn: (id, payload) =>
      request(`${base}/${id}/returns`, { method: 'POST', body: JSON.stringify(payload) }),
  }
}

export const INVOICE_DOCUMENT_TYPE = {
  Invoice: 1,
  PurchaseReturn: 2,
  SaleReturn: 3,
}

export const PURCHASE_ENTRY_SOURCE = {
  Market: 1,
  Production: 2,
}

export function getInvoiceDocumentTypeLabel(documentType, kind = 'purchase') {
  if (documentType === INVOICE_DOCUMENT_TYPE.PurchaseReturn) return 'برگشت از خرید'
  if (documentType === INVOICE_DOCUMENT_TYPE.SaleReturn) return 'برگشت از فروش'
  return kind === 'purchase' ? 'فاکتور خرید' : 'فاکتور فروش'
}

export function renderInvoiceDocumentTypeBadge(documentType) {
  if (documentType === INVOICE_DOCUMENT_TYPE.PurchaseReturn) {
    return '<span class="badge badge-warning">برگشت خرید</span>'
  }
  if (documentType === INVOICE_DOCUMENT_TYPE.SaleReturn) {
    return '<span class="badge badge-warning">برگشت فروش</span>'
  }
  return '<span class="badge badge-secondary">فاکتور</span>'
}

export function getPurchaseInvoicePrintUrl(purchaseInvoiceId) {
  return `/report-viewer/invoice?purchaseInvoiceId=${purchaseInvoiceId}`
}

export function getSaleInvoicePrintUrl(saleInvoiceId) {
  return `/report-viewer/sale-invoice?saleInvoiceId=${saleInvoiceId}`
}

export const purchaseInvoicesApi = {
  ...makeResource('/api/transactions/purchase-invoices'),
  getById: (id) => request(`/api/transactions/purchase-invoices/${id}`),
  fetchNextCodePreview: () => request('/api/transactions/purchase-invoices/next-code-preview'),
}

export const saleInvoicesApi = {
  ...makeResource('/api/transactions/sale-invoices'),
  getById: (id) => request(`/api/transactions/sale-invoices/${id}`),
  previewProfit: (payload) =>
    request('/api/transactions/sale-invoices/preview-profit', {
      method: 'POST',
      body: JSON.stringify(payload),
    }),
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

export const INVOICE_STATUSES = [
  { value: 1, label: 'استعلام قیمت' },
  { value: 2, label: 'پیش فاکتور' },
  { value: 3, label: 'آردر' },
  { value: 4, label: 'فاکتور' },
]

export function toBaseQuantity(quantity, meaurmentId, meaurments) {
  const qty = Number(quantity) || 0
  if (!meaurmentId || !meaurments?.length) return qty

  const unit = meaurments.find((m) => String(m.value) === String(meaurmentId))
  const factor = Number(unit?.factorToBase) || 1
  return qty * factor
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

export function convertUnitPrice(price, fromMeaurmentId, toMeaurmentId, meaurments) {
  const amount = Number(price)
  if (!amount || !fromMeaurmentId || !toMeaurmentId || String(fromMeaurmentId) === String(toMeaurmentId)) {
    return price
  }

  const from = meaurments.find((m) => String(m.value) === String(fromMeaurmentId))
  const to = meaurments.find((m) => String(m.value) === String(toMeaurmentId))
  if (!from || !to) return price

  const fromFactor = Number(from.factorToBase) || 1
  const toFactor = Number(to.factorToBase) || 1
  if (fromFactor <= 0 || toFactor <= 0) return price

  const converted = amount * (toFactor / fromFactor)
  return Number.isFinite(converted) ? converted : price
}

export function buildPurchasePayload(header, lines, exchangeRate) {
  const payload = {
    supplierId: Number(header.supplierId),
    warehouseId: Number(header.warehouseId),
    invoiceDate: header.invoiceDate,
    status: Number(header.status) || 4,
    currencyId: Number(header.currencyId),
    entrySource: PURCHASE_ENTRY_SOURCE.Market,
    productionBatchId: null,
    description: header.description || null,
    paidAmount: Number(header.paidAmount) || 0,
    freightMode: Number(header.freightMode) || 0,
    freightRatePerTon: Number(header.freightRatePerTon) || 0,
    freightWeightTon: Number(header.freightWeightTon) || 0,
    freightVehicleId: header.freightVehicleId ? Number(header.freightVehicleId) : null,
    freightCarrierName: header.freightCarrierName || null,
    items: lines.map((line) => ({
      purchaseItemId: line.purchaseItemId ?? null,
      productId: Number(line.productId),
      meaurmentId: Number(line.meaurmentId),
      quantity: Number(line.quantity),
      unitPrice: Number(line.unitPrice),
    })),
  }

  const rate = Number(exchangeRate)
  if (rate > 0) {
    payload.baseUnitsPerUnit = rate
  }

  return payload
}

export function buildSalePayload(header, lines, exchangeRate) {
  const payload = {
    customerId: Number(header.customerId),
    warehouseId: Number(header.warehouseId),
    invoiceDate: header.invoiceDate,
    status: Number(header.status) || 1,
    currencyId: Number(header.currencyId),
    description: header.description || null,
    paidAmount: Number(header.paidAmount) || 0,
    freightMode: Number(header.freightMode) || 0,
    freightRatePerTon: Number(header.freightRatePerTon) || 0,
    freightWeightTon: Number(header.freightWeightTon) || 0,
    freightVehicleId: header.freightVehicleId ? Number(header.freightVehicleId) : null,
    freightCarrierName: header.freightCarrierName || null,
    items: lines.map((line) => ({
      salesItemId: line.salesItemId ?? null,
      productId: Number(line.productId),
      meaurmentId: Number(line.meaurmentId),
      quantity: Number(line.quantity),
      unitPrice: Number(line.unitPrice),
    })),
  }

  const rate = Number(exchangeRate)
  if (rate > 0) {
    payload.baseUnitsPerUnit = rate
  }

  return payload
}

export function calcLineTotals(
  lines,
  rateSnapshot,
  customRate,
  meaurments = [],
  baseCurrencyId,
  invoiceCurrencyId,
) {
  let rate = 1
  if (
    invoiceCurrencyId &&
    baseCurrencyId &&
    String(invoiceCurrencyId) !== String(baseCurrencyId)
  ) {
    const manualRate = Number(customRate)
    rate = manualRate > 0 ? manualRate : Number(rateSnapshot?.baseUnitsPerUnit) || 1
  }

  return lines.map((line) => {
    const quantityInBase = toBaseQuantity(line.quantity, line.meaurmentId, meaurments)
    const price = Number(line.unitPrice) || 0
    const priceInBase =
      line.unitPriceInBase != null && line.unitPriceInBase !== ''
        ? Number(line.unitPriceInBase) || 0
        : String(invoiceCurrencyId) === String(baseCurrencyId)
          ? price
          : price * rate
    const lineTotal = quantityInBase * price
    const lineTotalBase = quantityInBase * priceInBase
    return { ...line, quantityInBase, lineTotal, lineTotalBase }
  })
}

export function sumTotals(computedLines) {
  return computedLines.reduce(
    (acc, line) => ({
      total: acc.total + line.lineTotal,
      totalBase: acc.totalBase + line.lineTotalBase,
    }),
    { total: 0, totalBase: 0 },
  )
}
