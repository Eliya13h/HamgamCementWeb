/** هم‌ترازی عرض ستون‌ها بعد از تغییر اندازهٔ کارت/سایدبار */

const FIT_KEY = '_hcDtFitObs'
const FIT_TIMER_KEY = '_hcDtFitTimers'

export function fitDataTableColumns(api) {
  if (!api?.table) return
  try {
    const node = api.table().node()
    if (!node?.isConnected) return
    api.columns.adjust()
  } catch {
    // جدول در حال destroy
  }
}

export function scheduleDataTableFit(api, { delays = [0, 100] } = {}) {
  if (!api?.table) return

  const container = api.table().container?.()
  if (container?.[FIT_TIMER_KEY]) {
    container[FIT_TIMER_KEY].forEach((id) => window.clearTimeout(id))
  }

  const timers = delays.map((ms) =>
    window.setTimeout(() => {
      requestAnimationFrame(() => fitDataTableColumns(api))
    }, ms),
  )

  if (container) container[FIT_TIMER_KEY] = timers
}

export function attachDataTableLayoutFit(api) {
  scheduleDataTableFit(api)

  let container
  try {
    container = api.table().container()
  } catch {
    return
  }
  if (!container || container[FIT_KEY]) return

  const target =
    container.closest('.users-table-wrapper') ||
    container.parentElement ||
    container

  let frame = 0
  const observer = new ResizeObserver(() => {
    window.cancelAnimationFrame(frame)
    frame = window.requestAnimationFrame(() => fitDataTableColumns(api))
  })

  observer.observe(target)
  container[FIT_KEY] = observer

  api.on('destroy', () => {
    window.cancelAnimationFrame(frame)
    observer.disconnect()
    if (container[FIT_TIMER_KEY]) {
      container[FIT_TIMER_KEY].forEach((id) => window.clearTimeout(id))
      delete container[FIT_TIMER_KEY]
    }
    delete container[FIT_KEY]
  })
}

/** پیش‌فرض بدون scrollX داخلی؛ اسکرول افقی با CSS روی wrapper */
export function withDataTableLayoutFit(options = {}) {
  const userInit = options.initComplete

  return {
    ...options,
    scrollX: options.scrollX === true,
    autoWidth: options.autoWidth === true ? true : false,
    initComplete(settings, json) {
      const api = this.api()
      attachDataTableLayoutFit(api)
      userInit?.call(this, settings, json)
    },
  }
}
