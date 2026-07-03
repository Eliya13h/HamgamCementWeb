import { useEffect, useId, useLayoutEffect, useMemo, useRef, useState } from 'react'
import { createPortal } from 'react-dom'
import Icon from './Icon'

/**
 * کامبوباکس با جستجوی داخل منو — الگوی Flowbite Dropdown with search
 * options: { value, label }[]
 */
function SearchableSelect({
  options = [],
  value,
  onChange,
  placeholder = 'انتخاب کنید...',
  searchPlaceholder = 'جستجو...',
  disabled = false,
  required = false,
  size = '',
  id: idProp,
  className = '',
}) {
  const autoId = useId()
  const id = idProp ?? autoId
  const containerRef = useRef(null)
  const triggerRef = useRef(null)
  const menuRef = useRef(null)
  const searchRef = useRef(null)
  const [open, setOpen] = useState(false)
  const [search, setSearch] = useState('')
  const [menuStyle, setMenuStyle] = useState(null)

  const selected = useMemo(
    () => options.find((o) => String(o.value) === String(value)),
    [options, value],
  )

  const filtered = useMemo(() => {
    const q = search.trim().toLowerCase()
    if (!q) return options
    return options.filter((o) => String(o.label).toLowerCase().includes(q))
  }, [options, search])

  useEffect(() => {
    if (!open) return undefined

    const handlePointerDown = (event) => {
      const target = event.target
      if (
        containerRef.current?.contains(target) ||
        menuRef.current?.contains(target)
      ) {
        return
      }
      setOpen(false)
    }

    document.addEventListener('mousedown', handlePointerDown)
    return () => document.removeEventListener('mousedown', handlePointerDown)
  }, [open])

  const updateMenuPosition = () => {
    const trigger = triggerRef.current
    if (!trigger) return

    const rect = trigger.getBoundingClientRect()
    const next = {
      top: rect.bottom + 4,
      left: rect.left,
      width: rect.width,
    }
    setMenuStyle((prev) => {
      if (
        prev &&
        prev.top === next.top &&
        prev.left === next.left &&
        prev.width === next.width
      ) {
        return prev
      }
      return next
    })
  }

  useLayoutEffect(() => {
    if (!open) {
      setMenuStyle(null)
      return undefined
    }

    updateMenuPosition()

    const handleReposition = () => updateMenuPosition()
    window.addEventListener('resize', handleReposition)
    window.addEventListener('scroll', handleReposition, true)

    return () => {
      window.removeEventListener('resize', handleReposition)
      window.removeEventListener('scroll', handleReposition, true)
    }
  }, [open])

  useEffect(() => {
    if (!open) return
    setSearch('')
  }, [open])

  useEffect(() => {
    if (open && menuStyle) {
      requestAnimationFrame(() => searchRef.current?.focus())
    }
  }, [open, menuStyle])

  const handleSelect = (optionValue) => {
    onChange(optionValue)
    setOpen(false)
    setSearch('')
  }

  const sizeClass = size === 'sm' ? 'searchable-select-trigger-sm' : ''

  return (
    <div
      ref={containerRef}
      className={`searchable-select ${size === 'sm' ? 'searchable-select-sm' : ''} ${className}`.trim()}
    >
      {required && (
        <input
          tabIndex={-1}
          aria-hidden
          className="searchable-select-validator"
          value={value ?? ''}
          required
          onChange={() => {}}
        />
      )}
      <button
        ref={triggerRef}
        type="button"
        id={id}
        className={`searchable-select-trigger d-flex align-items-center justify-content-between gap-2 ${sizeClass} ${
          disabled ? 'disabled' : ''
        }`.trim()}
        onClick={() => !disabled && setOpen((prev) => !prev)}
        disabled={disabled}
        aria-haspopup="listbox"
        aria-expanded={open}
      >
        <span className={`text-truncate ${selected ? '' : 'text-muted'}`}>
          {selected?.label ?? placeholder}
        </span>
        <Icon name={open ? 'chevron-up' : 'chevron-down'} className="searchable-select-chevron" />
      </button>
      {open &&
        menuStyle &&
        createPortal(
          <div
            ref={menuRef}
            className="searchable-select-menu searchable-select-menu-portal shadow"
            style={{
              top: menuStyle.top,
              left: menuStyle.left,
              width: menuStyle.width,
            }}
          >
            <div className="searchable-select-search border-bottom p-2">
              <input
                ref={searchRef}
                type="search"
                className="form-control form-control-sm"
                placeholder={searchPlaceholder}
                value={search}
                onChange={(e) => setSearch(e.target.value)}
                onKeyDown={(e) => {
                  if (e.key === 'Escape') {
                    setOpen(false)
                  }
                }}
              />
            </div>
            <ul className="searchable-select-list list-unstyled mb-0" role="listbox">
              {filtered.length === 0 ? (
                <li className="searchable-select-empty px-3 py-2 text-muted small">موردی یافت نشد</li>
              ) : (
                filtered.map((option) => (
                  <li
                    key={option.value}
                    role="option"
                    aria-selected={String(option.value) === String(value)}
                  >
                    <button
                      type="button"
                      className={`searchable-select-option w-100 text-start ${
                        String(option.value) === String(value) ? 'active' : ''
                      }`}
                      onClick={() => handleSelect(option.value)}
                    >
                      {option.label}
                    </button>
                  </li>
                ))
              )}
            </ul>
          </div>,
          document.body,
        )}
    </div>
  )
}

export default SearchableSelect
