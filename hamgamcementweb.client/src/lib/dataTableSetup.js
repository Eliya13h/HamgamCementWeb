import DataTable from 'datatables.net-react'
import DT from 'datatables.net-bs5'

import 'datatables.net-buttons'
import 'datatables.net-buttons/js/buttons.html5.mjs'
import 'datatables.net-buttons/js/buttons.print.mjs'
import 'datatables.net-buttons-bs5'
import 'datatables.net-responsive-bs5'

import pdfMake from 'pdfmake/build/pdfmake'
import pdfFonts from 'pdfmake/build/vfs_fonts'

import 'datatables.net-bs5/css/dataTables.bootstrap5.css'
import 'datatables.net-buttons-bs5/css/buttons.bootstrap5.css'
import 'datatables.net-responsive-bs5/css/responsive.bootstrap5.css'

pdfMake.vfs = pdfFonts.pdfMake?.vfs ?? pdfFonts.vfs

DataTable.use(DT)

Object.assign(DT.defaults, {
  processing: true,
  serverSide: true,
  paging: true,
  searching: true,
  ordering: true,
  info: true,
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

export default DataTable
