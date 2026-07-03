import { useEffect, useRef } from 'react'

const FOCUSABLE_SELECTOR =
  'input:not([disabled]):not([type="hidden"]), select:not([disabled]), textarea:not([disabled]), button:not([disabled])'

function getFocusableElements(form) {
  if (!form) return []
  return Array.from(form.querySelectorAll(FOCUSABLE_SELECTOR)).filter(
    (el) => el.offsetParent !== null,
  )
}

function focusSubmitButton(form) {
  const submit = form.querySelector('button[type="submit"]:not([disabled])')
  submit?.focus()
}

function focusNextField(event, form) {
  const target = event.target
  if (!(target instanceof HTMLElement) || !form.contains(target)) return false

  if (target.tagName === 'BUTTON' && target.getAttribute('type') === 'submit') {
    return false
  }

  event.preventDefault()
  event.stopPropagation()

  const bodyFocusable = getFocusableElements(form).filter(
    (el) => !el.closest('.modal-footer'),
  )
  const lastBodyField = bodyFocusable[bodyFocusable.length - 1]

  if (target === lastBodyField) {
    focusSubmitButton(form)
    return true
  }

  const allFocusable = getFocusableElements(form)
  const index = allFocusable.indexOf(target)

  if (index >= 0 && index < allFocusable.length - 1) {
    const next = allFocusable[index + 1]
    if (next.closest('.modal-footer') && next.getAttribute('type') !== 'submit') {
      focusSubmitButton(form)
    } else {
      next.focus()
    }
    return true
  }

  focusSubmitButton(form)
  return true
}

/**
 * Esc بستن مدال؛ Ctrl+S ذخیره بدون دیالوگ مرورگر؛ Enter حرکت بین فیلدها (نه ذخیره مستقیم).
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
        onCloseRef.current()
        return
      }

      const isSaveShortcut =
        (event.ctrlKey || event.metaKey) &&
        !event.altKey &&
        !event.shiftKey &&
        (event.code === 'KeyS' || event.key === 's' || event.key === 'S')

      if (isSaveShortcut) {
        event.preventDefault()
        event.stopPropagation()
        onSaveRef.current?.()
        return
      }

      if (event.key === 'Enter' && formRefRef.current?.current) {
        focusNextField(event, formRefRef.current.current)
      }
    }

    document.addEventListener('keydown', handleKeyDown, true)
    return () => document.removeEventListener('keydown', handleKeyDown, true)
  }, [open])
}
