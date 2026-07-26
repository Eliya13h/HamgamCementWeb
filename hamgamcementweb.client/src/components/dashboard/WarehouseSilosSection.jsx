import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { fetchWarehouseFillLevels } from '../../services/inventoryApi'
import WarehouseSiloCard from './WarehouseSiloCard'

function WarehouseSilosSection() {
  const [warehouses, setWarehouses] = useState([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')

  useEffect(() => {
    let cancelled = false

    async function load() {
      setLoading(true)
      setError('')
      try {
        const data = await fetchWarehouseFillLevels()
        if (!cancelled) {
          setWarehouses(Array.isArray(data) ? data : [])
        }
      } catch (err) {
        if (!cancelled) {
          setWarehouses([])
          setError(err.message || 'بارگذاری وضعیت انبارها با خطا مواجه شد.')
        }
      } finally {
        if (!cancelled) setLoading(false)
      }
    }

    load()
    return () => {
      cancelled = true
    }
  }, [])

  return (
    <section className="mb-4">
      <div className="content-card card border-0">
        <div className="card-header bg-transparent border-0 pt-4 px-4 pb-0 d-flex align-items-center justify-content-between flex-wrap gap-2">
          <div>
            <h3 className="card-title mb-1">مخازن و انبارها</h3>
            <p className="silo-section-subtitle mb-0">وضعیت پر بودن هر انبار بر اساس ظرفیت تعریف‌شده</p>
          </div>
          <Link to="/inventory/warehouses" className="btn btn-sm btn-outline-accent">
            مدیریت انبارها
          </Link>
        </div>

        <div className="card-body p-4">
          {loading && (
            <div className="silo-empty-state">
              <p className="placeholder-text mb-0">در حال بارگذاری وضعیت انبارها…</p>
            </div>
          )}

          {!loading && error && (
            <div className="silo-empty-state">
              <p className="text-danger mb-0">{error}</p>
            </div>
          )}

          {!loading && !error && warehouses.length === 0 && (
            <div className="silo-empty-state">
              <p className="placeholder-text mb-0">هنوز انباری ثبت نشده است.</p>
            </div>
          )}

          {!loading && !error && warehouses.length > 0 && (
            <div className="silo-grid">
              {warehouses.map((warehouse) => (
                <WarehouseSiloCard key={warehouse.warehouseId} warehouse={warehouse} />
              ))}
            </div>
          )}
        </div>
      </div>
    </section>
  )
}

export default WarehouseSilosSection
