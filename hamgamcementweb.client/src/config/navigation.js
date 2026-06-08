export const navItems = [
  { path: '/', label: 'داشبورد', icon: 'dashboard' },
    { path: '/reports', label: 'آمار و تحلیل', icon: 'analytics' },

  {
    id: 'employees',
    label: 'کارمندان',
    icon: 'employees',
    children: [
      { path: '/employees/org-chart', label: 'چارت سازمانی' },
      { path: '/employees/manage', label: 'مدیریت کارمندان' },
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
    id: 'sales',
    label: 'فروش',
    icon: 'sales',
    children: [
      { path: '/sales/orders', label: 'سفارش ها (آردر) ها' },
      { path: '/sales/customers', label: 'مشتریان' },
    ],
  },
  {
    id: 'inventory',
    label: 'انبار',
    icon: 'inventory',
    children: [
      { path: '/inventory/stock', label: 'موجودی' },
      { path: '/inventory/transfers', label: 'انتقالات' },
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
