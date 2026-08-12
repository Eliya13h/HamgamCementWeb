import { Navigate, useLocation } from 'react-router-dom'
import { useAuth } from '../../context/AuthContext'
import { canAccess, pathPermission } from '../../permissions/utils'

function AuthLoadingScreen() {
  return (
    <div className="auth-loading-screen">
      <div className="auth-loading-spinner" aria-hidden="true" />
      <p className="auth-loading-text">در حال بررسی نشست...</p>
    </div>
  )
}

function RequirePageAccess({ path, children }) {
  const { user, loading } = useAuth()
  const location = useLocation()

  if (loading) {
    return <AuthLoadingScreen />
  }

  const allowed = canAccess(
    user?.permissions,
    user?.hasFullAccess ?? false,
    pathPermission(path, 'view'),
  )

  if (!allowed) {
    return <Navigate to="/" replace state={{ from: location }} />
  }

  return children
}

export default RequirePageAccess
