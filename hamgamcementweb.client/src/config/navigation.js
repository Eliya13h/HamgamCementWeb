export const navItems = [
  { path: '/', label: 'داشبورد', icon: 'dashboard' },
    { path: '/reports', label: 'آمار و تحلیل', icon: 'analytics' },

  {
    id: 'people',
    label: 'افراد',
    icon: 'people',
    children: [
      { path: '/people/customers', label: 'مشتریان' },
      { path: '/people/suppliers', label: 'تأمین‌کننده‌ها' },
      { path: '/people/employees', label: 'کارمندان' },
      { path: '/people/drivers', label: 'رانندگان' },
      { path: '/people/vehicle-owners', label: 'موترداران' },
      { path: '/people/departments', label: 'بخش‌ها' },
      { path: '/people/shareholders', label: 'سهام‌داران' },
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
    id: 'production',
    label: 'تولید',
    icon: 'production',
    children: [
      { path: '/production/daily', label: 'گزارش روزانه' },
      { path: '/production/plan', label: 'برنامه تولید' },
    ],
  },
  {
    id: 'transport',
    label: 'حمل و نقل',
    icon: 'transport',
    children: [
      { path: '/transport/shipping', label: 'حمل و نقل' },
      { path: '/transport/routes', label: 'مسیرها' },
      { path: '/transport/vehicles', label: 'وسایل نقلیه' },
      { path: '/transport/vehicle-types', label: 'انواع وسایل نقلیه' },
      { path: '/transport/maintenance', label: 'تعمیر و نگهداری' },
      { path: '/transport/invoices', label: 'فاکتور مصارف' },
      { path: '/transport/expense-categories', label: 'دسته‌بندی مصارف' },
    ],
  },
  {
    id: 'transactions',
    label: 'معاملات',
    icon: 'transactions',
    children: [
      { path: '/transactions/purchase', label: 'خرید' },
      { path: '/transactions/sale', label: 'فروش' },
    ],
  },
  {
    id: 'products',
    label: 'محصولات',
    icon: 'products',
    children: [
      { path: '/products/list', label: 'لیست محصولات' },
      { path: '/products/categories', label: 'دسته‌بندی' },
      { path: '/products/meaurments', label: 'واحدها' },
    ],
  },
  {
    id: 'inventory',
    label: 'انبار',
    icon: 'inventory',
    children: [
      { path: '/inventory/warehouses', label: 'انبارها' },
      { path: '/inventory/stock', label: 'موجودی' },
      { path: '/inventory/turnover', label: 'گردش کالا' },
      { path: '/inventory/stocktaking', label: 'سابقه انبارگردانی' },
    ],
  },
  {
    id: 'accounting',
    label: 'حسابداری',
    icon: 'accounting',
    children: [
      { path: '/accounting/revenues', label: 'عواید' },
      { path: '/accounting/expenses', label: 'مصارف' },
      { path: '/accounting/revenue-categories', label: 'دسته‌بندی عواید' },
      { path: '/accounting/expense-categories', label: 'دسته‌بندی مصارف' },
    ],
  },
  {
    id: 'reporting',
    label: 'گزارشات',
    icon: 'reporting',
    children: [
      { path: '/reporting/products', label: 'محصولات' },
      { path: '/reporting/production', label: 'تولیدات' },
      { path: '/reporting/transport', label: 'ترانسپورت' },
      { path: '/reporting/revenues', label: 'عواید' },
      { path: '/reporting/expenses', label: 'مصارف' },
      { path: '/reporting/journal', label: 'روزنامچه' },
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
  return children.some(
    (child) => pathname === child.path || pathname.startsWith(`${child.path}/`),
  )
}
