function EmptyPage({ title }) {
  return (
    <div className="content-card card border-0">
      <div className="card-body p-4">
        <h2 className="card-title mb-2">{title}</h2>
        <p className="text-muted mb-0">محتوای این صفحه به‌زودی اضافه می‌شود.</p>
      </div>
    </div>
  )
}

export default EmptyPage
