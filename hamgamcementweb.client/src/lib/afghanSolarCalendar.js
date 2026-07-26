import DateObjectModule from 'react-date-object'
import gregorianModule from 'react-date-object/calendars/gregorian'
import persianModule from 'react-date-object/calendars/persian'

const DateObject = DateObjectModule.default ?? DateObjectModule
const gregorian = gregorianModule.default ?? gregorianModule
const persian = persianModule.default ?? persianModule

// تقویم شمسی افغانستان — نام ماه‌ها: حمل، ثور، جوزا و ...
export const afghanSolarLocale = {
  name: 'afghan_solar',
  months: [
    ['حمل', 'ح'],
    ['ثور', 'ث'],
    ['جوزا', 'ج'],
    ['سرطان', 'سر'],
    ['اسد', 'ا'],
    ['سنبله', 'س'],
    ['میزان', 'م'],
    ['عقرب', 'ع'],
    ['قوس', 'ق'],
    ['جدی', 'ج'],
    ['دلو', 'د'],
    ['حوت', 'ح'],
  ],
  weekDays: [
    ['شنبه', 'ش'],
    ['یکشنبه', 'ی'],
    ['دوشنبه', 'د'],
    ['سه‌شنبه', 'س'],
    ['چهارشنبه', 'چ'],
    ['پنجشنبه', 'پ'],
    ['جمعه', 'ج'],
  ],
  digits: ['۰', '۱', '۲', '۳', '۴', '۵', '۶', '۷', '۸', '۹'],
  meridiems: [
    ['ق.ظ', 'ق.ظ'],
    ['ب.ظ', 'ب.ظ'],
  ],
}

function toDateObject(value) {
  if (!value) return null
  if (value instanceof DateObject) return value
  return new DateObject(value)
}

export function isoToJalaliObject(iso) {
  if (!iso) return null

  const normalized = String(iso).slice(0, 10)
  const jalali = new DateObject({
    date: normalized,
    calendar: gregorian,
  }).convert(persian)

  jalali.setLocale(afghanSolarLocale)
  return jalali.isValid ? jalali : null
}

export function isoToJalaliString(iso) {
  const jalali = isoToJalaliObject(iso)
  return jalali ? jalali.format('YYYY/MM/DD') : ''
}

export function toLatinDigits(value) {
  if (value == null || value === '') return ''

  return String(value)
    .replace(/[۰-۹]/g, (digit) => String('۰۱۲۳۴۵۶۷۸۹'.indexOf(digit)))
    .replace(/[٠-٩]/g, (digit) => String('٠١٢٣٤٥٦٧٨٩'.indexOf(digit)))
}

export function toLatinIsoDate(iso) {
  if (!iso) return ''
  return toLatinDigits(String(iso).slice(0, 10))
}

export function jalaliObjectToIso(date) {
  if (!date) return ''

  try {
    let jalali = toDateObject(date)

    if ((!jalali || !jalali.isValid) && typeof date === 'string') {
      jalali = new DateObject({
        date: toLatinDigits(date),
        format: 'YYYY/MM/DD',
        calendar: persian,
        locale: afghanSolarLocale,
      })
    }

    if (!jalali?.isValid) return ''

    const gregorianDate = jalali.convert(gregorian)
    const year = gregorianDate.year
    const month = String(gregorianDate.month?.number ?? gregorianDate.month).padStart(2, '0')
    const day = String(gregorianDate.day).padStart(2, '0')

    return toLatinDigits(`${year}-${month}-${day}`)
  } catch {
    return ''
  }
}

export function formatJalaliDate(iso) {
  const jalali = isoToJalaliString(iso)
  return jalali || '—'
}

export function todayGregorianIso() {
  return new DateObject({ calendar: gregorian }).format('YYYY-MM-DD')
}

/** سال و ماه جاری شمسی */
export function currentJalaliYearMonth() {
  const now = new DateObject({ calendar: persian, locale: afghanSolarLocale })
  return {
    year: now.year,
    month: Number(now.month?.number ?? now.month),
  }
}

/**
 * ابتدا و انتهای یک ماه شمسی به صورت ISO میلادی.
 * برای فیلتر حضور/حقوق ماهانه.
 */
export function getJalaliMonthRange(year, month) {
  const start = new DateObject({
    year: Number(year),
    month: Number(month),
    day: 1,
    calendar: persian,
    locale: afghanSolarLocale,
  })

  if (!start.isValid) {
    return { from: '', to: '' }
  }

  const daysInMonth = start.month.length
  const end = new DateObject(start).setDay(daysInMonth)

  return {
    from: jalaliObjectToIso(start),
    to: jalaliObjectToIso(end),
  }
}

/** ابتدا و انتهای سال شمسی به صورت ISO میلادی */
export function getJalaliYearRange(year) {
  const start = new DateObject({
    year: Number(year),
    month: 1,
    day: 1,
    calendar: persian,
    locale: afghanSolarLocale,
  })

  if (!start.isValid) {
    return { from: '', to: '' }
  }

  const endMonth = new DateObject({
    year: Number(year),
    month: 12,
    day: 1,
    calendar: persian,
    locale: afghanSolarLocale,
  })
  const end = new DateObject(endMonth).setDay(endMonth.month.length)

  return {
    from: jalaliObjectToIso(start),
    to: jalaliObjectToIso(end),
  }
}

export { gregorian, persian }
