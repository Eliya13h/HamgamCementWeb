import overrides from '../styles/jalali-datepicker-overrides.css?inline'

const STYLE_ID = 'hc-rmdp-overrides'

/** استایل تقویم را بعد از CSS تزریق‌شده کتابخانه دوباره به انتهای head می‌برد */
export function applyRmdpTheme() {
  if (typeof document === 'undefined') return

  let style = document.getElementById(STYLE_ID)
  if (!style) {
    style = document.createElement('style')
    style.id = STYLE_ID
    style.textContent = overrides
    document.head.appendChild(style)
    return
  }

  document.head.appendChild(style)
}
