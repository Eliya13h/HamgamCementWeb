import { Toast } from 'bootstrap'

function escapeHtml(value) {
  return String(value)
    .replaceAll('&', '&amp;')
    .replaceAll('<', '&lt;')
    .replaceAll('>', '&gt;')
    .replaceAll('"', '&quot;')
    .replaceAll("'", '&#39;')
}

function ensureToastContainer() {
  let host = document.getElementById('app-toast-container')
  if (host) return host

  host = document.createElement('div')
  host.id = 'app-toast-container'
  host.className = 'toast-container position-fixed top-0 start-50 translate-middle-x p-3'
  host.style.zIndex = '2200'
  document.body.appendChild(host)
  return host
}

/**
 * Toast بوت‌استرپ ۵ بدون هدر — فقط متن و رنگ.
 * @param {string} message
 * @param {'danger'|'success'|'warning'|'info'|'primary'} [variant='danger']
 */
export function showAppToast(message, variant = 'danger') {
  if (!message) return

  const host = ensureToastContainer()
  const el = document.createElement('div')
  const bg =
    variant === 'danger' ||
    variant === 'success' ||
    variant === 'warning' ||
    variant === 'info' ||
    variant === 'primary'
      ? variant
      : 'danger'

  el.className = `toast align-items-center text-bg-${bg} border-0 shadow`
  el.setAttribute('role', 'alert')
  el.setAttribute('aria-live', 'assertive')
  el.setAttribute('aria-atomic', 'true')
  el.innerHTML = `
    <div class="d-flex">
      <div class="toast-body">${escapeHtml(message)}</div>
      <button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast" aria-label="بستن"></button>
    </div>
  `

  host.appendChild(el)
  // ۲/۳ زمان قبلی (۴۵۰۰ms → ۳۰۰۰ms) برای تست سریع‌تر
  const toast = Toast.getOrCreateInstance(el, { delay: 3000, autohide: true })

  // جلوگیری از دوبار کلیک بستن که با انیمیشن Bootstrap تداخل می‌کند
  // (https://github.com/twbs/bootstrap/issues/37265)
  el.addEventListener(
    'hide.bs.toast',
    () => {
      el.style.pointerEvents = 'none'
    },
    { once: true },
  )

  el.addEventListener(
    'hidden.bs.toast',
    () => {
      // dispose را بعد از اتمام صف transition Bootstrap انجام بده
      setTimeout(() => {
        toast.dispose()
        el.remove()
      }, 0)
    },
    { once: true },
  )
  toast.show()
}
