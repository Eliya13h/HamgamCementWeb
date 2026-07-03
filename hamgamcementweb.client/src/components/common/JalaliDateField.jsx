import { useCallback, useMemo } from 'react'
import MultiDatePicker from 'react-multi-date-picker'
import {
  afghanSolarLocale,
  isoToJalaliString,
  jalaliObjectToIso,
  persian,
} from '../../lib/afghanSolarCalendar'

const DatePicker = MultiDatePicker.default ?? MultiDatePicker

function JalaliDateField({
  value,
  onChange,
  required,
  placeholder,
  className,
  inputClass,
  containerClassName,
  small = false,
}) {
  const displayValue = useMemo(() => isoToJalaliString(value), [value])

  const handleChange = useCallback(
    (date) => {
      onChange(jalaliObjectToIso(date))
    },
    [onChange],
  )

  return (
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
      arrow={false}
      editable={false}
      scrollSensitive={false}
      zIndex={1060}
    />
  )
}

export default JalaliDateField
