/** تنظیمات مشترک DataTable سمت سرور */

import { withDataTableLayoutFit } from './dataTableLayout'

export const dataTableLanguage = {
  emptyTable: 'داده‌ای برای نمایش وجود ندارد',
  info: 'نمایش _START_ تا _END_ از _TOTAL_ ردیف',
  infoEmpty: 'رکوردی یافت نشد',
  infoFiltered: '(فیلتر شده از _MAX_ ردیف)',
  lengthMenu: 'نمایش _MENU_ ردیف',
  loadingRecords: 'در حال بارگذاری...',
  processing: 'در حال پردازش...',
  search: '',
  zeroRecords: 'رکوردی یافت نشد',
  paginate: {
    first: 'اول',
    last: 'آخر',
    next: 'بعدی',
    previous: 'قبلی',
  },
}

const PHONE_FIELD_PATTERN = /phone|mobile|tel|userName/i

/** فرمت مبلغ با جداکننده هزارگان؛ صفرهای اعشارِ انتهایی نمایش داده نمی‌شوند */
export function formatAmount(value) {
  if (value === null || value === undefined || value === '') return '—'
  const num = Number(value)
  if (Number.isNaN(num)) return String(value)

  const sign = num < 0 ? '-' : ''
  const abs = Math.abs(num)
  // حداکثر ۸ رقم اعشار (هم‌تراز decimalهای مالی)، سپس حذف صفرهای انتهایی
  const fixed = abs.toFixed(8).replace(/\.?0+$/, '')
  const [intPart, fracPart] = fixed.split('.')
  const grouped = intPart.replace(/\B(?=(\d{3})+(?!\d))/g, ',')
  return sign + (fracPart ? `${grouped}.${fracPart}` : grouped)
}

export function isPhoneField(fieldName) {
  if (!fieldName) return false
  return PHONE_FIELD_PATTERN.test(fieldName)
}

export function amountRender(data) {
  return formatAmount(data)
}

/** جلوگیری از نمایش دوبارهٔ جستجو در topEnd پیش‌فرض DataTables */
export function buildDataTableLayout({ searching = true } = {}) {
  return {
    topStart: {
      pageLength: { menu: [10, 15, 25, 50, 100] },
      ...(searching ? { search: { placeholder: 'جستجو در همه ستون‌ها...' } } : {}),
    },
    topEnd: null,
    bottomStart: 'info',
    bottomEnd: {
      paging: { firstLast: true, previousNext: true, numbers: 5 },
    },
  }
}

export const baseServerSideTableOptions = {
  processing: true,
  serverSide: true,
  paging: true,
  searching: true,
  ordering: true,
  info: true,
  // بدون scrollX داخلی تا هدر و بدنه یک جدول بمانند؛ اسکرول افقی با CSS
  scrollX: false,
  autoWidth: false,
  responsive: true,
  stripeClasses: ['odd', 'even'],
  pageLength: 15,
  lengthMenu: [10, 15, 25, 50, 100],
  language: dataTableLanguage,
}

export function createServerSideTableOptions({ searching = true, ...overrides } = {}) {
  return withDataTableLayoutFit({
    ...baseServerSideTableOptions,
    searching,
    layout: buildDataTableLayout({ searching }),
    ...overrides,
  })
}
