import { useState } from 'react'
import JalaliDateField from '../../components/common/JalaliDateField'
import { getJournalReportUrl } from '../../services/journalApi'

const journalSections = [
  {
    id: 'purchase',
    title: 'روزنامچه خرید',
    type: 'purchase',
    enabled: true,
  },
  {
    id: 'sale',
    title: 'روزنامچه فروش',
    type: 'sale',
    enabled: true,
  },
  {
    id: 'revenue',
    title: 'روزنامچه عواید',
    type: 'revenue',
    enabled: true,
  },
  {
    id: 'expense',
    title: 'روزنامچه مصارف',
    type: 'expense',
    enabled: true,
  },
  {
    id: 'production',
    title: 'روزنامچه تولید',
    type: 'production',
    enabled: true,
  },
  {
    id: 'general',
    title: 'روزنامچه عمومی',
    type: 'general',
    enabled: true,
  },
]

function JournalSection({ section, dateFrom, dateTo, onDateFromChange, onDateToChange, onError }) {
  const allowsOptionalDates =
    section.type === 'purchase' || section.type === 'sale' || section.type === 'general'

  const handleGenerate = () => {
    if (!section.enabled) {
      return
    }

    const hasFrom = Boolean(dateFrom)
    const hasTo = Boolean(dateTo)
    const hasBothDates = hasFrom && hasTo

    if (!allowsOptionalDates && !hasBothDates) {
      onError('لطفاً بازه تاریخ را انتخاب کنید.')
      return
    }

    if (hasBothDates && dateFrom > dateTo) {
      onError('تاریخ شروع نباید بعد از تاریخ پایان باشد.')
      return
    }

    onError('')
    window.open(getJournalReportUrl(section.type, dateFrom, dateTo), '_blank', 'noopener,noreferrer')
  }

  return (
    <div className="border rounded-3 p-3 mb-3">
      <div className="d-flex flex-wrap align-items-center justify-content-between gap-2 mb-3">
        <h3 className="h5 mb-0">{section.title}</h3>
        {!section.enabled && <span className="badge bg-secondary">به‌زودی</span>}
      </div>

      <div className="row g-3 align-items-end">
        <div className="col-md-3">
          <label className="form-label" htmlFor={`${section.id}-date-from`}>
            از تاریخ
          </label>
          <JalaliDateField value={dateFrom} onChange={onDateFromChange} />
        </div>
        <div className="col-md-3">
          <label className="form-label" htmlFor={`${section.id}-date-to`}>
            تا تاریخ
          </label>
          <JalaliDateField value={dateTo} onChange={onDateToChange} />
        </div>
        <div className="col-md-3">
          <button
            type="button"
            className="btn btn-primary w-100"
            onClick={handleGenerate}
            disabled={!section.enabled}
          >
            ساخت گزارش
          </button>
        </div>
      </div>
    </div>
  )
}

function JournalPage() {
  const [error, setError] = useState('')
  const [dates, setDates] = useState(() =>
    Object.fromEntries(journalSections.map((section) => [section.id, { from: '', to: '' }])),
  )

  const updateDate = (sectionId, field, value) => {
    setDates((prev) => ({
      ...prev,
      [sectionId]: {
        ...prev[sectionId],
        [field]: value,
      },
    }))
  }

  return (
    <div className="content-card card border-0">
      <div className="card-body p-4">
        <h2 className="card-title mb-2">روزنامچه</h2>
        <p className="text-muted mb-4">
          برای روزنامچه خرید، فروش و عمومی می‌توانید بازه تاریخ را خالی بگذارید (کل دوره)، فقط «از
          تاریخ» (تا انتها)، فقط «تا تاریخ» (از ابتدا)، یا هر دو را انتخاب کنید. برای سایر بخش‌ها بازه
          تاریخ الزامی است. روزنامچه عمومی دفتر روزنامه استاندارد دوطرفه است.
        </p>

        {error && <div className="alert alert-danger py-2 mb-3">{error}</div>}

        {journalSections.map((section) => (
          <JournalSection
            key={section.id}
            section={section}
            dateFrom={dates[section.id].from}
            dateTo={dates[section.id].to}
            onDateFromChange={(value) => updateDate(section.id, 'from', value)}
            onDateToChange={(value) => updateDate(section.id, 'to', value)}
            onError={setError}
          />
        ))}
      </div>
    </div>
  )
}

export default JournalPage
