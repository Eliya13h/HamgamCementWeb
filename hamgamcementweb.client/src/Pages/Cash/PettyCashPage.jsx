import { useEffect, useState } from 'react'
import JalaliDateField from '../../components/common/JalaliDateField'
import { fetchCashBoxOptions, rechargePettyCash } from '../../services/ledgerApi'
import { fetchCurrencyOptions } from '../../services/transportApi'

export default function PettyCashPage() {
  const [boxes, setBoxes] = useState([])
  const [currencies, setCurrencies] = useState([])
  const [form, setForm] = useState({ cashBoxId: '', currencyId: '', amount: '', transferDate: new Date().toISOString().slice(0, 10) })
  const [message, setMessage] = useState('')
  const [error, setError] = useState('')

  useEffect(() => {
    Promise.all([fetchCashBoxOptions(), fetchCurrencyOptions()])
      .then(([b, c]) => { setBoxes(b ?? []); setCurrencies(c ?? []) })
      .catch((e) => setError(e.message))
  }, [])

  const submit = async (event) => {
    event.preventDefault()
    setMessage('')
    setError('')
    try {
      const result = await rechargePettyCash(Number(form.cashBoxId), {
        transferDate: form.transferDate,
        lines: [{ currencyId: Number(form.currencyId), amount: Number(form.amount) }],
      })
      setMessage(result.message ?? 'تنخواه شارژ شد.')
      setForm((prev) => ({ ...prev, amount: '' }))
    } catch (e) { setError(e.message) }
  }

  return <div className="users-page"><div className="content-card card border-0">
    <div className="card-header bg-transparent border-0 pt-4 px-4"><h2 className="card-title mb-0">شارژ تنخواه</h2></div>
    <form className="card-body p-4 row g-3 align-items-end" onSubmit={submit}>
      {error && <div className="col-12 alert alert-danger py-2">{error}</div>}
      {message && <div className="col-12 alert alert-success py-2">{message}</div>}
      <div className="col-md-4"><label className="form-label">تنخواه</label><select required className="form-select" value={form.cashBoxId} onChange={(e) => setForm({ ...form, cashBoxId: e.target.value })}><option value="">انتخاب کنید</option>{boxes.map((b) => <option key={b.value} value={b.value}>{b.label}</option>)}</select></div>
      <div className="col-md-3"><label className="form-label">ارز</label><select required className="form-select" value={form.currencyId} onChange={(e) => setForm({ ...form, currencyId: e.target.value })}><option value="">انتخاب کنید</option>{currencies.map((c) => <option key={c.value} value={c.value}>{c.label}</option>)}</select></div>
      <div className="col-md-2"><label className="form-label">مبلغ</label><input required type="number" min="0.0001" step="0.0001" className="form-control" value={form.amount} onChange={(e) => setForm({ ...form, amount: e.target.value })} /></div>
      <div className="col-md-3"><label className="form-label">تاریخ</label><JalaliDateField value={form.transferDate} onChange={(v) => setForm({ ...form, transferDate: v })} required /></div>
      <div className="col-12"><button className="btn btn-accent">ثبت شارژ</button></div>
    </form>
  </div></div>
}
