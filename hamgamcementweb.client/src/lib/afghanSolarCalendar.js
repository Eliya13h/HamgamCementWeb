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

export const MIN_JALALI_YEAR = 1200
export const MAX_JALALI_YEAR = 1600

/** سال، ماه و روز شمسی از تاریخ میلادی ISO */
export function isoToJalaliParts(iso) {
  const jalali = isoToJalaliObject(iso)
  if (!jalali) return null
  return {
    year: jalali.year,
    month: Number(jalali.month?.number ?? jalali.month),
    day: jalali.day,
  }
}

export function currentJalaliParts() {
  const now = new DateObject({ calendar: persian, locale: afghanSolarLocale })
  return {
    year: now.year,
    month: Number(now.month?.number ?? now.month),
    day: now.day,
  }
}

export function jalaliDaysInMonth(year, month) {
  const start = new DateObject({
    year: Number(year),
    month: Number(month),
    day: 1,
    calendar: persian,
    locale: afghanSolarLocale,
  })
  return start.isValid ? start.month.length : 30
}

export function jalaliPartsToIso(year, month, day) {
  const y = Number(year)
  const m = Number(month)
  const d = Number(day)
  if (!Number.isFinite(y) || !Number.isFinite(m) || !Number.isFinite(d)) return ''

  const jalali = new DateObject({
    year: Math.min(MAX_JALALI_YEAR, Math.max(MIN_JALALI_YEAR, y)),
    month: Math.min(12, Math.max(1, m)),
    day: 1,
    calendar: persian,
    locale: afghanSolarLocale,
  })
  if (!jalali.isValid) return ''

  jalali.setDay(Math.min(Math.max(1, d), jalali.month.length))
  return jalaliObjectToIso(jalali)
}

/** افزایش/کاهش سال، ماه یا روز روی تاریخ شمسی (با عبور از مرز ماه/سال) */
export function addJalaliUnit(iso, unit, delta) {
  const base = isoToJalaliObject(iso) ?? new DateObject({ calendar: persian, locale: afghanSolarLocale })
  const next = new DateObject(base)
  const key = unit === 'year' ? 'years' : unit === 'month' ? 'months' : 'days'
  next.add(Number(delta) || 0, key)
  if (next.year > MAX_JALALI_YEAR) next.setYear(MAX_JALALI_YEAR)
  if (next.year < MIN_JALALI_YEAR) next.setYear(MIN_JALALI_YEAR)
  return jalaliObjectToIso(next)
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

/**
 * تعداد روزهای ماه شمسی تا امروز:
 * - presentDays: غیرجمعه
 * - holidayPaidDays: جمعه
 * ماه آینده → صفر؛ ماه گذشته → کل ماه؛ ماه جاری → تا امروز.
 */
export function countJalaliMonthDaysUntilToday(year, month) {
  const y = Number(year)
  const m = Number(month)
  const start = new DateObject({
    year: y,
    month: m,
    day: 1,
    calendar: persian,
    locale: afghanSolarLocale,
  })

  if (!start.isValid) {
    return { presentDays: 0, holidayPaidDays: 0 }
  }

  const today = new DateObject({ calendar: persian, locale: afghanSolarLocale })
  const todayY = today.year
  const todayM = Number(today.month?.number ?? today.month)
  const todayD = today.day
  const daysInMonth = start.month.length

  let lastDay = 0
  if (y > todayY || (y === todayY && m > todayM)) {
    lastDay = 0
  } else if (y === todayY && m === todayM) {
    lastDay = Math.min(daysInMonth, todayD)
  } else {
    lastDay = daysInMonth
  }

  let presentDays = 0
  let holidayPaidDays = 0

  for (let day = 1; day <= lastDay; day += 1) {
    const d = new DateObject({
      year: y,
      month: m,
      day,
      calendar: persian,
      locale: afghanSolarLocale,
    })
    const iso = jalaliObjectToIso(d)
    if (!iso) continue
    const jsDay = new Date(`${iso}T12:00:00`).getDay()
    if (jsDay === 5) holidayPaidDays += 1
    else presentDays += 1
  }

  return { presentDays, holidayPaidDays }
}

export { gregorian, persian }
