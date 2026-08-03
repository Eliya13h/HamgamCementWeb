import { useEffect, useRef } from 'react'

/**
 * کنترل‌های قابل‌فوکوس مثل ترتیب Tab داخل بدنهٔ مدال
 * (شامل کمبوباکس SearchableSelect؛ بدون اسپینر مبلغ و دکمه‌های هدر/فوتر)
 */
const FIELD_SELECTOR = [
  'input:not([disabled]):not([type="hidden"]):not([tabindex="-1"])',
  'select:not([disabled]):not([tabindex="-1"])',
  'textarea:not([disabled]):not([tabindex="-1"])',
  'button.searchable-select-trigger:not([disabled]):not([tabindex="-1"])',
].join(', ')

function isVisible(el) {
  if (!(el instanceof HTMLElement)) return false
  if (el.getClientRects().length > 0) return true
  return el.offsetParent !== null
}

function getFormFields(form) {
  if (!form) return []
  return Array.from(form.querySelectorAll(FIELD_SELECTOR)).filter((el) => {
    if (!isVisible(el)) return false
    if (el.closest('.modal-footer') || el.closest('.modal-header')) return false
    // ولیدیتور مخفی کمبوباکس
    if (el.classList.contains('searchable-select-validator')) return false
    return true
  })
}

/** اگر فوکوس داخل کمبوباکس است، همان تریگر را به‌عنوان فیلد جاری در نظر بگیر */
function resolveFieldElement(target, fields) {
  if (!(target instanceof HTMLElement)) return null
  const direct = fields.indexOf(target)
  if (direct !== -1) return target

  const trigger = target.closest?.('.searchable-select')?.querySelector(
    'button.searchable-select-trigger',
  )
  if (trigger && fields.includes(trigger)) return trigger

  return null
}

function focusSubmitButton(form) {
  const submit = form.querySelector(
    '.modal-footer button[type="submit"]:not([disabled]), button[type="submit"]:not([disabled])',
  )
  if (submit) {
    submit.focus()
    return true
  }
  return false
}

function isSubmitButton(el) {
  return (
    el instanceof HTMLElement &&
    el.tagName === 'BUTTON' &&
    el.getAttribute('type') === 'submit'
  )
}

function focusElement(el) {
  if (!(el instanceof HTMLElement)) return
  el.focus()
  if (
    typeof el.select === 'function' &&
    el.tagName === 'INPUT' &&
    el.type !== 'checkbox' &&
    el.type !== 'radio' &&
    el.type !== 'button'
  ) {
    try {
      el.select()
    } catch {
      // ignore
    }
  }
}

function focusNextField(event, form) {
  const target = event.target
  if (!(target instanceof HTMLElement) || !form.contains(target)) return false

  if (isSubmitButton(target)) {
    return false
  }

  if (target.tagName === 'TEXTAREA' && !event.ctrlKey && !event.metaKey) {
    return false
  }

  event.preventDefault()
  event.stopPropagation()

  if (
    target.closest('.modal-footer') ||
    target.closest('.modal-header') ||
    target.closest('.amount-field-spinners')
  ) {
    focusSubmitButton(form)
    return true
  }

  const fields = getFormFields(form)
  const current = resolveFieldElement(target, fields)
  const index = current ? fields.indexOf(current) : -1

  if (index === -1) {
    const following = fields.find((el) =>
      Boolean(target.compareDocumentPosition(el) & Node.DOCUMENT_POSITION_FOLLOWING),
    )
    if (following) {
      focusElement(following)
      return true
    }
    focusSubmitButton(form)
    return true
  }

  if (index >= fields.length - 1) {
    focusSubmitButton(form)
    return true
  }

  focusElement(fields[index + 1])
  return true
}

function isModKey(event) {
  return (event.ctrlKey || event.metaKey) && !event.altKey && !event.shiftKey
}

function isSaveShortcut(event) {
  return isModKey(event) && (event.code === 'KeyS' || event.key === 's' || event.key === 'S')
}

function isNewShortcut(event) {
  if (!isModKey(event)) return false
  // Ctrl+N طبق قرارداد پروژه؛ Ctrl+Space برای سازگاری با صفحات قبلی
  return (
    event.code === 'KeyN' ||
    event.key === 'n' ||
    event.key === 'N' ||
    event.code === 'Space' ||
    event.key === ' ' ||
    event.key === 'Spacebar'
  )
}

function blockBrowserShortcut(event) {
  event.preventDefault()
  event.stopPropagation()
  if (typeof event.stopImmediatePropagation === 'function') {
    event.stopImmediatePropagation()
  }
}

/**
 * با باز شدن مدال، فوکوس روی اولین فیلد قابل‌ویرایش بدنه.
 */
export function useModalAutoFocus({ open, formRef }) {
  useEffect(() => {
    if (!open) return undefined

    const timer = window.setTimeout(() => {
      const form = formRef?.current
      if (!form) return
      const fields = getFormFields(form)
      if (fields[0]) focusElement(fields[0])
    }, 50)

    return () => window.clearTimeout(timer)
  }, [open, formRef])
}

/**
 * Esc بستن؛ Ctrl+S ذخیره (بدون دیالوگ مرورگر)؛ Enter حرکت بین فیلدها سپس دکمه ذخیره.
 * Ctrl+S حتی وقتی فوکوس روی کمبوباکس/پورتال/سلکت است هم گرفته می‌شود.
 */
export function useModalKeyboardShortcuts({ open, onClose, onSave, formRef }) {
  const onCloseRef = useRef(onClose)
  const onSaveRef = useRef(onSave)
  const formRefRef = useRef(formRef)

  onCloseRef.current = onClose
  onSaveRef.current = onSave
  formRefRef.current = formRef

  useEffect(() => {
    if (!open) return undefined

    function handleKeyDown(event) {
      // Escape / Ctrl+S را قبل از defaultPrevented هندل کن
      // چون useBlockBrowserSaveShortcut ممکن است قبلاً preventDefault زده باشد
      if (event.key === 'Escape') {
        blockBrowserShortcut(event)
        onCloseRef.current?.()
        return
      }

      if (isSaveShortcut(event)) {
        blockBrowserShortcut(event)
        onSaveRef.current?.()
        return
      }

      if (event.defaultPrevented) return
      if (event.key !== 'Enter') return

      const form = formRefRef.current?.current
      if (!form) return

      // اگر فوکوس داخل پورتال کمبوباکس است، Enter را برای انتخاب گزینه رها کن
      if (
        event.target instanceof HTMLElement &&
        event.target.closest('.searchable-select-menu')
      ) {
        return
      }

      // روی دکمه ذخیره: سابمیت کن (نه برگشت به اول فیلدها)
      if (isSubmitButton(event.target)) {
        blockBrowserShortcut(event)
        onSaveRef.current?.()
        return
      }

      focusNextField(event, form)
    }

    // فقط document تا با window دوبار اجرا نشود
    document.addEventListener('keydown', handleKeyDown, true)
    return () => {
      document.removeEventListener('keydown', handleKeyDown, true)
    }
  }, [open])
}

/**
 * همیشه دیالوگ Save مرورگر برای Ctrl+S را بلاک می‌کند
 * (کمبوباکس باز، فوکوس روی input/select، خارج از مدال و …).
 * عمداً stopImmediatePropagation ندارد تا handler ذخیرهٔ مدال هم اجرا شود.
 */
export function useBlockBrowserSaveShortcut() {
  useEffect(() => {
    function handleKeyDown(event) {
      if (!isSaveShortcut(event)) return
      event.preventDefault()
    }

    document.addEventListener('keydown', handleKeyDown, true)
    return () => {
      document.removeEventListener('keydown', handleKeyDown, true)
    }
  }, [])
}

/**
 * Ctrl+N (و Ctrl+Space) → ایجاد جدید.
 */
export function usePageCreateShortcut({ enabled = true, onNew, isBlocked = false }) {
  const onNewRef = useRef(onNew)
  const enabledRef = useRef(enabled)
  const blockedRef = useRef(isBlocked)
  onNewRef.current = onNew
  enabledRef.current = enabled
  blockedRef.current = isBlocked

  useEffect(() => {
    function handleKeyDown(event) {
      if (!isNewShortcut(event)) return

      blockBrowserShortcut(event)

      if (!enabledRef.current || blockedRef.current) return
      onNewRef.current?.()
    }

    document.addEventListener('keydown', handleKeyDown, true)
    return () => {
      document.removeEventListener('keydown', handleKeyDown, true)
    }
  }, [])
}
