import { useCallback, useRef, useState } from 'react'
import { formatAmount } from '../../lib/dataTableOptions'
import { parseFormattedAmount } from '../../lib/currencyFormat'
import { PERSIAN_VALIDATION } from '../../lib/persianFormValidity'

function resolveStep(step) {
  if (step === 'any' || step === undefined || step === null || step === '') return 1
  const n = Number(step)
  return Number.isFinite(n) && n > 0 ? n : 1
}

/** فرمت زنده با حفظ اعشارِ در حال تایپ (مثلاً 1,234.) */
function formatLiveAmount(raw) {
  if (raw === '' || raw === null || raw === undefined) return ''
  const cleaned = String(raw).replace(/,/g, '').trim()
  if (cleaned === '' || cleaned === '-') return cleaned
  if (!/^-?\d*\.?\d*$/.test(cleaned)) return null

  const negative = cleaned.startsWith('-')
  const unsigned = negative ? cleaned.slice(1) : cleaned
  const endsWithDot = unsigned.endsWith('.')
  const [intPartRaw = '', fracPart] = unsigned.split('.')
  const intDigits = intPartRaw.replace(/\D/g, '')
  const grouped =
    intDigits === ''
      ? endsWithDot || fracPart !== undefined
        ? '0'
        : ''
      : intDigits.replace(/\B(?=(\d{3})+(?!\d))/g, ',')

  let result = negative ? `-${grouped}` : grouped
  if (endsWithDot) result += '.'
  else if (fracPart !== undefined) result += `.${fracPart}`
  return result
}

function clampValue(next, min, max) {
  let value = next
  if (min !== undefined && value < Number(min)) value = Number(min)
  if (max !== undefined && value > Number(max)) value = Number(max)
  return value
}

/**
 * فیلد مبلغ: جداکننده هزارگان زنده + اسپینر با step قابل تنظیم.
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
  requiredMessage = PERSIAN_VALIDATION.required,
  disabled = false,
  readOnly = false,
  id,
  placeholder,
}) {
  const inputRef = useRef(null)
  const [focused, setFocused] = useState(false)
  const stepValue = resolveStep(step)

  const displayValue =
    value === '' || value === null || value === undefined
      ? ''
      : focused
        ? formatLiveAmount(value) ?? String(value).replace(/,/g, '')
        : formatAmount(value)

  const commitNumeric = useCallback(
    (numeric) => {
      if (Number.isNaN(numeric)) return
      const clamped = clampValue(numeric, min, max)
      // عدد صحیح بدون اعشارِ الکی؛ اعشار واقعی حفظ می‌شود
      const asString = Number.isInteger(clamped) ? String(clamped) : String(clamped)
      onChange(asString)
    },
    [onChange, min, max],
  )

  const bump = useCallback(
    (direction) => {
      if (disabled || readOnly) return
      const current = Number(parseFormattedAmount(value) || 0)
      const base = Number.isNaN(current) ? 0 : current
      commitNumeric(base + direction * stepValue)
    },
    [disabled, readOnly, value, stepValue, commitNumeric],
  )

  const handleFocus = () => setFocused(true)

  const handleBlur = (event) => {
    setFocused(false)
    const parsed = parseFormattedAmount(event.target.value)
    if (parsed === null || parsed === '' || parsed === '-') {
      onChange(parsed === null ? value : '')
      return
    }
    commitNumeric(Number(parsed))
  }

  const handleChange = (event) => {
    const next = formatLiveAmount(event.target.value)
    if (next === null) return
    // به parent مقدار خام بدون ویرگول می‌دهیم؛ فقط UI فرمت می‌شود
    const raw = parseFormattedAmount(next)
    if (raw === null) return
    onChange(raw)
  }

  const handleKeyDown = (event) => {
    if (event.key === 'ArrowUp') {
      event.preventDefault()
      bump(1)
    } else if (event.key === 'ArrowDown') {
      event.preventDefault()
      bump(-1)
    }
  }

  const handleWheel = (event) => {
    if (!focused || disabled || readOnly) return
    event.preventDefault()
    bump(event.deltaY < 0 ? 1 : -1)
  }

  return (
    <div className={`amount-field ${className}`.trim()}>
      <div
        className="amount-field-wrap"
        dir="ltr"
        {...(symbol ? { 'data-currency': symbol } : {})}
      >
        <input
          ref={inputRef}
          id={id}
          type="text"
          inputMode="decimal"
          dir="ltr"
          className={`form-control amount-field-input text-end ${inputClassName}`.trim()}
          value={displayValue}
          onFocus={readOnly || disabled ? undefined : handleFocus}
          onBlur={readOnly || disabled ? undefined : handleBlur}
          onChange={readOnly || disabled ? undefined : handleChange}
          onKeyDown={readOnly || disabled ? undefined : handleKeyDown}
          onWheel={readOnly || disabled ? undefined : handleWheel}
          required={required}
          disabled={disabled}
          readOnly={readOnly}
          placeholder={placeholder}
          autoComplete="off"
          {...(required
            ? {
                'data-required-message': requiredMessage,
                onInvalid: (event) => {
                  event.target.setCustomValidity(requiredMessage)
                },
                onInput: (event) => {
                  event.target.setCustomValidity('')
                },
              }
            : {})}
        />
        {!readOnly && !disabled && (
          <div className="amount-field-spinners" aria-hidden="true">
            <button
              type="button"
              className="amount-field-spinner-btn"
              tabIndex={-1}
              onMouseDown={(e) => e.preventDefault()}
              onClick={() => {
                bump(1)
                inputRef.current?.focus()
              }}
              title={`+${stepValue}`}
            >
              ▲
            </button>
            <button
              type="button"
              className="amount-field-spinner-btn"
              tabIndex={-1}
              onMouseDown={(e) => e.preventDefault()}
              onClick={() => {
                bump(-1)
                inputRef.current?.focus()
              }}
              title={`-${stepValue}`}
            >
              ▼
            </button>
          </div>
        )}
      </div>
    </div>
  )
}

export default AmountField
