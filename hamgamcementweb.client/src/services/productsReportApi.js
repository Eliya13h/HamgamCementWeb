export function getProductsReportUrl({ categoryId, activeOnly, belowMinStock } = {}) {
  const params = new URLSearchParams()

  if (categoryId) {
    params.set('categoryId', String(categoryId))
  }

  if (activeOnly === true || activeOnly === 'true' || activeOnly === 'active') {
    params.set('activeOnly', 'true')
  } else if (activeOnly === false || activeOnly === 'false' || activeOnly === 'inactive') {
    params.set('activeOnly', 'false')
  }

  if (belowMinStock) {
    params.set('belowMinStock', 'true')
  }

  const query = params.toString()
  return query ? `/report-viewer/products?${query}` : '/report-viewer/products'
}
