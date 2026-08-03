const STORAGE_PREFIX = 'hc-notif-state'
const LEGACY_SESSION_KEY = 'hc-push-notif-shown'
const MAX_NATIVE = 3
const MAX_STORED_KEYS = 120
const nativeFired = new Set()

/**
 * کلید پایدار اعلان — بدون title/message تا با تغییر موجودی دوباره «جدید» نشود.
 */
export function alertKey(item) {
  if (!item) return 'unknown'
  if (item.productId != null && item.productId !== '') {
    return `${item.type ?? 'unknown'}|p:${item.productId}`
  }
  if (item.warehouseId != null && item.warehouseId !== '') {
    return `${item.type ?? 'unknown'}|w:${item.warehouseId}`
  }
  return `${item.type ?? 'unknown'}|x:${item.title ?? 'na'}`
}

function storageKey(userId) {
  return `${STORAGE_PREFIX}:${userId ?? 'anon'}`
}

function emptyState() {
  return { read: [], pushed: [], seeded: false }
}

function readState(userId) {
  try {
    const raw = localStorage.getItem(storageKey(userId))
    if (!raw) return emptyState()
    const parsed = JSON.parse(raw)
    return {
      read: Array.isArray(parsed?.read) ? parsed.read : [],
      pushed: Array.isArray(parsed?.pushed) ? parsed.pushed : [],
      seeded: Boolean(parsed?.seeded),
    }
  } catch {
    return emptyState()
  }
}

function writeState(userId, state) {
  try {
    localStorage.setItem(
      storageKey(userId),
      JSON.stringify({
        read: [...state.read].slice(-MAX_STORED_KEYS),
        pushed: [...state.pushed].slice(-MAX_STORED_KEYS),
        seeded: Boolean(state.seeded),
      }),
    )
  } catch {
    /* ignore quota / private mode */
  }
}

function clearLegacySessionStore() {
  try {
    sessionStorage.removeItem(LEGACY_SESSION_KEY)
  } catch {
    /* ignore */
  }
}

function toSet(list) {
  return new Set(Array.isArray(list) ? list : [])
}

function enrichItems(items) {
  return (Array.isArray(items) ? items : []).map((item, index) => {
    const fingerprint = alertKey(item)
    return {
      ...item,
      fingerprint,
      key: `${fingerprint}#${index}`,
    }
  })
}

/** کلیدهایی که دیگر در لیست فعال نیستند را پاک می‌کند تا در بازگشت مجدد، دوباره پوش شوند. */
function pruneState(state, activeKeys) {
  const active = toSet(activeKeys)
  return {
    ...state,
    read: state.read.filter((key) => active.has(key)),
    pushed: state.pushed.filter((key) => active.has(key)),
  }
}

/**
 * همگام‌سازی وضعیت با لیست فعلی API.
 * بار اول: اعلان‌های موجود را بدون اسپم پوش، فقط به‌عنوان «قبلاً پوش‌شده» seed می‌کند
 * تا فقط اعلان‌های بعدی پوش شوند.
 */
export function syncNotificationState(items, userId) {
  clearLegacySessionStore()
  const enriched = enrichItems(items)
  const activeKeys = enriched.map((item) => item.fingerprint)
  let state = pruneState(readState(userId), activeKeys)

  if (!state.seeded) {
    state = {
      read: [...state.read],
      pushed: [...new Set([...state.pushed, ...activeKeys])],
      seeded: true,
    }
    writeState(userId, state)
  } else {
    writeState(userId, state)
  }

  const read = toSet(state.read)
  const pushed = toSet(state.pushed)

  return enriched.map((item) => ({
    ...item,
    isRead: read.has(item.fingerprint),
    isPushed: pushed.has(item.fingerprint),
  }))
}

export function getUnreadNotifications(items) {
  return (Array.isArray(items) ? items : []).filter((item) => !item.isRead)
}

export function getUnpushedNotifications(items) {
  return (Array.isArray(items) ? items : []).filter((item) => !item.isPushed)
}

export function markNotificationsRead(items, userId) {
  if (!items?.length) return
  const state = readState(userId)
  const read = toSet(state.read)
  items.forEach((item) => {
    const key = item.fingerprint || alertKey(item)
    if (key) read.add(key)
  })
  writeState(userId, { ...state, read: [...read], seeded: true })
}

export function markNotificationsPushed(items, userId) {
  if (!items?.length) return
  const state = readState(userId)
  const pushed = toSet(state.pushed)
  items.forEach((item) => {
    const key = item.fingerprint || alertKey(item)
    if (key) pushed.add(key)
  })
  writeState(userId, { ...state, pushed: [...pushed], seeded: true })
}

/** فقط اعلان‌های جدید (هنوز پوش‌نشده) را برای توست/Notification آماده می‌کند. */
export function preparePushToasts(items, userId) {
  const unpushed = getUnpushedNotifications(items)
  if (!unpushed.length) return []
  markNotificationsPushed(unpushed, userId)
  return unpushed
}

export function clearPushToastBatch() {
  /* نگه داشته شده برای سازگاری با Header؛ دیگر batch در حافظه نداریم */
}

/** سازگاری با فراخوانی‌های قبلی */
export function getUnseenNotifications(items) {
  return getUnreadNotifications(items)
}

export function markNotificationsSeen(items, userId) {
  markNotificationsRead(items, userId)
  markNotificationsPushed(items, userId)
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
        renotify: false,
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
