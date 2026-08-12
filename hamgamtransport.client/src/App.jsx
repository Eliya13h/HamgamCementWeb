import AppRoutes from './routes/AppRoutes'
import { useBlockBrowserSaveShortcut } from './hooks/useModalKeyboardShortcuts'

function App() {
  useBlockBrowserSaveShortcut()
  return <AppRoutes />
}

export default App
