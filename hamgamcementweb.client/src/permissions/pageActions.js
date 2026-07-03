/**
 * عملیات اضافهٔ هر صفحه (غیر از CRUD استاندارد).
 * کلید: pageKey مثل people.employees
 */
export const PAGE_EXTRA_ACTIONS = {
  'users.list': [{ key: 'changePassword', label: 'تغییر رمز' }],
  'users.roles': [{ key: 'manage', label: 'مدیریت دسترسی' }],
  'currencies.list': [
    { key: 'setRate', label: 'ثبت نرخ' },
    { key: 'setBase', label: 'تعیین ارز پایه' },
  ],
  'people.customers': [{ key: 'viewDeleted', label: 'مشاهده حذف‌شده‌ها' }],
  'people.suppliers': [{ key: 'viewDeleted', label: 'مشاهده حذف‌شده‌ها' }],
}
