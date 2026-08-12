import { getIcon } from '../../config/icons'

/**
 * Central icon component — Font Awesome 6 Pro (self-hosted zip).
 * CSS loaded from public/fontawesome/css/all.min.css
 */
function Icon({ name, className, ...props }) {
  const iconClass = getIcon(name)

  if (!iconClass) {
    if (import.meta.env.DEV) {
      console.warn(`Icon "${name}" is not registered in src/config/icons.js`)
    }
    return null
  }

  const classes = className ? `${iconClass} ${className}` : iconClass

  return <i className={classes} aria-hidden="true" {...props} />
}

export default Icon
