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

export default DataTable
