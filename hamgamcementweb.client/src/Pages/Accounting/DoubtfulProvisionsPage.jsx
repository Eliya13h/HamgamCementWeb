import { useEffect, useState } from 'react'
import JalaliDateField from '../../components/common/JalaliDateField'
import AmountDisplay from '../../components/common/AmountDisplay'
import { doubtfulProvisionsApi } from '../../services/ledgerApi'

export default function DoubtfulProvisionsPage() {
  const [rows, setRows] = useState([])
  const [form, setForm] = useState({ provisionDate: new Date().toISOString().slice(0, 10), amountInBaseCurrency: '', description: '' })
  const [error, setError] = useState('')
  const [message, setMessage] = useState('')

  const load = () => doubtfulProvisionsApi.list().then(setRows).catch((e) => setError(e.message))
  useEffect(() => { load() }, [])

  const submit = async (event) => {
    event.preventDefault()
    setError('')
    try {
      const result = await doubtfulProvisionsApi.create({
        ...form,
        amountInBaseCurrency: Number(form.amountInBaseCurrency),
      })
      setMessage(result.message)
      setForm((prev) => ({ ...prev, amountInBaseCurrency: '', description: '' }))
      load()
    } catch (e) { setError(e.message) }
  }

  const remove = async (id) => {
    if (!window.confirm('ذخیره و سند مرتبط ابطال شود؟')) return
    try { await doubtfulProvisionsApi.remove(id); load() } catch (e) { setError(e.message) }
  }

  return (
    <div className="users-page">
      <div className="content-card card border-0">
        <div className="card-header bg-transparent border-0 pt-4 px-4"><h2 className="card-title mb-0">ذخیره مطالبات مشکوک</h2></div>
        <div className="card-body p-4">
          {error && <div className="alert alert-danger py-2">{error}</div>}
          {message && <div className="alert alert-success py-2">{message}</div>}
          <form className="row g-3 align-items-end mb-4" onSubmit={submit}>
            <div className="col-md-3"><label className="form-label">تاریخ</label><JalaliDateField value={form.provisionDate} onChange={(v) => setForm({ ...form, provisionDate: v })} required /></div>
            <div className="col-md-3"><label className="form-label">مبلغ پایه</label><input className="form-control" type="number" min="0.01" step="0.01" required value={form.amountInBaseCurrency} onChange={(e) => setForm({ ...form, amountInBaseCurrency: e.target.value })} /></div>
            <div className="col-md-4"><label className="form-label">توضیحات</label><input className="form-control" value={form.description} onChange={(e) => setForm({ ...form, description: e.target.value })} /></div>
            <div className="col-md-2"><button className="btn btn-accent w-100">ثبت ذخیره</button></div>
          </form>
          <div className="table-responsive"><table className="table align-middle mb-0"><thead><tr><th>تاریخ</th><th>توضیحات</th><th>مبلغ پایه</th><th>سند</th><th /></tr></thead><tbody>
            {rows.map((row) => <tr key={row.doubtfulDebtProvisionId}><td>{String(row.provisionDate).slice(0, 10)}</td><td>{row.description || '—'}</td><td><AmountDisplay value={row.amountInBaseCurrency} /></td><td>{row.journalEntryId || '—'}</td><td><button type="button" className="btn btn-sm btn-outline-danger" onClick={() => remove(row.doubtfulDebtProvisionId)}>ابطال</button></td></tr>)}
          </tbody></table></div>
        </div>
      </div>
    </div>
  )
}
