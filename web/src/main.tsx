/**
 * File        : main.tsx
 * Project     : Smart Solar Microgrid Trading System - Web client
 * Description : React entry point. The web app is a UI layer only - every
 *               business rule is enforced by the Web API. See
 *               docs/architecture/system-overview.md section 2.
 * Author      : <member name - M2>
 */

import React from 'react'
import ReactDOM from 'react-dom/client'
import App from './App'
import './index.css'

ReactDOM.createRoot(document.getElementById('root')!).render(
  <React.StrictMode>
    <App />
  </React.StrictMode>,
)
