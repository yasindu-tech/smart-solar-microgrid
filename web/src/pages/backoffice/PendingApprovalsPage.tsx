import { useState } from 'react'
import { UserCheck, Clock, Download } from 'lucide-react'
import { activateProsumer, usePendingProsumersInfinite } from '../../api/ProsumersApi'
import { getErrorMessage } from '../../api/errors'
import { DEFAULT_PAGE_SIZE } from '../../lib/constants'
import type { Prosumer } from '../../types/ProsumerTypes'
export default function PendingApprovalsPage() {
  const {
    data,
    isLoading,
    isError,
    error,
    fetchNextPage,
    hasNextPage,
    isFetchingNextPage,
    refetch
  } = usePendingProsumersInfinite(DEFAULT_PAGE_SIZE)

  const [actionError, setActionError] = useState<string | null>(null)

  const handleApprove = async (nic: string, name: string) => {
    if (!confirm(`Approve prosumer ${name} (${nic})? They will be able to log in immediately.`))
      return

    try {
      setActionError(null)
      await activateProsumer(nic)
      await refetch()
    } catch (err) {
      setActionError(getErrorMessage(err))
    }
  }

  // Get total count from the first page if it exists
  const totalCount = data?.pages[0]?.total ?? 0
  const allItems = data?.pages.flatMap((page) => page.items) ?? []

  return (
    <div className="sg-stack-lg">
      <div className="sg-row-between">
        <div>
          <h2 className="sg-h3">Pending Activations</h2>
          <p className="sg-body-sm mt-1">
            Prosumers waiting for Backoffice approval
          </p>
        </div>
        {totalCount > 0 && (
          <span className="sg-badge sg-badge-warning" style={{ fontSize: 'var(--sg-text-md)' }}>
            <Clock size={14} /> {totalCount} pending
          </span>
        )}
      </div>

      {isError && <div className="sg-alert sg-alert-danger"><span>{error?.message || 'Failed to load pending prosumers'}</span></div>}
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
              ) : allItems.length === 0 ? (
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
                allItems.map((p: Prosumer) => (
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

        {/* Load More Pagination */}
        {hasNextPage && (
          <div className="sg-card-footer justify-center">
            <button
              className="sg-btn sg-btn-secondary sg-btn-sm"
              disabled={isFetchingNextPage}
              onClick={() => fetchNextPage()}
            >
              {isFetchingNextPage ? 'Loading more...' : <><Download size={14} className="mr-2"/> Load more</>}
            </button>
          </div>
        )}
      </div>
    </div>
  )
}
