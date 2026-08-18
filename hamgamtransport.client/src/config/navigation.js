export const navItems = [
  { path: '/', label: 'داشبورد', icon: 'dashboard' },
  { path: '/reports', label: 'آمار و تحلیل', icon: 'analytics' },
  {
    id: 'transport',
    label: 'حمل‌ونقل',
    icon: 'transactions',
    children: [
      { path: '/transport/trips', label: 'سرویس‌ها' },
      { path: '/transport/vehicles', label: 'وسایل نقلیه' },
      { path: '/transport/vehicle-pairs', label: 'جفت وسیله' },
      { path: '/transport/trip-expense-categories', label: 'دسته هزینه سفر' },
    ],
  },
  {
    id: 'people',
    label: 'افراد',
    icon: 'people',
    children: [
      { path: '/people/drivers', label: 'رانندگان' },
      { path: '/people/vehicle-owners', label: 'مالکان وسیله' },
      { path: '/people/customers', label: 'مشتریان' },
    ],
  },
  {
    id: 'currencies',
    label: 'ارزها',
    icon: 'currencies',
    children: [
      { path: '/currencies/list', label: 'لیست ارزها' },
      { path: '/currencies/exchange', label: 'نوسانات' },
    ],
  },
  {
    id: 'accounting',
    label: 'حسابداری',
    icon: 'accounting',
    children: [
      { path: '/accounting/accounts', label: 'کدینگ حساب‌ها' },
      { path: '/accounting/journal-entries', label: 'اسناد دفتر' },
      { path: '/accounting/settlements', label: 'دریافت و پرداخت' },
      { path: '/accounting/cost-centers', label: 'مراکز هزینه' },
      { path: '/accounting/doubtful-provisions', label: 'ذخیره مطالبات مشکوک' },
      { path: '/accounting/recurring-journals', label: 'اسناد تکرارشونده' },
      { path: '/accounting/currency-exchange', label: 'خرید و فروش ارز' },
      { path: '/accounting/equity', label: 'حقوق صاحبان سهام' },
      { path: '/accounting/fixed-assets', label: 'دارایی‌های ثابت' },
      { path: '/accounting/revenues', label: 'عواید' },
      { path: '/accounting/expenses', label: 'مصارف' },
      {
        path: '/accounting/categories',
        label: 'دسته‌بندی‌ها',
        permissionPages: [
          { path: '/accounting/expense-categories', label: 'دسته‌بندی مصارف' },
          { path: '/accounting/revenue-categories', label: 'دسته‌بندی عواید' },
          { path: '/accounting/fixed-asset-categories', label: 'دسته‌بندی دارایی ثابت' },
        ],
      },
      { path: '/settings#fiscal-years', label: 'سال مالی', skipPermissionTree: true },
    ],
  },
  {
    id: 'cash',
    label: 'صندوق',
    icon: 'currencies',
    children: [
      { path: '/cash/boxes', label: 'تعریف صندوق' },
      { path: '/cash/banks', label: 'بانک‌ها' },
      { path: '/cash/shifts', label: 'شیفت و تحویل' },
      { path: '/cash/petty-cash', label: 'شارژ تنخواه' },
    ],
  },
  {
    id: 'reporting',
    label: 'گزارشات',
    icon: 'reporting',
    children: [
      { path: '/reporting/fleet', label: 'ناوگان' },
      { path: '/reporting/revenues', label: 'عواید' },
      { path: '/reporting/expenses', label: 'مصارف' },
      { path: '/reporting/journal', label: 'روزنامچه' },
      { path: '/reporting/ledger', label: 'دفتر کل' },
    ],
  },
  {
    id: 'users',
    label: 'کاربران',
    icon: 'users',
    children: [
      { path: '/users/list', label: 'کاربران' },
      { path: '/users/roles', label: 'سطح دسترسی' },
    ],
  },
]

export const settingsNavItem = {
  path: '/settings',
  label: 'تنظیمات',
  icon: 'settings',
}

export function isNavGroup(item) {
  return Array.isArray(item.children) && item.children.length > 0
}

export function isChildActive(pathname, children) {
  return children.some((child) => {
    const path = String(child.path ?? '').split('#')[0]
    return pathname === path || pathname.startsWith(`${path}/`)
  })
}
