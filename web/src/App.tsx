/**
 * File        : App.tsx
 * Project     : Smart Solar Microgrid Trading System - Web client
 * Description : Root component and route shell. Role routing lands Backoffice
 *               and Grid Operator users on different dashboards (REQ-AUTH).
 * Author      : <member name - M2>
 */

import { BrowserRouter, Route, Routes } from 'react-router-dom'
import Home from './pages/Home'

export default function App() {
  return (
    <BrowserRouter>
      <Routes>
        <Route path="/" element={<Home />} />
      </Routes>
    </BrowserRouter>
  )
}
