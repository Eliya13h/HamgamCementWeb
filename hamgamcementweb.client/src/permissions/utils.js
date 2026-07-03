import { pathToPageKey } from './registry'

/** ساخت کلید دسترسی: pagePermission('people.customers', 'create') */
export function pagePermission(pageKey, action = 'view') {
  return `${pageKey}.${action}`
}

/** ساخت کلید از مسیر: pathPermission('/people/customers', 'edit') */
export function pathPermission(path, action = 'view') {
  return pagePermission(pathToPageKey(path), action)
}

export function canAccess(permissions, hasFullAccess, key) {
  if (hasFullAccess) return true
  return permissions?.includes(key) ?? false
}

export function canViewPage(permissions, hasFullAccess, path) {
  return canAccess(permissions, hasFullAccess, pathPermission(path, 'view'))
}

/** فیلتر آیتم‌های سایدبار بر اساس دسترسی مشاهده */
export function filterNavByPermission(items, can) {
  return items
    .map((item) => {
      if (item.children?.length) {
        const children = item.children.filter((child) =>
          can(pathPermission(child.path, 'view')),
        )
        if (children.length === 0) return null
        return { ...item, children }
      }

      return can(pathPermission(item.path, 'view')) ? item : null
    })
    .filter(Boolean)
}
