import { toLatinIsoDate } from '../lib/afghanSolarCalendar'

export function getJournalReportUrl(type, dateFrom, dateTo) {
  const params = new URLSearchParams({ type })
  const from = toLatinIsoDate(dateFrom)
  const to = toLatinIsoDate(dateTo)
  if (from) params.set('dateFrom', from)
  if (to) params.set('dateTo', to)
  return `/report-viewer/journal?${params.toString()}`
}

export function getLedgerReportUrl(accountId, dateFrom, dateTo, partyId) {
  const params = new URLSearchParams({ accountId: String(accountId) })
  const from = toLatinIsoDate(dateFrom)
  const to = toLatinIsoDate(dateTo)
  if (from) params.set('dateFrom', from)
  if (to) params.set('dateTo', to)
  if (partyId) params.set('partyId', String(partyId))
  return `/report-viewer/ledger?${params.toString()}`
}
