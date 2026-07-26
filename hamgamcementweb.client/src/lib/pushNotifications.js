const STORAGE_KEY = 'hc-push-notif-shown'
const MAX_NATIVE = 3
const nativeFired = new Set()
let lastToastBatch = []

function notificationKey(item) {
  return [
    item?.type ?? 'unknown',
    item?.productId ?? '',
    item?.warehouseId ?? '',
    item?.title ?? '',
    item?.message ?? '',
  ].join('|')
}

function readShownKeys() {
  try {
    const raw = sessionStorage.getItem(STORAGE_KEY)
    const parsed = raw ? JSON.parse(raw) : []
    return new Set(Array.isArray(parsed) ? parsed : [])
  } catch {
    return new Set()
  }
}

function writeShownKeys(keys) {
  try {
    sessionStorage.setItem(STORAGE_KEY, JSON.stringify([...keys].slice(-80)))
  } catch {
    /* ignore quota / private mode */
  }
}

export function getUnseenNotifications(items) {
  const list = Array.isArray(items) ? items : []
  const shown = readShownKeys()
  return list
    .map((item, index) => ({
      ...item,
      key: `${notificationKey(item)}#${index}`,
      fingerprint: notificationKey(item),
    }))
    .filter((item) => !shown.has(item.fingerprint))
}

export function markNotificationsSeen(items) {
  if (!items?.length) return
  const shown = readShownKeys()
  items.forEach((item) => {
    if (item.fingerprint) shown.add(item.fingerprint)
  })
  writeShownKeys(shown)
}

/** آماده‌سازی توست‌ها؛ در remountهای StrictMode همان دسته را برمی‌گرداند. */
export function preparePushToasts(items) {
  const unseen = getUnseenNotifications(items)
  if (unseen.length) {
    lastToastBatch = unseen
    markNotificationsSeen(unseen)
    return unseen
  }
  return lastToastBatch
}

export function clearPushToastBatch() {
  lastToastBatch = []
}

export function isBrowserNotificationSupported() {
  return typeof window !== 'undefined' && 'Notification' in window
}

export function getBrowserNotificationPermission() {
  if (!isBrowserNotificationSupported()) return 'unsupported'
  return Notification.permission
}

export async function requestBrowserNotificationPermission() {
  if (!isBrowserNotificationSupported()) return 'unsupported'
  if (Notification.permission !== 'default') return Notification.permission
  try {
    return await Notification.requestPermission()
  } catch {
    return Notification.permission
  }
}

export function showBrowserNotifications(items) {
  if (!isBrowserNotificationSupported()) return
  if (Notification.permission !== 'granted') return

  items.slice(0, MAX_NATIVE).forEach((item) => {
    const tag = item.fingerprint || item.key
    if (!tag || nativeFired.has(tag)) return
    nativeFired.add(tag)

    try {
      const n = new Notification(item.title || 'اعلان همگام سمنت', {
        body: item.message || '',
        icon: '/favicon.svg',
        badge: '/favicon.svg',
        tag,
        dir: 'rtl',
        lang: 'fa',
      })
      if (item.href) {
        n.onclick = () => {
          window.focus()
          window.location.assign(item.href)
          n.close()
        }
      }
    } catch {
      /* some browsers throw if document is not visible */
    }
  })
}
