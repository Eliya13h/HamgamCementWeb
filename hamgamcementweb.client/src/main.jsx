import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import { BrowserRouter } from 'react-router-dom'
import { AuthProvider } from './context/AuthContext'
import { ThemeProvider } from './context/ThemeContext'
import 'bootstrap/dist/css/bootstrap.rtl.min.css'
import './assets/Fonts/FontAwesome/css/all.min.css'
import 'bootstrap/dist/js/bootstrap.bundle.min.js'
import './styles/fonts.css'
import './styles/theme.css'
import './styles/datatable.css'
import './index.css'
import App from './App.jsx'

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
