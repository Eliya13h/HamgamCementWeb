import { useEffect, useState } from 'react'

import CrudTablePage, { formatAmount } from '../Transport/CrudTablePage'

import {

  fetchBaseMeaurmentOptions,

  fetchMeaurmentRatios,

  meaurmentsApi,

} from '../../services/productsApi'



const unitColumns = [

  {

    data: 'isBaseUnit',

    title: 'نوع',

    className: 'text-center',

    orderable: false,

    render: (data) =>

      data

        ? '<span class="badge bg-primary">واحد پایه</span>'

        : '<span class="badge bg-light text-dark">مشتق</span>',

  },

  {

    data: 'baseUnitName',

    title: 'واحد پایه',

    orderable: false,

    render: (data, type, row) =>

      type === 'display' ? (row.isBaseUnit ? '—' : data || '—') : data,

  },

  { data: 'name', title: 'نام واحد' },

  { data: 'symbol', title: 'نماد', orderable: false },

  {

    data: 'factorToBase',

    title: 'ضریب نسبت به پایه',

    className: 'text-end',

    render: (data, type, row) =>

      type === 'display'

        ? row.isBaseUnit

          ? '۱'

          : formatAmount(data)

        : data,

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

]



const unitFields = [

  { name: 'name', label: 'نام واحد', type: 'text', required: true, col: 6 },

  { name: 'symbol', label: 'نماد', type: 'text', col: 6 },

  {

    name: 'isBaseUnit',

    label: 'این یک واحد پایه است (مثل کیلو، متر)',

    type: 'switch',

    default: false,

    col: 12,
    readOnlyOnEdit: true,
  },
  {
    name: 'baseMeaurmentId',
    label: 'واحد پایه خانواده',
    type: 'select',
    col: 6,
    loadOptions: fetchBaseMeaurmentOptions,
    showWhen: (form) => !form.isBaseUnit,
    readOnlyOnEdit: true,
  },

  {

    name: 'factorToBase',

    label: 'ضریب — ۱ واحد این ردیف = چند واحد پایه؟',

    type: 'number',

    step: 'any',

    required: true,

    default: 1,

    col: 6,

    showWhen: (form) => !form.isBaseUnit,

  },

  { name: 'isActive', label: 'فعال', type: 'switch', default: true, col: 12 },

]



function handleUnitFormChange(name, value) {

  if (name === 'isBaseUnit' && value) {

    return { factorToBase: 1, baseMeaurmentId: '' }

  }

  return null

}



function MeaurmentsPage() {

  const [ratios, setRatios] = useState([])

  const [ratiosError, setRatiosError] = useState('')



  useEffect(() => {

    let cancelled = false

    fetchMeaurmentRatios()

      .then((items) => {

        if (!cancelled) setRatios(items ?? [])

      })

      .catch((error) => {

        if (!cancelled) setRatiosError(error.message)

      })

    return () => {

      cancelled = true

    }

  }, [])



  return (

    <div className="d-flex flex-column gap-3">

      <CrudTablePage

        title="واحدهای اندازه‌گیری"

        createLabel="واحد جدید"

        api={meaurmentsApi}

        idField="meaurmentId"

        nameField="name"

        columns={unitColumns}

        fields={unitFields}

        permissionPath="/products/meaurments"

        onFormChange={handleUnitFormChange}

      />



      <div className="content-card card border-0">

        <div className="card-body p-4">

          <h3 className="h6 mb-2">راهنما</h3>

          <p className="text-muted small mb-2">

            ابتدا <strong>واحدهای پایه</strong> را تعریف کنید (مثل کیلوگرم، متر).

            سپس برای هر پایه، واحدهای مشتق اضافه کنید (مثل تن، پاکت، کیلومتر،

            سانتی‌متر).

          </p>

          <p className="text-muted small mb-3">

            مثال: واحد پایه «کیلو» — مشتق «تن» با ضریب ۱۰۰۰ (۱ تن = ۱۰۰۰ کیلو).

            تبدیل فقط بین واحدهای یک خانواده (همان پایه) انجام می‌شود.

          </p>



          {ratiosError && (

            <div className="alert alert-warning py-2 small mb-3">{ratiosError}</div>

          )}



          {ratios.length > 0 ? (

            <div className="table-responsive">

              <table className="table table-sm table-hover mb-0">

                <thead>

                  <tr>

                    <th>نسبت تبدیل</th>

                  </tr>

                </thead>

                <tbody>

                  {ratios.map((item) => (

                    <tr key={`${item.fromMeaurmentId}-${item.toMeaurmentId}`}>

                      <td>{item.description}</td>

                    </tr>

                  ))}

                </tbody>

              </table>

            </div>

          ) : (

            !ratiosError && (

              <p className="text-muted small mb-0">

                پس از ثبت حداقل دو واحد در یک خانواده، نسبت تبدیل‌ها اینجا نمایش

                داده می‌شود.

              </p>

            )

          )}

        </div>

      </div>

    </div>

  )

}



export default MeaurmentsPage


