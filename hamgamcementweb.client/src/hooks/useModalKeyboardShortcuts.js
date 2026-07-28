import { useEffect, useRef } from 'react'

/** فقط فیلدهای فرم — بدون دکمه بستن/انصراف/اسپینر */
const FIELD_SELECTOR = [
  'input:not([disabled]):not([type="hidden"]):not([tabindex="-1"])',
  'select:not([disabled]):not([tabindex="-1"])',
  'textarea:not([disabled]):not([tabindex="-1"])',
].join(', ')

function isVisible(el) {
  if (!(el instanceof HTMLElement)) return false
  if (el.getClientRects().length > 0) return true
  // برخی چک‌باکس‌های سوئیچ ممکن است offsetParent خاصی داشته باشند
  return el.offsetParent !== null
}

function getFormFields(form) {
  if (!form) return []
  return Array.from(form.querySelectorAll(FIELD_SELECTOR)).filter((el) => {
    if (!isVisible(el)) return false
    if (el.closest('.modal-footer') || el.closest('.modal-header')) return false
    return true
  })
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

function focusNextField(event, form) {
  const target = event.target
  if (!(target instanceof HTMLElement) || !form.contains(target)) return false

  // روی دکمه ذخیره: اجازه بده Enter فرم را submit کند
  if (isSubmitButton(target)) {
    return false
  }

  // textarea: Enter = خط جدید (مگر Ctrl+Enter → برو بعدی)
  if (target.tagName === 'TEXTAREA' && !event.ctrlKey && !event.metaKey) {
    return false
  }

  event.preventDefault()
  event.stopPropagation()

  // اگر فوکوس روی انصراف/اسپینر/بستن است → مستقیم ذخیره
  if (
    target.closest('.modal-footer') ||
    target.closest('.modal-header') ||
    target.closest('.amount-field-spinners')
  ) {
    focusSubmitButton(form)
    return true
  }

  const fields = getFormFields(form)
  const index = fields.indexOf(target)

  if (index === -1) {
    // فوکوس روی عنصری غیر فیلد (مثلاً اسپینر) — اولین فیلد بعدی یا ذخیره
    const following = fields.find((el) =>
      Boolean(target.compareDocumentPosition(el) & Node.DOCUMENT_POSITION_FOLLOWING),
    )
    if (following) {
      following.focus()
    } else {
      focusSubmitButton(form)
    }
    return true
  }

  if (index >= fields.length - 1) {
    focusSubmitButton(form)
    return true
  }

  const next = fields[index + 1]
  next.focus()
  if (typeof next.select === 'function' && next.type !== 'checkbox' && next.type !== 'radio') {
    try {
      next.select()
    } catch {
      // ignore
    }
  }
  return true
}

function isModKey(event) {
  return (event.ctrlKey || event.metaKey) && !event.altKey && !event.shiftKey
}

/**
 * Esc بستن؛ Ctrl+S ذخیره (بدون دیالوگ مرورگر)؛ Enter حرکت بین فیلدها سپس دکمه ذخیره.
 */
export function useModalKeyboardShortcuts({ open, onClose, onSave, formRef }) {
  const onCloseRef = useRef(onClose)
  const onSaveRef = useRef(onSave)
  const formRefRef = useRef(formRef)

  onCloseRef.current = onClose
  onSaveRef.current = onSave
  formRefRef.current = formRef

  useEffect(() => {
    if (!open) return

    function handleKeyDown(event) {
      if (event.key === 'Escape') {
        event.preventDefault()
        event.stopPropagation()
        onCloseRef.current?.()
        return
      }

      if (
        isModKey(event) &&
        (event.code === 'KeyS' || event.key === 's' || event.key === 'S')
      ) {
        event.preventDefault()
        event.stopPropagation()
        onSaveRef.current?.()
        return
      }

      if (event.key !== 'Enter') return

      const form = formRefRef.current?.current
      if (!form) return

      focusNextField(event, form)
    }

    document.addEventListener('keydown', handleKeyDown, true)
    return () => document.removeEventListener('keydown', handleKeyDown, true)
  }, [open])
}

/**
 * Ctrl+N → ایجاد جدید؛ همیشه preventDefault تا تب جدید مرورگر باز نشود.
 * وقتی مدال باز است فقط تب مرورگر را بلاک می‌کند و دوباره مدال باز نمی‌کند.
 */
export function usePageCreateShortcut({ enabled = true, onNew, isBlocked = false }) {
  const onNewRef = useRef(onNew)
  onNewRef.current = onNew

  useEffect(() => {
    if (!enabled) return

    function handleKeyDown(event) {
      if (
        !isModKey(event) ||
        !(event.code === 'KeyN' || event.key === 'n' || event.key === 'N')
      ) {
        return
      }

      event.preventDefault()
      event.stopPropagation()

      if (isBlocked) return
      onNewRef.current?.()
    }

    document.addEventListener('keydown', handleKeyDown, true)
    return () => document.removeEventListener('keydown', handleKeyDown, true)
  }, [enabled, isBlocked])
}
