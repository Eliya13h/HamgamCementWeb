import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import { BrowserRouter } from 'react-router-dom'
import { AuthProvider } from './context/AuthContext'
import { ThemeProvider } from './context/ThemeContext'
import 'bootstrap/dist/css/bootstrap.rtl.min.css'
import './assets/Fonts/FontAwesome/css/all.min.css'
import 'bootstrap/dist/js/bootstrap.bundle.min.js'
import 'react-multi-date-picker'
import './styles/fonts.css'
import './styles/theme.css'
import './styles/datatable.css'
import './styles/searchable-select.css'
import './styles/permissions.css'
import './styles/jalali-datepicker.css'
import { applyRmdpTheme } from './lib/applyRmdpTheme'
import './index.css'
import App from './App.jsx'

applyRmdpTheme()

createRoot(document.getElementById('root')).render(
  <StrictMode>
    <BrowserRouter>
      <ThemeProvider>
        <AuthProvider>
          <App />
        </AuthProvider>
      </ThemeProvider>
    </BrowserRouter>
  </StrictMode>,
)
