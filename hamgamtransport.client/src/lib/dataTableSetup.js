import DataTable from 'datatables.net-react'
import DT from 'datatables.net-bs5'
// هستهٔ Responsive (نه *-bs5) — پکیج bs5 در Vite به instance اشتباه می‌چسبد و کرش می‌کند
import 'datatables.net-responsive'

import 'datatables.net-bs5/css/dataTables.bootstrap5.css'
import 'datatables.net-responsive-bs5/css/responsive.bootstrap5.css'

import {
  attachDataTableLayoutFit,
} from './dataTableLayout'

DataTable.use(DT)

Object.assign(DT.defaults, {
  processing: true,
  serverSide: true,
  paging: true,
  searching: true,
  ordering: true,
  info: true,
  // اسکرول افقی با CSS روی wrapper؛ scrollX داخلی DT هدر را از بدنه جدا می‌کند
  scrollX: false,
  autoWidth: false,
  pageLength: 15,
  lengthMenu: [10, 15, 25, 50, 100],
  language: {
    search: '',
    lengthMenu: 'نمایش _MENU_ ردیف',
  },
  layout: {
    topStart: {
      pageLength: { menu: [10, 15, 25, 50, 100] },
      search: { placeholder: 'جستجو در همه ستون‌ها...' },
    },
    topEnd: null,
    bottomStart: 'info',
    bottomEnd: {
      paging: { firstLast: true, previousNext: true, numbers: 5 },
    },
  },
})

// فقط بعد از init و تغییر عرض کارت؛ نه روی هر draw (تا هدر به هم نریزد)
DT.$(document).on('init.dt', (event, settings) => {
  if (event.namespace !== 'dt') return
  attachDataTableLayoutFit(new DT.Api(settings))
})

export default DataTable
