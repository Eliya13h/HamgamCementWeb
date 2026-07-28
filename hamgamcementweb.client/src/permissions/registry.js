import { navItems, settingsNavItem } from '../config/navigation'
import { CRUD_ACTIONS } from './actions'
import { PAGE_EXTRA_ACTIONS } from './pageActions'

/** تبدیل مسیر به کلید صفحه: /people/customers → people.customers */
export function pathToPageKey(path) {
  if (!path || path === '/') return 'dashboard'
  const clean = String(path).split('#')[0]
  if (!clean || clean === '/') return 'dashboard'
  return clean.replace(/^\//, '').replace(/\//g, '.')
}

function buildPageNode(path, label) {
  const pageKey = pathToPageKey(path)
  const extras = PAGE_EXTRA_ACTIONS[pageKey] ?? []
  const actions = [...CRUD_ACTIONS, ...extras].map((action) => ({
    key: `${pageKey}.${action.key}`,
    label: action.label,
  }))

  return {
    key: pageKey,
    label,
    type: 'page',
    actions,
  }
}

function buildNavPageNodes(child) {
  if (child.skipPermissionTree) {
    return []
  }
  if (child.permissionPages?.length) {
    return child.permissionPages.map((page) => buildPageNode(page.path, page.label))
  }
  return [buildPageNode(child.path, child.label)]
}

function buildModuleNode(item) {
  return {
    key: item.id,
    label: item.label,
    type: 'module',
    children: item.children.flatMap(buildNavPageNodes),
  }
}

/**
 * درخت کامل دسترسی‌ها — منبع واحد برای UI و مستندات.
 * ساختار: ماژول سایدبار → صفحه (با عملیات CRUD به‌صورت inline)
 */
export function buildPermissionTree() {
  const tree = navItems.map((item) =>
    item.children?.length ? buildModuleNode(item) : buildPageNode(item.path, item.label),
  )

  tree.push(buildPageNode(settingsNavItem.path, settingsNavItem.label))

  return tree
}

export function collectLeafKeys(node) {
  if (node.type === 'page') {
    return node.actions.map((action) => action.key)
  }

  if (node.children?.length) {
    return node.children.flatMap(collectLeafKeys)
  }

  return []
}

/** همه کلیدهای برگ (عملیات) */
export function getAllLeafPermissionKeys(tree = buildPermissionTree()) {
  return tree.flatMap(collectLeafKeys)
}

export const permissionTree = buildPermissionTree()
