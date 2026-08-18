import { useEffect, useMemo, useRef, useState } from 'react'
import Icon from '../components/common/Icon'
import { collectLeafKeys, getAllLeafPermissionKeys } from './registry'

const ROOT_KEY = '__full_access__'

function useIndeterminateCheckbox(ref, indeterminate) {
  useEffect(() => {
    if (ref.current) {
      ref.current.indeterminate = indeterminate
    }
  }, [ref, indeterminate])
}

function getChildren(node) {
  if (node.type === 'page') {
    return (node.actions ?? []).map((action) => ({
      key: action.key,
      label: action.label,
      type: 'action',
    }))
  }
  return node.children ?? []
}

function isExpandable(node) {
  return node.type === 'root' || node.type === 'module' || node.type === 'page'
}

function collectExpandableKeys(nodes, acc = new Set()) {
  for (const node of nodes) {
    if (isExpandable(node)) acc.add(node.key)
    const children = getChildren(node)
    if (children.length) collectExpandableKeys(children, acc)
  }
  return acc
}

function nodeLeafKeys(node) {
  if (node.type === 'action') return [node.key]
  if (node.type === 'page') return collectLeafKeys(node)
  if (node.type === 'root') return node.children.flatMap(collectLeafKeys)
  return collectLeafKeys(node)
}

function TreeNode({
  node,
  depth,
  selected,
  hasFullAccess,
  expandedKeys,
  onToggleExpand,
  onToggleNode,
  disabled,
}) {
  const checkboxRef = useRef(null)
  const children = getChildren(node)
  const expandable = isExpandable(node) && children.length > 0
  const expanded = expandable && expandedKeys.has(node.key)
  const leaves = useMemo(() => nodeLeafKeys(node), [node])

  const checkedCount = hasFullAccess
    ? leaves.length
    : leaves.filter((key) => selected.has(key)).length
  const allChecked = leaves.length > 0 && checkedCount === leaves.length
  const indeterminate = checkedCount > 0 && !allChecked

  useIndeterminateCheckbox(checkboxRef, indeterminate)

  return (
    <div className="permission-tree-node">
      <div
        className="permission-tree-row"
        style={{ paddingInlineStart: `calc(${depth} * 1.25rem)` }}
      >
        {expandable ? (
          <button
            type="button"
            className="permission-tree-expand"
            onClick={() => onToggleExpand(node.key)}
            aria-expanded={expanded}
            aria-label={expanded ? 'بستن' : 'باز کردن'}
          >
            <Icon
              name="chevron-down"
              className={`permission-tree-expand-icon${expanded ? '' : ' is-collapsed'}`}
            />
          </button>
        ) : (
          <span className="permission-tree-expand-spacer" aria-hidden="true" />
        )}

        <label className="permission-tree-label">
          <input
            ref={checkboxRef}
            type="checkbox"
            className="form-check-input"
            checked={allChecked}
            disabled={disabled}
            onChange={() => onToggleNode(node)}
          />
          <span
            className={
              node.type === 'root'
                ? 'permission-tree-label-text is-root'
                : 'permission-tree-label-text'
            }
          >
            {node.label}
          </span>
        </label>
      </div>

      {expandable && expanded && (
        <div className="permission-tree-children">
          {children.map((child) => (
            <TreeNode
              key={child.key}
              node={child}
              depth={depth + 1}
              selected={selected}
              hasFullAccess={hasFullAccess}
              expandedKeys={expandedKeys}
              onToggleExpand={onToggleExpand}
              onToggleNode={onToggleNode}
              disabled={disabled}
            />
          ))}
        </div>
      )}
    </div>
  )
}

function PermissionTree({
  tree,
  value,
  hasFullAccess = false,
  onChange,
  disabled = false,
}) {
  const selected = value instanceof Set ? value : new Set(value ?? [])
  const allLeafKeys = useMemo(() => getAllLeafPermissionKeys(tree), [tree])

  const rootNode = useMemo(
    () => ({
      key: ROOT_KEY,
      label: 'دسترسی کامل',
      type: 'root',
      children: tree,
    }),
    [tree],
  )

  const [expandedKeys, setExpandedKeys] = useState(() =>
    collectExpandableKeys([rootNode]),
  )

  const emit = (permissions, nextHasFullAccess) => {
    if (disabled) return
    onChange?.({ permissions, hasFullAccess: nextHasFullAccess })
  }

  const toggleExpand = (key) => {
    setExpandedKeys((prev) => {
      const next = new Set(prev)
      if (next.has(key)) next.delete(key)
      else next.add(key)
      return next
    })
  }

  const toggleNode = (node) => {
    const leaves = nodeLeafKeys(node)
    const effective = hasFullAccess ? new Set(allLeafKeys) : selected
    const allChecked = leaves.length > 0 && leaves.every((key) => effective.has(key))

    if (node.type === 'root') {
      if (allChecked || hasFullAccess) emit(new Set(), false)
      else emit(new Set(), true)
      return
    }

    if (hasFullAccess) {
      const next = new Set(allLeafKeys)
      leaves.forEach((key) => next.delete(key))
      emit(next, false)
      return
    }

    const next = new Set(selected)
    if (allChecked) {
      leaves.forEach((key) => next.delete(key))
    } else {
      leaves.forEach((key) => next.add(key))
    }

    if (allLeafKeys.length > 0 && allLeafKeys.every((key) => next.has(key))) {
      emit(new Set(), true)
    } else {
      emit(next, false)
    }
  }

  return (
    <div className={`permission-tree ${disabled ? 'is-disabled' : ''}`}>
      <div className="permission-tree-body hc-scroll">
        <TreeNode
          node={rootNode}
          depth={0}
          selected={selected}
          hasFullAccess={hasFullAccess}
          expandedKeys={expandedKeys}
          onToggleExpand={toggleExpand}
          onToggleNode={toggleNode}
          disabled={disabled}
        />
      </div>
    </div>
  )
}

export default PermissionTree
