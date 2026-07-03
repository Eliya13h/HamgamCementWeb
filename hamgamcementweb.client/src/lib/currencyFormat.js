import { formatAmount } from './dataTableOptions'

/** تبدیل ورودی فرمت‌شده به مقدار خام برای ارسال به API */
export function parseFormattedAmount(input) {
  if (input === null || input === undefined) return ''
  const cleaned = String(input).replace(/,/g, '').trim()
  if (cleaned === '' || cleaned === '-') return cleaned
  if (!/^-?\d*\.?\d*$/.test(cleaned)) return null
  return cleaned
}

function escapeHtmlAttr(value) {
  return String(value)
    .replace(/&/g, '&amp;')
    .replace(/"/g, '&quot;')
    .replace(/</g, '&lt;')
}

/** HTML سلول مبلغ: فقط عدد؛ سمبل با data-currency و ::after */
export function amountWithSymbolHtml(value, symbol) {
  const formatted = formatAmount(value)
  if (formatted === '—') return formatted
  const currencyAttr = symbol ? ` data-currency="${escapeHtmlAttr(symbol)}"` : ''
  return `<span dir="ltr" class="amount-cell"${currencyAttr}>${formatted}</span>`
}

/** رندر DataTable: display با سمبل، sort/filter/type مقدار عددی خام */
export function makeAmountCurrencyRender(symbolOrGetter) {
  const getSymbol =
    typeof symbolOrGetter === 'function' ? symbolOrGetter : () => symbolOrGetter

  return (data, type) => {
    if (type === 'sort' || type === 'type' || type === 'filter') {
      if (data === null || data === undefined || data === '') return 0
      const num = Number(data)
      return Number.isNaN(num) ? 0 : num
    }
    return amountWithSymbolHtml(data, getSymbol())
  }
}

/** HTML سلول بلانس: علامت + یا - حتماً قبل از عدد */
export function signedBalanceHtml(balance, accountStatusCode, symbol) {
  const num = Math.abs(Number(balance))
  if (accountStatusCode === 'settled' || num === 0) {
    return amountWithSymbolHtml(0, symbol)
  }

  const formatted = formatAmount(num)
  if (formatted === '—') return formatted

  const sign = accountStatusCode === 'debtor' ? '-' : accountStatusCode === 'creditor' ? '+' : ''
  const currencyAttr = symbol ? ` data-currency="${escapeHtmlAttr(symbol)}"` : ''
  return `<span dir="ltr" class="amount-cell"${currencyAttr}>${sign}${formatted}</span>`
}

/** رندر DataTable برای ستون بلانس با علامت قبل از عدد */
export function makeSignedBalanceRender(symbolOrGetter) {
  const getSymbol =
    typeof symbolOrGetter === 'function' ? symbolOrGetter : () => symbolOrGetter

  return (data, type, row) => {
    const balance = data ?? row?.balance ?? 0
    const code = row?.accountStatusCode ?? 'settled'

    if (type === 'sort' || type === 'type' || type === 'filter') {
      const num = Math.abs(Number(balance))
      if (Number.isNaN(num)) return 0
      if (code === 'debtor') return -num
      if (code === 'creditor') return num
      return 0
    }

    return signedBalanceHtml(balance, code, getSymbol())
  }
}
