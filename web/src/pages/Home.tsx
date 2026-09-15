/**
 * File        : Home.tsx
 * Project     : Smart Solar Microgrid Trading System - Web client
 * Description : Index page / sign-in gateway for Backoffice and Grid Operator
 *               staff (Prosumers are Android-only - see project-config.md
 *               section 3). Built from the shared design system, not ad-hoc
 *               styles - see docs/architecture/ui-design-system.md. Every
 *               color/spacing/radius here comes from a base.css token or a
 *               .sg-* shared class; there is no literal hex or Tailwind color
 *               utility on this page (ui-design-system.md section 2).
 * Author      : <member name - M2>
 */

import { BatteryCharging, CalendarCheck, QrCode } from 'lucide-react'

const FEATURES = [
  {
    icon: BatteryCharging,
    title: 'Manage nodes & slots',
    body: 'Register microgrid nodes, edit capacity and schedules, and publish bookable energy slots.',
  },
  {
    icon: CalendarCheck,
    title: 'Approve & track reservations',
    body: 'Review pending reservations, approve or cancel within the 12-hour window, and search booking history.',
  },
  {
    icon: QrCode,
    title: 'Verify transfers via QR',
    body: 'Scan a prosumer’s QR code on-site; the server verifies it and finalises the energy transfer.',
  },
] as const

export default function Home() {
  return (
    <div className="sg-page">
      {/* ---------------------------------------------------------------- */}
      {/* Header                                                           */}
      {/* ---------------------------------------------------------------- */}
      <header
        className="sg-row-between"
        style={{
          position: 'sticky',
          top: 0,
          zIndex: 10,
          padding: 'var(--sg-space-4) var(--sg-space-6)',
          background: 'var(--sg-surface)',
          borderBottom: '1px solid var(--sg-border)',
        }}
      >
        <img src="/logo-navbar.png" alt="SunGrid" style={{ height: 32, width: 'auto' }} />
        <button type="button" className="sg-btn sg-btn-primary">
          Sign in
        </button>
      </header>

      <main className="sg-page-content">
        <div className="sg-container">
          {/* ---------------------------------------------------------- */}
          {/* Hero                                                       */}
          {/* ---------------------------------------------------------- */}
          <section
            className="sg-row"
            style={{
              alignItems: 'center',
              gap: 'var(--sg-space-8)',
              paddingBlock: 'var(--sg-space-12)',
              flexWrap: 'wrap',
            }}
          >
            <div style={{ flex: '1 1 360px', minWidth: 0 }}>
              <h1 className="sg-display" style={{ textWrap: 'balance' }}>
                Manage and verify solar energy reservations across your microgrid
              </h1>
              <p className="sg-body-lg sg-secondary-text" style={{ marginTop: 'var(--sg-space-4)' }}>
                SunGrid is the back-office and grid-operations console for the Smart Solar Microgrid
                Trading System — sign in to manage nodes, review reservations, and verify energy
                transfers.
              </p>
              <button
                type="button"
                className="sg-btn sg-btn-primary sg-btn-lg"
                style={{ marginTop: 'var(--sg-space-6)' }}
              >
                Sign in to continue
              </button>
            </div>

            <div style={{ flex: '1 1 320px', minWidth: 0 }}>
              <img
                src="/hero-solar-grid.jpg"
                alt="Diagram of solar panels connecting through a grid node to a mobile device"
                style={{ width: '100%', height: 'auto', borderRadius: 'var(--sg-radius-lg)' }}
              />
            </div>
          </section>

          {/* ---------------------------------------------------------- */}
          {/* Feature cards                                              */}
          {/* ---------------------------------------------------------- */}
          <section className="sg-grid sg-grid-3" style={{ paddingBlock: 'var(--sg-space-8)' }}>
            {FEATURES.map(({ icon: Icon, title, body }) => (
              <div key={title} className="sg-card">
                <div className="sg-card-body sg-stack-sm">
                  <div
                    style={{
                      display: 'grid',
                      placeItems: 'center',
                      width: 40,
                      height: 40,
                      borderRadius: 'var(--sg-radius-md)',
                      background: 'var(--sg-primary-soft)',
                      color: 'var(--sg-primary-hover)',
                    }}
                  >
                    <Icon size={20} aria-hidden="true" />
                  </div>
                  <h3 className="sg-h4" style={{ marginTop: 'var(--sg-space-2)' }}>
                    {title}
                  </h3>
                  <p className="sg-body-sm">{body}</p>
                </div>
              </div>
            ))}
          </section>
        </div>
      </main>

      {/* ---------------------------------------------------------------- */}
      {/* Footer                                                           */}
      {/* ---------------------------------------------------------------- */}
      <footer
        className="sg-container"
        style={{
          paddingBlock: 'var(--sg-space-6)',
          borderTop: '1px solid var(--sg-border)',
        }}
      >
        <p className="sg-caption">SE4040 Enterprise Application Development &middot; SunGrid</p>
      </footer>
    </div>
  )
}
