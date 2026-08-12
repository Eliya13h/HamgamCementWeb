import { formatAmount } from '../../lib/dataTableOptions'

/** نمایش مبلغ: عدد سپس سمبل ارز (سمبل با CSS ::after) */
function AmountDisplay({ value, symbol, className = '' }) {
  const formatted = formatAmount(value)
  if (formatted === '—') return formatted

  return (
    <span
      dir="ltr"
      className={`amount-cell ${className}`.trim()}
      {...(symbol ? { 'data-currency': symbol } : {})}
    >
      {formatted}
    </span>
  )
}

export default AmountDisplay
