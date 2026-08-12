import { useEffect, useState } from 'react'
import { fetchCategoryOptions } from '../../services/productsApi'
import { getProductsReportUrl } from '../../services/productsReportApi'

function ProductsReportPage() {
  const [categoryOptions, setCategoryOptions] = useState([])
  const [categoryId, setCategoryId] = useState('')
  const [activeOnly, setActiveOnly] = useState('all')
  const [belowMinStock, setBelowMinStock] = useState(false)
  const [error, setError] = useState('')

  useEffect(() => {
    let cancelled = false

    fetchCategoryOptions()
      .then((rows) => {
        if (!cancelled) setCategoryOptions(Array.isArray(rows) ? rows : [])
      })
      .catch(() => {
        if (!cancelled) setCategoryOptions([])
      })

    return () => {
      cancelled = true
    }
  }, [])

  const handleGenerate = () => {
    setError('')
    const url = getProductsReportUrl({
      categoryId: categoryId ? Number(categoryId) : null,
      activeOnly: activeOnly === 'all' ? null : activeOnly === 'active',
      belowMinStock,
    })
    window.open(url, '_blank', 'noopener,noreferrer')
  }

  return (
    <div className="content-card card border-0">
      <div className="card-body p-4">
        <h2 className="card-title mb-2">گزارش جامع محصولات</h2>
        <p className="text-muted mb-4">
          فهرست کامل محصولات با کد، دسته‌بندی، واحد، موجودی، حداقل موجودی، بهای خرید لحظه‌ای، قیمت فروش
          پیشنهادی و وضعیت. در صورت نیاز می‌توانید بر اساس دسته‌بندی، وضعیت فعال بودن یا کمبود موجودی فیلتر
          کنید.
        </p>

        {error && <div className="alert alert-danger py-2 mb-3">{error}</div>}

        <div className="row g-3 align-items-end">
          <div className="col-md-3">
            <label className="form-label" htmlFor="products-report-category">
              دسته‌بندی
            </label>
            <select
              id="products-report-category"
              className="form-select"
              value={categoryId}
              onChange={(e) => setCategoryId(e.target.value)}
            >
              <option value="">همه دسته‌ها</option>
              {categoryOptions.map((opt) => (
                <option key={opt.value ?? opt.id} value={opt.value ?? opt.id}>
                  {opt.label ?? opt.name}
                </option>
              ))}
            </select>
          </div>

          <div className="col-md-3">
            <label className="form-label" htmlFor="products-report-active">
              وضعیت
            </label>
            <select
              id="products-report-active"
              className="form-select"
              value={activeOnly}
              onChange={(e) => setActiveOnly(e.target.value)}
            >
              <option value="all">همه</option>
              <option value="active">فقط فعال</option>
              <option value="inactive">فقط غیرفعال</option>
            </select>
          </div>

          <div className="col-md-3">
            <div className="form-check mt-4">
              <input
                id="products-report-below-min"
                className="form-check-input"
                type="checkbox"
                checked={belowMinStock}
                onChange={(e) => setBelowMinStock(e.target.checked)}
              />
              <label className="form-check-label" htmlFor="products-report-below-min">
                فقط زیر حداقل موجودی
              </label>
            </div>
          </div>

          <div className="col-md-3">
            <button type="button" className="btn btn-primary w-100" onClick={handleGenerate}>
              ساخت گزارش
            </button>
          </div>
        </div>
      </div>
    </div>
  )
}

export default ProductsReportPage
