import { useCallback, useEffect, useState } from 'react'
import { Search, ChevronLeft, ChevronRight, UserCheck, UserX } from 'lucide-react'
import {
  fetchProsumers,
  activateProsumer,
  deactivateProsumer,
} from '../../api/ProsumersApi'
import type { Prosumer } from '../../types/ProsumerTypes'
import type { PagedResponse } from '../../types/PagedResponse'
import { getErrorMessage } from '../../api/errors'

export default function ProsumersPage() {
  const [data, setData] = useState<PagedResponse<Prosumer> | null>(null)
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [actionError, setActionError] = useState<string | null>(null)

  // Filters
  const [statusFilter, setStatusFilter] = useState<string>('')
  const [searchQuery, setSearchQuery] = useState('')
  const [page, setPage] = useState(1)
  const pageSize = 10

  const loadProsumers = useCallback(async () => {
    try {
      setIsLoading(true)
      setError(null)
      const result = await fetchProsumers({
        status: statusFilter || undefined,
        search: searchQuery || undefined,
        page,
        pageSize,
      })
      setData(result)
    } catch (err) {
      setError(getErrorMessage(err))
    } finally {
      setIsLoading(false)
    }
  }, [statusFilter, searchQuery, page])

  useEffect(() => {
    loadProsumers()
  }, [loadProsumers])

  const handleSearchSubmit = (e: React.FormEvent) => {
    e.preventDefault()
    setPage(1)
    loadProsumers()
  }

  const handleActivate = async (nic: string, name: string) => {
    if (!confirm(`Activate / reactivate prosumer ${name} (${nic})?`)) return
    try {
      setActionError(null)
      await activateProsumer(nic)
      await loadProsumers()
    } catch (err) {
      setActionError(getErrorMessage(err))
    }
  }

  const handleDeactivate = async (nic: string, name: string) => {
    if (
      !confirm(
        `Deactivate prosumer ${name} (${nic})?\n\nThis will cancel all their future reservations (BR-04).`
      )
    )
      return
    try {
      setActionError(null)
      await deactivateProsumer(nic)
      await loadProsumers()
    } catch (err) {
      setActionError(getErrorMessage(err))
    }
  }

  const statusBadge = (status: string) => {
    switch (status) {
      case 'Active':
        return <span className="sg-badge sg-badge-success"><span className="sg-status-dot" />Active</span>
      case 'PendingActivation':
        return <span className="sg-badge sg-badge-warning"><span className="sg-status-dot" />Pending</span>
      case 'Deactivated':
        return <span className="sg-badge sg-badge-danger"><span className="sg-status-dot" />Deactivated</span>
      default:
        return <span className="sg-badge sg-badge-neutral">{status}</span>
    }
  }

  const totalPages = data ? Math.ceil(data.total / pageSize) : 0

  return (
    <div className="sg-stack-lg">
      <div>
        <h2 className="sg-h3">Prosumer Management</h2>
        <p className="sg-body-sm mt-1">View and manage all prosumer accounts</p>
      </div>

      {/* Filters */}
      <div className="sg-card sg-card-body">
        <form className="sg-row gap-4" onSubmit={handleSearchSubmit}>
          <div className="sg-form-field" style={{ minWidth: 180 }}>
            <label className="sg-label">Status</label>
            <select
              id="filter-prosumer-status"
              className="sg-select"
              value={statusFilter}
              onChange={(e) => { setStatusFilter(e.target.value); setPage(1) }}
            >
              <option value="">All Statuses</option>
              <option value="Active">Active</option>
              <option value="PendingActivation">Pending Activation</option>
              <option value="Deactivated">Deactivated</option>
            </select>
          </div>
          <div className="sg-form-field" style={{ flex: 1 }}>
            <label className="sg-label">Search</label>
            <div className="relative">
              <input
                id="search-prosumers"
                className="sg-input"
                placeholder="Search by NIC or name…"
                value={searchQuery}
                onChange={(e) => setSearchQuery(e.target.value)}
              />
            </div>
          </div>
          <div className="sg-form-field" style={{ justifyContent: 'flex-end' }}>
            <label className="sg-label">&nbsp;</label>
            <button type="submit" className="sg-btn sg-btn-primary sg-btn-sm">
              <Search size={16} /> Search
            </button>
          </div>
        </form>
      </div>

      {/* Error messages */}
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
                <th>Capacity (kW)</th>
                <th>Status</th>
                <th>Registered</th>
                <th style={{ textAlign: 'right' }}>Actions</th>
              </tr>
            </thead>
            <tbody>
              {isLoading ? (
                <tr>
                  <td colSpan={8} style={{ textAlign: 'center', padding: '2rem' }}>
                    <span className="sg-body-sm text-sg-text-muted">Loading…</span>
                  </td>
                </tr>
              ) : !data?.items.length ? (
                <tr>
                  <td colSpan={8} style={{ textAlign: 'center', padding: '2rem' }}>
                    <span className="sg-body-sm text-sg-text-muted">No prosumers found</span>
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
                    <td>{statusBadge(p.status)}</td>
                    <td className="sg-caption">
                      {new Date(p.createdAt).toLocaleDateString()}
                    </td>
                    <td>
                      <div className="flex justify-end gap-2">
                        {p.status !== 'Active' && (
                          <button
                            className="sg-btn sg-btn-success sg-btn-sm"
                            onClick={() => handleActivate(p.nic, p.fullName)}
                            title="Activate / Reactivate"
                          >
                            <UserCheck size={14} /> Activate
                          </button>
                        )}
                        {p.status === 'Active' && (
                          <button
                            className="sg-btn sg-btn-danger sg-btn-sm"
                            onClick={() => handleDeactivate(p.nic, p.fullName)}
                            title="Deactivate"
                          >
                            <UserX size={14} /> Deactivate
                          </button>
                        )}
                      </div>
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
