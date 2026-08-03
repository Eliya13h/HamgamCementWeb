import { useEffect } from 'react'
import { Tooltip } from 'bootstrap'

function readTitle(el) {
  return el.getAttribute('data-bs-title') || el.getAttribute('title') || ''
}

function disposeTooltip(el) {
  const instance = Tooltip.getInstance(el)
  if (!instance) return
  try {
    // hide() نزن — اگر transition ناتمام باشد _activeTrigger خالی می‌شود و BS خطا می‌دهد
    instance.dispose()
  } catch {
    // ignore
  }
}

function removeOrphanTooltips() {
  document.querySelectorAll('body > .tooltip.hc-tooltip').forEach((node) => {
    node.remove()
  })
}

function ensureTooltips(root) {
  if (!root) return
  root.querySelectorAll('[data-bs-toggle="tooltip"]').forEach((el) => {
    if (Tooltip.getInstance(el)) return
    const title = readTitle(el)
    if (!title) return
    new Tooltip(el, {
      container: 'body',
      customClass: 'hc-tooltip',
      trigger: 'hover focus',
      placement: el.getAttribute('data-bs-placement') || 'top',
      title,
    })
  })
}

function disposeAllIn(root) {
  if (!root) return
  root.querySelectorAll('[data-bs-toggle="tooltip"]').forEach(disposeTooltip)
  removeOrphanTooltips()
}

/**
 * Tooltipهای بوت‌استرپ ۵ داخل یک کانتینر را با کلاس سفارشی تم پروژه مقداردهی می‌کند.
 * برای فیلدهای disabled باید data-bs-toggle روی wrapper باشد.
 */
export function useBootstrapTooltips(containerRef, enabled = true, deps = []) {
  useEffect(() => {
    if (!enabled) {
      disposeAllIn(containerRef?.current)
      return undefined
    }

    const timer = window.setTimeout(() => {
      ensureTooltips(containerRef?.current)
    }, 0)

    return () => {
      window.clearTimeout(timer)
      disposeAllIn(containerRef?.current)
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [enabled, containerRef, ...deps])
}

/** اتریبیوت‌های استاندارد tooltip سفارشی پروژه */
export function tipProps(title, placement = 'top') {
  if (!title) return {}
  return {
    title,
    'data-bs-toggle': 'tooltip',
    'data-bs-placement': placement,
    'data-bs-custom-class': 'hc-tooltip',
    'data-bs-title': title,
  }
}
