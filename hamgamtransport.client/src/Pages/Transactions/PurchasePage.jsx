import { useCallback, useEffect, useMemo, useRef, useState } from 'react'
import Icon from '../../components/common/Icon'
import AmountDisplay from '../../components/common/AmountDisplay'
import AmountField from '../../components/common/AmountField'
import JalaliDateField from '../../components/common/JalaliDateField'
import PrefixNumberField from '../../components/common/PrefixNumberField'
import SearchableSelect from '../../components/common/SearchableSelect'
import {
    useModalAutoFocus,
    useModalKeyboardShortcuts,
    usePageCreateShortcut,
} from '../../hooks/useModalKeyboardShortcuts'
import { todayGregorianIso } from '../../lib/afghanSolarCalendar'
import DataTable from '../../lib/dataTableSetup'
import { fetchBaseCurrency, fetchCurrencyRates } from '../../services/currenciesApi'
import { fetchGeneralSettings } from '../../services/settingsApi'
import { usePageCrud } from '../../permissions/usePageCrud'
import { fetchWarehouseOptions } from '../../services/inventoryApi'
import {
    fetchMeaurmentOptions,
    fetchProductOptions,
    fetchSuggestedPurchasePrice,
} from '../../services/productsApi'
import { fetchCurrencyOptions } from '../../services/currencyApi'
import { showAppToast } from '../../lib/appToast'
import { persianValidity, validateFormPersian } from '../../lib/persianFormValidity'
import {
    INVOICE_STATUSES,
    INVOICE_DOCUMENT_TYPE,
    buildPurchasePayload,
    calcLineTotals,
    convertAmountFromBase,
    convertAmountToBase,
    fetchCurrencyRateAt,
    getCurrencyRateToBase,
    fetchSupplierOptions,
    purchaseInvoicesApi,
    getPurchaseInvoicePrintUrl,
    renderInvoiceDocumentTypeBadge,
    sumTotals,
} from '../../services/transactionsApi'
import { invoiceInstallmentsApi } from '../../services/ledgerApi'
import InvoiceReturnModal from '../../components/transactions/InvoiceReturnModal'
import { amountWithSymbolHtml } from '../../lib/currencyFormat'
import { createServerSideTableOptions, formatAmount } from '../../lib/dataTableOptions'
import { formatJalaliDate } from '../../lib/afghanSolarCalendar'
import '../../styles/purchase-invoice-lines.css'

const emptyHeader = {
    supplierId: '',
    warehouseId: '',
    invoiceDate: '',
    status: '4',
    currencyId: '',
    description: '',
    paidAmount: '',
    cashBoxId: '',
    paymentTermDays: '',
    dueDate: '',
    taxPercent: '',
    taxAmount: '',
}

const emptyLine = {
    purchaseItemId: null,
    productId: '',
    meaurmentId: '',
    quantity: '',
    unitPrice: '',
    unitPriceInBase: '',
    purchasePriceSourceLabel: '',
}

function PurchasePage() {
    const tableRef = useRef(null)
    const formRef = useRef(null)
    const baseCurrencySymbolRef = useRef('')
    const { canCreate, canEdit, canDelete } = usePageCrud('/transactions/purchase')
    const [loadError, setLoadError] = useState('')
    const [showForm, setShowForm] = useState(false)
    const [editId, setEditId] = useState(null)
    const [viewPosted, setViewPosted] = useState(false)
    const [deleteRow, setDeleteRow] = useState(null)
    const [formError, setFormError] = useState('')
    const [submitting, setSubmitting] = useState(false)
    const [header, setHeader] = useState(emptyHeader)
    const [lines, setLines] = useState([{ ...emptyLine }])
    const [suppliers, setSuppliers] = useState([])
    const [warehouses, setWarehouses] = useState([])
    const [products, setProducts] = useState([])
    const [meaurments, setMeaurments] = useState([])
    const [currencies, setCurrencies] = useState([])
    const [rateSnapshot, setRateSnapshot] = useState(null)
    const [exchangeRate, setExchangeRate] = useState('')
    const [exchangeRateTouched, setExchangeRateTouched] = useState(false)
    const [paidAmountTouched, setPaidAmountTouched] = useState(false)
    const [baseCurrencyId, setBaseCurrencyId] = useState('')
    const [currencyRates, setCurrencyRates] = useState({})
    const [invoiceCodePreview, setInvoiceCodePreview] = useState('')
    const [baseCurrencySymbol, setBaseCurrencySymbol] = useState('')
    const [documentType, setDocumentType] = useState(INVOICE_DOCUMENT_TYPE.Invoice)
    const [referenceInvoiceNumber, setReferenceInvoiceNumber] = useState('')
    const [pastReturns, setPastReturns] = useState([])
    const [returnSource, setReturnSource] = useState(null)
    const [cashBoxes, setCashBoxes] = useState([])
    const [formTab, setFormTab] = useState('header')
    const [lookupsReady, setLookupsReady] = useState(false)
    const lookupsPromiseRef = useRef(null)
    const [installments, setInstallments] = useState([])
    const [installmentCount, setInstallmentCount] = useState('1')
    const [installmentsLoading, setInstallmentsLoading] = useState(false)
    const [defaultTaxPercent, setDefaultTaxPercent] = useState('')

    const currencySymbolById = useMemo(
        () => Object.fromEntries(currencies.map((c) => [String(c.value), c.symbol ?? ''])),
        [currencies],
    )

    const invoiceCurrencySymbol = useMemo(() => {
        return currencySymbolById[String(header.currencyId)] ?? ''
    }, [currencySymbolById, header.currencyId])

    const invoiceTotalRender = useMemo(
        () => (data, type, row) => {
            if (type === 'sort' || type === 'type' || type === 'filter') {
                const num = Number(data)
                return Number.isNaN(num) ? 0 : num
            }
            return amountWithSymbolHtml(data, row?.currencySymbol ?? '')
        },
        [],
    )

    const baseTotalRender = useMemo(
        () => (data, type, row) => {
            if (type === 'sort' || type === 'type' || type === 'filter') {
                const num = Number(data)
                return Number.isNaN(num) ? 0 : num
            }
            const symbol = row?.baseCurrencySymbol || baseCurrencySymbolRef.current || ''
            return amountWithSymbolHtml(data, symbol)
        },
        [],
    )

    const meaurmentSymbol = useCallback(
        (meaurmentId) => {
            const unit = meaurments.find((m) => String(m.value) === String(meaurmentId))
            return unit?.symbol || unit?.label || ''
        },
        [meaurments],
    )

    // فقط سمبل ارز پایه برای جدول لیست — بقیهٔ لوکاپ‌ها هنگام باز شدن فرم لود می‌شوند
    useEffect(() => {
        let cancelled = false
        fetchBaseCurrency()
            .then((base) => {
                if (cancelled) return
                const symbol = base?.symbol ?? ''
                setBaseCurrencySymbol(symbol)
                baseCurrencySymbolRef.current = symbol
                if (base?.currencyID) {
                    setBaseCurrencyId(String(base.currencyID))
                }
            })
            .catch(() => {
                if (cancelled) return
                setBaseCurrencySymbol('')
                baseCurrencySymbolRef.current = ''
            })
        return () => {
            cancelled = true
        }
    }, [])

    useEffect(() => {
        fetchGeneralSettings()
            .then((settings) => setDefaultTaxPercent(String(settings.defaultTaxPercent ?? '')))
            .catch(() => setDefaultTaxPercent(''))
    }, [])

    const ensureLookups = useCallback(() => {
        if (lookupsReady) return Promise.resolve()
        if (lookupsPromiseRef.current) return lookupsPromiseRef.current

        lookupsPromiseRef.current = Promise.all([
            fetchSupplierOptions().catch(() => []),
            fetchWarehouseOptions().catch(() => []),
            fetchProductOptions().catch(() => []),
            fetchMeaurmentOptions().catch(() => []),
            fetchCurrencyOptions().catch(() => []),
            purchaseInvoicesApi.fetchCashBoxOptions().catch(() => []),
            fetchCurrencyRates().catch(() => null),
        ])
            .then(([supplierRows, warehouseRows, productRows, meaurmentRows, currencyRows, cashBoxRows, ratesData]) => {
                setSuppliers(supplierRows)
                setWarehouses(warehouseRows)
                setProducts(productRows)
                setMeaurments(meaurmentRows)
                setCurrencies(currencyRows)
                setCashBoxes(
                    (cashBoxRows ?? []).map((b) => ({
                        value: String(b.value),
                        label: b.label,
                    })),
                )
                if (ratesData) {
                    setBaseCurrencyId(String(ratesData.baseCurrencyId ?? ''))
                    const map = {}
                    for (const row of ratesData.rates ?? []) {
                        map[String(row.currencyId)] = row.baseUnitsPerUnit
                    }
                    setCurrencyRates(map)
                }
                setLookupsReady(true)
            })
            .finally(() => {
                lookupsPromiseRef.current = null
            })

        return lookupsPromiseRef.current
    }, [lookupsReady])

    useEffect(() => {
        baseCurrencySymbolRef.current = baseCurrencySymbol
        const dt = tableRef.current?.dt()
        if (dt && baseCurrencySymbol) {
            dt.rows().invalidate('data').draw(false)
        }
    }, [baseCurrencySymbol])

    useEffect(() => {
        if (!header.currencyId) {
            return
        }

        let cancelled = false
        fetchCurrencyRateAt(header.currencyId, header.invoiceDate || undefined)
            .then((snapshot) => {
                if (cancelled) return
                setRateSnapshot(snapshot)
                const rate = snapshot.isBaseCurrency ? '1' : String(snapshot.baseUnitsPerUnit ?? '')
                setExchangeRate(rate)
                if (!exchangeRateTouched) {
                    setLines((prev) =>
                        prev.map((line) => {
                            if (line.unitPriceInBase == null || line.unitPriceInBase === '') return line
                            return {
                                ...line,
                                unitPrice: convertAmountFromBase(
                                    line.unitPriceInBase,
                                    header.currencyId,
                                    baseCurrencyId,
                                    rate,
                                ),
                            }
                        }),
                    )
                }
            })
            .catch(() => {
                if (cancelled) return
                setRateSnapshot(null)
                setExchangeRate('')
            })

        return () => {
            cancelled = true
        }
    }, [header.currencyId, header.invoiceDate, baseCurrencyId, exchangeRateTouched])

    const isNonBaseCurrency = Boolean(header.currencyId && rateSnapshot && !rateSnapshot.isBaseCurrency)

    const computedLines = useMemo(
        () =>
            calcLineTotals(
                lines,
                rateSnapshot,
                exchangeRate,
                meaurments,
                baseCurrencyId,
                header.currencyId,
            ),
        [lines, rateSnapshot, exchangeRate, meaurments, baseCurrencyId, header.currencyId],
    )
    const lineTotals = useMemo(() => sumTotals(computedLines), [computedLines])
    const taxAmount = useMemo(
        () => Math.round(lineTotals.total * (Number(header.taxPercent) || 0)) / 100,
        [lineTotals.total, header.taxPercent],
    )
    const totals = useMemo(
        () => ({
            total: lineTotals.total + taxAmount,
            totalBase:
                lineTotals.totalBase +
                Math.round(lineTotals.totalBase * (Number(header.taxPercent) || 0)) / 100,
        }),
        [lineTotals, header.taxPercent, taxAmount],
    )

    const handleHeaderChange = useCallback((name, value) => {
        setHeader((prev) => ({ ...prev, [name]: value }))
    }, [])

    const effectivePaidAmount = useMemo(() => {
        if (paidAmountTouched || viewPosted) {
            return header.paidAmount
        }
        return totals.total > 0 ? String(totals.total) : ''
    }, [paidAmountTouched, viewPosted, header.paidAmount, totals.total])

    const paidAmountNumeric = Number(effectivePaidAmount) || 0
    const remainingAmount = Math.max(0, totals.total - paidAmountNumeric)
    const isCashInvoice = totals.total > 0 && paidAmountNumeric >= totals.total
    const isInvoiceStatus = String(header.status) === '4'
    const showReturnedQty = viewPosted && documentType === INVOICE_DOCUMENT_TYPE.Invoice

    // وقتی مبلغ پرداخت خودکار پر می‌شود، صندوق پیش‌فرض را هم ست کن
    useEffect(() => {
        if (viewPosted || !showForm) return
        if (paidAmountNumeric <= 0) {
            if (header.cashBoxId) {
                setHeader((prev) => ({ ...prev, cashBoxId: '' }))
            }
            return
        }
        if (!header.cashBoxId && cashBoxes[0]?.value) {
            setHeader((prev) => ({ ...prev, cashBoxId: cashBoxes[0].value }))
        }
    }, [paidAmountNumeric, cashBoxes, header.cashBoxId, viewPosted, showForm])

    const reloadTable = useCallback(() => {
        tableRef.current?.dt()?.ajax.reload(null, false)
    }, [])

    const openPrint = useCallback((purchaseInvoiceId) => {
        if (!purchaseInvoiceId) return
        window.open(getPurchaseInvoicePrintUrl(purchaseInvoiceId), '_blank', 'noopener,noreferrer')
    }, [])

    const closeModals = useCallback(() => {
        setShowForm(false)
        setEditId(null)
        setViewPosted(false)
        setDeleteRow(null)
        setFormError('')
        setSubmitting(false)
        setExchangeRate('')
        setRateSnapshot(null)
        setExchangeRateTouched(false)
        setPaidAmountTouched(false)
        setInvoiceCodePreview('')
        setFormTab('header')
        setDocumentType(INVOICE_DOCUMENT_TYPE.Invoice)
        setReferenceInvoiceNumber('')
        setPastReturns([])
        setReturnSource(null)
        setInstallments([])
        setInstallmentCount('1')
    }, [])

    const openCreate = useCallback(async () => {
        setFormError('')
        setFormTab('header')
        setPaidAmountTouched(false)
        setExchangeRateTouched(false)
        setExchangeRate('')

        let defaultCurrencyId = baseCurrencyId
        try {
            const [, codePreview, base] = await Promise.all([
                ensureLookups(),
                purchaseInvoicesApi.fetchNextCodePreview().catch(() => ({ code: '' })),
                baseCurrencyId
                    ? Promise.resolve(null)
                    : fetchBaseCurrency().catch(() => null),
            ])
            if (base?.currencyID) {
                defaultCurrencyId = String(base.currencyID)
                setBaseCurrencyId(defaultCurrencyId)
                setBaseCurrencySymbol(base.symbol ?? '')
            }
            setInvoiceCodePreview(codePreview?.code ?? '')
        } catch {
            defaultCurrencyId = baseCurrencyId
        }

        setHeader({
            ...emptyHeader,
            invoiceDate: todayGregorianIso(),
            currencyId: defaultCurrencyId,
            taxPercent: defaultTaxPercent,
            cashBoxId: cashBoxes[0]?.value ?? '',
        })
        setLines([{ ...emptyLine }])
        setEditId(null)
        setViewPosted(false)
        setShowForm(true)
    }, [baseCurrencyId, ensureLookups, defaultTaxPercent, cashBoxes])

    const openEdit = useCallback(async (row, readOnly = false) => {
        setFormError('')
        setPaidAmountTouched(true)
        try {
            const needsReturns =
                (readOnly || row.isPosted) &&
                (row.documentType ?? INVOICE_DOCUMENT_TYPE.Invoice) === INVOICE_DOCUMENT_TYPE.Invoice

            const [invoice, , history] = await Promise.all([
                purchaseInvoicesApi.getById(row.purchaseInvoiceId),
                ensureLookups(),
                needsReturns
                    ? purchaseInvoicesApi.fetchReturns(row.purchaseInvoiceId).catch(() => [])
                    : Promise.resolve([]),
            ])

            setInvoiceCodePreview(invoice.invoiceNumber ?? '')
            const invoiceRate = invoice.baseUnitsPerUnitAtTransaction || 1
            setHeader({
                supplierId: invoice.supplierId,
                warehouseId: invoice.warehouseId,
                invoiceDate: String(invoice.invoiceDate).slice(0, 10),
                status: String(invoice.status),
                currencyId: invoice.currencyId,
                description: invoice.description ?? '',
                paidAmount: invoice.paidAmount ?? invoice.totalAmount ?? '',
                cashBoxId: invoice.cashBoxId ? String(invoice.cashBoxId) : '',
                paymentTermDays: invoice.paymentTermDays ?? '',
                dueDate: invoice.dueDate ? String(invoice.dueDate).slice(0, 10) : '',
                taxPercent: invoice.taxPercent ?? '',
                taxAmount: invoice.taxAmount ?? '',
            })
            setExchangeRate(
                invoice.baseUnitsPerUnitAtTransaction != null
                    ? String(invoice.baseUnitsPerUnitAtTransaction)
                    : '',
            )
            setLines(
                (invoice.items ?? []).map((item) => {
                    const qtyBase = Number(item.quantityInBase) || 0
                    const unitPriceInBase =
                        qtyBase > 0
                            ? Number(item.lineTotalInBaseCurrency) / qtyBase
                            : convertAmountToBase(
                                item.unitPrice,
                                invoice.currencyId,
                                invoice.baseCurrencyId,
                                invoiceRate,
                            )
                    return {
                        purchaseItemId: item.purchaseItemId,
                        productId: item.productId,
                        meaurmentId: item.meaurmentId,
                        quantity: item.quantity,
                        returnedQuantity: Number(item.returnedQuantity) || 0,
                        unitPrice: item.unitPrice,
                        unitPriceInBase: unitPriceInBase || '',
                    }
                }),
            )
            setEditId(invoice.purchaseInvoiceId)
            invoiceInstallmentsApi
                .list(2, invoice.purchaseInvoiceId)
                .then((items) => setInstallments(items ?? []))
                .catch(() => setInstallments([]))
            setDocumentType(invoice.documentType ?? INVOICE_DOCUMENT_TYPE.Invoice)
            setReferenceInvoiceNumber(invoice.referenceInvoiceNumber ?? '')
            setViewPosted(readOnly || invoice.isPosted)
            setPastReturns(history ?? [])
            setFormTab('header')
            setShowForm(true)
        } catch (error) {
            setLoadError(error.message)
        }
    }, [ensureLookups])

    const handleCurrencyChange = (newCurrencyId) => {
        if (!newCurrencyId) {
            setRateSnapshot(null)
            setExchangeRate('')
            setExchangeRateTouched(false)
            handleHeaderChange('currencyId', newCurrencyId)
            return
        }

        const oldCurrencyId = header.currencyId
        if (oldCurrencyId && newCurrencyId && oldCurrencyId !== newCurrencyId) {
            const oldRate = getCurrencyRateToBase(
                oldCurrencyId,
                baseCurrencyId,
                currencyRates,
                exchangeRate,
            )
            const newRate = getCurrencyRateToBase(newCurrencyId, baseCurrencyId, currencyRates)

            setLines((prev) =>
                prev.map((line) => {
                    const priceInBase =
                        line.unitPriceInBase != null && line.unitPriceInBase !== ''
                            ? Number(line.unitPriceInBase)
                            : convertAmountToBase(line.unitPrice, oldCurrencyId, baseCurrencyId, oldRate)

                    if (!priceInBase && line.unitPrice === '') {
                        return { ...line }
                    }

                    return {
                        ...line,
                        unitPriceInBase: priceInBase || line.unitPriceInBase || '',
                        unitPrice: convertAmountFromBase(
                            priceInBase,
                            newCurrencyId,
                            baseCurrencyId,
                            newRate,
                        ),
                    }
                }),
            )
            setExchangeRate(String(newCurrencyId) === String(baseCurrencyId) ? '1' : String(newRate))
            setExchangeRateTouched(false)
        }
        handleHeaderChange('currencyId', newCurrencyId)
    }

    const setDueDateFromTerm = () => {
        if (header.dueDate || !header.invoiceDate || Number(header.paymentTermDays) <= 0) return
        const date = new Date(`${header.invoiceDate}T00:00:00`)
        if (Number.isNaN(date.getTime())) return
        date.setDate(date.getDate() + Number(header.paymentTermDays))
        handleHeaderChange('dueDate', date.toISOString().slice(0, 10))
    }

    const generateInstallments = async () => {
        if (!editId || Number(installmentCount) < 1) return
        setInstallmentsLoading(true)
        setFormError('')
        try {
            const items = await invoiceInstallmentsApi.generate({
                kind: 2,
                invoiceId: editId,
                count: Number(installmentCount),
                firstDueDate: header.dueDate || null,
            })
            setInstallments(items ?? [])
        } catch (error) {
            setFormError(error.message)
        } finally {
            setInstallmentsLoading(false)
        }
    }

    const handleExchangeRateChange = (value) => {
        setExchangeRateTouched(true)
        setExchangeRate(value)
        setLines((prev) =>
            prev.map((line) => {
                if (line.unitPriceInBase == null || line.unitPriceInBase === '') return line
                return {
                    ...line,
                    unitPrice: convertAmountFromBase(
                        line.unitPriceInBase,
                        header.currencyId,
                        baseCurrencyId,
                        value,
                    ),
                }
            }),
        )
    }

    const handleLineChange = (index, name, value) => {
        if (name === 'unitPrice') {
            const rate = getCurrencyRateToBase(
                header.currencyId,
                baseCurrencyId,
                currencyRates,
                exchangeRate,
            )
            setLines((prev) =>
                prev.map((line, i) => {
                    if (i !== index) return line
                    return {
                        ...line,
                        unitPrice: value,
                        unitPriceInBase:
                            value === ''
                                ? ''
                                : convertAmountToBase(value, header.currencyId, baseCurrencyId, rate),
                        purchasePriceSourceLabel: '',
                    }
                }),
            )
            return
        }

        setLines((prev) =>
            prev.map((line, i) => (i === index ? { ...line, [name]: value } : line)),
        )
    }

    const handleMeaurmentChange = (index, newMeaurmentId) => {
        handleLineChange(index, 'meaurmentId', newMeaurmentId)
    }

    const applySuggestedPurchasePrice = async (index, productId) => {
        if (!productId) return
        const rate = getCurrencyRateToBase(
            header.currencyId,
            baseCurrencyId,
            currencyRates,
            exchangeRate,
        )
        try {
            const hint = await fetchSuggestedPurchasePrice(productId, header.warehouseId || undefined)
            const priceInBase =
                hint?.unitCostInBase != null && hint.unitCostInBase !== ''
                    ? Number(hint.unitCostInBase)
                    : ''
            setLines((prev) =>
                prev.map((line, i) => {
                    if (i !== index || String(line.productId) !== String(productId)) return line
                    return {
                        ...line,
                        unitPriceInBase: priceInBase === '' ? '' : priceInBase,
                        unitPrice:
                            priceInBase === ''
                                ? ''
                                : convertAmountFromBase(
                                      priceInBase,
                                      header.currencyId,
                                      baseCurrencyId,
                                      rate,
                                  ),
                        purchasePriceSourceLabel: hint?.sourceLabel || '',
                    }
                }),
            )
        } catch {
            setLines((prev) =>
                prev.map((line, i) => {
                    if (i !== index || String(line.productId) !== String(productId)) return line
                    return {
                        ...line,
                        unitPriceInBase: '',
                        unitPrice: '',
                        purchasePriceSourceLabel: '',
                    }
                }),
            )
        }
    }

    const handleProductChange = (index, productId) => {
        const product = products.find((p) => String(p.value) === String(productId))
        setLines((prev) =>
            prev.map((line, i) => {
                if (i !== index) return line
                return {
                    ...line,
                    productId,
                    meaurmentId: product?.defaultMeaurmentId ?? '',
                    unitPriceInBase: '',
                    unitPrice: '',
                    purchasePriceSourceLabel: '',
                }
            }),
        )
        if (productId) {
            void applySuggestedPurchasePrice(index, productId)
        }
    }

    const meaurmentsForProduct = (productId) => {
        const product = products.find((p) => String(p.value) === String(productId))
        if (!product?.baseMeaurmentId) return meaurments
        return meaurments.filter(
            (m) =>
                m.baseMeaurmentId === product.baseMeaurmentId ||
                m.value === product.baseMeaurmentId,
        )
    }

    const addLine = () => setLines((prev) => [...prev, { ...emptyLine }])
    const removeLine = (index) =>
        setLines((prev) => (prev.length > 1 ? prev.filter((_, i) => i !== index) : prev))

    const handleSubmit = async (event) => {
        event.preventDefault()
        if (viewPosted) return

        const formEl = event.currentTarget
        const message = validateFormPersian(formEl)
        if (message) {
            showAppToast(message)
            formEl.reportValidity()
            return
        }

        const paid = Number(effectivePaidAmount) || 0
        if (paid < 0) {
            setFormError('مبلغ پرداخت‌شده نمی‌تواند منفی باشد.')
            return
        }
        if (totals.total > 0 && paid > totals.total) {
            setFormError('مبلغ پرداخت‌شده نمی‌تواند بیشتر از جمع فاکتور باشد.')
            return
        }
        if (paid > 0 && !header.cashBoxId) {
            setFormError('برای پرداخت نقدی، صندوق را انتخاب کنید.')
            return
        }

        setSubmitting(true)
        setFormError('')

        try {
            const payload = buildPurchasePayload(
                { ...header, paidAmount: effectivePaidAmount, taxAmount },
                lines,
                exchangeRate,
            )
            if (editId) {
                await purchaseInvoicesApi.update(editId, payload)
            } else {
                await purchaseInvoicesApi.create(payload)
            }
            closeModals()
            reloadTable()
        } catch (error) {
            setFormError(error.message)
            setSubmitting(false)
        }
    }

    const handleDeleteConfirm = async () => {
        if (!deleteRow) return
        setSubmitting(true)
        setFormError('')
        try {
            await purchaseInvoicesApi.remove(deleteRow.purchaseInvoiceId)
            closeModals()
            reloadTable()
        } catch (error) {
            setFormError(error.message)
            setSubmitting(false)
        }
    }

    const triggerSave = useCallback(() => {
        if (!submitting && !viewPosted) {
            formRef.current?.requestSubmit()
        }
    }, [submitting, viewPosted])

    useModalKeyboardShortcuts({
        open: showForm,
        onClose: closeModals,
        onSave: !viewPosted ? triggerSave : undefined,
        formRef,
    })

    useModalKeyboardShortcuts({
        open: Boolean(deleteRow),
        onClose: closeModals,
    })

    usePageCreateShortcut({
        enabled: canCreate,
        onNew: openCreate,
        isBlocked: showForm || Boolean(deleteRow) || Boolean(returnSource),
    })

    useModalAutoFocus({ open: showForm, formRef })

    const openReturn = useCallback((row) => {
        setReturnSource({
            purchaseInvoiceId: row.purchaseInvoiceId,
            invoiceNumber: row.invoiceNumber,
        })
    }, [])

    const handleReturnSuccess = useCallback(() => {
        reloadTable()
    }, [reloadTable])

    const tableOptions = useMemo(
        () =>
            createServerSideTableOptions({
                ajax: purchaseInvoicesApi.createDataTableAjax(setLoadError),
                order: [[5, 'desc']],
                columns: [
                    { data: 'rowNumber', name: 'rowNumber' },
                    { data: 'invoiceNumber', name: 'invoiceNumber' },
                    {
                        data: 'documentType',
                        name: 'documentType',
                        render: (data, _type, row) => {
                            const badge = renderInvoiceDocumentTypeBadge(data)
                            if (row.referenceInvoiceNumber) {
                                return `${badge}<div class="small text-muted mt-1">مبدأ: ${row.referenceInvoiceNumber}</div>`
                            }
                            return badge
                        },
                    },
                    { data: 'supplierName', name: 'supplierName' },
                    { data: 'warehouseName', name: 'warehouseName' },
                    {
                        data: 'invoiceDate',
                        name: 'invoiceDate',
                        render: (data) => formatJalaliDate(data),
                    },
                    {
                        data: 'totalAmount',
                        name: 'totalAmount',
                        render: invoiceTotalRender,
                    },
                    {
                        data: 'totalAmountInBaseCurrency',
                        name: 'totalAmountInBaseCurrency',
                        render: baseTotalRender,
                    },
                    {
                        data: 'isPosted',
                        name: 'isPosted',
                        render: (data) =>
                            data
                                ? '<span class="badge badge-active">ثبت‌شده</span>'
                                : '<span class="badge badge-inactive">پیش‌نویس</span>',
                    },
                    { data: null, name: 'actions', defaultContent: '' },
                ],
                columnDefs: [
                    { targets: 0, orderable: true, searchable: false, width: '56px', className: 'text-center' },
                    { targets: [3, 4, 8], orderable: true, className: 'text-center' },
                    { targets: [2, 5, 6, 7, 8], className: 'text-center', orderable: true },
                    {
                        targets: 9,
                        orderable: false,
                        searchable: false,
                        fixed: true,
                        className: 'text-center all dt-actions-col',
                        width: '196px',
                    },
                ],
            }),
        [invoiceTotalRender, baseTotalRender],
    )

    const actionSlots = useMemo(
        () => ({
            9: (_data, _type, row) => (
                <div className="dt-actions">
                    {(canEdit || row.isPosted) && (
                        <button
                            type="button"
                            className="dt-action-btn"
                            title={row.isPosted ? 'مشاهده' : 'ویرایش'}
                            onClick={() => openEdit(row, row.isPosted)}
                        >
                            <Icon name={row.isPosted ? 'eye' : 'edit'} />
                        </button>
                    )}
                    {canCreate &&
                        row.isPosted &&
                        row.documentType === INVOICE_DOCUMENT_TYPE.Invoice && (
                            <button
                                type="button"
                                className="dt-action-btn"
                                title="برگشت از خرید"
                                onClick={() => openReturn(row)}
                            >
                                <Icon name="rotate-left" />
                            </button>
                        )}
                    {canDelete && !row.isPosted && (
                        <button
                            type="button"
                            className="dt-action-btn btn-delete"
                            title="حذف"
                            onClick={() => setDeleteRow(row)}
                        >
                            <Icon name="trash" />
                        </button>
                    )}
                    <button
                        type="button"
                        className="dt-action-btn"
                        title="چاپ"
                        onClick={() => openPrint(row.purchaseInvoiceId)}
                    >
                        <Icon name="print" />
                    </button>
                </div>
            ),
        }),
        [openEdit, openPrint, openReturn, canEdit, canDelete, canCreate],
    )

    return (
        <div className="users-page">
            <div className="content-card card border-0 h-100">
                <div className="card-header bg-transparent border-0 pt-4 px-4 pb-0 d-flex align-items-center justify-content-between gap-3 flex-wrap">
                    <h2 className="card-title mb-0">فاکتورهای خرید</h2>
                    {canCreate && (
                        <button
                            type="button"
                            className="btn btn-sm btn-accent btn-users-new d-inline-flex align-items-center gap-2"
                            title="فاکتور خرید جدید (Ctrl+N)"
                            onClick={openCreate}
                        >
                            <Icon name="plus" />
                            <span>فاکتور خرید جدید</span>
                        </button>
                    )}
                </div>

                <div className="card-body card-body-table">
                    {loadError && <div className="alert alert-danger py-2 mb-0">{loadError}</div>}
                    <div className="users-table-wrapper">
                        <DataTable
                            ref={tableRef}
                            className="table table-hover w-100 align-middle"
                            options={tableOptions}
                            slots={actionSlots}
                        >
                            <thead>
                                <tr>
                                    <th>#</th>
                                    <th>شماره</th>
                                    <th>نوع</th>
                                    <th>تأمین‌کننده</th>
                                    <th>انبار</th>
                                    <th>تاریخ</th>
                                    <th>جمع (ارز فاکتور)</th>
                                    <th>جمع (ارز پایه)</th>
                                    <th>وضعیت</th>
                                    <th>عملیات</th>
                                </tr>
                            </thead>
                        </DataTable>
                    </div>
                </div>
            </div>

            {showForm && (
                <>
                    <div className="modal-backdrop show users-modal-backdrop" onClick={closeModals} />
                    <div className="modal show d-block users-modal" tabIndex="-1" data-bs-focus="false">
                        <div className="modal-dialog modal-dialog-centered modal-dialog-scrollable modal-xl">
                            <form ref={formRef} className="modal-content" noValidate onSubmit={handleSubmit}>
                                <div className="modal-header">
                                    <h5 className="modal-title">
                                        {viewPosted
                                            ? documentType === INVOICE_DOCUMENT_TYPE.PurchaseReturn
                                                ? `مشاهده برگشت از خرید${invoiceCodePreview ? ` — ${invoiceCodePreview}` : ''}`
                                                : `مشاهده فاکتور خرید${invoiceCodePreview ? ` — ${invoiceCodePreview}` : ''}`
                                            : editId
                                                ? `ویرایش فاکتور خرید${invoiceCodePreview ? ` — ${invoiceCodePreview}` : ''}`
                                                : `فاکتور خرید جدید${invoiceCodePreview ? ` — ${invoiceCodePreview}` : ''}`}
                                    </h5>
                                    <button type="button" className="btn-close" aria-label="بستن" onClick={closeModals} />
                                </div>
                                <div className="modal-body">
                                    {formError && <div className="alert alert-danger py-2">{formError}</div>}
                                    {documentType === INVOICE_DOCUMENT_TYPE.PurchaseReturn &&
                                        referenceInvoiceNumber && (
                                            <div className="alert alert-secondary py-2">
                                                برگشت از فاکتور مبدأ: <strong>{referenceInvoiceNumber}</strong>
                                            </div>
                                        )}
                                    {viewPosted &&
                                        documentType === INVOICE_DOCUMENT_TYPE.Invoice &&
                                        pastReturns.length > 0 && (
                                            <div className="mb-3">
                                                <h6 className="mb-2">سوابق برگشت این فاکتور</h6>
                                                <div className="table-responsive">
                                                    <table className="table table-sm table-bordered mb-0">
                                                        <thead>
                                                            <tr>
                                                                <th>شماره برگشت</th>
                                                                <th>تاریخ</th>
                                                                <th>مبلغ</th>
                                                            </tr>
                                                        </thead>
                                                        <tbody>
                                                            {pastReturns.map((row) => (
                                                                <tr key={row.invoiceId}>
                                                                    <td>{row.invoiceNumber}</td>
                                                                    <td>{formatJalaliDate(row.invoiceDate)}</td>
                                                                    <td>{formatAmount(row.totalAmount)}</td>
                                                                </tr>
                                                            ))}
                                                        </tbody>
                                                    </table>
                                                </div>
                                            </div>
                                        )}
                                    {editId && documentType === INVOICE_DOCUMENT_TYPE.Invoice && (
                                        <div className="mb-3">
                                            <h6 className="mb-2">اقساط</h6>
                                            {!viewPosted && (
                                                <div className="d-flex align-items-end gap-2 mb-2">
                                                    <div>
                                                        <label className="form-label small mb-1">تعداد</label>
                                                        <input
                                                            type="number"
                                                            min="1"
                                                            className="form-control form-control-sm"
                                                            value={installmentCount}
                                                            onChange={(e) => setInstallmentCount(e.target.value)}
                                                        />
                                                    </div>
                                                    <button
                                                        type="button"
                                                        className="btn btn-sm btn-outline-primary"
                                                        disabled={installmentsLoading}
                                                        onClick={generateInstallments}
                                                    >
                                                        {installmentsLoading ? 'در حال ایجاد...' : 'ایجاد اقساط'}
                                                    </button>
                                                </div>
                                            )}
                                            {installments.length > 0 ? (
                                                <div className="table-responsive">
                                                    <table className="table table-sm table-bordered mb-0">
                                                        <thead><tr><th>شماره</th><th>سررسید</th><th>مبلغ</th><th>مانده</th></tr></thead>
                                                        <tbody>
                                                            {installments.map((item, index) => (
                                                                <tr key={item.invoiceInstallmentId ?? index}>
                                                                    <td>{item.installmentNo ?? index + 1}</td>
                                                                    <td>{formatJalaliDate(item.dueDate)}</td>
                                                                    <td><AmountDisplay value={item.amount} symbol={invoiceCurrencySymbol} /></td>
                                                                    <td><AmountDisplay value={item.remaining} symbol={invoiceCurrencySymbol} /></td>
                                                                </tr>
                                                            ))}
                                                        </tbody>
                                                    </table>
                                                </div>
                                            ) : <small className="text-muted">قسطی ایجاد نشده است.</small>}
                                        </div>
                                    )}

                                    <ul className="nav nav-tabs mb-3">
                                        <li className="nav-item">
                                            <button
                                                type="button"
                                                className={`nav-link${formTab === 'header' ? ' active' : ''}`}
                                                onClick={() => setFormTab('header')}
                                            >
                                                مشخصات
                                            </button>
                                        </li>
                                        <li className="nav-item">
                                            <button
                                                type="button"
                                                className={`nav-link${formTab === 'lines' ? ' active' : ''}`}
                                                onClick={() => setFormTab('lines')}
                                            >
                                                اقلام
                                            </button>
                                        </li>
                                    </ul>

                                    {formTab === 'header' && (
                                    <div className="row g-3 mb-3">
                                        <div className="col-md-3">
                                            <label className="form-label">تأمین‌کننده</label>
                                            <SearchableSelect
                                                options={suppliers}
                                                value={header.supplierId}
                                                onChange={(next) => handleHeaderChange('supplierId', next)}
                                                placeholder="انتخاب کنید..."
                                                searchPlaceholder="جستجوی تأمین‌کننده..."
                                                required
                                                requiredMessage="لطفاً تأمین‌کننده را انتخاب کنید."
                                                disabled={viewPosted}
                                            />
                                        </div>
                                        <div className="col-md-3">
                                            <label className="form-label">انبار</label>
                                            <select
                                                className="form-select"
                                                value={header.warehouseId}
                                                required
                                                disabled={viewPosted}
                                                onChange={(e) => handleHeaderChange('warehouseId', e.target.value)}
                                                {...persianValidity('لطفاً انبار را انتخاب کنید.')}
                                            >
                                                <option value="">انتخاب کنید...</option>
                                                {warehouses.map((o) => (
                                                    <option key={o.value} value={o.value}>
                                                        {o.label}
                                                    </option>
                                                ))}
                                            </select>
                                        </div>
                                        <div className="col-md-3">
                                            <label className="form-label">تاریخ (شمسی)</label>
                                            <JalaliDateField
                                                value={header.invoiceDate}
                                                onChange={(next) => handleHeaderChange('invoiceDate', next)}
                                                required
                                                requiredMessage="لطفاً تاریخ فاکتور را انتخاب کنید."
                                                disabled={viewPosted}
                                            />
                                        </div>
                                        <div className="col-md-3">
                                            <label className="form-label">ارز فاکتور</label>
                                            <select
                                                className="form-select"
                                                value={header.currencyId}
                                                required
                                                disabled={viewPosted}
                                                onChange={(e) => handleCurrencyChange(e.target.value)}
                                                {...persianValidity('لطفاً ارز فاکتور را انتخاب کنید.')}
                                            >
                                                <option value="">انتخاب کنید...</option>
                                                {currencies.map((o) => (
                                                    <option key={o.value} value={o.value}>
                                                        {o.label}
                                                    </option>
                                                ))}
                                            </select>
                                        </div>
                                        <div className="col-md-3">
                                            <label className="form-label">مهلت پرداخت (روز)</label>
                                            <input
                                                type="number"
                                                min="0"
                                                className="form-control"
                                                value={header.paymentTermDays}
                                                disabled={viewPosted}
                                                onChange={(e) => handleHeaderChange('paymentTermDays', e.target.value)}
                                                onBlur={setDueDateFromTerm}
                                            />
                                        </div>
                                        <div className="col-md-3">
                                            <label className="form-label">تاریخ سررسید (شمسی)</label>
                                            <JalaliDateField
                                                value={header.dueDate}
                                                onChange={(next) => handleHeaderChange('dueDate', next)}
                                                disabled={viewPosted}
                                            />
                                        </div>
                                        <div className="col-md-3">
                                            <label className="form-label">وضعیت</label>
                                            <select
                                                className="form-select"
                                                value={header.status}
                                                disabled={viewPosted}
                                                onChange={(e) => handleHeaderChange('status', e.target.value)}
                                            >
                                                {INVOICE_STATUSES.map((s) => (
                                                    <option key={s.value} value={s.value}>
                                                        {s.label}
                                                    </option>
                                                ))}
                                            </select>
                                        </div>
                                        <div className="col-md-3">
                                            <label className="form-label">نرخ به ارز پایه</label>
                                            {isNonBaseCurrency ? (
                                                <input
                                                    type="number"
                                                    min="0"
                                                    step="any"
                                                    className="form-control"
                                                    value={exchangeRate}
                                                    required
                                                    disabled={viewPosted}
                                                    onChange={(e) => handleExchangeRateChange(e.target.value)}
                                                    {...persianValidity('لطفاً نرخ ارز را وارد کنید.')}
                                                />
                                            ) : (
                                                <input
                                                    type="text"
                                                    className="form-control"
                                                    readOnly
                                                    value={
                                                        rateSnapshot
                                                            ? rateSnapshot.isBaseCurrency
                                                                ? 'ارز پایه (۱:۱)'
                                                                : formatAmount(rateSnapshot.baseUnitsPerUnit)
                                                            : header.currencyId
                                                                ? 'در حال بارگذاری...'
                                                                : '—'
                                                    }
                                                />
                                            )}
                                        </div>
                                        <div className="col-md-3">
                                            <label className="form-label">جمع اقلام</label>
                                            <AmountField
                                                value={lineTotals.total}
                                                onChange={() => { }}
                                                symbol={invoiceCurrencySymbol}
                                                readOnly
                                            />
                                        </div>
                                        <div className="col-md-3">
                                            <label className="form-label">مالیات (%)</label>
                                            <input
                                                type="number"
                                                min="0"
                                                max="100"
                                                step="any"
                                                className="form-control"
                                                value={header.taxPercent}
                                                disabled={viewPosted}
                                                onChange={(e) => handleHeaderChange('taxPercent', e.target.value)}
                                            />
                                        </div>
                                        <div className="col-md-3">
                                            <label className="form-label">مبلغ مالیات</label>
                                            <AmountField
                                                value={taxAmount}
                                                onChange={() => { }}
                                                symbol={invoiceCurrencySymbol}
                                                readOnly
                                            />
                                        </div>
                                        <div className="col-md-3">
                                            <label className="form-label">کل فاکتور</label>
                                            <AmountField
                                                value={totals.total}
                                                onChange={() => { }}
                                                symbol={invoiceCurrencySymbol}
                                                readOnly
                                            />
                                        </div>
                                        <div className="col-md-3">
                                            <label className="form-label">مقدار پرداخت‌شده</label>
                                            <AmountField
                                                value={effectivePaidAmount}
                                                onChange={(next) => {
                                                    setPaidAmountTouched(true)
                                                    setHeader((prev) => ({
                                                        ...prev,
                                                        paidAmount: next,
                                                        cashBoxId:
                                                            Number(next) > 0
                                                                ? prev.cashBoxId || cashBoxes[0]?.value || ''
                                                                : '',
                                                    }))
                                                }}
                                                symbol={invoiceCurrencySymbol}
                                                required
                                                disabled={viewPosted}
                                                min="0"
                                                max={totals.total > 0 ? String(totals.total) : undefined}
                                            />
                                            {totals.total > 0 && (
                                                <small className={`text-muted d-block mt-1${isCashInvoice ? '' : ' text-warning'}`}>
                                                    {isCashInvoice ? (
                                                        'فاکتور نقدی — کل مبلغ پرداخت می‌شود'
                                                    ) : (
                                                        <>
                                                            فاکتور نسیه — مانده:{' '}
                                                            <AmountDisplay value={remainingAmount} symbol={invoiceCurrencySymbol} />
                                                        </>
                                                    )}
                                                </small>
                                            )}
                                        </div>
                                        {paidAmountNumeric > 0 && (
                                            <div className="col-md-3">
                                                <label className="form-label">صندوق پرداخت</label>
                                                <select
                                                    className="form-select"
                                                    value={header.cashBoxId}
                                                    required
                                                    disabled={viewPosted}
                                                    onChange={(e) => handleHeaderChange('cashBoxId', e.target.value)}
                                                    {...persianValidity('لطفاً صندوق را انتخاب کنید.')}
                                                >
                                                    <option value="">انتخاب کنید</option>
                                                    {cashBoxes.map((b) => (
                                                        <option key={b.value} value={b.value}>
                                                            {b.label}
                                                        </option>
                                                    ))}
                                                </select>
                                            </div>
                                        )}
                                        <div className="col-12">
                                            <label className="form-label">توضیحات</label>
                                            <input
                                                type="text"
                                                className="form-control"
                                                value={header.description}
                                                disabled={viewPosted}
                                                onChange={(e) => handleHeaderChange('description', e.target.value)}
                                            />
                                        </div>
                                    </div>
                                    )}

                                    {formTab === 'lines' && (
                                    <>
                                    <div className="d-flex align-items-center justify-content-between mb-2">
                                        <h6 className="mb-0">ردیف‌های خرید</h6>
                                        {!viewPosted && (
                                            <button
                                                type="button"
                                                className="btn btn-sm btn-outline-secondary d-inline-flex align-items-center gap-1"
                                                onClick={addLine}
                                            >
                                                <Icon name="plus" />
                                                <span>ردیف جدید</span>
                                            </button>
                                        )}
                                    </div>

                                    <div className="table-responsive">
                                        <table className="table align-middle purchase-lines-table">
                                            <colgroup>
                                                <col className="col-product" />
                                                <col className="col-unit" />
                                                <col className="col-qty" />
                                                <col className="col-price" />
                                                <col className="col-total" />
                                                <col className="col-total-base" />
                                                {!viewPosted && <col className="col-actions" />}
                                            </colgroup>
                                            <thead>
                                                <tr>
                                                    <th className="col-product">محصول</th>
                                                    <th className="col-unit">واحد</th>
                                                    <th className="col-qty">مقدار</th>
                                                    <th className="col-price">
                                                        قیمت واحد ({invoiceCurrencySymbol || '—'})
                                                        <div className="small fw-normal text-muted">
                                                            پیشنهاد از میانگین موجودی / آخرین خرید
                                                        </div>
                                                    </th>
                                                    <th className="col-total">جمع ({invoiceCurrencySymbol || '—'})</th>
                                                    <th className="col-total-base">جمع ({baseCurrencySymbol || '—'})</th>
                                                    {!viewPosted && <th className="col-actions" />}
                                                </tr>
                                            </thead>
                                            <tbody>
                                                {computedLines.flatMap((line, index) => {
                                                    const unitLabel = meaurmentSymbol(line.meaurmentId)
                                                    const returnedQty = Number(line.returnedQuantity) || 0
                                                    const qty = Number(line.quantity) || 0
                                                    const returnRatio = qty > 0 ? returnedQty / qty : 0
                                                    const returnLineTotal = (Number(line.lineTotal) || 0) * returnRatio
                                                    const returnLineTotalBase =
                                                        (Number(line.lineTotalBase) || 0) * returnRatio

                                                    const mainRow = (
                                                        <tr key={index}>
                                                            <td className="col-product">
                                                                <SearchableSelect
                                                                    options={products}
                                                                    value={line.productId}
                                                                    onChange={(next) => handleProductChange(index, next)}
                                                                    placeholder="انتخاب..."
                                                                    searchPlaceholder="جستجوی محصول..."
                                                                    size="sm"
                                                                    className="invoice-line-control-height"
                                                                    required
                                                                    requiredMessage="لطفاً محصول را انتخاب کنید."
                                                                    disabled={viewPosted}
                                                                />
                                                            </td>
                                                            <td className="col-unit">
                                                                <select
                                                                    className="form-select form-select-sm invoice-line-control-height"
                                                                    value={line.meaurmentId}
                                                                    required
                                                                    disabled={viewPosted}
                                                                    onChange={(e) => handleMeaurmentChange(index, e.target.value)}
                                                                    {...persianValidity('لطفاً واحد را انتخاب کنید.')}
                                                                >
                                                                    <option value="">—</option>
                                                                    {meaurmentsForProduct(line.productId).map((m) => (
                                                                        <option key={m.value} value={m.value}>
                                                                            {m.label}
                                                                        </option>
                                                                    ))}
                                                                </select>
                                                            </td>
                                                            <td className="col-qty">
                                                                <PrefixNumberField
                                                                    prefix={meaurmentSymbol(line.meaurmentId)}
                                                                    value={line.quantity}
                                                                    onChange={(next) => handleLineChange(index, 'quantity', next)}
                                                                    min="0"
                                                                    step="any"
                                                                    className="amount-field-sm invoice-line-control-height"
                                                                    required
                                                                    requiredMessage="لطفاً مقدار را وارد کنید."
                                                                    disabled={viewPosted}
                                                                />
                                                            </td>
                                                            <td className="col-price">
                                                                <AmountField
                                                                    value={line.unitPrice}
                                                                    onChange={(next) => handleLineChange(index, 'unitPrice', next)}
                                                                    symbol={invoiceCurrencySymbol}
                                                                    className="amount-field-sm invoice-line-control-height"
                                                                    min="0"
                                                                    step="any"
                                                                    required
                                                                    requiredMessage="لطفاً قیمت واحد را وارد کنید."
                                                                    disabled={viewPosted}
                                                                />
                                                                {!viewPosted && line.purchasePriceSourceLabel ? (
                                                                    <div className="small text-muted mt-1">
                                                                        {line.purchasePriceSourceLabel}
                                                                    </div>
                                                                ) : null}
                                                            </td>
                                                            <td className="col-total text-center">
                                                                <AmountDisplay value={line.lineTotal} symbol={invoiceCurrencySymbol} />
                                                            </td>
                                                            <td className="col-total-base text-center">
                                                                <AmountDisplay value={line.lineTotalBase} symbol={baseCurrencySymbol} />
                                                            </td>
                                                            {!viewPosted && (
                                                                <td className="col-actions">
                                                                    <button
                                                                        type="button"
                                                                        className="btn btn-sm btn-outline-danger"
                                                                        onClick={() => removeLine(index)}
                                                                    >
                                                                        <Icon name="trash" />
                                                                    </button>
                                                                </td>
                                                            )}
                                                        </tr>
                                                    )

                                                    if (!showReturnedQty || returnedQty <= 0) {
                                                        return [mainRow]
                                                    }

                                                    return [
                                                        mainRow,
                                                        <tr key={`${index}-return`} className="invoice-return-subrow">
                                                            <td className="col-product">
                                                                <span className="text-warning small ps-3">↳ برگشت</span>
                                                            </td>
                                                            <td className="col-unit">
                                                                <span className="small text-muted">{unitLabel || '—'}</span>
                                                            </td>
                                                            <td className="col-qty text-center text-warning">
                                                                {formatAmount(returnedQty)}
                                                                {unitLabel ? ` ${unitLabel}` : ''}
                                                            </td>
                                                            <td className="col-price text-center">
                                                                <AmountDisplay
                                                                    value={line.unitPrice}
                                                                    symbol={invoiceCurrencySymbol}
                                                                />
                                                            </td>
                                                            <td className="col-total text-center text-warning">
                                                                <AmountDisplay
                                                                    value={returnLineTotal}
                                                                    symbol={invoiceCurrencySymbol}
                                                                />
                                                            </td>
                                                            <td className="col-total-base text-center text-warning">
                                                                <AmountDisplay
                                                                    value={returnLineTotalBase}
                                                                    symbol={baseCurrencySymbol}
                                                                />
                                                            </td>
                                                            {!viewPosted && <td className="col-actions" />}
                                                        </tr>,
                                                    ]
                                                })}
                                            </tbody>
                                            <tfoot>
                                                <tr>
                                                    <th colSpan={4} className="text-end">
                                                        جمع کل
                                                    </th>
                                                    <th className="text-center">
                                                        <AmountDisplay value={totals.total} symbol={invoiceCurrencySymbol} />
                                                    </th>
                                                    <th className="text-center">
                                                        <AmountDisplay value={totals.totalBase} symbol={baseCurrencySymbol} />
                                                    </th>
                                                    {!viewPosted && <th />}
                                                </tr>
                                            </tfoot>
                                        </table>
                                    </div>
                                    </>
                                    )}
                                </div>
                                <div className="modal-footer">
                                    <button type="button" className="btn btn-secondary" onClick={closeModals}>
                                        بستن
                                    </button>
                                    {editId && (
                                        <button
                                            type="button"
                                            className="btn btn-outline-primary d-inline-flex align-items-center gap-2"
                                            onClick={() => openPrint(editId)}
                                        >
                                            <Icon name="print" />
                                            <span>چاپ</span>
                                        </button>
                                    )}
                                    {!viewPosted && (
                                        <button type="submit" className="btn btn-primary" disabled={submitting}>
                                            {submitting
                                                ? 'در حال ذخیره...'
                                                : isInvoiceStatus
                                                    ? editId
                                                        ? 'ذخیره و ثبت فاکتور'
                                                        : 'ثبت فاکتور خرید'
                                                    : editId
                                                        ? 'ذخیره تغییرات'
                                                        : 'ایجاد فاکتور'}
                                        </button>
                                    )}
                                    {viewPosted &&
                                        canCreate &&
                                        documentType === INVOICE_DOCUMENT_TYPE.Invoice &&
                                        editId && (
                                            <button
                                                type="button"
                                                className="btn btn-warning d-inline-flex align-items-center gap-2"
                                                onClick={() =>
                                                    setReturnSource({
                                                        purchaseInvoiceId: editId,
                                                        invoiceNumber: invoiceCodePreview,
                                                    })
                                                }
                                            >
                                                <Icon name="rotate-left" />
                                                <span>برگشت از خرید</span>
                                            </button>
                                        )}
                                </div>
                            </form>
                        </div>
                    </div>
                </>
            )}

            {deleteRow && (
                <>
                    <div className="modal-backdrop show users-modal-backdrop" onClick={closeModals} />
                    <div className="modal show d-block users-modal" tabIndex="-1">
                        <div className="modal-dialog modal-dialog-centered">
                            <div className="modal-content">
                                <div className="modal-header">
                                    <h5 className="modal-title">حذف فاکتور</h5>
                                    <button type="button" className="btn-close" onClick={closeModals} />
                                </div>
                                <div className="modal-body">
                                    {formError && <div className="alert alert-danger py-2">{formError}</div>}
                                    <p>فاکتور «{deleteRow.invoiceNumber}» حذف شود؟</p>
                                </div>
                                <div className="modal-footer">
                                    <button type="button" className="btn btn-secondary" onClick={closeModals}>
                                        انصراف
                                    </button>
                                    <button
                                        type="button"
                                        className="btn btn-danger"
                                        disabled={submitting}
                                        onClick={handleDeleteConfirm}
                                    >
                                        حذف
                                    </button>
                                </div>
                            </div>
                        </div>
                    </div>
                </>
            )}

            <InvoiceReturnModal
                open={Boolean(returnSource)}
                onClose={() => setReturnSource(null)}
                mode="purchase"
                sourceInvoiceId={returnSource?.purchaseInvoiceId}
                sourceInvoiceNumber={returnSource?.invoiceNumber}
                onSuccess={handleReturnSuccess}
                api={purchaseInvoicesApi}
            />
        </div>
    )
}

export default PurchasePage
