import { useCallback } from 'react'
import { useAuth } from '../context/AuthContext'
import { canAccess } from './utils'

export function usePermission() {
  const { user } = useAuth()

  const can = useCallback(
    (permissionKey) =>
      canAccess(user?.permissions, user?.hasFullAccess ?? false, permissionKey),
    [user],
  )

  return {
    can,
    hasFullAccess: user?.hasFullAccess ?? false,
    permissions: user?.permissions ?? [],
  }
}
