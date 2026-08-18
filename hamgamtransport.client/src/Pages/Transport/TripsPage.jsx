import { useCallback, useEffect, useMemo, useRef, useState } from 'react'
import Icon from '../../components/common/Icon'
import JalaliDateField from '../../components/common/JalaliDateField'
import {
    useModalAutoFocus,
    useModalKeyboardShortcuts,
    usePageCreateShortcut,
} from '../../hooks/useModalKeyboardShortcuts'
import { showAppToast } from '../../lib/appToast'
import DataTable from '../../lib/dataTableSetup'
import { createServerSideTableOptions, formatAmount } from '../../lib/dataTableOptions'
import { validateFormPersian } from '../../lib/persianFormValidity'
import { fetchSupplierOptions } from '../../services/transactionsApi'
import {
    driversApi,
    fetchBankAccountOptions,
    fetchCashBoxOptions,
    fetchCurrenciesOptions,
    fetchCustomersOptions,
    tripExpenseCategoriesApi,
    tripsApi,
    vehicleOwnersApi,
    vehiclePairsApi,
    vehiclesApi,
} from '../../services/transportApi'

const STATUS_LABELS = {
    1: 'برنامه‌ریزی',
    2: 'در مسیر',
    3: 'تحویل‌شده',
    4: 'تسویه‌شده',
    5: 'لغو',
}

const FREIGHT_MODE = {
    LUMP_SUM: 1,
    WEIGHT_BASED: 2,
}

const PARTY_CUSTOMER = 1
const PARTY_SUPPLIER = 2
const PARTY_OWNER = 3
const PARTY_DRIVER = 4

const PARTY_TYPE_OPTIONS = [
    { value: PARTY_CUSTOMER, label: 'مشتری' },
    { value: PARTY_SUPPLIER, label: 'تأمین‌کننده' },
    { value: PARTY_OWNER, label: 'مالک وسیله' },
    { value: PARTY_DRIVER, label: 'راننده' },
]

let draftExpenseSeq = 0

const emptyForm = () => ({
    tripDate: new Date().toISOString().slice(0, 10),
    status: 1,
    customerId: '',
    origin: '',
    destination: '',
    freightMode: FREIGHT_MODE.WEIGHT_BASED,
    weightTon: '',
    ratePerTon: '',
    amount: '',
    currencyId: '',
    exchangeRate: 1,
    vehiclePairId: '',
    primaryVehicleId: '',
    secondaryVehicleId: '',
    driverId: '',
    primaryOwnerSharePercent: '',
    secondaryOwnerSharePercent: '',
    driverCompensationType: 1,
    driverFixedAmount: '',
    driverProfitSharePercent: '',
    notes: '',
    draftExpenses: [],
})

const emptyExpense = () => ({
    id: `draft-${++draftExpenseSeq}`,
    tripExpenseCategoryId: '',
    title: '',
    expenseDate: new Date().toISOString().slice(0, 10),
    amount: '',
    currencyId: '',
    exchangeRate: 1,
    vehicleId: '',
    paymentSource: 'cash',
    cashBoxId: '',
    bankAccountId: '',
    partyType: String(PARTY_OWNER),
    partyId: '',
})

function isWeightBased(form) {
    return Number(form.freightMode) === FREIGHT_MODE.WEIGHT_BASED
}

function buildPayload(form) {
    const weightBased = isWeightBased(form)
    const weightTon = Number(form.weightTon) || 0
    const ratePerTon = Number(form.ratePerTon) || 0
    const amount = weightBased
        ? Math.round(weightTon * ratePerTon * 10000) / 10000
        : Number(form.amount) || 0

    return {
        tripDate: form.tripDate,
        status: Number(form.status),
        customerId: Number(form.customerId),
        origin: form.origin,
        destination: form.destination,
        freightMode: Number(form.freightMode),
        weightTon,
        ratePerTon,
        amount,
        currencyId: Number(form.currencyId),
        exchangeRate: Number(form.exchangeRate) || 1,
        vehiclePairId: form.vehiclePairId ? Number(form.vehiclePairId) : null,
        primaryVehicleId: form.primaryVehicleId ? Number(form.primaryVehicleId) : null,
        secondaryVehicleId: form.secondaryVehicleId ? Number(form.secondaryVehicleId) : null,
        driverId: form.driverId ? Number(form.driverId) : null,
        primaryOwnerSharePercent: form.primaryOwnerSharePercent
            ? Number(form.primaryOwnerSharePercent)
            : null,
        secondaryOwnerSharePercent: form.secondaryOwnerSharePercent
            ? Number(form.secondaryOwnerSharePercent)
            : null,
        driverCompensationType: Number(form.driverCompensationType),
        driverFixedAmount: form.driverFixedAmount ? Number(form.driverFixedAmount) : null,
        driverProfitSharePercent: form.driverProfitSharePercent
            ? Number(form.driverProfitSharePercent)
            : null,
        notes: form.notes || null,
    }
}

function buildExpensePayload(expense, fallbackCurrencyId) {
    const payload = {
        tripExpenseCategoryId: Number(expense.tripExpenseCategoryId),
        title: expense.title,
        expenseDate: expense.expenseDate,
        amount: Number(expense.amount),
        currencyId: Number(expense.currencyId || fallbackCurrencyId),
        exchangeRate: Number(expense.exchangeRate) || 1,
        vehicleId: expense.vehicleId ? Number(expense.vehicleId) : null,
        cashBoxId: null,
        bankAccountId: null,
        partyType: null,
        partyId: null,
    }

    if (expense.paymentSource === 'cash') {
        payload.cashBoxId = Number(expense.cashBoxId)
    } else if (expense.paymentSource === 'bank') {
        payload.bankAccountId = Number(expense.bankAccountId)
    } else if (expense.paymentSource === 'party') {
        payload.partyType = Number(expense.partyType)
        payload.partyId = Number(expense.partyId)
    }

    return payload
}

function validateDraftExpense(expense) {
    if (!expense.tripExpenseCategoryId || !expense.title || !expense.amount) {
        return 'دسته، عنوان و مبلغ هر هزینه الزامی است.'
    }
    if (expense.paymentSource === 'cash' && !expense.cashBoxId) {
        return 'صندوق پرداخت را برای همه هزینه‌ها انتخاب کنید.'
    }
    if (expense.paymentSource === 'bank' && !expense.bankAccountId) {
        return 'حساب بانکی پرداخت را برای همه هزینه‌ها انتخاب کنید.'
    }
    if (expense.paymentSource === 'party' && (!expense.partyType || !expense.partyId)) {
        return 'طرف حساب پرداخت را برای همه هزینه‌ها انتخاب کنید.'
    }
    return null
}

export default function TripsPage() {
    const tableRef = useRef(null)
    const modalFormRef = useRef(null)
    const [loadError, setLoadError] = useState('')
    const [showModal, setShowModal] = useState(false)
    const [showDetail, setShowDetail] = useState(false)
    const [form, setForm] = useState(emptyForm())
    const [expenseForm, setExpenseForm] = useState(emptyExpense())
    const [editId, setEditId] = useState(null)
    const [detail, setDetail] = useState(null)
    const [saving, setSaving] = useState(false)
    const [savingExpense, setSavingExpense] = useState(false)
    const [options, setOptions] = useState({
        customers: [],
        suppliers: [],
        currencies: [],
        pairs: [],
        vehiclesPrimary: [],
        vehiclesSecondary: [],
        vehiclesAll: [],
        drivers: [],
        owners: [],
        expenseCategories: [],
        cashBoxes: [],
        bankAccounts: [],
    })

    const openCreate = useCallback(() => {
        setEditId(null)
        setForm(emptyForm())
        setShowModal(true)
    }, [])

    const reloadTable = useCallback(() => {
        tableRef.current?.dt()?.ajax.reload(null, false)
    }, [])

    const loadDetail = useCallback(async (id) => {
        const data = await tripsApi.get(id)
        setDetail(data)
        setExpenseForm((prev) => ({
            ...emptyExpense(),
            currencyId: data.currencyId ? String(data.currencyId) : prev.currencyId,
            vehicleId: data.primaryVehicleId ? String(data.primaryVehicleId) : '',
        }))
        setShowDetail(true)
    }, [])

    const handleUpdateStatus = useCallback(
        async (id, status) => {
            try {
                await tripsApi.updateStatus(id, status)
                showAppToast('وضعیت سفر به‌روز شد.', 'success')
                reloadTable()
                if (detail?.transportTripId === id) {
                    await loadDetail(id)
                }
            } catch (err) {
                showAppToast(err.message, 'danger')
            }
        },
        [reloadTable, detail, loadDetail],
    )

    const handlePostRevenue = useCallback(
        async (id) => {
            try {
                await tripsApi.postRevenue(id)
                showAppToast('درآمد سفر ثبت شد.', 'success')
                reloadTable()
                if (detail?.transportTripId === id) {
                    await loadDetail(id)
                }
            } catch (err) {
                showAppToast(err.message, 'danger')
            }
        },
        [reloadTable, detail, loadDetail],
    )

    const handleSettle = useCallback(
        async (id) => {
            try {
                await tripsApi.settle(id)
                showAppToast('تسویه انجام شد.', 'success')
                reloadTable()
                if (detail?.transportTripId === id) {
                    await loadDetail(id)
                }
            } catch (err) {
                showAppToast(err.message, 'danger')
            }
        },
        [reloadTable, detail, loadDetail],
    )

    const handleDelivered = useCallback(
        async (id) => {
            await handleUpdateStatus(id, 3)
        },
        [handleUpdateStatus],
    )

    useEffect(() => {
        Promise.all([
            fetchCustomersOptions(),
            fetchSupplierOptions(),
            fetchCurrenciesOptions(),
            vehiclePairsApi.options(),
            vehiclesApi.options(1),
            vehiclesApi.options(2),
            vehiclesApi.options(),
            driversApi.options(),
            vehicleOwnersApi.options(),
            tripExpenseCategoriesApi.options(),
            fetchCashBoxOptions(),
            fetchBankAccountOptions(),
        ]).then(
            ([
                customers,
                suppliers,
                currencies,
                pairs,
                vehiclesPrimary,
                vehiclesSecondary,
                vehiclesAll,
                drivers,
                owners,
                expenseCategories,
                cashBoxes,
                bankAccounts,
            ]) => {
                setOptions({
                    customers,
                    suppliers,
                    currencies,
                    pairs,
                    vehiclesPrimary,
                    vehiclesSecondary,
                    vehiclesAll,
                    drivers,
                    owners,
                    expenseCategories,
                    cashBoxes,
                    bankAccounts,
                })
                setForm((prev) => ({
                    ...prev,
                    currencyId: prev.currencyId || (currencies[0]?.value ? String(currencies[0].value) : ''),
                }))
            },
        )
    }, [])

    const partyOptions = useMemo(() => {
        const type = Number(expenseForm.partyType)
        if (type === PARTY_CUSTOMER) return options.customers
        if (type === PARTY_SUPPLIER) return options.suppliers
        if (type === PARTY_OWNER) return options.owners
        if (type === PARTY_DRIVER) return options.drivers
        return []
    }, [expenseForm.partyType, options])

    const draftPartyOptions = useCallback(
        (partyType) => {
            const type = Number(partyType)
            if (type === PARTY_CUSTOMER) return options.customers
            if (type === PARTY_SUPPLIER) return options.suppliers
            if (type === PARTY_OWNER) return options.owners
            if (type === PARTY_DRIVER) return options.drivers
            return []
        },
        [options],
    )

    const dataColumns = useMemo(
        () => [
            { data: 'tripNumber', title: 'شماره' },
            { data: 'tripDate', title: 'تاریخ', render: (v) => String(v ?? '').slice(0, 10) },
            { data: 'customerName', title: 'مشتری' },
            { data: 'origin', title: 'مبدأ' },
            { data: 'destination', title: 'مقصد' },
            { data: 'weightTon', title: 'تن', render: (v) => formatAmount(v) },
            { data: 'amount', title: 'مبلغ', render: (v) => formatAmount(v) },
            {
                data: 'status',
                title: 'وضعیت',
                render: (v) => STATUS_LABELS[Number(v)] ?? v,
            },
            {
                data: 'isRevenuePosted',
                title: 'درآمد',
                render: (v) => (v ? '✓' : '—'),
            },
        ],
        [],
    )

    const actionsIndex = dataColumns.length

    const tableOptions = useMemo(
        () =>
            createServerSideTableOptions({
                ajax: tripsApi.createDataTableAjax(setLoadError),
                columns: [
                    ...dataColumns.map((col) => ({
                        data: col.data,
                        name: col.data,
                        render: col.render,
                    })),
                    { data: null, name: 'actions', defaultContent: '' },
                ],
                columnDefs: [
                    {
                        targets: actionsIndex,
                        orderable: false,
                        searchable: false,
                        className: 'text-center text-nowrap dt-actions-col',
                    },
                ],
            }),
        // eslint-disable-next-line react-hooks/exhaustive-deps
        [],
    )

    const actionSlots = useMemo(
        () => ({
            [actionsIndex]: (_data, _type, row) => {
                const id = row.transportTripId
                const status = Number(row.status)
                return (
                    <div className="dt-actions d-inline-flex gap-1 flex-wrap justify-content-center">
                        <button
                            type="button"
                            className="btn btn-sm btn-outline-secondary"
                            onClick={() => loadDetail(id)}
                        >
                            جزئیات
                        </button>
                        {status < 3 && status !== 5 && (
                            <button
                                type="button"
                                className="btn btn-sm btn-outline-info"
                                onClick={() => handleDelivered(id)}
                            >
                                تحویل
                            </button>
                        )}
                        {!row.isRevenuePosted && status !== 5 && (
                            <button
                                type="button"
                                className="btn btn-sm btn-outline-primary"
                                onClick={() => handlePostRevenue(id)}
                            >
                                ثبت درآمد
                            </button>
                        )}
                        {row.isRevenuePosted && status !== 4 && status !== 5 && (
                            <button
                                type="button"
                                className="btn btn-sm btn-outline-success"
                                onClick={() => handleSettle(id)}
                            >
                                تسویه
                            </button>
                        )}
                    </div>
                )
            },
        }),
        [actionsIndex, loadDetail, handleDelivered, handlePostRevenue, handleSettle],
    )

    const postDraftExpenses = useCallback(async (tripId, draftExpenses, currencyId) => {
        for (const expense of draftExpenses) {
            const validation = validateDraftExpense(expense)
            if (validation) {
                throw new Error(validation)
            }
            const result = await tripsApi.addExpense(tripId, buildExpensePayload(expense, currencyId))
            if (result?.tripExpenseId) {
                await tripsApi.postExpense(result.tripExpenseId)
            }
        }
    }, [])

    const handleSave = useCallback(async () => {
        const formEl = modalFormRef.current
        if (formEl && !validateFormPersian(formEl)) return

        if (isWeightBased(form)) {
            if (!form.weightTon || !form.ratePerTon) {
                showAppToast('در حالت وزنی، وزن و نرخ هر تن الزامی است.', 'warning')
                return
            }
        } else if (!form.amount) {
            showAppToast('در حالت کلی، مبلغ کرایه الزامی است.', 'warning')
            return
        }

        for (const expense of form.draftExpenses) {
            const validation = validateDraftExpense(expense)
            if (validation) {
                showAppToast(validation, 'warning')
                return
            }
        }

        setSaving(true)
        try {
            const payload = buildPayload(form)
            if (editId) {
                await tripsApi.update(editId, payload)
                if (form.draftExpenses.length > 0) {
                    await postDraftExpenses(editId, form.draftExpenses, payload.currencyId)
                }
                showAppToast('سفر به‌روزرسانی شد.', 'success')
            } else {
                const created = await tripsApi.create(payload)
                const tripId = created?.transportTripId
                if (tripId && form.draftExpenses.length > 0) {
                    await postDraftExpenses(tripId, form.draftExpenses, payload.currencyId)
                }
                showAppToast('سفر ثبت شد.', 'success')
            }
            setShowModal(false)
            reloadTable()
        } catch (err) {
            showAppToast(err.message, 'danger')
        } finally {
            setSaving(false)
        }
    }, [editId, form, postDraftExpenses, reloadTable])

    useModalKeyboardShortcuts({
        open: showModal,
        onClose: () => setShowModal(false),
        onSave: handleSave,
        formRef: modalFormRef,
    })
    usePageCreateShortcut({
        enabled: true,
        onNew: openCreate,
        isBlocked: showModal || showDetail,
    })
    useModalAutoFocus({ open: showModal, formRef: modalFormRef })

    const setField = (name, value) => setForm((prev) => ({ ...prev, [name]: value }))
    const setExpenseField = (name, value) => setExpenseForm((prev) => ({ ...prev, [name]: value }))

    const addDraftExpense = () => {
        setForm((prev) => ({
            ...prev,
            draftExpenses: [
                ...prev.draftExpenses,
                {
                    ...emptyExpense(),
                    currencyId: prev.currencyId || (options.currencies[0]?.value ? String(options.currencies[0].value) : ''),
                },
            ],
        }))
    }

    const updateDraftExpense = (id, name, value) => {
        setForm((prev) => ({
            ...prev,
            draftExpenses: prev.draftExpenses.map((row) =>
                row.id === id
                    ? {
                        ...row,
                        [name]: value,
                        ...(name === 'partyType' ? { partyId: '' } : {}),
                        ...(name === 'paymentSource'
                            ? { cashBoxId: '', bankAccountId: '', partyId: '' }
                            : {}),
                    }
                    : row,
            ),
        }))
    }

    const removeDraftExpense = (id) => {
        setForm((prev) => ({
            ...prev,
            draftExpenses: prev.draftExpenses.filter((row) => row.id !== id),
        }))
    }

    const openEditFromDetail = () => {
        if (!detail) return
        setEditId(detail.transportTripId)
        setForm({
            tripDate: String(detail.tripDate ?? '').slice(0, 10),
            status: detail.status ?? 1,
            customerId: detail.customerId ? String(detail.customerId) : '',
            origin: detail.origin ?? '',
            destination: detail.destination ?? '',
            freightMode: detail.freightMode ?? FREIGHT_MODE.WEIGHT_BASED,
            weightTon: detail.weightTon ?? '',
            ratePerTon: detail.ratePerTon ?? '',
            amount: detail.amount ?? '',
            currencyId: detail.currencyId ? String(detail.currencyId) : '',
            exchangeRate: detail.exchangeRate ?? 1,
            vehiclePairId: detail.vehiclePairId ? String(detail.vehiclePairId) : '',
            primaryVehicleId: detail.primaryVehicleId ? String(detail.primaryVehicleId) : '',
            secondaryVehicleId: detail.secondaryVehicleId ? String(detail.secondaryVehicleId) : '',
            driverId: detail.driverId ? String(detail.driverId) : '',
            primaryOwnerSharePercent: detail.primaryOwnerSharePercent ?? '',
            secondaryOwnerSharePercent: detail.secondaryOwnerSharePercent ?? '',
            driverCompensationType: detail.driverCompensationType ?? 1,
            driverFixedAmount: detail.driverFixedAmount ?? '',
            driverProfitSharePercent: detail.driverProfitSharePercent ?? '',
            notes: detail.notes ?? '',
            draftExpenses: [],
        })
        setShowDetail(false)
        setShowModal(true)
    }

    const handleAddExpense = async () => {
        if (!detail) return
        const validation = validateDraftExpense(expenseForm)
        if (validation) {
            showAppToast(validation, 'warning')
            return
        }
        setSavingExpense(true)
        try {
            const result = await tripsApi.addExpense(
                detail.transportTripId,
                buildExpensePayload(expenseForm, detail.currencyId),
            )
            if (result?.tripExpenseId) {
                await tripsApi.postExpense(result.tripExpenseId)
            }
            showAppToast('هزینه ثبت و در دفترروزنامه منعکس شد.', 'success')
            await loadDetail(detail.transportTripId)
            reloadTable()
        } catch (err) {
            showAppToast(err.message, 'danger')
        } finally {
            setSavingExpense(false)
        }
    }

    const handlePostExpense = async (expenseId) => {
        try {
            await tripsApi.postExpense(expenseId)
            showAppToast('هزینه در دفتر ثبت شد.', 'success')
            await loadDetail(detail.transportTripId)
        } catch (err) {
            showAppToast(err.message, 'danger')
        }
    }

    const estimatedAmount = isWeightBased(form)
        ? form.weightTon && form.ratePerTon
            ? Number(form.weightTon) * Number(form.ratePerTon)
            : null
        : form.amount
            ? Number(form.amount)
            : null

    const renderDraftExpenseRow = (expense) => (
        <div className="border rounded p-3 mb-2" key={expense.id}>
            <div className="row g-2">
                <div className="col-md-3">
                    <label className="form-label">دسته</label>
                    <select
                        className="form-select"
                        value={expense.tripExpenseCategoryId}
                        onChange={(e) => updateDraftExpense(expense.id, 'tripExpenseCategoryId', e.target.value)}
                    >
                        <option value="">انتخاب...</option>
                        {options.expenseCategories.map((o) => (
                            <option key={o.value} value={o.value}>
                                {o.label}
                            </option>
                        ))}
                    </select>
                </div>
                <div className="col-md-3">
                    <label className="form-label">عنوان</label>
                    <input
                        className="form-control"
                        value={expense.title}
                        onChange={(e) => updateDraftExpense(expense.id, 'title', e.target.value)}
                    />
                </div>
                <div className="col-md-2">
                    <JalaliDateField
                        label="تاریخ"
                        value={expense.expenseDate}
                        onChange={(v) => updateDraftExpense(expense.id, 'expenseDate', v)}
                    />
                </div>
                <div className="col-md-2">
                    <label className="form-label">مبلغ</label>
                    <input
                        type="number"
                        step="0.01"
                        className="form-control"
                        value={expense.amount}
                        onChange={(e) => updateDraftExpense(expense.id, 'amount', e.target.value)}
                    />
                </div>
                <div className="col-md-2">
                    <label className="form-label">وسیله</label>
                    <select
                        className="form-select"
                        value={expense.vehicleId}
                        onChange={(e) => updateDraftExpense(expense.id, 'vehicleId', e.target.value)}
                    >
                        <option value="">—</option>
                        {options.vehiclesAll.map((o) => (
                            <option key={o.value} value={o.value}>
                                {o.label}
                            </option>
                        ))}
                    </select>
                </div>
                <div className="col-md-3">
                    <label className="form-label">منبع پرداخت</label>
                    <select
                        className="form-select"
                        value={expense.paymentSource}
                        onChange={(e) => updateDraftExpense(expense.id, 'paymentSource', e.target.value)}
                    >
                        <option value="cash">صندوق</option>
                        <option value="bank">بانک</option>
                        <option value="party">طرف حساب</option>
                    </select>
                </div>
                {expense.paymentSource === 'cash' && (
                    <div className="col-md-3">
                        <label className="form-label">صندوق</label>
                        <select
                            className="form-select"
                            value={expense.cashBoxId}
                            onChange={(e) => updateDraftExpense(expense.id, 'cashBoxId', e.target.value)}
                        >
                            <option value="">انتخاب...</option>
                            {options.cashBoxes.map((o) => (
                                <option key={o.value} value={o.value}>
                                    {o.label}
                                </option>
                            ))}
                        </select>
                    </div>
                )}
                {expense.paymentSource === 'bank' && (
                    <div className="col-md-3">
                        <label className="form-label">بانک</label>
                        <select
                            className="form-select"
                            value={expense.bankAccountId}
                            onChange={(e) => updateDraftExpense(expense.id, 'bankAccountId', e.target.value)}
                        >
                            <option value="">انتخاب...</option>
                            {options.bankAccounts.map((o) => (
                                <option key={o.value} value={o.value}>
                                    {o.label}
                                </option>
                            ))}
                        </select>
                    </div>
                )}
                {expense.paymentSource === 'party' && (
                    <>
                        <div className="col-md-3">
                            <label className="form-label">نوع طرف</label>
                            <select
                                className="form-select"
                                value={expense.partyType}
                                onChange={(e) => updateDraftExpense(expense.id, 'partyType', e.target.value)}
                            >
                                {PARTY_TYPE_OPTIONS.map((o) => (
                                    <option key={o.value} value={o.value}>
                                        {o.label}
                                    </option>
                                ))}
                            </select>
                        </div>
                        <div className="col-md-3">
                            <label className="form-label">طرف حساب</label>
                            <select
                                className="form-select"
                                value={expense.partyId}
                                onChange={(e) => updateDraftExpense(expense.id, 'partyId', e.target.value)}
                            >
                                <option value="">انتخاب...</option>
                                {draftPartyOptions(expense.partyType).map((o) => (
                                    <option key={o.value} value={o.value}>
                                        {o.label}
                                    </option>
                                ))}
                            </select>
                        </div>
                    </>
                )}
                <div className="col-md-12 d-flex justify-content-end">
                    <button
                        type="button"
                        className="btn btn-sm btn-outline-danger"
                        onClick={() => removeDraftExpense(expense.id)}
                    >
                        حذف ردیف
                    </button>
                </div>
            </div>
        </div>
    )

    return (
        <div className="page-content trips-page">
            <div className="d-flex justify-content-between align-items-center mb-3">
                <h1 className="page-title mb-0">سرویس‌ها / سفرها</h1>
                <button type="button" className="btn btn-primary" onClick={openCreate}>
                    <Icon name="add" className="me-1" />
                    سفر جدید
                </button>
            </div>

            {loadError && <div className="alert alert-danger">{loadError}</div>}

            <div className="card">
                <div className="card-body card-body-table">
                    <div className="users-table-wrapper">
                        <DataTable
                            ref={tableRef}
                            className="table table-hover w-100 align-middle"
                            options={tableOptions}
                            slots={actionSlots}
                        >
                            <thead>
                                <tr>
                                    {dataColumns.map((c) => (
                                        <th key={c.data}>{c.title}</th>
                                    ))}
                                    <th>عملیات</th>
                                </tr>
                            </thead>
                        </DataTable>
                    </div>
                </div>
            </div>

            {showModal && (
                <div
                    className="modal d-block trips-form-modal"
                    tabIndex={-1}
                    style={{ backgroundColor: 'rgba(0,0,0,0.5)' }}
                >
                    <div className="modal-dialog modal-xl modal-dialog-scrollable modal-dialog-centered">
                        <form
                            ref={modalFormRef}
                            className="modal-content"
                            noValidate
                            onSubmit={(e) => {
                                e.preventDefault()
                                handleSave()
                            }}
                        >
                            <div className="modal-header">
                                <h5 className="modal-title">{editId ? 'ویرایش سفر' : 'سفر جدید'}</h5>
                                <button type="button" className="btn-close" onClick={() => setShowModal(false)} />
                            </div>
                            <div className="modal-body">
                                <div className="row g-3">
                                    <div className="col-md-4">
                                        <label className="form-label">تاریخ</label>
                                        <JalaliDateField
                                            label="تاریخ"
                                            value={form.tripDate}
                                            onChange={(v) => setField('tripDate', v)}
                                            required
                                        />
                                    </div>
                                    <div className="col-md-4">
                                        <label className="form-label">وضعیت</label>
                                        <select
                                            className="form-select"
                                            value={form.status}
                                            onChange={(e) => setField('status', e.target.value)}
                                        >
                                            {Object.entries(STATUS_LABELS).map(([value, label]) => (
                                                <option key={value} value={value}>
                                                    {label}
                                                </option>
                                            ))}
                                        </select>
                                    </div>
                                    <div className="col-md-4">
                                        <label className="form-label">مشتری</label>
                                        <select
                                            className="form-select"
                                            value={form.customerId}
                                            onChange={(e) => setField('customerId', e.target.value)}
                                            required
                                        >
                                            <option value="">انتخاب...</option>
                                            {options.customers.map((o) => (
                                                <option key={o.value} value={o.value}>
                                                    {o.label}
                                                </option>
                                            ))}
                                        </select>
                                    </div>
                                    <div className="col-md-6">
                                        <label className="form-label">مبدأ</label>
                                        <input
                                            className="form-control"
                                            value={form.origin}
                                            onChange={(e) => setField('origin', e.target.value)}
                                            required
                                        />
                                    </div>
                                    <div className="col-md-6">
                                        <label className="form-label">مقصد</label>
                                        <input
                                            className="form-control"
                                            value={form.destination}
                                            onChange={(e) => setField('destination', e.target.value)}
                                            required
                                        />
                                    </div>
                                    <div className="col-md-4">
                                        <label className="form-label">نحوه کرایه</label>
                                        <select
                                            className="form-select"
                                            value={form.freightMode}
                                            onChange={(e) => setField('freightMode', e.target.value)}
                                        >
                                            <option value={FREIGHT_MODE.LUMP_SUM}>کلی</option>
                                            <option value={FREIGHT_MODE.WEIGHT_BASED}>وزنی</option>
                                        </select>
                                    </div>
                                    {isWeightBased(form) ? (
                                        <>
                                            <div className="col-md-4">
                                                <label className="form-label">وزن (تن)</label>
                                                <input
                                                    type="number"
                                                    step="0.001"
                                                    className="form-control"
                                                    value={form.weightTon}
                                                    onChange={(e) => setField('weightTon', e.target.value)}
                                                    required
                                                />
                                            </div>
                                            <div className="col-md-4">
                                                <label className="form-label">نرخ هر تن</label>
                                                <input
                                                    type="number"
                                                    step="0.01"
                                                    className="form-control"
                                                    value={form.ratePerTon}
                                                    onChange={(e) => setField('ratePerTon', e.target.value)}
                                                    required
                                                />
                                            </div>
                                        </>
                                    ) : (
                                        <>
                                            <div className="col-md-4">
                                                <label className="form-label">مبلغ کرایه</label>
                                                <input
                                                    type="number"
                                                    step="0.01"
                                                    className="form-control"
                                                    value={form.amount}
                                                    onChange={(e) => setField('amount', e.target.value)}
                                                    required
                                                />
                                            </div>
                                            <div className="col-md-4">
                                                <label className="form-label">وزن (تن) — اختیاری</label>
                                                <input
                                                    type="number"
                                                    step="0.001"
                                                    className="form-control"
                                                    value={form.weightTon}
                                                    onChange={(e) => setField('weightTon', e.target.value)}
                                                />
                                            </div>
                                        </>
                                    )}
                                    <div className="col-md-4">
                                        <label className="form-label">ارز</label>
                                        <select
                                            className="form-select"
                                            value={form.currencyId}
                                            onChange={(e) => setField('currencyId', e.target.value)}
                                            required
                                        >
                                            <option value="">انتخاب...</option>
                                            {options.currencies.map((o) => (
                                                <option key={o.value} value={o.value}>
                                                    {o.label}
                                                </option>
                                            ))}
                                        </select>
                                    </div>
                                    <div className="col-md-4">
                                        <label className="form-label">مبلغ</label>
                                        <input
                                            className="form-control"
                                            value={estimatedAmount != null ? formatAmount(estimatedAmount) : '—'}
                                            readOnly
                                        />
                                    </div>
                                    <div className="col-md-4">
                                        <label className="form-label">جفت وسیله</label>
                                        <select
                                            className="form-select"
                                            value={form.vehiclePairId}
                                            onChange={(e) => {
                                                const pairId = e.target.value
                                                setField('vehiclePairId', pairId)
                                                const pair = options.pairs.find((p) => String(p.value) === String(pairId))
                                                if (pair) {
                                                    setForm((prev) => ({
                                                        ...prev,
                                                        vehiclePairId: pairId,
                                                        primaryVehicleId: pair.primaryVehicleId ? String(pair.primaryVehicleId) : '',
                                                        secondaryVehicleId: pair.secondaryVehicleId ? String(pair.secondaryVehicleId) : '',
                                                        primaryOwnerSharePercent: pair.primarySharePercent ?? '',
                                                        secondaryOwnerSharePercent: pair.secondarySharePercent ?? '',
                                                    }))
                                                }
                                            }}
                                        >
                                            <option value="">—</option>
                                            {options.pairs.map((o) => (
                                                <option key={o.value} value={o.value}>
                                                    {o.label}
                                                </option>
                                            ))}
                                        </select>
                                    </div>
                                    <div className="col-md-4">
                                        <label className="form-label">کشنده</label>
                                        <select
                                            className="form-select"
                                            value={form.primaryVehicleId}
                                            onChange={(e) => setField('primaryVehicleId', e.target.value)}
                                        >
                                            <option value="">—</option>
                                            {options.vehiclesPrimary.map((o) => (
                                                <option key={o.value} value={o.value}>
                                                    {o.label}
                                                </option>
                                            ))}
                                        </select>
                                    </div>
                                    <div className="col-md-4">
                                        <label className="form-label">بونکر</label>
                                        <select
                                            className="form-select"
                                            value={form.secondaryVehicleId}
                                            onChange={(e) => setField('secondaryVehicleId', e.target.value)}
                                        >
                                            <option value="">—</option>
                                            {options.vehiclesSecondary.map((o) => (
                                                <option key={o.value} value={o.value}>
                                                    {o.label}
                                                </option>
                                            ))}
                                        </select>
                                    </div>
                                    <div className="col-md-4">
                                        <label className="form-label">سهم کشنده %</label>
                                        <input
                                            type="number"
                                            step="0.01"
                                            className="form-control"
                                            value={form.primaryOwnerSharePercent}
                                            onChange={(e) => setField('primaryOwnerSharePercent', e.target.value)}
                                        />
                                    </div>
                                    <div className="col-md-4">
                                        <label className="form-label">سهم بونکر %</label>
                                        <input
                                            type="number"
                                            step="0.01"
                                            className="form-control"
                                            value={form.secondaryOwnerSharePercent}
                                            onChange={(e) => setField('secondaryOwnerSharePercent', e.target.value)}
                                        />
                                    </div>
                                    <div className="col-md-4">
                                        <label className="form-label">راننده</label>
                                        <select
                                            className="form-select"
                                            value={form.driverId}
                                            onChange={(e) => setField('driverId', e.target.value)}
                                        >
                                            <option value="">—</option>
                                            {options.drivers.map((o) => (
                                                <option key={o.value} value={o.value}>
                                                    {o.label}
                                                </option>
                                            ))}
                                        </select>
                                    </div>
                                    <div className="col-md-4">
                                        <label className="form-label">نوع پرداخت راننده</label>
                                        <select
                                            className="form-select"
                                            value={form.driverCompensationType}
                                            onChange={(e) => setField('driverCompensationType', e.target.value)}
                                        >
                                            <option value={1}>مبلغ ثابت</option>
                                            <option value={2}>درصد از سود</option>
                                        </select>
                                    </div>
                                    {Number(form.driverCompensationType) === 1 ? (
                                        <div className="col-md-4">
                                            <label className="form-label">مبلغ ثابت راننده</label>
                                            <input
                                                type="number"
                                                step="0.01"
                                                className="form-control"
                                                value={form.driverFixedAmount}
                                                onChange={(e) => setField('driverFixedAmount', e.target.value)}
                                            />
                                        </div>
                                    ) : (
                                        <div className="col-md-4">
                                            <label className="form-label">درصد سود راننده</label>
                                            <input
                                                type="number"
                                                step="0.01"
                                                className="form-control"
                                                value={form.driverProfitSharePercent}
                                                onChange={(e) => setField('driverProfitSharePercent', e.target.value)}
                                            />
                                        </div>
                                    )}
                                    <div className="col-md-12">
                                        <label className="form-label">یادداشت</label>
                                        <textarea
                                            className="form-control"
                                            rows={2}
                                            value={form.notes}
                                            onChange={(e) => setField('notes', e.target.value)}
                                        />
                                    </div>

                                    <div className="col-md-12">
                                        <div className="d-flex justify-content-between align-items-center mb-2">
                                            <h6 className="mb-0">هزینه‌های سفر</h6>
                                            <button type="button" className="btn btn-sm btn-outline-primary" onClick={addDraftExpense}>
                                                افزودن هزینه
                                            </button>
                                        </div>
                                        {form.draftExpenses.length === 0 && (
                                            <div className="text-muted small">هزینه‌ای اضافه نشده است.</div>
                                        )}
                                        {form.draftExpenses.map(renderDraftExpenseRow)}
                                    </div>
                                </div>
                            </div>
                            <div className="modal-footer">
                                <button type="button" className="btn btn-secondary" onClick={() => setShowModal(false)}>
                                    بستن
                                </button>
                                <button type="submit" className="btn btn-primary" disabled={saving}>
                                    {saving ? 'در حال ذخیره...' : 'ذخیره'}
                                </button>
                            </div>
                        </form>
                    </div>
                </div>
            )}

            {showDetail && detail && (
                <div className="modal d-block" tabIndex={-1} style={{ backgroundColor: 'rgba(0,0,0,0.5)' }}>
                    <div className="modal-dialog modal-xl modal-dialog-scrollable modal-dialog-centered">
                        <div className="modal-content">
                            <div className="modal-header">
                                <h5 className="modal-title">
                                    جزئیات سفر {detail.tripNumber} — {STATUS_LABELS[detail.status] ?? detail.status}
                                </h5>
                                <button type="button" className="btn-close" onClick={() => setShowDetail(false)} />
                            </div>
                            <div className="modal-body">
                                <div className="row g-3 mb-4">
                                    <div className="col-md-3">
                                        <div className="text-muted small">مبلغ</div>
                                        <div className="fw-semibold">{formatAmount(detail.amount)}</div>
                                    </div>
                                    <div className="col-md-3">
                                        <div className="text-muted small">وزن × نرخ</div>
                                        <div>
                                            {formatAmount(detail.weightTon)} × {formatAmount(detail.ratePerTon)}
                                        </div>
                                    </div>
                                    <div className="col-md-3">
                                        <div className="text-muted small">مسیر</div>
                                        <div>
                                            {detail.origin} → {detail.destination}
                                        </div>
                                    </div>
                                    <div className="col-md-3">
                                        <div className="text-muted small">ثبت درآمد</div>
                                        <div>{detail.isRevenuePosted ? 'انجام شده' : 'انجام نشده'}</div>
                                    </div>
                                </div>

                                <div className="d-flex gap-2 mb-3 flex-wrap">
                                    {!detail.isRevenuePosted && (
                                        <button type="button" className="btn btn-sm btn-outline-primary" onClick={openEditFromDetail}>
                                            ویرایش
                                        </button>
                                    )}
                                    {Number(detail.status) < 3 && Number(detail.status) !== 5 && (
                                        <button
                                            type="button"
                                            className="btn btn-sm btn-outline-info"
                                            onClick={() => handleDelivered(detail.transportTripId)}
                                        >
                                            تحویل
                                        </button>
                                    )}
                                    {!detail.isRevenuePosted && Number(detail.status) !== 5 && (
                                        <button
                                            type="button"
                                            className="btn btn-sm btn-primary"
                                            onClick={() => handlePostRevenue(detail.transportTripId)}
                                        >
                                            ثبت درآمد
                                        </button>
                                    )}
                                    {detail.isRevenuePosted && detail.status !== 4 && detail.status !== 5 && (
                                        <button
                                            type="button"
                                            className="btn btn-sm btn-success"
                                            onClick={() => handleSettle(detail.transportTripId)}
                                        >
                                            تسویه سهم‌ها
                                        </button>
                                    )}
                                </div>

                                <h6 className="mb-2">هزینه‌های سفر</h6>
                                <div className="table-responsive mb-3">
                                    <table className="table table-sm">
                                        <thead>
                                            <tr>
                                                <th>عنوان</th>
                                                <th>دسته</th>
                                                <th>تاریخ</th>
                                                <th>مبلغ</th>
                                                <th>وضعیت</th>
                                                <th />
                                            </tr>
                                        </thead>
                                        <tbody>
                                            {(detail.expenses ?? []).length === 0 && (
                                                <tr>
                                                    <td colSpan={6} className="text-muted">
                                                        هزینه‌ای ثبت نشده است.
                                                    </td>
                                                </tr>
                                            )}
                                            {(detail.expenses ?? []).map((e) => (
                                                <tr key={e.tripExpenseId}>
                                                    <td>{e.title}</td>
                                                    <td>{e.categoryName ?? '—'}</td>
                                                    <td>{String(e.expenseDate ?? '').slice(0, 10)}</td>
                                                    <td>{formatAmount(e.amount)}</td>
                                                    <td>{e.isPosted ? 'ثبت‌شده' : 'پیش‌نویس'}</td>
                                                    <td>
                                                        {!e.isPosted && (
                                                            <button
                                                                type="button"
                                                                className="btn btn-sm btn-outline-primary"
                                                                onClick={() => handlePostExpense(e.tripExpenseId)}
                                                            >
                                                                ثبت دفتر
                                                            </button>
                                                        )}
                                                    </td>
                                                </tr>
                                            ))}
                                        </tbody>
                                    </table>
                                </div>

                                {detail.status !== 4 && detail.status !== 5 && (
                                    <div className="border rounded p-3">
                                        <h6 className="mb-3">افزودن هزینه</h6>
                                        <div className="row g-2">
                                            <div className="col-md-3">
                                                <label className="form-label">دسته</label>
                                                <select
                                                    className="form-select"
                                                    value={expenseForm.tripExpenseCategoryId}
                                                    onChange={(e) => setExpenseField('tripExpenseCategoryId', e.target.value)}
                                                >
                                                    <option value="">انتخاب...</option>
                                                    {options.expenseCategories.map((o) => (
                                                        <option key={o.value} value={o.value}>
                                                            {o.label}
                                                        </option>
                                                    ))}
                                                </select>
                                            </div>
                                            <div className="col-md-3">
                                                <label className="form-label">عنوان</label>
                                                <input
                                                    className="form-control"
                                                    value={expenseForm.title}
                                                    onChange={(e) => setExpenseField('title', e.target.value)}
                                                />
                                            </div>
                                            <div className="col-md-2">
                                                <label className="form-label">تاریخ</label>
                                                <JalaliDateField
                                                    label="تاریخ"
                                                    value={expenseForm.expenseDate}
                                                    onChange={(v) => setExpenseField('expenseDate', v)}
                                                />
                                            </div>
                                            <div className="col-md-2">
                                                <label className="form-label">مبلغ</label>
                                                <input
                                                    type="number"
                                                    step="0.01"
                                                    className="form-control"
                                                    value={expenseForm.amount}
                                                    onChange={(e) => setExpenseField('amount', e.target.value)}
                                                />
                                            </div>
                                            <div className="col-md-2">
                                                <label className="form-label">وسیله</label>
                                                <select
                                                    className="form-select"
                                                    value={expenseForm.vehicleId}
                                                    onChange={(e) => setExpenseField('vehicleId', e.target.value)}
                                                >
                                                    <option value="">—</option>
                                                    {options.vehiclesAll.map((o) => (
                                                        <option key={o.value} value={o.value}>
                                                            {o.label}
                                                        </option>
                                                    ))}
                                                </select>
                                            </div>
                                            <div className="col-md-3">
                                                <label className="form-label">منبع پرداخت</label>
                                                <select
                                                    className="form-select"
                                                    value={expenseForm.paymentSource}
                                                    onChange={(e) => setExpenseField('paymentSource', e.target.value)}
                                                >
                                                    <option value="cash">صندوق</option>
                                                    <option value="bank">بانک</option>
                                                    <option value="party">طرف حساب</option>
                                                </select>
                                            </div>
                                            {expenseForm.paymentSource === 'cash' && (
                                                <div className="col-md-3">
                                                    <label className="form-label">صندوق</label>
                                                    <select
                                                        className="form-select"
                                                        value={expenseForm.cashBoxId}
                                                        onChange={(e) => setExpenseField('cashBoxId', e.target.value)}
                                                    >
                                                        <option value="">انتخاب...</option>
                                                        {options.cashBoxes.map((o) => (
                                                            <option key={o.value} value={o.value}>
                                                                {o.label}
                                                            </option>
                                                        ))}
                                                    </select>
                                                </div>
                                            )}
                                            {expenseForm.paymentSource === 'bank' && (
                                                <div className="col-md-3">
                                                    <label className="form-label">بانک</label>
                                                    <select
                                                        className="form-select"
                                                        value={expenseForm.bankAccountId}
                                                        onChange={(e) => setExpenseField('bankAccountId', e.target.value)}
                                                    >
                                                        <option value="">انتخاب...</option>
                                                        {options.bankAccounts.map((o) => (
                                                            <option key={o.value} value={o.value}>
                                                                {o.label}
                                                            </option>
                                                        ))}
                                                    </select>
                                                </div>
                                            )}
                                            {expenseForm.paymentSource === 'party' && (
                                                <>
                                                    <div className="col-md-3">
                                                        <label className="form-label">نوع طرف</label>
                                                        <select
                                                            className="form-select"
                                                            value={expenseForm.partyType}
                                                            onChange={(e) =>
                                                                setExpenseField('partyType', e.target.value)
                                                            }
                                                        >
                                                            {PARTY_TYPE_OPTIONS.map((o) => (
                                                                <option key={o.value} value={o.value}>
                                                                    {o.label}
                                                                </option>
                                                            ))}
                                                        </select>
                                                    </div>
                                                    <div className="col-md-3">
                                                        <label className="form-label">طرف حساب</label>
                                                        <select
                                                            className="form-select"
                                                            value={expenseForm.partyId}
                                                            onChange={(e) => setExpenseField('partyId', e.target.value)}
                                                        >
                                                            <option value="">انتخاب...</option>
                                                            {partyOptions.map((o) => (
                                                                <option key={o.value} value={o.value}>
                                                                    {o.label}
                                                                </option>
                                                            ))}
                                                        </select>
                                                    </div>
                                                </>
                                            )}
                                            <div className="col-md-3 d-flex align-items-end">
                                                <button
                                                    type="button"
                                                    className="btn btn-primary"
                                                    disabled={savingExpense}
                                                    onClick={handleAddExpense}
                                                >
                                                    {savingExpense ? '...' : 'ثبت هزینه'}
                                                </button>
                                            </div>
                                        </div>
                                    </div>
                                )}
                            </div>
                            <div className="modal-footer">
                                <button type="button" className="btn btn-secondary" onClick={() => setShowDetail(false)}>
                                    بستن
                                </button>
                            </div>
                        </div>
                    </div>
                </div>
            )}
        </div>
    )
}
