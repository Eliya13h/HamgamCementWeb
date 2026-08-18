import { useCallback, useEffect, useMemo, useRef, useState } from 'react'
import MultiDatePicker from 'react-multi-date-picker'
import {
  addJalaliUnit,
  afghanSolarLocale,
  currentJalaliParts,
  isoToJalaliObject,
  isoToJalaliParts,
  jalaliDaysInMonth,
  jalaliObjectToIso,
  jalaliPartsToIso,
  MAX_JALALI_YEAR,
  MIN_JALALI_YEAR,
  persian,
  toLatinDigits,
} from '../../lib/afghanSolarCalendar'
import { PERSIAN_VALIDATION } from '../../lib/persianFormValidity'

const DatePicker = MultiDatePicker.default ?? MultiDatePicker

const PARTS = ['year', 'month', 'day']
const PART_META = {
  year: { label: 'سال', maxLength: 4, placeholder: '----', min: MIN_JALALI_YEAR, max: MAX_JALALI_YEAR },
  month: { label: 'ماه', maxLength: 2, placeholder: '--', min: 1, max: 12 },
  day: { label: 'روز', maxLength: 2, placeholder: '--', min: 1, max: 31 },
}

function CalendarGlyph() {
  return (
    <svg className="hc-jalali-glyph" viewBox="0 0 24 24" aria-hidden="true" focusable="false">
      <rect x="3.25" y="5.25" width="17.5" height="15.5" rx="2.2" />
      <path d="M8 3.5v3.5M16 3.5v3.5M3.25 10.25h17.5" />
      <path d="M8 14h.01M12 14h.01M16 14h.01M8 17.25h.01M12 17.25h.01M16 17.25h.01" />
    </svg>
  )
}

function padPart(part, value) {
  if (value == null || value === '') return PART_META[part].placeholder
  const text = String(value)
  return part === 'year' ? text : text.padStart(2, '0')
}

function parsePastedDate(text) {
  const latin = toLatinDigits(text).trim()
  const match = latin.match(/(\d{4})\D+(\d{1,2})\D+(\d{1,2})/)
  if (!match) return ''
  return jalaliPartsToIso(match[1], match[2], match[3])
}

function shouldAutoAdvance(part, digits, parts) {
  if (part === 'year') return digits.length >= 4
  if (part === 'month') return digits.length >= 2 || (digits.length === 1 && Number(digits) >= 2)
  if (digits.length >= 2) return true
  const maxDay = jalaliDaysInMonth(parts.year, parts.month)
  return digits.length === 1 && Number(digits) * 10 > maxDay
}

function clampPartValue(part, raw, parts) {
  const n = parseInt(raw, 10)
  if (!Number.isFinite(n)) return null
  if (part === 'year') return Math.min(MAX_JALALI_YEAR, Math.max(MIN_JALALI_YEAR, n))
  if (part === 'month') return Math.min(12, Math.max(1, n))
  return Math.min(jalaliDaysInMonth(parts.year, parts.month), Math.max(1, n))
}

function JalaliSegmentedInput({
  isoValue,
  onIsoChange,
  open,
  toggleCalendar,
  closeCalendar,
  disabled,
  required,
  requiredMessage,
  small,
  inputClass,
  placeholder,
}) {
  const yearRef = useRef(null)
  const monthRef = useRef(null)
  const dayRef = useRef(null)
  const [draft, setDraft] = useState(null)

  const parts = useMemo(() => isoToJalaliParts(isoValue), [isoValue])
  const liveParts = parts ?? currentJalaliParts()

  useEffect(() => {
    setDraft(null)
  }, [isoValue])

  const inputOf = useCallback((part) => {
    if (part === 'year') return yearRef.current
    if (part === 'month') return monthRef.current
    return dayRef.current
  }, [])

  const focusPart = useCallback((part) => {
    const el = inputOf(part)
    if (!el) return
    el.focus()
    el.select()
  }, [inputOf])

  const movePart = useCallback((from, delta) => {
    const index = PARTS.indexOf(from) + delta
    if (index < 0 || index >= PARTS.length) return
    focusPart(PARTS[index])
  }, [focusPart])

  const commitDigits = useCallback(
    (part, digits, { advance } = {}) => {
      if (!digits) {
        setDraft(null)
        return false
      }
      if (part === 'year' && digits.length < 4) return false
      const base = parts ?? currentJalaliParts()
      const nextValue = clampPartValue(part, digits, base)
      if (nextValue == null) return false
      onIsoChange(jalaliPartsToIso(
        part === 'year' ? nextValue : base.year,
        part === 'month' ? nextValue : base.month,
        part === 'day' ? nextValue : base.day,
      ))
      setDraft(null)
      if (advance) movePart(part, 1)
      return true
    },
    [movePart, onIsoChange, parts],
  )

  const displayOf = (part) => {
    if (draft?.part === part) return draft.text
    return padPart(part, parts?.[part])
  }

  const handleToggle = (event) => {
    event.preventDefault()
    event.stopPropagation()
    toggleCalendar()
  }

  const handleSegmentBlur = (part) => {
    if (draft?.part !== part) return
    commitDigits(part, draft.text)
    setDraft(null)
  }

  const handleSegmentChange = (event, part) => {
    const digits = toLatinDigits(event.target.value).replace(/\D/g, '').slice(0, PART_META[part].maxLength)
    setDraft({ part, text: digits })
    if (shouldAutoAdvance(part, digits, liveParts)) {
      commitDigits(part, digits, { advance: true })
    }
  }

  const handleSegmentKeyDown = (event, part) => {
    if (disabled) return

    if (event.key === 'F4' || (event.altKey && (event.key === 'ArrowDown' || event.key === 'ArrowUp'))) {
      event.preventDefault()
      event.stopPropagation()
      toggleCalendar()
      return
    }

    if (event.key === 'ArrowUp' || event.key === 'ArrowDown') {
      event.preventDefault()
      event.stopPropagation()
      setDraft(null)
      onIsoChange(addJalaliUnit(isoValue, part, event.key === 'ArrowUp' ? 1 : -1))
      return
    }

    if (event.key === 'ArrowLeft') {
      event.preventDefault()
      movePart(part, -1)
      return
    }

    if (event.key === 'ArrowRight') {
      event.preventDefault()
      movePart(part, 1)
      return
    }

    if (event.key === 'Home') {
      event.preventDefault()
      focusPart('year')
      return
    }

    if (event.key === 'End') {
      event.preventDefault()
      focusPart('day')
      return
    }

    if (event.key === 'Escape' && open) {
      event.preventDefault()
      closeCalendar()
    }
  }

  const handlePaste = (event) => {
    const iso = parsePastedDate(event.clipboardData.getData('text'))
    if (!iso) return
    event.preventDefault()
    setDraft(null)
    onIsoChange(iso)
  }

  const handleInvalid = (event) => {
    event.target.setCustomValidity(requiredMessage || PERSIAN_VALIDATION.required)
    yearRef.current?.focus()
  }

  const fieldClass = [
    'hc-jalali-field',
    small ? 'hc-jalali-field-sm' : '',
    disabled ? 'is-disabled' : '',
    open ? 'is-open' : '',
    inputClass ?? '',
  ].filter(Boolean).join(' ')

  return (
    <div
      className={fieldClass}
      role="group"
      aria-label={placeholder || 'تاریخ'}
      onPaste={handlePaste}
    >
      <button
        type="button"
        className="hc-jalali-toggle"
        onMouseDown={(event) => event.preventDefault()}
        onClick={handleToggle}
        disabled={disabled}
        aria-label={open ? 'بستن تقویم' : 'باز کردن تقویم'}
        aria-expanded={open}
        tabIndex={-1}
      >
        <CalendarGlyph />
      </button>

      <div className="hc-jalali-segments">
        {PARTS.map((part, index) => (
          <span key={part} className="hc-jalali-part">
            {index > 0 ? <span className="hc-jalali-sep">/</span> : null}
            <input
              ref={part === 'year' ? yearRef : part === 'month' ? monthRef : dayRef}
              className={`hc-jalali-seg hc-jalali-seg-${part}${!parts && draft?.part !== part ? ' is-placeholder' : ''}`}
              type="text"
              inputMode="numeric"
              autoComplete="off"
              spellCheck="false"
              role="spinbutton"
              aria-label={PART_META[part].label}
              aria-valuemin={part === 'day' && parts ? 1 : PART_META[part].min}
              aria-valuemax={part === 'day' && parts ? jalaliDaysInMonth(parts.year, parts.month) : PART_META[part].max}
              aria-valuenow={parts?.[part] || undefined}
              maxLength={PART_META[part].maxLength}
              value={displayOf(part)}
              disabled={disabled}
              onFocus={(event) => {
                setDraft(null)
                event.target.select()
              }}
              onBlur={() => handleSegmentBlur(part)}
              onChange={(event) => handleSegmentChange(event, part)}
              onKeyDown={(event) => handleSegmentKeyDown(event, part)}
            />
          </span>
        ))}
      </div>

      <input
        className="hc-jalali-value"
        tabIndex={-1}
        value={isoValue || ''}
        required={required}
        disabled={disabled}
        aria-hidden="true"
        data-required-message={required ? (requiredMessage || PERSIAN_VALIDATION.required) : undefined}
        onChange={() => {}}
        onInvalid={required ? handleInvalid : undefined}
        onInput={(event) => event.target.setCustomValidity('')}
      />
    </div>
  )
}

function JalaliDateField({
  value,
  onChange,
  required,
  requiredMessage = 'لطفاً تاریخ را انتخاب کنید.',
  disabled = false,
  placeholder,
  className,
  inputClass,
  containerClassName,
  small = false,
}) {
  const pickerRef = useRef(null)
  const ignoreToggleUntilRef = useRef(0)
  const [open, setOpen] = useState(false)
  const jalaliValue = useMemo(() => isoToJalaliObject(value), [value])

  const handleChange = useCallback(
    (date) => {
      onChange(jalaliObjectToIso(date))
    },
    [onChange],
  )

  const closeCalendar = useCallback(() => {
    pickerRef.current?.closeCalendar()
  }, [])

  const toggleCalendar = useCallback(() => {
    if (disabled) return
    if (performance.now() < ignoreToggleUntilRef.current) return
    const picker = pickerRef.current
    if (!picker) return
    if (picker.isOpen) picker.closeCalendar()
    else picker.openCalendar()
  }, [disabled])

  const renderInput = useCallback(
    () => (
      <JalaliSegmentedInput
        isoValue={value}
        onIsoChange={onChange}
        open={open}
        toggleCalendar={toggleCalendar}
        closeCalendar={closeCalendar}
        disabled={disabled}
        required={required}
        requiredMessage={requiredMessage}
        small={small}
        inputClass={inputClass}
        placeholder={placeholder}
      />
    ),
    [closeCalendar, disabled, inputClass, onChange, open, placeholder, required, requiredMessage, small, toggleCalendar, value],
  )

  return (
    <DatePicker
      ref={pickerRef}
      value={jalaliValue || undefined}
      onChange={handleChange}
      render={renderInput}
      onOpen={() => setOpen(true)}
      onClose={() => {
        setOpen(false)
        ignoreToggleUntilRef.current = performance.now() + 280
      }}
      calendar={persian}
      locale={afghanSolarLocale}
      format="YYYY/MM/DD"
      calendarPosition="bottom-right"
      className={`hc-rmdp ${className ?? ''}`.trim()}
      containerClassName={`hc-jalali-picker ${small ? 'hc-jalali-picker-sm' : ''} ${containerClassName ?? ''}`.trim()}
      placeholder={placeholder ?? 'انتخاب تاریخ...'}
      disabled={disabled}
      arrow={false}
      editable={false}
      portal
      fixMainPosition={false}
      zIndex={2100}
    />
  )
}

export default JalaliDateField
