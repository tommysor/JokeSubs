import './App.css'
import { Navigate, Route, Routes } from 'react-router-dom'
import StoreDetailPage from './StoreDetailPage'
import StoresPage from './StoresPage'

function App() {
  return (
    <div className="app-shell">
      <Routes>
        <Route path="/" element={<StoresPage />} />
        <Route path="/stores/:storeId" element={<StoreDetailPage />} />
        <Route path="*" element={<Navigate replace to="/" />} />
      </Routes>

      <footer className="app-footer">
        <p>
          Icons by <a target="_blank" href="https://icons8.com" rel="noreferrer">Icons8</a>
        </p>
      </footer>
    </div>
  )
}

export default App
