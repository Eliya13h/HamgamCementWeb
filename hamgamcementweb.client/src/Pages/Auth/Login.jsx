import { useEffect, useState } from 'react'
import { Navigate, useLocation, useNavigate } from 'react-router-dom'
import Icon from '../../components/common/Icon'
import { useAuth } from '../../context/AuthContext'
import { useTheme } from '../../context/ThemeContext'
import '../../styles/auth.css'

function Login() {
    const { isAuthenticated, loading, login } = useAuth()
    const { theme, toggleTheme } = useTheme()
    const navigate = useNavigate()
    const location = useLocation()

    const [userName, setUserName] = useState('')
    const [password, setPassword] = useState('')
    const [showPassword, setShowPassword] = useState(false)
    const [submitting, setSubmitting] = useState(false)
    const [error, setError] = useState('')

    const redirectTo = location.state?.from?.pathname ?? '/'

    useEffect(() => {
        if (!loading && isAuthenticated) {
            navigate(redirectTo, { replace: true })
        }
    }, [isAuthenticated, loading, navigate, redirectTo])

    if (loading) {
        return (
            <div className="auth-loading-screen">
                <div className="auth-loading-spinner" aria-hidden="true" />
                <p className="auth-loading-text">در حال بارگذاری...</p>
            </div>
        )
    }

    if (isAuthenticated) {
        return <Navigate to={redirectTo} replace />
    }

    const handleSubmit = async (event) => {
        event.preventDefault()
        setError('')

        const trimmedUserName = userName.trim()
        if (!trimmedUserName || !password) {
            setError('لطفاً نام کاربری و رمز عبور را وارد کنید.')
            return
        }

        setSubmitting(true)
        try {
            await login(trimmedUserName, password)
            navigate(redirectTo, { replace: true })
        } catch (err) {
            setError(err.message || 'ورود ناموفق بود. لطفاً دوباره تلاش کنید.')
        } finally {
            setSubmitting(false)
        }
    }

    return (
        <div className="login-page">
            <section className="login-form-panel">
                <div className="login-form-wrap">
                    <div className="login-mobile-brand">
                        <div className="login-visual-icon">
                            <Icon name="building" />
                        </div>
                        <div>
                            <h1 className="login-visual-title" style={{ color: 'var(--hc-text)', fontSize: '1.1rem' }}>
                                همگام سیمان
                            </h1>
                            <p className="login-visual-subtitle" style={{ color: 'var(--hc-text-muted)' }}>
                                پنل مدیریت
                            </p>
                        </div>
                    </div>

                    <div className="login-card">
                        <header className="login-card-header">
                            <h2 className="login-card-title">ورود به سامانه</h2>
                            <p className="login-card-subtitle">برای ادامه، اطلاعات حساب کاربری خود را وارد کنید</p>
                        </header>

                        <form onSubmit={handleSubmit} noValidate>
                            {error && (
                                <div className="login-error" role="alert">
                                    <Icon name="circle-exclamation" />
                                    <span>{error}</span>
                                </div>
                            )}

                            <div className="login-field">
                                <label className="login-field-label" htmlFor="userName">
                                    نام کاربری
                                </label>
                                <div className="login-input-wrap">
                                    <input
                                        id="userName"
                                        type="text"
                                        className={`login-input ${error && !userName.trim() ? 'is-invalid' : ''}`}
                                        placeholder="نام کاربری خود را وارد کنید"
                                        value={userName}
                                        onChange={(e) => setUserName(e.target.value)}
                                        autoComplete="username"
                                        disabled={submitting}
                                        dir="ltr"
                                    />
                                    <Icon name="user" className="login-input-icon" />
                                </div>
                            </div>

                            <div className="login-field">
                                <label className="login-field-label" htmlFor="password">
                                    رمز عبور
                                </label>
                                <div className="login-input-wrap">
                                    <button
                                        type="button"
                                        className="login-toggle-password"
                                        onClick={() => setShowPassword((prev) => !prev)}
                                        aria-label={showPassword ? 'مخفی کردن رمز عبور' : 'نمایش رمز عبور'}
                                        tabIndex={-1}
                                    >
                                        <Icon name={showPassword ? 'eye-slash' : 'eye'} />
                                    </button>
                                    <input
                                        id="password"
                                        type={showPassword ? 'text' : 'password'}
                                        className={`login-input ${error && !password ? 'is-invalid' : ''}`}
                                        placeholder="رمز عبور خود را وارد کنید"
                                        value={password}
                                        onChange={(e) => setPassword(e.target.value)}
                                        autoComplete="current-password"
                                        disabled={submitting}
                                        dir="ltr"
                                        style={{ paddingInlineStart: '2.75rem' }}
                                    />
                                    <Icon name="lock" className="login-input-icon" />
                                </div>
                            </div>

                            <button type="submit" className="login-submit" disabled={submitting}>
                                {submitting ? (
                                    <>
                                        <span className="login-submit-spinner" aria-hidden="true" />
                                        در حال ورود...
                                    </>
                                ) : (
                                    <>
                                        <Icon name="sign-in" />
                                        ورود به پنل
                                    </>
                                )}
                            </button>
                        </form>

                        <footer className="login-footer">
                            <p className="login-footer-note">نشست شما به‌صورت امن ذخیره می‌شود</p>
                            <button
                                type="button"
                                className="login-theme-btn"
                                onClick={toggleTheme}
                                aria-label={theme === 'dark' ? 'حالت روشن' : 'حالت تاریک'}
                            >
                                <Icon name={theme === 'dark' ? 'sun' : 'moon'} />
                            </button>
                        </footer>
                    </div>
                </div>
            </section>
        </div>
    )
}

export default Login
