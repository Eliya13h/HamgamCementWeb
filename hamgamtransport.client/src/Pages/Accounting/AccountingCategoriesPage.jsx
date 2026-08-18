import { useEffect, useMemo, useState } from 'react'
import { useSearchParams } from 'react-router-dom'
import CrudTablePage from '../../components/common/CrudTablePage'
import {
  expenseCategoriesApi,
  revenueCategoriesApi,
  fixedAssetCategoriesApi,
} from '../../services/financeApi'
import { usePermission } from '../../permissions/usePermission'
import { pathPermission } from '../../permissions/utils'

const TABS = [
  {
    id: 'expenses',
    label: 'مصارف',
    title: 'دسته‌بندی مصارف',
    createLabel: 'دسته‌بندی جدید',
    api: expenseCategoriesApi,
    idField: 'expenseCategoryId',
    nameField: 'name',
    permissionPath: '/accounting/expense-categories',
    canDeleteRow: (row) => !row.isSystem,
    canEditRow: (row) => !row.isSystem || row.name === 'متفرقه',
    columns: [
      { data: 'name', title: 'نام دسته‌بندی' },
      { data: 'description', title: 'توضیحات', orderable: false },
      {
        data: 'isSystem',
        title: 'نوع',
        orderable: false,
        className: 'text-center',
        render: (data) =>
          data
            ? '<span class="badge bg-secondary">سیستمی</span>'
            : '<span class="badge bg-light text-dark">کاربری</span>',
      },
      {
        data: 'expensesCount',
        title: 'تعداد مصارف',
        orderable: false,
        className: 'text-center',
      },
      {
        data: 'isActive',
        title: 'وضعیت',
        className: 'text-center',
        render: (data) =>
          data
            ? '<span class="badge badge-active">فعال</span>'
            : '<span class="badge badge-inactive">غیرفعال</span>',
      },
    ],
    fields: [
      { name: 'name', label: 'نام دسته‌بندی', type: 'text', required: true },
      { name: 'description', label: 'توضیحات', type: 'textarea' },
      { name: 'isActive', label: 'فعال', type: 'switch', default: true },
    ],
  },
  {
    id: 'revenues',
    label: 'عواید',
    title: 'دسته‌بندی عواید',
    createLabel: 'دسته‌بندی جدید',
    api: revenueCategoriesApi,
    idField: 'revenueCategoryId',
    nameField: 'name',
    permissionPath: '/accounting/revenue-categories',
    canDeleteRow: (row) => !row.isSystem,
    canEditRow: (row) => !row.isSystem || row.name === 'متفرقه',
    columns: [
      { data: 'name', title: 'نام دسته‌بندی' },
      { data: 'description', title: 'توضیحات', orderable: false },
      {
        data: 'isSystem',
        title: 'نوع',
        orderable: false,
        className: 'text-center',
        render: (data) =>
          data
            ? '<span class="badge bg-secondary">سیستمی</span>'
            : '<span class="badge bg-light text-dark">کاربری</span>',
      },
      {
        data: 'revenuesCount',
        title: 'تعداد عواید',
        orderable: false,
        className: 'text-center',
      },
      {
        data: 'isActive',
        title: 'وضعیت',
        className: 'text-center',
        render: (data) =>
          data
            ? '<span class="badge badge-active">فعال</span>'
            : '<span class="badge badge-inactive">غیرفعال</span>',
      },
    ],
    fields: [
      { name: 'name', label: 'نام دسته‌بندی', type: 'text', required: true },
      { name: 'description', label: 'توضیحات', type: 'textarea' },
      { name: 'isActive', label: 'فعال', type: 'switch', default: true },
    ],
  },
  {
    id: 'fixed-assets',
    label: 'دارایی ثابت',
    title: 'دسته‌بندی دارایی‌های ثابت',
    createLabel: 'دسته‌بندی جدید',
    api: fixedAssetCategoriesApi,
    idField: 'fixedAssetCategoryId',
    nameField: 'name',
    permissionPath: '/accounting/fixed-asset-categories',
    canDeleteRow: (row) => !row.isSystem,
    columns: [
      { data: 'name', title: 'نام دسته‌بندی' },
      { data: 'assetAccountName', title: 'حساب دارایی', orderable: false },
      {
        data: 'defaultUsefulLifeMonths',
        title: 'عمر مفید (ماه)',
        className: 'text-center',
      },
      {
        data: 'assetsCount',
        title: 'تعداد دارایی',
        orderable: false,
        className: 'text-center',
      },
      {
        data: 'isSystem',
        title: 'نوع',
        orderable: false,
        className: 'text-center',
        render: (data) =>
          data
            ? '<span class="badge bg-secondary">سیستمی</span>'
            : '<span class="badge bg-light text-dark">کاربری</span>',
      },
      {
        data: 'isActive',
        title: 'وضعیت',
        className: 'text-center',
        render: (data) =>
          data
            ? '<span class="badge badge-active">فعال</span>'
            : '<span class="badge badge-inactive">غیرفعال</span>',
      },
    ],
    fields: [
      { name: 'name', label: 'نام دسته‌بندی', type: 'text', required: true, col: 8 },
      {
        name: 'defaultUsefulLifeMonths',
        label: 'عمر مفید پیش‌فرض (ماه)',
        type: 'number',
        required: true,
        col: 4,
        default: 60,
      },
      { name: 'description', label: 'توضیحات', type: 'textarea', col: 12 },
      { name: 'isActive', label: 'فعال', type: 'switch', default: true, col: 4 },
    ],
  },
]

function AccountingCategoriesPage() {
  const { can } = usePermission()
  const [searchParams, setSearchParams] = useSearchParams()

  const visibleTabs = useMemo(
    () => TABS.filter((tab) => can(pathPermission(tab.permissionPath, 'view'))),
    [can],
  )

  const requestedTab = searchParams.get('tab')
  const [activeTab, setActiveTab] = useState(
    () => visibleTabs.find((t) => t.id === requestedTab)?.id ?? visibleTabs[0]?.id,
  )

  useEffect(() => {
    if (!visibleTabs.length) return
    if (!visibleTabs.some((t) => t.id === activeTab)) {
      setActiveTab(visibleTabs[0].id)
    }
  }, [visibleTabs, activeTab])

  useEffect(() => {
    if (!activeTab) return
    if (searchParams.get('tab') === activeTab) return
    setSearchParams({ tab: activeTab }, { replace: true })
  }, [activeTab, searchParams, setSearchParams])

  const tab = visibleTabs.find((t) => t.id === activeTab)

  if (!tab) {
    return (
      <div className="content-card card border-0 h-100">
        <div className="card-body p-4 text-muted">دسترسی به هیچ دسته‌بندی‌ای ندارید.</div>
      </div>
    )
  }

  return (
    <div className="content-card card border-0 h-100">
      <div className="card-header bg-transparent border-0 pt-3 px-4 pb-0">
        <ul className="nav nav-tabs card-header-tabs">
          {visibleTabs.map((t) => (
            <li className="nav-item" key={t.id}>
              <button
                type="button"
                className={`nav-link ${t.id === activeTab ? 'active' : ''}`}
                onClick={() => setActiveTab(t.id)}
              >
                {t.label}
              </button>
            </li>
          ))}
        </ul>
      </div>

      <CrudTablePage
        key={tab.id}
        embedded
        title={tab.title}
        createLabel={tab.createLabel}
        api={tab.api}
        idField={tab.idField}
        nameField={tab.nameField}
        columns={tab.columns}
        fields={tab.fields}
        permissionPath={tab.permissionPath}
        canDeleteRow={tab.canDeleteRow}
        canEditRow={tab.canEditRow}
      />
    </div>
  )
}

export default AccountingCategoriesPage
