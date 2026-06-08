import Icon from '../../components/common/Icon'

const statCards = [
  {
    title: 'تولید امروز',
    value: '—',
    unit: 'تن',
    icon: 'production',
    trend: null,
  },
  {
    title: 'فروش ماه جاری',
    value: '—',
    unit: 'میلیون تومان',
    icon: 'sales-check',
    trend: null,
  },
  {
    title: 'موجودی انبار',
    value: '—',
    unit: 'تن',
    icon: 'inventory',
    trend: null,
  },
  {
    title: 'سفارشات فعال',
    value: '—',
    unit: 'مورد',
    icon: 'clipboard-check',
    trend: null,
  },
]

function DashboardPage() {
  return (
    <div className="dashboard-page">
      <section className="mb-4">
        <div className="page-welcome card border-0">
          <div className="card-body p-4">
            <h2 className="welcome-title mb-2">خوش آمدید</h2>
            <p className="welcome-text mb-0">
              این صفحه اسکلت داشبورد است. محتوای واقعی هر بخش بعداً از API یا
              کامپوننت‌های مربوطه بارگذاری می‌شود.
            </p>
          </div>
        </div>
      </section>

      <section className="mb-4">
        <div className="row g-3">
          {statCards.map((card) => (
            <div key={card.title} className="col-12 col-sm-6 col-xl-3">
              <div className="stat-card card h-100 border-0">
                <div className="card-body p-3 p-md-4">
                  <div className="d-flex align-items-start justify-content-between mb-3">
                    <div className="stat-icon">
                      <Icon name={card.icon} />
                    </div>
                  </div>
                  <p className="stat-label mb-1">{card.title}</p>
                  <div className="d-flex align-items-baseline gap-2">
                    <span className="stat-value">{card.value}</span>
                    <span className="stat-unit">{card.unit}</span>
                  </div>
                </div>
              </div>
            </div>
          ))}
        </div>
      </section>

      <section className="mb-4">
        <div className="row g-3">
          <div className="col-12 col-lg-8">
            <div className="content-card card border-0 h-100">
              <div className="card-header bg-transparent border-0 pt-4 px-4 pb-0">
                <h3 className="card-title mb-0">نمودار عملکرد</h3>
              </div>
              <div className="card-body p-4">
                <div className="chart-placeholder d-flex align-items-center justify-content-center">
                  <div className="text-center">
                    <Icon name="chart-up" className="placeholder-icon" />
                    <p className="placeholder-text mb-0 mt-2">
                      محل نمایش نمودار
                    </p>
                  </div>
                </div>
              </div>
            </div>
          </div>

          <div className="col-12 col-lg-4">
            <div className="content-card card border-0 h-100">
              <div className="card-header bg-transparent border-0 pt-4 px-4 pb-0">
                <h3 className="card-title mb-0">فعالیت‌های اخیر</h3>
              </div>
              <div className="card-body p-4">
                <ul className="activity-list list-unstyled mb-0">
                  {[1, 2, 3, 4].map((item) => (
                    <li key={item} className="activity-item">
                      <div className="activity-dot" />
                      <div>
                        <p className="activity-title mb-1">فعالیت نمونه {item}</p>
                        <span className="activity-time">—</span>
                      </div>
                    </li>
                  ))}
                </ul>
              </div>
            </div>
          </div>
        </div>
      </section>

      <section>
        <div className="content-card card border-0">
          <div className="card-header bg-transparent border-0 pt-4 px-4 pb-0 d-flex align-items-center justify-content-between">
            <h3 className="card-title mb-0">آخرین رکوردها</h3>
            <button type="button" className="btn btn-sm btn-outline-accent">
              مشاهده همه
            </button>
          </div>
          <div className="card-body p-4">
            <div className="table-responsive">
              <table className="table table-dark table-hover dashboard-table mb-0">
                <thead>
                  <tr>
                    <th scope="col">#</th>
                    <th scope="col">عنوان</th>
                    <th scope="col">وضعیت</th>
                    <th scope="col">تاریخ</th>
                  </tr>
                </thead>
                <tbody>
                  {[1, 2, 3, 4, 5].map((row) => (
                    <tr key={row}>
                      <td>{row}</td>
                      <td>رکورد نمونه {row}</td>
                      <td>
                        <span className="badge badge-status">در انتظار</span>
                      </td>
                      <td>—</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </div>
        </div>
      </section>
    </div>
  )
}

export default DashboardPage
