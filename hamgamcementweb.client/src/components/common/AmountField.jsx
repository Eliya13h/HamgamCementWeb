import { useCallback, useState } from 'react'
import { formatAmount } from '../../lib/dataTableOptions'
import { parseFormattedAmount } from '../../lib/currencyFormat'

/**
 * فیلد مبلغ: جداکننده هزارگان + اسپینر و اسکرول عددی (type=number هنگام فوکوس).
 */
function AmountField({
  value,
  onChange,
  symbol = '',
  step = 'any',
  min,
  max,
  className = '',
  inputClassName = '',
  required = false,
  disabled = false,
  readOnly = false,
  id,
  placeholder,
}) {
  const [focused, setFocused] = useState(false)

  const toNumericString = useCallback((raw) => {
    if (raw === '' || raw === null || raw === undefined) return ''
    return String(raw)
  }, [])

  const displayValue = readOnly
    ? value === '' || value === null || value === undefined
      ? ''
      : formatAmount(value)
    : focused
      ? toNumericString(value)
      : value === '' || value === null || value === undefined
        ? ''
        : formatAmount(value)

  const commitValue = useCallback(
    (raw) => {
      const parsed = parseFormattedAmount(raw)
      if (parsed === null) return
      onChange(parsed)
    },
    [onChange],
  )

  const handleFocus = () => {
    setFocused(true)
  }

  const handleBlur = (event) => {
    setFocused(false)
    commitValue(event.target.value)
  }

  const handleChange = (event) => {
    if (focused) {
      onChange(event.target.value)
      return
    }
    commitValue(event.target.value)
  }

  const handleWheel = (event) => {
    if (!focused) return
    event.preventDefault()

    const current = Number(parseFormattedAmount(event.target.value) || 0)
    if (Number.isNaN(current)) return

    const stepValue = step === 'any' ? 1 : Number(step) || 1
    const delta = event.deltaY < 0 ? stepValue : -stepValue
    const next = current + delta

    if (min !== undefined && next < Number(min)) return
    if (max !== undefined && next > Number(max)) return

    onChange(String(next))
  }

  return (
    <div className={`amount-field ${className}`.trim()}>
      <div
        className="amount-field-wrap"
        dir="ltr"
        {...(symbol ? { 'data-currency': symbol } : {})}
      >
        <input
          id={id}
          type={readOnly ? 'text' : focused ? 'number' : 'text'}
          inputMode="decimal"
          dir="ltr"
          step={focused && !readOnly ? step : undefined}
          min={focused && !readOnly ? min : undefined}
          max={focused && !readOnly ? max : undefined}
          className={`form-control amount-field-input text-end ${inputClassName}`.trim()}
          value={displayValue}
          onFocus={readOnly || disabled ? undefined : handleFocus}
          onBlur={readOnly || disabled ? undefined : handleBlur}
          onChange={readOnly || disabled ? undefined : handleChange}
          onWheel={readOnly || disabled ? undefined : handleWheel}
          required={required}
          disabled={disabled}
          readOnly={readOnly}
          placeholder={placeholder}
          autoComplete="off"
        />
      </div>
    </div>
  )
}

export default AmountField
