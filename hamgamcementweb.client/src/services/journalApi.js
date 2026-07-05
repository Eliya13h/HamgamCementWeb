import { toLatinIsoDate } from '../lib/afghanSolarCalendar'

export function getJournalReportUrl(type, dateFrom, dateTo) {
  const params = new URLSearchParams({
    type,
    dateFrom: toLatinIsoDate(dateFrom),
    dateTo: toLatinIsoDate(dateTo),
  })
  return `/report-viewer/journal?${params.toString()}`
}
