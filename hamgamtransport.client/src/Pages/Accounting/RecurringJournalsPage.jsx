import { useEffect, useState } from 'react'
import JalaliDateField from '../../components/common/JalaliDateField'
import { recurringJournalsApi } from '../../services/ledgerApi'

export default function RecurringJournalsPage() {
  const [rows, setRows] = useState([])
  const [date, setDate] = useState(new Date().toISOString().slice(0, 10))
  const [error, setError] = useState('')
  const [message, setMessage] = useState('')
  const load = () => recurringJournalsApi.list().then(setRows).catch((e) => setError(e.message))
  useEffect(() => { load() }, [])

  const generate = async (id) => {
    setError('')
    try {
      const result = await recurringJournalsApi.generate(id, { entryDate: date })
      setMessage(result.message ?? 'سند صادر شد.')
    } catch (e) { setError(e.message) }
  }
  const remove = async (id) => {
    if (!window.confirm('قالب حذف شود؟')) return
    try { await recurringJournalsApi.remove(id); load() } catch (e) { setError(e.message) }
  }

  return <div className="users-page"><div className="content-card card border-0">
    <div className="card-header bg-transparent border-0 pt-4 px-4 d-flex justify-content-between"><div><h2 className="card-title mb-1">اسناد تکرارشونده</h2><p className="text-muted mb-0 small">قالب‌های ثبت‌شده را در تاریخ انتخابی به سند دفتر تبدیل کنید.</p></div><div><label className="form-label small">تاریخ صدور</label><JalaliDateField value={date} onChange={setDate} /></div></div>
    <div className="card-body p-4">
      {error && <div className="alert alert-danger py-2">{error}</div>}{message && <div className="alert alert-success py-2">{message}</div>}
      <div className="table-responsive"><table className="table align-middle"><thead><tr><th>کد</th><th>نام</th><th>توضیحات</th><th>ردیف‌ها</th><th>وضعیت</th><th /></tr></thead><tbody>
        {rows.map((row) => <tr key={row.recurringJournalTemplateId}><td>{row.code}</td><td>{row.name}</td><td>{row.description || '—'}</td><td>{row.lineCount}</td><td>{row.isActive ? 'فعال' : 'غیرفعال'}</td><td className="d-flex gap-2"><button type="button" className="btn btn-sm btn-accent" disabled={!row.isActive} onClick={() => generate(row.recurringJournalTemplateId)}>صدور سند</button><button type="button" className="btn btn-sm btn-outline-danger" onClick={() => remove(row.recurringJournalTemplateId)}>حذف</button></td></tr>)}
      </tbody></table></div>
    </div>
  </div></div>
}
