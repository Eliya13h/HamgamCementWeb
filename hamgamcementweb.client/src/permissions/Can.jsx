import { usePermission } from './usePermission'

export function Can({ permission, children, fallback = null }) {
  const { can } = usePermission()
  return can(permission) ? children : fallback
}
