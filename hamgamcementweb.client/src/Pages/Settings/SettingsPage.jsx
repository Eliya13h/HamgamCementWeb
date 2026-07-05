import { useEffect, useState } from 'react'
import Icon from '../../components/common/Icon'
import { usePageCrud } from '../../permissions/usePageCrud'
import {
  fetchGeneralSettings,
  updateGeneralSettings,
  uploadCompanyLogo,
} from '../../services/settingsApi'

const DEFAULT_ZM_LOGO_PATH = '/zm_logo.jpg'

const emptyForm = {
  persianCompanyName: '',
  englishCompanyName: '',
  zmLogoPath: DEFAULT_ZM_LOGO_PATH,
  companyLogoPath: '',
  companyAddress: '',
  companyPhoneNumber1: '',
  companyPhoneNumber2: '',
  companyPhoneNumber3: '',
  companyEmail: '',
  companySite: '',
}

function SettingsPage() {
  const { canEdit } = usePageCrud('/settings')
  const [form, setForm] = useState(emptyForm)
  const [loading, setLoading] = useState(true)
  const [submitting, setSubmitting] = useState(false)
  const [uploadingLogo, setUploadingLogo] = useState(false)
  const [loadError, setLoadError] = useState('')
  const [formError, setFormError] = useState('')
  const [successMessage, setSuccessMessage] = useState('')

  useEffect(() => {
    let active = true

    fetchGeneralSettings()
      .then((data) => {
        if (!active) return
        setForm({
          persianCompanyName: data.persianCompanyName ?? '',
          englishCompanyName: data.englishCompanyName ?? '',
          zmLogoPath: data.zmLogoPath || DEFAULT_ZM_LOGO_PATH,
          companyLogoPath: data.companyLogoPath ?? '',
          companyAddress: data.companyAddress ?? '',
          companyPhoneNumber1: data.companyPhoneNumber1 ?? '',
          companyPhoneNumber2: data.companyPhoneNumber2 ?? '',
          companyPhoneNumber3: data.companyPhoneNumber3 ?? '',
          companyEmail: data.companyEmail ?? '',
          companySite: data.companySite ?? '',
        })
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

    setUploadingLogo(true)
    setFormError('')
    setSuccessMessage('')

    try {
      const result = await uploadCompanyLogo(file)
      setForm((prev) => ({
        ...prev,
        companyLogoPath: result.companyLogoPath ?? prev.companyLogoPath,
      }))
      setSuccessMessage(result.message ?? 'لوگوی سازمان آپلود شد.')
    } catch (error) {
      setFormError(error.message)
    } finally {
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
        setForm({
          persianCompanyName: result.settings.persianCompanyName ?? '',
          englishCompanyName: result.settings.englishCompanyName ?? '',
          zmLogoPath: result.settings.zmLogoPath || DEFAULT_ZM_LOGO_PATH,
          companyLogoPath: result.settings.companyLogoPath ?? '',
          companyAddress: result.settings.companyAddress ?? '',
          companyPhoneNumber1: result.settings.companyPhoneNumber1 ?? '',
          companyPhoneNumber2: result.settings.companyPhoneNumber2 ?? '',
          companyPhoneNumber3: result.settings.companyPhoneNumber3 ?? '',
          companyEmail: result.settings.companyEmail ?? '',
          companySite: result.settings.companySite ?? '',
        })
      }

      setSuccessMessage(result.message ?? 'تنظیمات ذخیره شد.')
    } catch (error) {
      setFormError(error.message)
    } finally {
      setSubmitting(false)
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
                      وب‌سایت
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
                    />
                  </div>

                  <div className="col-md-4">
                    <label className="form-label" htmlFor="companyPhoneNumber2">
                      تلفن ۲
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
                      تلفن ۳
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
                    />
                  </div>

                  <div className="col-md-6">
                    <label className="form-label" htmlFor="zmLogoPath">
                      آدرس لوگوی ZM
                    </label>
                    <input
                      id="zmLogoPath"
                      type="text"
                      className="form-control"
                      dir="ltr"
                      value={form.zmLogoPath}
                      readOnly
                    />
                    <div className="form-text">لوگوی ZM ثابت است و از فایل داخل پروژه استفاده می‌شود.</div>
                  </div>

                  <div className="col-md-6">
                    <label className="form-label">پیش‌نمایش لوگوی ZM</label>
                    <div className="settings-logo-preview border rounded p-2 bg-light">
                      <img
                        src={form.zmLogoPath || DEFAULT_ZM_LOGO_PATH}
                        alt="لوگوی ZM"
                        className="settings-logo-image"
                      />
                    </div>
                  </div>

                  <div className="col-md-6">
                    <label className="form-label" htmlFor="companyLogoPath">
                      آدرس لوگوی سازمان
                    </label>
                    <input
                      id="companyLogoPath"
                      type="text"
                      className="form-control"
                      dir="ltr"
                      value={form.companyLogoPath}
                      readOnly
                      placeholder="هنوز لوگویی آپلود نشده"
                    />
                  </div>

                  <div className="col-md-6">
                    <label className="form-label" htmlFor="companyLogoFile">
                      آپلود لوگوی سازمان
                    </label>
                    <input
                      id="companyLogoFile"
                      type="file"
                      className="form-control"
                      accept="image/jpeg,image/png,image/webp"
                      onChange={handleLogoSelect}
                      disabled={!canEdit || uploadingLogo}
                    />
                    <div className="form-text">
                      فایل در سرور ذخیره می‌شود و در گزارش‌ها قابل استفاده است.
                    </div>
                    {uploadingLogo && (
                      <div className="small text-muted mt-1">در حال آپلود لوگو...</div>
                    )}
                  </div>

                  {form.companyLogoPath && (
                    <div className="col-md-6">
                      <label className="form-label">پیش‌نمایش لوگوی سازمان</label>
                      <div className="settings-logo-preview border rounded p-2 bg-light">
                        <img
                          src={form.companyLogoPath}
                          alt="لوگوی سازمان"
                          className="settings-logo-image"
                        />
                      </div>
                    </div>
                  )}
                </div>
              </section>

              <section className="settings-group mb-4 opacity-50">
                <div className="settings-group-header d-flex align-items-center gap-2 mb-2 pb-2 border-bottom">
                  <Icon name="settings" />
                  <h3 className="h5 mb-0">سایر گروه‌های تنظیمات</h3>
                </div>
                <p className="text-muted mb-0 small">به‌زودی اضافه می‌شود.</p>
              </section>

              {canEdit && (
                <div className="d-flex justify-content-end">
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
        </div>
      </div>
    </div>
  )
}

export default SettingsPage
