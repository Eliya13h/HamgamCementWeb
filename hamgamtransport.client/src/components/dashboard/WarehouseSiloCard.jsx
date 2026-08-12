import { useState } from 'react'
import { createPortal } from 'react-dom'

function formatQuantity(value) {
  if (value == null || Number.isNaN(Number(value))) return null
  return Number(value).toLocaleString('fa-IR', {
    maximumFractionDigits: 2,
  })
}

function WarehouseSiloCard({ warehouse }) {
  const [tooltip, setTooltip] = useState({ visible: false, x: 0, y: 0 })

  const hasCapacity =
    warehouse.fillPercent != null && warehouse.capacity != null && warehouse.capacity > 0
  const percent = hasCapacity ? Math.max(0, Math.min(100, Number(warehouse.fillPercent))) : 0
  const usedText = formatQuantity(warehouse.usedQuantity)
  const capacityText = formatQuantity(warehouse.capacity)
  const unit = warehouse.capacityUnit ?? ''

  const updateTooltipPosition = (clientX, clientY) => {
    const offset = 16
    const approxWidth = 220
    const approxHeight = 160
    const maxX = window.innerWidth - approxWidth - 8
    const maxY = window.innerHeight - approxHeight - 8

    setTooltip({
      visible: true,
      x: Math.max(8, Math.min(clientX + offset, maxX)),
      y: Math.max(8, Math.min(clientY + offset, maxY)),
    })
  }

  const hideTooltip = () => {
    setTooltip((prev) => ({ ...prev, visible: false }))
  }

  return (
    <>
      <div
        className={`silo-card${tooltip.visible ? ' is-tooltip-open' : ''}`}
        onMouseEnter={(event) => updateTooltipPosition(event.clientX, event.clientY)}
        onMouseMove={(event) => updateTooltipPosition(event.clientX, event.clientY)}
        onMouseLeave={hideTooltip}
      >
        <div className="silo-visual" aria-hidden="true">
          <div className="silo-wrapper">
            <div className="silo-roof" />
            <div className="silo-body">
              <div
                className="silo-fill"
                style={{ height: hasCapacity ? `${percent}%` : '0%' }}
              />
            </div>
            <div className="silo-hopper" />
            <div className="silo-outlet" />
          </div>
        </div>
      </div>

      {tooltip.visible &&
        createPortal(
          <div
            className="silo-tooltip"
            role="tooltip"
            style={{ left: `${tooltip.x}px`, top: `${tooltip.y}px` }}
          >
            <span className="silo-tooltip-line silo-tooltip-title">{warehouse.name}</span>
            <span className="silo-tooltip-line">{warehouse.warehouseTypeLabel}</span>
            {warehouse.location ? (
              <span className="silo-tooltip-line">موقعیت: {warehouse.location}</span>
            ) : null}
            {hasCapacity ? (
              <>
                <span className="silo-tooltip-line silo-tooltip-accent">
                  پرشدگی: {percent.toLocaleString('fa-IR')}٪
                </span>
                <span className="silo-tooltip-line">
                  موجودی: {usedText != null ? usedText : '۰'}
                  {unit ? ` ${unit}` : ''}
                </span>
                <span className="silo-tooltip-line">
                  ظرفیت: {capacityText}
                  {unit ? ` ${unit}` : ''}
                </span>
              </>
            ) : (
              <span className="silo-tooltip-line silo-tooltip-muted">ظرفیت تعریف نشده</span>
            )}
          </div>,
          document.body,
        )}
    </>
  )
}

export default WarehouseSiloCard
