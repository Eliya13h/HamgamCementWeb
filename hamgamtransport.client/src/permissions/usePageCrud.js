import { useCallback, useMemo } from 'react'
import { pathToPageKey } from './registry'
import { usePermission } from './usePermission'
import { pathPermission } from './utils'

/** دسترسی‌های CRUD و عملیات اضافهٔ یک صفحه */
export function usePageCrud(path) {
  const { can } = usePermission()
  const pageKey = useMemo(() => (path ? pathToPageKey(path) : ''), [path])

  const canAction = useCallback(
    (action) => {
      if (!path) return true
      return can(pathPermission(path, action))
    },
    [can, path],
  )

  return useMemo(
    () => ({
      pageKey,
      canView: canAction('view'),
      canCreate: canAction('create'),
      canEdit: canAction('edit'),
      canDelete: canAction('delete'),
      can: (action) => {
        if (!path) return true
        return can(`${pageKey}.${action}`)
      },
    }),
    [pageKey, canAction, can, path],
  )
}
