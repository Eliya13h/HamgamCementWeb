import { describe, expect, it, vi } from 'vitest'
import {
  PERSIAN_VALIDATION,
  applyPersianValidity,
  getFirstPersianValidationMessage,
  needsPersianCustomValidity,
  persianValidity,
  validateFormPersian,
} from './persianFormValidity.js'

function makeField({
  message = PERSIAN_VALIDATION.required,
  disabled = false,
  validity = {},
} = {}) {
  let custom = ''
  return {
    disabled,
    dataset: { requiredMessage: message },
    validity: {
      valueMissing: false,
      rangeUnderflow: false,
      badInput: false,
      typeMismatch: false,
      stepMismatch: false,
      ...validity,
    },
    setCustomValidity: vi.fn((value) => {
      custom = value
    }),
    get customValidity() {
      return custom
    },
  }
}

function makeForm(fields) {
  return {
    querySelectorAll: (selector) => {
      if (selector === '[data-required-message]') return fields
      return []
    },
    querySelector: (selector) => {
      if (selector === ':invalid') {
        return fields.find((f) => f.customValidity) ?? null
      }
      return null
    },
    checkValidity: () => fields.every((f) => !f.customValidity),
  }
}

describe('persianValidity', () => {
  it('props فارسی و پاک‌کردن پیام روی input را برمی‌گرداند', () => {
    const props = persianValidity('لطفاً نام را وارد کنید.')
    expect(props['data-required-message']).toBe('لطفاً نام را وارد کنید.')

    const target = { setCustomValidity: vi.fn() }
    props.onInvalid({ target })
    expect(target.setCustomValidity).toHaveBeenCalledWith('لطفاً نام را وارد کنید.')

    props.onInput({ target })
    expect(target.setCustomValidity).toHaveBeenCalledWith('')
  })

  it('پیام پیش‌فرض required را دارد', () => {
    expect(persianValidity()['data-required-message']).toBe(PERSIAN_VALIDATION.required)
  })
})

describe('needsPersianCustomValidity', () => {
  it('برای valueMissing و badInput و typeMismatch true است', () => {
    expect(needsPersianCustomValidity({ valueMissing: true })).toBe(true)
    expect(needsPersianCustomValidity({ badInput: true })).toBe(true)
    expect(needsPersianCustomValidity({ typeMismatch: true })).toBe(true)
    expect(needsPersianCustomValidity({ rangeUnderflow: true })).toBe(true)
    expect(needsPersianCustomValidity({})).toBe(false)
    expect(needsPersianCustomValidity(null)).toBe(false)
  })
})

describe('applyPersianValidity', () => {
  it('روی فیلد خالی پیام فارسی می‌گذارد', () => {
    const field = makeField({
      message: 'لطفاً مقدار تولید را وارد کنید.',
      validity: { valueMissing: true },
    })
    const count = applyPersianValidity(makeForm([field]))
    expect(count).toBe(1)
    expect(field.setCustomValidity).toHaveBeenCalledWith('')
    expect(field.setCustomValidity).toHaveBeenCalledWith('لطفاً مقدار تولید را وارد کنید.')
    expect(field.customValidity).toBe('لطفاً مقدار تولید را وارد کنید.')
  })

  it('فیلد معتبر را خالی می‌گذارد', () => {
    const field = makeField({ validity: { valueMissing: false } })
    applyPersianValidity(makeForm([field]))
    expect(field.customValidity).toBe('')
  })

  it('فیلد disabled را نادیده می‌گیرد', () => {
    const field = makeField({
      disabled: true,
      validity: { valueMissing: true },
    })
    const count = applyPersianValidity(makeForm([field]))
    expect(count).toBe(0)
    expect(field.setCustomValidity).not.toHaveBeenCalled()
  })

  it('برای form خالی صفر برمی‌گرداند', () => {
    expect(applyPersianValidity(null)).toBe(0)
    expect(applyPersianValidity(undefined)).toBe(0)
  })
})

describe('validateFormPersian / getFirstPersianValidationMessage', () => {
  it('وقتی فرم معتبر است null برمی‌گرداند', () => {
    const field = makeField({ validity: { valueMissing: false } })
    const form = makeForm([field])
    expect(validateFormPersian(form)).toBeNull()
  })

  it('اولین پیام فارسی فیلد نامعتبر را برمی‌گرداند', () => {
    const field = makeField({
      message: 'لطفاً فرمول ساخت را انتخاب کنید.',
      validity: { valueMissing: true },
    })
    // validationMessage مرورگر را شبیه‌سازی می‌کنیم
    Object.defineProperty(field, 'validationMessage', {
      get() {
        return this.customValidity
      },
    })
    const form = makeForm([field])
    expect(validateFormPersian(form)).toBe('لطفاً فرمول ساخت را انتخاب کنید.')
    expect(getFirstPersianValidationMessage(form)).toBe('لطفاً فرمول ساخت را انتخاب کنید.')
  })

  it('برای form تهی پیام پیش‌فرض دارد', () => {
    expect(validateFormPersian(null)).toBeNull()
    expect(getFirstPersianValidationMessage(null)).toBe(PERSIAN_VALIDATION.required)
  })
})
