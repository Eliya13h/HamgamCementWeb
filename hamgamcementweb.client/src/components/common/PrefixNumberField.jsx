/** فیلد عددی با پیشوند (مثلاً سمبل واحد اندازه‌گیری) */
function PrefixNumberField({
  prefix = '',
  value,
  onChange,
  min,
  max,
  step = 'any',
  required = false,
  disabled = false,
  inputClassName = '',
  className = '',
}) {
  return (
    <div className={`amount-field ${className}`.trim()}>
      <div
        className="amount-field-wrap"
        dir="ltr"
        {...(prefix ? { 'data-unit': prefix } : {})}
      >
        <input
          type="number"
          inputMode="decimal"
          dir="ltr"
          min={min}
          max={max}
          step={step}
          className={`form-control amount-field-input text-end form-control-sm ${inputClassName}`.trim()}
          value={value}
          onChange={(e) => onChange(e.target.value)}
          required={required}
          disabled={disabled}
        />
      </div>
    </div>
  )
}

export default PrefixNumberField
