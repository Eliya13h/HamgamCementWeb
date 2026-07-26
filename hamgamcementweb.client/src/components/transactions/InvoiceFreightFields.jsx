import AmountDisplay from '../common/AmountDisplay'
import AmountField from '../common/AmountField'
import SearchableSelect from '../common/SearchableSelect'

export const FREIGHT_MODE = {
  None: 0,
  OwnFleet: 1,
  Hired: 2,
}

export const FREIGHT_MODE_OPTIONS = [
  { value: '0', label: 'بدون حمل' },
  { value: '1', label: 'ناوگان خودی' },
  { value: '2', label: 'کرایه‌ای' },
]

export const emptyFreight = {
  freightMode: '0',
  freightRatePerTon: '',
  freightWeightTon: '',
  freightVehicleId: '',
  freightCarrierName: '',
}

/** وزن تن از جمع مقدار پایه خطوط (کیلو ÷ ۱۰۰۰) */
export function freightWeightTonFromLines(computedLines) {
  const kg = (computedLines ?? []).reduce(
    (sum, line) => sum + (Number(line.quantityInBase) || 0),
    0,
  )
  return Math.round((kg / 1000) * 10000) / 10000
}

export function calcFreightAmount(ratePerTon, weightTon) {
  const rate = Number(ratePerTon) || 0
  const weight = Number(weightTon) || 0
  if (rate <= 0 || weight <= 0) return 0
  return Math.round(rate * weight * 10000) / 10000
}

/**
 * بلوک جمع‌وجور کرایه حمل روی فاکتور خرید/فروش
 */
function InvoiceFreightFields({
  freight,
  onChange,
  vehicleOptions,
  currencySymbol,
  disabled,
  weightAutoHint,
}) {
  const mode = Number(freight.freightMode) || 0
  const amount = calcFreightAmount(freight.freightRatePerTon, freight.freightWeightTon)

  return (
    <div className="border rounded p-3 mb-3 bg-light-subtle">
      <div className="d-flex align-items-center justify-content-between mb-2">
        <h6 className="mb-0">حمل و کرایه</h6>
        {mode !== FREIGHT_MODE.None && (
          <small className="text-muted">
            مبلغ کرایه: <AmountDisplay value={amount} symbol={currencySymbol} />
          </small>
        )}
      </div>
      <div className="row g-3">
        <div className="col-md-3">
          <label className="form-label">نوع حمل</label>
          <select
            className="form-select"
            value={String(freight.freightMode ?? '0')}
            disabled={disabled}
            onChange={(e) => onChange('freightMode', e.target.value)}
          >
            {FREIGHT_MODE_OPTIONS.map((o) => (
              <option key={o.value} value={o.value}>
                {o.label}
              </option>
            ))}
          </select>
        </div>

        {mode === FREIGHT_MODE.OwnFleet && (
          <div className="col-md-3">
            <label className="form-label">وسیله نقلیه</label>
            <SearchableSelect
              options={vehicleOptions}
              value={freight.freightVehicleId === '' || freight.freightVehicleId == null
                ? ''
                : String(freight.freightVehicleId)}
              onChange={(next) => onChange('freightVehicleId', next)}
              disabled={disabled}
              placeholder="انتخاب وسیله..."
            />
          </div>
        )}

        {mode === FREIGHT_MODE.Hired && (
          <div className="col-md-3">
            <label className="form-label">نام باربری / مالک</label>
            <input
              type="text"
              className="form-control"
              value={freight.freightCarrierName ?? ''}
              disabled={disabled}
              onChange={(e) => onChange('freightCarrierName', e.target.value)}
              maxLength={200}
            />
          </div>
        )}

        {mode !== FREIGHT_MODE.None && (
          <>
            <div className="col-md-3">
              <label className="form-label">نرخ هر تن</label>
              <AmountField
                value={freight.freightRatePerTon}
                onChange={(next) => onChange('freightRatePerTon', next)}
                symbol={currencySymbol}
                disabled={disabled}
                min="0"
              />
            </div>
            <div className="col-md-3">
              <label className="form-label">وزن (تن)</label>
              <AmountField
                value={freight.freightWeightTon}
                onChange={(next) => onChange('freightWeightTon', next)}
                disabled={disabled}
                min="0"
              />
              {weightAutoHint ? (
                <small className="text-muted d-block mt-1">{weightAutoHint}</small>
              ) : null}
            </div>
          </>
        )}
      </div>
    </div>
  )
}

export default InvoiceFreightFields
