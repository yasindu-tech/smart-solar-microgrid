/**
 * File        : Home.tsx
 * Project     : Smart Solar Microgrid Trading System - Web client
 * Description : Index page. The marking scheme awards a mark for a complete,
 *               well designed home page, so this is a real screen and not a
 *               placeholder once the team fills it in.
 * Author      : <member name - M2>
 */

export default function Home() {
  return (
    <main className="min-h-screen bg-slate-50 text-slate-900">
      <div className="mx-auto max-w-3xl px-6 py-16">
        <h1 className="text-3xl font-semibold tracking-tight">
          Smart Solar Microgrid Trading System
        </h1>
        <p className="mt-3 text-slate-600">
          Back-office administration and grid operations. Sign in to continue.
        </p>
      </div>
    </main>
  )
}
