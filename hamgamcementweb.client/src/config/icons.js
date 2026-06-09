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
  people: 'fa-solid fa-users',
  currencies: 'fa-solid fa-coins',
  production: 'fa-solid fa-gears',
  transport: 'fa-solid fa-truck',
  transactions: 'fa-solid fa-handshake',
  'sales-check': 'fa-solid fa-cart-circle-check',
  products: 'fa-solid fa-cubes',
  inventory: 'fa-solid fa-boxes-stacked',
  accounting: 'fa-solid fa-calculator',
  reporting: 'fa-solid fa-file-chart-column',
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
  lock: 'fa-solid fa-lock',
  eye: 'fa-solid fa-eye',
  'eye-slash': 'fa-solid fa-eye-slash',
  'sign-in': 'fa-solid fa-right-to-bracket',
  'circle-exclamation': 'fa-solid fa-circle-exclamation',
  'sign-out': 'fa-solid fa-right-from-bracket',
  users: 'fa-solid fa-user-gear',
  'user-shield': 'fa-solid fa-user-shield',
  plus: 'fa-solid fa-plus',
  edit: 'fa-solid fa-pen-to-square',
  trash: 'fa-solid fa-trash-can',
  'key': 'fa-solid fa-key',
  exchange: 'fa-solid fa-arrow-right-arrow-left',
  star: 'fa-solid fa-star',
}

export function getIcon(name) {
  return icons[name] ?? null
}
