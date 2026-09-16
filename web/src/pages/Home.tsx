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
      <header className="sticky top-0 z-sticky flex items-center justify-between gap-4 border-b border-sg-border bg-sg-surface px-6 py-4">
        <img src="/logo-navbar.png" alt="SunGrid" className="h-8 w-auto" />
        <button type="button" className="sg-btn sg-btn-primary">
          Sign in
        </button>
      </header>

      <main className="sg-page-content">
        <div className="sg-container">
          <section className="grid items-center gap-8 py-12 md:grid-cols-2">
            <div>
              <h1 className="sg-display text-balance">
                Manage and verify solar energy reservations across your microgrid
              </h1>
              <p className="sg-body-lg sg-secondary-text mt-4">
                SunGrid is the back-office and grid-operations console for the Smart Solar Microgrid
                Trading System — sign in to manage nodes, review reservations, and verify energy
                transfers.
              </p>
              <button type="button" className="sg-btn sg-btn-primary sg-btn-lg mt-6">
                Sign in to continue
              </button>
            </div>

            <img
              src="/hero-solar-grid.jpg"
              alt="Diagram of solar panels connecting through a grid node to a mobile device"
              className="h-auto w-full rounded-lg"
            />
          </section>

          <section className="grid grid-cols-1 gap-4 py-8 sm:grid-cols-2 lg:grid-cols-3">
            {FEATURES.map(({ icon: Icon, title, body }) => (
              <div key={title} className="sg-card">
                <div className="sg-card-body sg-stack-sm">
                  <div className="grid h-10 w-10 place-items-center rounded-md bg-sg-primary-soft text-sg-primary-hover">
                    <Icon size={20} aria-hidden="true" />
                  </div>
                  <h3 className="sg-h4 mt-2">{title}</h3>
                  <p className="sg-body-sm">{body}</p>
                </div>
              </div>
            ))}
          </section>
        </div>
      </main>

      <footer className="sg-container border-t border-sg-border py-6">
        <p className="sg-caption">SE4040 Enterprise Application Development &middot; SunGrid</p>
      </footer>
    </div>
  )
}
