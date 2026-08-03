import { useCallback, useEffect, useMemo, useRef } from 'react'
import MultiDatePicker from 'react-multi-date-picker'
import {
  afghanSolarLocale,
  isoToJalaliString,
  jalaliObjectToIso,
  persian,
} from '../../lib/afghanSolarCalendar'
import { PERSIAN_VALIDATION } from '../../lib/persianFormValidity'

const DatePicker = MultiDatePicker.default ?? MultiDatePicker

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
  const displayValue = useMemo(() => isoToJalaliString(value), [value])
  const wrapRef = useRef(null)

  const handleChange = useCallback(
    (date) => {
      onChange(jalaliObjectToIso(date))
    },
    [onChange],
  )

  // استامپ پیام فارسی روی input داخلی DatePicker
  useEffect(() => {
    const input = wrapRef.current?.querySelector('input')
    if (!input) return
    if (required) {
      input.setAttribute('data-required-message', requiredMessage || PERSIAN_VALIDATION.required)
      input.oninvalid = () => {
        input.setCustomValidity(requiredMessage || PERSIAN_VALIDATION.required)
      }
      input.oninput = () => {
        input.setCustomValidity('')
      }
    } else {
      input.removeAttribute('data-required-message')
      input.oninvalid = null
      input.oninput = null
      input.setCustomValidity('')
    }
  }, [required, requiredMessage, displayValue])

  return (
    <div ref={wrapRef}>
      <DatePicker
        value={displayValue || undefined}
        onChange={handleChange}
        calendar={persian}
        locale={afghanSolarLocale}
        format="YYYY/MM/DD"
        calendarPosition="bottom-right"
        className={`hc-rmdp ${className ?? ''}`.trim()}
        containerClassName={`hc-jalali-picker ${small ? 'hc-jalali-picker-sm' : ''} ${containerClassName ?? ''}`.trim()}
        inputClass={inputClass ?? 'form-control hc-jalali-input'}
        placeholder={placeholder ?? 'انتخاب تاریخ...'}
        required={required}
        disabled={disabled}
        arrow={false}
        editable={false}
        scrollSensitive={false}
        zIndex={1060}
      />
    </div>
  )
}

export default JalaliDateField
