import { useEffect, useState } from 'react'
import JalaliDateField from '../../components/common/JalaliDateField'
import Icon from '../../components/common/Icon'
import { todayGregorianIso, toLatinIsoDate } from '../../lib/afghanSolarCalendar'
import { fetchWarehouseOptions } from '../../services/inventoryApi'
import { postInventoryOpening } from '../../services/ledgerApi'
import { fetchProductOptions } from '../../services/productsApi'

const emptyLine = { productId: '', quantityInBase: '', unitCost: '' }

function InventoryOpeningPage() {
  const [warehouseOptions, setWarehouseOptions] = useState([])
  const [productOptions, setProductOptions] = useState([])
  const [warehouseId, setWarehouseId] = useState('')
  const [date, setDate] = useState(todayGregorianIso())
  const [lines, setLines] = useState([{ ...emptyLine }])
  const [submitting, setSubmitting] = useState(false)
  const [error, setError] = useState('')
  const [message, setMessage] = useState('')

  useEffect(() => {
    let cancelled = false
    async function loadOptions() {
      try {
        const [warehouses, products] = await Promise.all([
          fetchWarehouseOptions(),
          fetchProductOptions(),
        ])
        if (!cancelled) {
          setWarehouseOptions(warehouses ?? [])
          setProductOptions(products ?? [])
        }
      } catch {
        if (!cancelled) {
          setWarehouseOptions([])
          setProductOptions([])
        }
      }
    }
    loadOptions()
    return () => {
      cancelled = true
    }
  }, [])

  const updateLine = (index, patch) => {
    setLines((prev) => prev.map((line, i) => (i === index ? { ...line, ...patch } : line)))
  }

  const addLine = () => setLines((prev) => [...prev, { ...emptyLine }])

  const removeLine = (index) => {
    setLines((prev) => (prev.length <= 1 ? prev : prev.filter((_, i) => i !== index)))
  }

  const handleSubmit = async (event) => {
    event.preventDefault()
    setError('')
    setMessage('')

    if (!warehouseId) {
      setError('انتخاب انبار الزامی است.')
      return
    }

    const validLines = lines
      .map((line) => ({
        productId: Number(line.productId),
        quantityInBase: Number(line.quantityInBase),
        unitCost: Number(line.unitCost),
      }))
      .filter(
        (line) =>
          line.productId > 0 &&
          Number.isFinite(line.quantityInBase) &&
          line.quantityInBase > 0 &&
          Number.isFinite(line.unitCost) &&
          line.unitCost >= 0,
      )

    if (validLines.length === 0) {
      setError('حداقل یک ردیف معتبر (محصول، مقدار، بهای واحد) الزامی است.')
      return
    }

    setSubmitting(true)
    try {
      const result = await postInventoryOpening({
        warehouseId: Number(warehouseId),
        date: toLatinIsoDate(date) || date || null,
        lines: validLines,
      })
      setMessage(
        result.message ||
          `موجودی اول دوره ثبت شد${result.entryNumber ? ` — سند ${result.entryNumber}` : ''}.`,
      )
      setLines([{ ...emptyLine }])
    } catch (err) {
      setError(err.message || 'ثبت موجودی اول دوره با خطا مواجه شد.')
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <div className="content-card card border-0">
      <div className="card-body p-4">
        <h2 className="card-title mb-2">موجودی اول دوره</h2>
        <p className="text-muted mb-4">
          ثبت موجودی ابتدای دوره برای انبار انتخابی؛ پس از ثبت، سند دفترروزنامه و لات موجودی ایجاد می‌شود.
        </p>

        {message && <div className="alert alert-success py-2">{message}</div>}
        {error && <div className="alert alert-danger py-2">{error}</div>}

        <form onSubmit={handleSubmit}>
          <div className="row g-3 mb-4">
            <div className="col-md-4">
              <label className="form-label">انبار</label>
              <select
                className="form-select"
                required
                value={warehouseId}
                onChange={(e) => setWarehouseId(e.target.value)}
              >
                <option value="">انتخاب کنید</option>
                {warehouseOptions.map((w) => (
                  <option key={w.value} value={w.value}>
                    {w.label}
                  </option>
                ))}
              </select>
            </div>
            <div className="col-md-3">
              <label className="form-label">تاریخ</label>
              <JalaliDateField value={date} onChange={setDate} />
            </div>
          </div>

          <div className="d-flex justify-content-between align-items-center mb-2">
            <strong>ردیف‌ها</strong>
            <button type="button" className="btn btn-sm btn-outline-secondary" onClick={addLine}>
              <Icon name="plus" size={14} className="me-1" />
              ردیف جدید
            </button>
          </div>

          {lines.map((line, index) => (
            <div className="row g-2 mb-2 align-items-end" key={index}>
              <div className="col-md-5">
                <label className="form-label">محصول</label>
                <select
                  className="form-select"
                  required
                  value={line.productId}
                  onChange={(e) => updateLine(index, { productId: e.target.value })}
                >
                  <option value="">انتخاب کنید</option>
                  {productOptions.map((p) => (
                    <option key={p.value} value={p.value}>
                      {p.label}
                    </option>
                  ))}
                </select>
              </div>
              <div className="col-md-3">
                <label className="form-label">مقدار (واحد پایه)</label>
                <input
                  type="number"
                  step="any"
                  min="0.0001"
                  className="form-control"
                  required
                  value={line.quantityInBase}
                  onChange={(e) => updateLine(index, { quantityInBase: e.target.value })}
                />
              </div>
              <div className="col-md-3">
                <label className="form-label">بهای واحد</label>
                <input
                  type="number"
                  step="any"
                  min="0"
                  className="form-control"
                  required
                  value={line.unitCost}
                  onChange={(e) => updateLine(index, { unitCost: e.target.value })}
                />
              </div>
              <div className="col-md-1">
                <button
                  type="button"
                  className="btn btn-outline-danger w-100"
                  title="حذف ردیف"
                  disabled={lines.length <= 1}
                  onClick={() => removeLine(index)}
                >
                  <Icon name="trash" size={14} />
                </button>
              </div>
            </div>
          ))}

          <div className="mt-4">
            <button type="submit" className="btn btn-primary" disabled={submitting}>
              {submitting ? 'در حال ثبت...' : 'ثبت موجودی اول دوره'}
            </button>
          </div>
        </form>
      </div>
    </div>
  )
}

export default InventoryOpeningPage
