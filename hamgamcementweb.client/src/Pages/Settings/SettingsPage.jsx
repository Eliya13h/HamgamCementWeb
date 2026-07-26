import { useCallback, useEffect, useState } from 'react'
import Icon from '../../components/common/Icon'
import { useAuth } from '../../context/AuthContext'
import { formatJalaliDate } from '../../lib/afghanSolarCalendar'
import { formatAmount } from '../../lib/dataTableOptions'
import { usePageCrud } from '../../permissions/usePageCrud'
import {
  closeFiscalYear,
  fetchFiscalYearClosingPreview,
  fetchFiscalYears,
  fetchGeneralSettings,
  reopenFiscalYear,
  updateGeneralSettings,
  uploadCompanyLogo,
} from '../../services/settingsApi'

const emptyForm = {
  persianCompanyName: '',
  englishCompanyName: '',
  companyLogoPath: '',
  companyAddress: '',
  companyPhoneNumber1: '',
  companyPhoneNumber2: '',
  companyPhoneNumber3: '',
  companyEmail: '',
  companySite: '',
}

const emptyModal = {
  mode: null,
  year: null,
  preview: null,
  password: '',
  loading: false,
  error: '',
}

function withCacheBust(path) {
  if (!path) return ''
  const separator = path.includes('?') ? '&' : '?'
  return `${path}${separator}t=${Date.now()}`
}

function SettingsPage() {
  const { canEdit } = usePageCrud('/settings')
  const { user } = useAuth()
  const [form, setForm] = useState(emptyForm)
  const [logoPreviewSrc, setLogoPreviewSrc] = useState('')
  const [logoFileName, setLogoFileName] = useState('')
  const [loading, setLoading] = useState(true)
  const [submitting, setSubmitting] = useState(false)
  const [uploadingLogo, setUploadingLogo] = useState(false)
  const [loadError, setLoadError] = useState('')
  const [formError, setFormError] = useState('')
  const [successMessage, setSuccessMessage] = useState('')

  const [fiscalYears, setFiscalYears] = useState([])
  const [fiscalIsAdmin, setFiscalIsAdmin] = useState(false)
  const [fiscalLoading, setFiscalLoading] = useState(true)
  const [fiscalError, setFiscalError] = useState('')
  const [modal, setModal] = useState(emptyModal)

  // نقش مدیر سیستم از سرور؛ نام کاربری مهم نیست
  const canManageFiscalYear =
    fiscalIsAdmin || user?.roleName === 'مدیر سیستم'

  const loadFiscalYears = useCallback(async () => {
    setFiscalLoading(true)
    setFiscalError('')
    try {
      const data = await fetchFiscalYears()
      setFiscalYears(data.items ?? [])
      setFiscalIsAdmin(Boolean(data.isAdmin))
    } catch (error) {
      setFiscalError(error.message)
    } finally {
      setFiscalLoading(false)
    }
  }, [])

  useEffect(() => {
    let active = true

    fetchGeneralSettings()
      .then((data) => {
        if (!active) return
        const companyLogoPath = data.companyLogoPath ?? ''
        setForm({
          persianCompanyName: data.persianCompanyName ?? '',
          englishCompanyName: data.englishCompanyName ?? '',
          companyLogoPath,
          companyAddress: data.companyAddress ?? '',
          companyPhoneNumber1: data.companyPhoneNumber1 ?? '',
          companyPhoneNumber2: data.companyPhoneNumber2 ?? '',
          companyPhoneNumber3: data.companyPhoneNumber3 ?? '',
          companyEmail: data.companyEmail ?? '',
          companySite: data.companySite ?? '',
        })
        setLogoPreviewSrc(companyLogoPath ? withCacheBust(companyLogoPath) : '')
        setLoadError('')
      })
      .catch((error) => {
        if (!active) return
        setLoadError(error.message)
      })
      .finally(() => {
        if (active) setLoading(false)
      })

    return () => {
      active = false
    }
  }, [])

  useEffect(() => {
    loadFiscalYears()
  }, [loadFiscalYears])

  const updateField = (field, value) => {
    setForm((prev) => ({ ...prev, [field]: value }))
    setSuccessMessage('')
  }

  const handleLogoSelect = async (event) => {
    const file = event.target.files?.[0]
    event.target.value = ''

    if (!file || !canEdit) {
      return
    }

    setLogoFileName(file.name)
    const localPreview = URL.createObjectURL(file)
    setLogoPreviewSrc(localPreview)
    setUploadingLogo(true)
    setFormError('')
    setSuccessMessage('')

    try {
      const result = await uploadCompanyLogo(file)
      const companyLogoPath = result.companyLogoPath ?? ''
      setForm((prev) => ({
        ...prev,
        companyLogoPath: companyLogoPath || prev.companyLogoPath,
      }))
      setLogoPreviewSrc(withCacheBust(companyLogoPath || form.companyLogoPath))
      setSuccessMessage(result.message ?? 'لوگوی سازمان آپلود شد.')
    } catch (error) {
      setFormError(error.message)
      setLogoFileName('')
      setLogoPreviewSrc(form.companyLogoPath ? withCacheBust(form.companyLogoPath) : '')
    } finally {
      requestAnimationFrame(() => URL.revokeObjectURL(localPreview))
      setUploadingLogo(false)
    }
  }

  const handleSubmit = async (event) => {
    event.preventDefault()

    if (!canEdit) {
      return
    }

    setSubmitting(true)
    setFormError('')
    setSuccessMessage('')

    try {
      const result = await updateGeneralSettings({
        persianCompanyName: form.persianCompanyName,
        englishCompanyName: form.englishCompanyName,
        companyLogoPath: form.companyLogoPath,
        companyAddress: form.companyAddress,
        companyPhoneNumber1: form.companyPhoneNumber1,
        companyPhoneNumber2: form.companyPhoneNumber2,
        companyPhoneNumber3: form.companyPhoneNumber3,
        companyEmail: form.companyEmail,
        companySite: form.companySite,
      })

      if (result.settings) {
        const companyLogoPath = result.settings.companyLogoPath ?? ''
        setForm({
          persianCompanyName: result.settings.persianCompanyName ?? '',
          englishCompanyName: result.settings.englishCompanyName ?? '',
          companyLogoPath,
          companyAddress: result.settings.companyAddress ?? '',
          companyPhoneNumber1: result.settings.companyPhoneNumber1 ?? '',
          companyPhoneNumber2: result.settings.companyPhoneNumber2 ?? '',
          companyPhoneNumber3: result.settings.companyPhoneNumber3 ?? '',
          companyEmail: result.settings.companyEmail ?? '',
          companySite: result.settings.companySite ?? '',
        })
        setLogoPreviewSrc(companyLogoPath ? withCacheBust(companyLogoPath) : '')
      }

      setSuccessMessage(result.message ?? 'تنظیمات ذخیره شد.')
    } catch (error) {
      setFormError(error.message)
    } finally {
      setSubmitting(false)
    }
  }

  const openCloseModal = async (year) => {
    setModal({
      mode: 'close',
      year,
      preview: null,
      password: '',
      loading: true,
      error: '',
    })

    try {
      const preview = await fetchFiscalYearClosingPreview(year.fiscalYearId)
      setModal((prev) => ({ ...prev, preview, loading: false }))
    } catch (error) {
      setModal((prev) => ({ ...prev, loading: false, error: error.message }))
    }
  }

  const openSummaryModal = async (year) => {
    setModal({
      mode: 'summary',
      year,
      preview: null,
      password: '',
      loading: true,
      error: '',
    })

    try {
      const preview = await fetchFiscalYearClosingPreview(year.fiscalYearId)
      setModal((prev) => ({ ...prev, preview, loading: false }))
    } catch (error) {
      setModal((prev) => ({ ...prev, loading: false, error: error.message }))
    }
  }

  const openReopenModal = (year) => {
    setModal({
      mode: 'reopen',
      year,
      preview: null,
      password: '',
      loading: false,
      error: '',
    })
  }

  const closeModal = () => setModal(emptyModal)

  const submitModal = async (event) => {
    event.preventDefault()
    if (!modal.year || (modal.mode !== 'close' && modal.mode !== 'reopen')) {
      return
    }

    if (!modal.password.trim()) {
      setModal((prev) => ({ ...prev, error: 'رمز عبور الزامی است.' }))
      return
    }

    setModal((prev) => ({ ...prev, loading: true, error: '' }))

    try {
      const result =
        modal.mode === 'close'
          ? await closeFiscalYear(modal.year.fiscalYearId, modal.password)
          : await reopenFiscalYear(modal.year.fiscalYearId, modal.password)

      setSuccessMessage(result.message ?? 'عملیات با موفقیت انجام شد.')
      closeModal()
      await loadFiscalYears()
    } catch (error) {
      setModal((prev) => ({ ...prev, loading: false, error: error.message }))
    }
  }

  return (
    <div className="settings-page">
      <div className="content-card card border-0">
        <div className="card-header bg-transparent border-0 pt-4 px-4 pb-0">
          <h2 className="card-title mb-1">تنظیمات</h2>
          <p className="text-muted mb-0 small">پیکربندی عمومی سامانه و اطلاعات سازمان</p>
        </div>

        <div className="card-body p-4">
          {loading && (
            <div className="d-flex justify-content-center py-5">
              <div className="spinner-border text-primary" role="status" aria-label="در حال بارگذاری" />
            </div>
          )}

          {!loading && loadError && (
            <div className="alert alert-danger py-2">{loadError}</div>
          )}

          {!loading && !loadError && (
            <form onSubmit={handleSubmit}>
              {formError && <div className="alert alert-danger py-2">{formError}</div>}
              {successMessage && <div className="alert alert-success py-2">{successMessage}</div>}

              <section className="settings-group mb-4">
                <div className="settings-group-header d-flex align-items-center gap-2 mb-3 pb-2 border-bottom">
                  <Icon name="settings" className="text-primary" />
                  <h3 className="h5 mb-0">تنظیمات عمومی سایت</h3>
                </div>

                <div className="row g-3">
                  <div className="col-md-6">
                    <label className="form-label" htmlFor="persianCompanyName">
                      نام فارسی شرکت
                    </label>
                    <input
                      id="persianCompanyName"
                      type="text"
                      className="form-control"
                      value={form.persianCompanyName}
                      onChange={(e) => updateField('persianCompanyName', e.target.value)}
                      disabled={!canEdit}
                      required
                    />
                  </div>

                  <div className="col-md-6">
                    <label className="form-label" htmlFor="englishCompanyName">
                      نام انگلیسی شرکت
                    </label>
                    <input
                      id="englishCompanyName"
                      type="text"
                      className="form-control"
                      dir="ltr"
                      value={form.englishCompanyName}
                      onChange={(e) => updateField('englishCompanyName', e.target.value)}
                      disabled={!canEdit}
                      required
                    />
                  </div>

                  <div className="col-md-6">
                    <label className="form-label" htmlFor="companyAddress">
                      آدرس
                    </label>
                    <input
                      id="companyAddress"
                      type="text"
                      className="form-control"
                      value={form.companyAddress}
                      onChange={(e) => updateField('companyAddress', e.target.value)}
                      disabled={!canEdit}
                    />
                  </div>

                  <div className="col-md-6">
                    <label className="form-label" htmlFor="companySite">
                      وب‌سایت <span className="text-muted fw-normal">(اختیاری)</span>
                    </label>
                    <input
                      id="companySite"
                      type="text"
                      className="form-control"
                      dir="ltr"
                      value={form.companySite}
                      onChange={(e) => updateField('companySite', e.target.value)}
                      disabled={!canEdit}
                    />
                  </div>

                  <div className="col-md-4">
                    <label className="form-label" htmlFor="companyPhoneNumber1">
                      تلفن ۱
                    </label>
                    <input
                      id="companyPhoneNumber1"
                      type="text"
                      className="form-control"
                      dir="ltr"
                      value={form.companyPhoneNumber1}
                      onChange={(e) => updateField('companyPhoneNumber1', e.target.value)}
                      disabled={!canEdit}
                      required
                    />
                  </div>

                  <div className="col-md-4">
                    <label className="form-label" htmlFor="companyPhoneNumber2">
                      تلفن ۲ <span className="text-muted fw-normal">(اختیاری)</span>
                    </label>
                    <input
                      id="companyPhoneNumber2"
                      type="text"
                      className="form-control"
                      dir="ltr"
                      value={form.companyPhoneNumber2}
                      onChange={(e) => updateField('companyPhoneNumber2', e.target.value)}
                      disabled={!canEdit}
                    />
                  </div>

                  <div className="col-md-4">
                    <label className="form-label" htmlFor="companyPhoneNumber3">
                      تلفن ۳ <span className="text-muted fw-normal">(اختیاری)</span>
                    </label>
                    <input
                      id="companyPhoneNumber3"
                      type="text"
                      className="form-control"
                      dir="ltr"
                      value={form.companyPhoneNumber3}
                      onChange={(e) => updateField('companyPhoneNumber3', e.target.value)}
                      disabled={!canEdit}
                    />
                  </div>

                  <div className="col-md-6">
                    <label className="form-label" htmlFor="companyEmail">
                      ایمیل
                    </label>
                    <input
                      id="companyEmail"
                      type="email"
                      className="form-control"
                      dir="ltr"
                      value={form.companyEmail}
                      onChange={(e) => updateField('companyEmail', e.target.value)}
                      disabled={!canEdit}
                      required
                    />
                  </div>

                  <div className="col-md-6">
                    <label className="form-label" htmlFor="companyLogoFile">
                      آپلود لوگوی سازمان
                    </label>
                    <div className={`settings-file-input${(!canEdit || uploadingLogo) ? ' is-disabled' : ''}`}>
                      <input
                        id="companyLogoFile"
                        type="file"
                        className="settings-file-input__native"
                        accept="image/jpeg,image/png,image/webp"
                        onChange={handleLogoSelect}
                        disabled={!canEdit || uploadingLogo}
                      />
                      <label
                        htmlFor="companyLogoFile"
                        className="settings-file-input__button"
                      >
                        انتخاب فایل
                      </label>
                      <span className="settings-file-input__name" title={logoFileName || undefined}>
                        {logoFileName || 'فایل انتخاب نشده است'}
                      </span>
                    </div>
                    <div className="form-text">
                      فایل در سرور ذخیره می‌شود و در گزارش‌ها قابل استفاده است.
                    </div>
                    {uploadingLogo && (
                      <div className="small text-muted mt-1">در حال آپلود لوگو...</div>
                    )}
                  </div>

                  <div className="col-md-6">
                    <label className="form-label">پیش‌نمایش لوگوی سازمان</label>
                    <div className="settings-logo-preview border rounded p-2 bg-light">
                      {logoPreviewSrc ? (
                        <img
                          src={logoPreviewSrc}
                          alt="لوگوی سازمان"
                          className="settings-logo-image"
                        />
                      ) : (
                        <span className="text-muted small">هنوز لوگویی انتخاب نشده</span>
                      )}
                    </div>
                  </div>
                </div>
              </section>

              {canEdit && (
                <div className="d-flex justify-content-end mb-4">
                  <button
                    type="submit"
                    className="btn btn-accent d-inline-flex align-items-center gap-2"
                    disabled={submitting || uploadingLogo}
                  >
                    {submitting ? (
                      <>
                        <span className="spinner-border spinner-border-sm" role="status" aria-hidden="true" />
                        در حال ذخیره...
                      </>
                    ) : (
                      <>
                        <Icon name="edit" />
                        ذخیره تنظیمات
                      </>
                    )}
                  </button>
                </div>
              )}
            </form>
          )}

          <section className="settings-group mb-2">
            <div className="settings-group-header d-flex align-items-center gap-2 mb-3 pb-2 border-bottom">
              <Icon name="accounting" className="text-primary" />
              <h3 className="h5 mb-0">سال مالی</h3>
            </div>
            <p className="text-muted small mb-3">
              بستن سال مالی فقط توسط نقش «مدیر سیستم» و با تأیید رمز ورود امکان‌پذیر است.
              اسناد سال‌های بسته حذف نمی‌شوند و خلاصه آن‌ها در دسترس می‌ماند.
            </p>

            {fiscalLoading && (
              <div className="d-flex justify-content-center py-3">
                <div className="spinner-border spinner-border-sm text-primary" role="status" />
              </div>
            )}

            {!fiscalLoading && fiscalError && (
              <div className="alert alert-danger py-2">{fiscalError}</div>
            )}

            {!fiscalLoading && !fiscalError && (
              <div className="table-responsive">
                <table className="table table-sm align-middle mb-0">
                  <thead>
                    <tr>
                      <th>سال شمسی</th>
                      <th>بازه</th>
                      <th>وضعیت</th>
                      <th>سود/زیان خالص</th>
                      <th>سند اختتام</th>
                      <th className="text-end">عملیات</th>
                    </tr>
                  </thead>
                  <tbody>
                    {fiscalYears.length === 0 && (
                      <tr>
                        <td colSpan={6} className="text-muted text-center py-3">
                          سال مالی‌ای ثبت نشده است.
                        </td>
                      </tr>
                    )}
                    {fiscalYears.map((year) => {
                      const isClosed = year.status === 2
                      return (
                        <tr key={year.fiscalYearId}>
                          <td>{year.solarYear}</td>
                          <td className="small">
                            {formatJalaliDate(year.startDate)} تا {formatJalaliDate(year.endDate)}
                          </td>
                          <td>
                            <span className={`badge ${isClosed ? 'badge-inactive' : 'badge-active'}`}>
                              {year.statusLabel}
                            </span>
                          </td>
                          <td dir="ltr" className="text-start">
                            {formatAmount(year.netIncomeInBaseCurrency)}
                          </td>
                          <td>{year.closingEntryNumber || '—'}</td>
                          <td className="text-end">
                            <div className="d-inline-flex flex-wrap gap-1 justify-content-end">
                              <button
                                type="button"
                                className="btn btn-sm btn-outline-secondary"
                                onClick={() => openSummaryModal(year)}
                              >
                                خلاصه
                              </button>
                              {canManageFiscalYear && !isClosed && (
                                <button
                                  type="button"
                                  className="btn btn-sm btn-accent"
                                  onClick={() => openCloseModal(year)}
                                >
                                  بستن سال
                                </button>
                              )}
                              {canManageFiscalYear && isClosed && (
                                <button
                                  type="button"
                                  className="btn btn-sm btn-outline-danger"
                                  onClick={() => openReopenModal(year)}
                                >
                                  بازگشایی
                                </button>
                              )}
                            </div>
                          </td>
                        </tr>
                      )
                    })}
                  </tbody>
                </table>
              </div>
            )}
          </section>
        </div>
      </div>

      {modal.mode && (
        <div
          className="modal fade show d-block"
          tabIndex={-1}
          role="dialog"
          style={{ background: 'rgba(0,0,0,0.45)' }}
        >
          <div className="modal-dialog modal-dialog-centered">
            <div className="modal-content">
              <div className="modal-header">
                <h5 className="modal-title">
                  {modal.mode === 'close' && `بستن سال مالی ${modal.year?.solarYear ?? ''}`}
                  {modal.mode === 'reopen' && `بازگشایی سال مالی ${modal.year?.solarYear ?? ''}`}
                  {modal.mode === 'summary' && `خلاصه سال مالی ${modal.year?.solarYear ?? ''}`}
                </h5>
                <button type="button" className="btn-close" aria-label="بستن" onClick={closeModal} />
              </div>

              <form onSubmit={submitModal}>
                <div className="modal-body">
                  {modal.error && <div className="alert alert-danger py-2">{modal.error}</div>}

                  {modal.loading && !modal.preview && modal.mode !== 'reopen' && (
                    <div className="d-flex justify-content-center py-3">
                      <div className="spinner-border text-primary" role="status" />
                    </div>
                  )}

                  {(modal.mode === 'close' || modal.mode === 'summary') && modal.preview && (
                    <div className="mb-3">
                      <div className="row g-2 small">
                        <div className="col-6 text-muted">جمع درآمد</div>
                        <div className="col-6 text-end" dir="ltr">
                          {formatAmount(modal.preview.totalRevenueInBase)}
                        </div>
                        <div className="col-6 text-muted">جمع هزینه</div>
                        <div className="col-6 text-end" dir="ltr">
                          {formatAmount(modal.preview.totalExpenseInBase)}
                        </div>
                        <div className="col-6 text-muted">بهای تمام‌شده</div>
                        <div className="col-6 text-end" dir="ltr">
                          {formatAmount(modal.preview.totalCogsInBase)}
                        </div>
                        <div className="col-6 fw-semibold">سود/زیان خالص</div>
                        <div className="col-6 text-end fw-semibold" dir="ltr">
                          {formatAmount(modal.preview.netIncomeInBase)}
                        </div>
                      </div>
                      {modal.year?.closingEntryNumber && (
                        <div className="mt-3 small text-muted">
                          سند اختتام: {modal.year.closingEntryNumber}
                        </div>
                      )}
                    </div>
                  )}

                  {modal.mode === 'reopen' && (
                    <p className="small text-muted mb-3">
                      با بازگشایی، سند معکوس اختتام ثبت می‌شود و امکان ثبت مجدد در این سال فعال می‌گردد.
                    </p>
                  )}

                  {(modal.mode === 'close' || modal.mode === 'reopen') && (
                    <div>
                      <label className="form-label" htmlFor="fiscalPassword">
                        رمز ورود شما
                      </label>
                      <input
                        id="fiscalPassword"
                        type="password"
                        className="form-control"
                        autoComplete="current-password"
                        value={modal.password}
                        onChange={(e) =>
                          setModal((prev) => ({ ...prev, password: e.target.value, error: '' }))
                        }
                        disabled={modal.loading}
                        required
                      />
                      <div className="form-text">برای تأیید هویت مجدد، رمز ورود فعلی را وارد کنید.</div>
                    </div>
                  )}
                </div>

                <div className="modal-footer">
                  <button type="button" className="btn btn-outline-secondary" onClick={closeModal}>
                    انصراف
                  </button>
                  {(modal.mode === 'close' || modal.mode === 'reopen') && (
                    <button
                      type="submit"
                      className={`btn ${modal.mode === 'close' ? 'btn-accent' : 'btn-danger'}`}
                      disabled={modal.loading || !modal.password.trim()}
                    >
                      {modal.loading ? 'در حال انجام...' : modal.mode === 'close' ? 'تأیید بستن' : 'تأیید بازگشایی'}
                    </button>
                  )}
                </div>
              </form>
            </div>
          </div>
        </div>
      )}
    </div>
  )
}

export default SettingsPage
