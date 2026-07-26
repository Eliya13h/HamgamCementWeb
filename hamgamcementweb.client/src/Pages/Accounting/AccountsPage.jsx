import { useEffect, useMemo, useState } from 'react'
import { fetchAccountTree } from '../../services/ledgerApi'

const LEVEL_LABEL = { 1: 'گروه', 2: 'کل', 3: 'معین', 4: 'تفصیلی' }

function AccountsPage() {
  const [rows, setRows] = useState([])
  const [error, setError] = useState('')
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    let cancelled = false
    ;(async () => {
      try {
        setLoading(true)
        const data = await fetchAccountTree()
        if (!cancelled) setRows(data)
      } catch (e) {
        if (!cancelled) setError(e.message)
      } finally {
        if (!cancelled) setLoading(false)
      }
    })()
    return () => {
      cancelled = true
    }
  }, [])

  const tree = useMemo(() => {
    const byParent = new Map()
    for (const row of rows) {
      const key = row.parentAccountId ?? 0
      if (!byParent.has(key)) byParent.set(key, [])
      byParent.get(key).push(row)
    }
    const walk = (parentId, depth) => {
      const children = byParent.get(parentId) ?? []
      return children.flatMap((node) => [
        { ...node, depth },
        ...walk(node.accountId, depth + 1),
      ])
    }
    return walk(0, 0)
  }, [rows])

  return (
    <div className="content-card card border-0">
      <div className="card-body p-4">
        <h2 className="card-title mb-3">کدینگ حساب‌ها</h2>
        {error ? <div className="alert alert-danger">{error}</div> : null}
        {loading ? (
          <div className="text-muted">در حال بارگذاری...</div>
        ) : (
          <div className="table-responsive">
            <table className="table table-sm table-hover align-middle mb-0">
              <thead>
                <tr>
                  <th>کد</th>
                  <th>نام</th>
                  <th>سطح</th>
                  <th>قابل ثبت</th>
                </tr>
              </thead>
              <tbody>
                {tree.map((row) => (
                  <tr key={row.accountId}>
                    <td style={{ paddingInlineStart: `${row.depth * 1.25}rem`, fontFamily: 'monospace' }}>
                      {row.code}
                    </td>
                    <td>{row.name}</td>
                    <td>{LEVEL_LABEL[row.level] ?? row.level}</td>
                    <td>{row.isPostable ? 'بله' : '—'}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>
    </div>
  )
}

export default AccountsPage
