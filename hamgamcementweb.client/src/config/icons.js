/**
 * Font Awesome 6 Pro — self-hosted from team zip.
 * Icon keys map to FA class strings. Register new icons here.
 *
 * Find icon names: https://fontawesome.com/icons (Pro)
 * Style prefix: fa-solid | fa-regular | fa-light | fa-thin | fa-duotone
 */
export const icons = {
  dashboard: 'fa-solid fa-grid-2',
  analytics: 'fa-solid fa-chart-line',
  employees: 'fa-solid fa-users',
  production: 'fa-solid fa-gears',
  sales: 'fa-solid fa-cart-shopping',
  'sales-check': 'fa-solid fa-cart-circle-check',
  inventory: 'fa-solid fa-boxes-stacked',
  settings: 'fa-solid fa-gear',
  search: 'fa-solid fa-magnifying-glass',
  bell: 'fa-solid fa-bell',
  sun: 'fa-solid fa-sun',
  moon: 'fa-solid fa-moon',
  'sidebar-open': 'fa-solid fa-sidebar',
  'sidebar-close': 'fa-solid fa-sidebar-flip',
  user: 'fa-solid fa-user',
  'chevron-down': 'fa-solid fa-chevron-down',
  'chevron-up': 'fa-solid fa-chevron-up',
  building: 'fa-solid fa-building',
  'clipboard-check': 'fa-solid fa-clipboard-check',
  'chart-up': 'fa-solid fa-chart-line-up',
  'submenu-dot': 'fa-regular fa-circle',
}

export function getIcon(name) {
  return icons[name] ?? null
}
