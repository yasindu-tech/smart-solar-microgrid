import { useCallback, useEffect, useState } from 'react'
import { UserCheck, ChevronLeft, ChevronRight, Clock } from 'lucide-react'
import { fetchPendingProsumers, activateProsumer } from '../../api/prosumersApi'
import type { Prosumer } from '../../types/ProsumerTypes'
import type { PagedResponse } from '../../types/PagedResponse'
import { getErrorMessage } from '../../api/errors'

export default function PendingApprovalsPage() {
  const [data, setData] = useState<PagedResponse<Prosumer> | null>(null)
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [actionError, setActionError] = useState<string | null>(null)
  const [page, setPage] = useState(1)
  const pageSize = 10

  const loadPending = useCallback(async () => {
    try {
      setIsLoading(true)
      setError(null)
      const result = await fetchPendingProsumers({ page, pageSize })
      setData(result)
    } catch (err) {
      setError(getErrorMessage(err))
    } finally {
      setIsLoading(false)
    }
  }, [page])

  useEffect(() => {
    loadPending()
  }, [loadPending])

  const handleApprove = async (nic: string, name: string) => {
    if (!confirm(`Approve prosumer ${name} (${nic})? They will be able to log in immediately.`))
      return

    try {
      setActionError(null)
      await activateProsumer(nic)
      await loadPending()
    } catch (err) {
      setActionError(getErrorMessage(err))
    }
  }

  const totalPages = data ? Math.ceil(data.total / pageSize) : 0

  return (
    <div className="sg-stack-lg">
      <div className="sg-row-between">
        <div>
          <h2 className="sg-h3">Pending Activations</h2>
          <p className="sg-body-sm mt-1">
            Prosumers waiting for Backoffice approval
          </p>
        </div>
        {data && (
          <span className="sg-badge sg-badge-warning" style={{ fontSize: 'var(--sg-text-md)' }}>
            <Clock size={14} /> {data.total} pending
          </span>
        )}
      </div>

      {error && <div className="sg-alert sg-alert-danger"><span>{error}</span></div>}
      {actionError && <div className="sg-alert sg-alert-danger"><span>{actionError}</span></div>}

      {/* Table */}
      <div className="sg-card" style={{ overflow: 'hidden' }}>
        <div style={{ overflowX: 'auto' }}>
          <table className="sg-table">
            <thead>
              <tr>
                <th>NIC</th>
                <th>Full Name</th>
                <th>Email</th>
                <th>Phone</th>
                <th>Solar Capacity (kW)</th>
                <th>Requested</th>
                <th style={{ textAlign: 'right' }}>Action</th>
              </tr>
            </thead>
            <tbody>
              {isLoading ? (
                <tr>
                  <td colSpan={7} style={{ textAlign: 'center', padding: '2rem' }}>
                    <span className="sg-body-sm text-sg-text-muted">Loading…</span>
                  </td>
                </tr>
              ) : !data?.items.length ? (
                <tr>
                  <td colSpan={7} style={{ textAlign: 'center', padding: '3rem' }}>
                    <div className="sg-stack items-center">
                      <UserCheck size={40} style={{ color: 'var(--sg-success)' }} />
                      <p className="sg-body text-sg-text-muted">
                        No pending activations — all caught up!
                      </p>
                    </div>
                  </td>
                </tr>
              ) : (
                data.items.map((p) => (
                  <tr key={p.nic}>
                    <td className="sg-caption" style={{ fontFamily: 'var(--sg-font-mono)' }}>
                      {p.nic}
                    </td>
                    <td>{p.fullName}</td>
                    <td className="sg-body-sm">{p.email}</td>
                    <td className="sg-body-sm">{p.phone}</td>
                    <td className="sg-body-sm">{p.solarCapacityKw}</td>
                    <td className="sg-caption">
                      {new Date(p.createdAt).toLocaleDateString()}
                    </td>
                    <td style={{ textAlign: 'right' }}>
                      <button
                        className="sg-btn sg-btn-success sg-btn-sm"
                        onClick={() => handleApprove(p.nic, p.fullName)}
                      >
                        <UserCheck size={14} /> Approve
                      </button>
                    </td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>

        {/* Pagination */}
        {data && data.total > pageSize && (
          <div className="sg-card-footer">
            <span className="sg-caption">
              Showing {(page - 1) * pageSize + 1}–{Math.min(page * pageSize, data.total)} of{' '}
              {data.total}
            </span>
            <div className="sg-row gap-2">
              <button
                className="sg-btn sg-btn-secondary sg-btn-sm sg-btn-icon"
                disabled={page <= 1}
                onClick={() => setPage(page - 1)}
              >
                <ChevronLeft size={16} />
              </button>
              <button
                className="sg-btn sg-btn-secondary sg-btn-sm sg-btn-icon"
                disabled={page >= totalPages}
                onClick={() => setPage(page + 1)}
              >
                <ChevronRight size={16} />
              </button>
            </div>
          </div>
        )}
      </div>
    </div>
  )
}
