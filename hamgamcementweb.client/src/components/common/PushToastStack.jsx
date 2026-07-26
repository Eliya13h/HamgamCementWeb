import { useEffect, useRef, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import Icon from './Icon'

const AUTO_DISMISS_MS = 7000

function severityIcon(type, severity) {
  if (type === 'product_shortage') return 'triangle-exclamation'
  if (type === 'warehouse_full') return 'warehouse'
  if (type === 'warehouse_low') return 'boxes-stacked'
  if (severity === 'danger') return 'triangle-exclamation'
  if (severity === 'warning') return 'triangle-exclamation'
  return 'bell'
}

function PushToastStack({ items = [], onDismissAll }) {
  const navigate = useNavigate()
  const [visible, setVisible] = useState([])
  const timersRef = useRef(new Map())
  const onDismissAllRef = useRef(onDismissAll)
  onDismissAllRef.current = onDismissAll

  const removeKeyRef = useRef(null)
  removeKeyRef.current = (key) => {
    const timer = timersRef.current.get(key)
    if (timer) {
      window.clearTimeout(timer)
      timersRef.current.delete(key)
    }

    setVisible((prev) =>
      prev.map((item) => (item.key === key ? { ...item, leaving: true } : item)),
    )

    window.setTimeout(() => {
      setVisible((prev) => {
        const next = prev.filter((item) => item.key !== key)
        if (next.length === 0) onDismissAllRef.current?.()
        return next
      })
    }, 280)
  }

  useEffect(() => {
    if (!items.length) return

    setVisible((prev) => {
      const existing = new Set(prev.map((p) => p.key))
      const next = [...prev]
      items.forEach((item) => {
        if (!existing.has(item.key)) next.push({ ...item, leaving: false })
      })
      return next.slice(-5)
    })
  }, [items])

  useEffect(() => {
    visible.forEach((item) => {
      if (item.leaving || timersRef.current.has(item.key)) return
      timersRef.current.set(
        item.key,
        window.setTimeout(() => removeKeyRef.current?.(item.key), AUTO_DISMISS_MS),
      )
    })
  }, [visible])

  useEffect(() => {
    return () => {
      timersRef.current.forEach((id) => window.clearTimeout(id))
      timersRef.current.clear()
    }
  }, [])

  const handleOpen = (item) => {
    removeKeyRef.current?.(item.key)
    if (item.href) navigate(item.href)
  }

  if (!visible.length) return null

  return (
    <div className="push-toast-stack" aria-live="polite" aria-relevant="additions">
      {visible.map((item) => (
        <article
          key={item.key}
          className={`push-toast is-${item.severity || 'info'}${item.leaving ? ' is-leaving' : ''}`}
          role="status"
        >
          <button
            type="button"
            className="push-toast-card"
            onClick={() => handleOpen(item)}
          >
            <span className="push-toast-icon" aria-hidden="true">
              <Icon name={severityIcon(item.type, item.severity)} />
            </span>
            <span className="push-toast-body">
              <span className="push-toast-meta">
                <span className="push-toast-app">همگام سمنت</span>
                <span className="push-toast-now">اکنون</span>
              </span>
              <span className="push-toast-title">{item.title}</span>
              <span className="push-toast-message">{item.message}</span>
            </span>
          </button>
          <button
            type="button"
            className="push-toast-close"
            aria-label="بستن اعلان"
            onClick={() => removeKeyRef.current?.(item.key)}
          >
            <Icon name="xmark" />
          </button>
        </article>
      ))}
    </div>
  )
}

export default PushToastStack
