/** پیام‌های پیش‌فرض ولیدیشن فرم (فارسی) */
export const PERSIAN_VALIDATION = {
  required: 'لطفاً این فیلد را تکمیل کنید.',
  requiredSelect: 'لطفاً این فیلد را انتخاب کنید.',
  invalidNumber: 'لطفاً یک عدد معتبر وارد کنید.',
  minZero: 'مقدار نمی‌تواند منفی باشد.',
}

/**
 * props برای input/select تا پیام HTML5 validation فارسی شود.
 * @param {string} message
 */
export function persianValidity(message = PERSIAN_VALIDATION.required) {
  return {
    'data-required-message': message,
    onInvalid: (event) => {
      event.target.setCustomValidity(message)
    },
    onInput: (event) => {
      event.target.setCustomValidity('')
    },
  }
}

/**
 * آیا ValidityState نیاز به پیام فارسی سفارشی دارد؟
 * @param {Pick<ValidityState, 'valueMissing' | 'rangeUnderflow' | 'badInput' | 'typeMismatch' | 'stepMismatch'>} validity
 */
export function needsPersianCustomValidity(validity) {
  if (!validity) return false
  return Boolean(
    validity.valueMissing
      || validity.rangeUnderflow
      || validity.badInput
      || validity.typeMismatch
      || validity.stepMismatch,
  )
}

/**
 * قبل از checkValidity/reportValidity پیام فارسی را روی فیلدهای دارای data-required-message می‌گذارد.
 * @param {ParentNode | null | undefined} formEl
 * @returns {number} تعداد فیلدهایی که پیام سفارشی گرفتند
 */
export function applyPersianValidity(formEl) {
  if (!formEl?.querySelectorAll) return 0

  let applied = 0
  for (const el of formEl.querySelectorAll('[data-required-message]')) {
    if (el.disabled) continue
    const message = el.dataset.requiredMessage || PERSIAN_VALIDATION.required
    el.setCustomValidity('')
    if (needsPersianCustomValidity(el.validity)) {
      el.setCustomValidity(message)
      applied += 1
    }
  }
  return applied
}

/**
 * اولین پیام خطای فارسی فرم (بعد از applyPersianValidity).
 * @param {HTMLFormElement | null | undefined} formEl
 */
export function getFirstPersianValidationMessage(formEl) {
  if (!formEl) return PERSIAN_VALIDATION.required
  applyPersianValidity(formEl)
  const invalid = formEl.querySelector?.(':invalid')
  return invalid?.validationMessage || PERSIAN_VALIDATION.required
}

/**
 * اگر فرم نامعتبر است، پیام فارسی می‌دهد؛ وگرنه null.
 * @param {HTMLFormElement | null | undefined} formEl
 */
export function validateFormPersian(formEl) {
  if (!formEl) return null
  applyPersianValidity(formEl)
  if (formEl.checkValidity()) return null
  const invalid = formEl.querySelector(':invalid')
  return invalid?.validationMessage || PERSIAN_VALIDATION.required
}
