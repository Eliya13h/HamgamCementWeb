import { useEffect, useMemo, useRef, useState } from 'react'
import Icon from '../components/common/Icon'
import { collectLeafKeys } from './registry'

function useIndeterminateCheckbox(ref, indeterminate) {
  useEffect(() => {
    if (ref.current) {
      ref.current.indeterminate = indeterminate
    }
  }, [ref, indeterminate])
}

function PermissionTreePage({ page, selected, onChange, disabled }) {
  const headRef = useRef(null)
  const actionKeys = useMemo(() => page.actions.map((a) => a.key), [page.actions])
  const checkedCount = actionKeys.filter((key) => selected.has(key)).length
  const allChecked = checkedCount === actionKeys.length && actionKeys.length > 0
  const indeterminate = checkedCount > 0 && !allChecked

  useIndeterminateCheckbox(headRef, indeterminate)

  const toggleAll = () => {
    const next = new Set(selected)
    if (allChecked) {
      actionKeys.forEach((key) => next.delete(key))
    } else {
      actionKeys.forEach((key) => next.add(key))
    }
    onChange(next)
  }

  const toggleOne = (key) => {
    const next = new Set(selected)
    if (next.has(key)) next.delete(key)
    else next.add(key)
    onChange(next)
  }

  return (
    <div className="permission-tree-page">
      <div className="permission-tree-page-head">
        <label className="permission-tree-page-title">
          <input
            ref={headRef}
            type="checkbox"
            className="form-check-input"
            checked={allChecked}
            disabled={disabled}
            onChange={toggleAll}
          />
          <span>{page.label}</span>
        </label>
      </div>
      <div className="permission-tree-action-grid">
        {page.actions.map((action) => (
          <label key={action.key} className="permission-tree-action">
            <input
              type="checkbox"
              className="form-check-input"
              checked={selected.has(action.key)}
              disabled={disabled}
              onChange={() => toggleOne(action.key)}
            />
            <span>{action.label}</span>
          </label>
        ))}
      </div>
    </div>
  )
}

function PermissionTreeModule({ module, selected, onChange, disabled, expanded, onToggle }) {
  const headRef = useRef(null)
  const leafKeys = useMemo(() => collectLeafKeys(module), [module])
  const checkedCount = leafKeys.filter((key) => selected.has(key)).length
  const allChecked = checkedCount === leafKeys.length && leafKeys.length > 0
  const indeterminate = checkedCount > 0 && !allChecked

  useIndeterminateCheckbox(headRef, indeterminate)

  const toggleModule = () => {
    const next = new Set(selected)
    if (allChecked) {
      leafKeys.forEach((key) => next.delete(key))
    } else {
      leafKeys.forEach((key) => next.add(key))
    }
    onChange(next)
  }

  return (
    <section className="permission-tree-module">
      <div className="permission-tree-module-head">
        <button
          type="button"
          className="permission-tree-expand"
          onClick={onToggle}
          aria-expanded={expanded}
          aria-label={expanded ? 'بستن' : 'باز کردن'}
        >
          <Icon name={expanded ? 'chevron-down' : 'chevron-left'} />
        </button>
        <label className="permission-tree-module-title">
          <input
            ref={headRef}
            type="checkbox"
            className="form-check-input"
            checked={allChecked}
            disabled={disabled}
            onChange={toggleModule}
          />
          <span>{module.label}</span>
        </label>
      </div>

      {expanded && (
        <div className="permission-tree-module-body">
          {module.children.map((page) => (
            <PermissionTreePage
              key={page.key}
              page={page}
              selected={selected}
              onChange={onChange}
              disabled={disabled}
            />
          ))}
        </div>
      )}
    </section>
  )
}

function PermissionTree({ tree, value, onChange, disabled = false }) {
  const selected = value instanceof Set ? value : new Set(value ?? [])
  const [expandedKeys, setExpandedKeys] = useState(
    () => new Set(tree.filter((n) => n.type === 'module').map((n) => n.key)),
  )

  const handleChange = (next) => {
    if (!disabled) onChange?.(next)
  }

  const toggleExpand = (key) => {
    setExpandedKeys((prev) => {
      const next = new Set(prev)
      if (next.has(key)) next.delete(key)
      else next.add(key)
      return next
    })
  }

  const selectAll = () => {
    handleChange(new Set(tree.flatMap(collectLeafKeys)))
  }

  const clearAll = () => {
    handleChange(new Set())
  }

  return (
    <div className={`permission-tree ${disabled ? 'is-disabled' : ''}`}>
      <div className="permission-tree-toolbar">
        <button
          type="button"
          className="btn btn-sm btn-outline-secondary"
          onClick={selectAll}
          disabled={disabled}
        >
          انتخاب همه
        </button>
        <button
          type="button"
          className="btn btn-sm btn-outline-secondary"
          onClick={clearAll}
          disabled={disabled}
        >
          حذف همه
        </button>
      </div>

      <div className="permission-tree-body hc-scroll">
        {tree.map((node) =>
          node.type === 'module' ? (
            <PermissionTreeModule
              key={node.key}
              module={node}
              selected={selected}
              onChange={handleChange}
              disabled={disabled}
              expanded={expandedKeys.has(node.key)}
              onToggle={() => toggleExpand(node.key)}
            />
          ) : (
            <PermissionTreePage
              key={node.key}
              page={node}
              selected={selected}
              onChange={handleChange}
              disabled={disabled}
            />
          ),
        )}
      </div>
    </div>
  )
}

export default PermissionTree
