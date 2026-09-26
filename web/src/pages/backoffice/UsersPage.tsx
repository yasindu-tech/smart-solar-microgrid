import { useCallback, useEffect, useState } from 'react'
import { Plus, ChevronLeft, ChevronRight } from 'lucide-react'
import { fetchUsers, setUserStatus } from '../../api/usersApi'
import type { WebUser } from '../../types/UserTypes'
import type { PagedResponse } from '../../types/PagedResponse'
import { getErrorMessage } from '../../api/errors'
import UserFormModal from '../backoffice/UserFormModal'

export default function UsersPage() {
  const [data, setData] = useState<PagedResponse<WebUser> | null>(null)
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [actionError, setActionError] = useState<string | null>(null)

  // Filters
  const [roleFilter, setRoleFilter] = useState<string>('')
  const [page, setPage] = useState(1)
  const pageSize = 10

  // Modal
  const [showModal, setShowModal] = useState(false)
  const [editingUser, setEditingUser] = useState<WebUser | null>(null)

  const loadUsers = useCallback(async () => {
    try {
      setIsLoading(true)
      setError(null)
      const result = await fetchUsers({
        role: roleFilter || undefined,
        page,
        pageSize,
      })
      setData(result)
    } catch (err) {
      setError(getErrorMessage(err))
    } finally {
      setIsLoading(false)
    }
  }, [roleFilter, page])

  useEffect(() => {
    loadUsers()
  }, [loadUsers])

  const handleToggleStatus = async (user: WebUser) => {
    const action = user.isActive ? 'deactivate' : 'activate'
    if (!confirm(`Are you sure you want to ${action} ${user.fullName}?`)) return

    try {
      setActionError(null)
      await setUserStatus(user.userId, !user.isActive)
      await loadUsers()
    } catch (err) {
      setActionError(getErrorMessage(err))
    }
  }

  const handleCreateClick = () => {
    setEditingUser(null)
    setShowModal(true)
  }

  const handleEditClick = (user: WebUser) => {
    setEditingUser(user)
    setShowModal(true)
  }

  const handleModalClose = () => {
    setShowModal(false)
    setEditingUser(null)
  }

  const handleModalSaved = () => {
    setShowModal(false)
    setEditingUser(null)
    loadUsers()
  }

  const totalPages = data ? Math.ceil(data.total / pageSize) : 0

  return (
    <div className="sg-stack-lg">
      <div className="sg-row-between">
        <div>
          <h2 className="sg-h3">User Management</h2>
          <p className="sg-body-sm mt-1">Manage Backoffice and Operator accounts</p>
        </div>
        <button
          id="btn-create-user"
          className="sg-btn sg-btn-primary"
          onClick={handleCreateClick}
        >
          <Plus size={18} />
          Add User
        </button>
      </div>

      {/* Filters */}
      <div className="sg-card sg-card-body">
        <div className="sg-row gap-4">
          <div className="sg-form-field" style={{ minWidth: 180 }}>
            <label className="sg-label">Role</label>
            <select
              id="filter-role"
              className="sg-select"
              value={roleFilter}
              onChange={(e) => { setRoleFilter(e.target.value); setPage(1) }}
            >
              <option value="">All Roles</option>
              <option value="Backoffice">Backoffice</option>
              <option value="Operator">Operator</option>
            </select>
          </div>
        </div>
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
                <th>User ID</th>
                <th>Full Name</th>
                <th>Email</th>
                <th>Role</th>
                <th>Status</th>
                <th>Created</th>
                <th style={{ textAlign: 'right' }}>Actions</th>
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
                  <td colSpan={7} style={{ textAlign: 'center', padding: '2rem' }}>
                    <span className="sg-body-sm text-sg-text-muted">No users found</span>
                  </td>
                </tr>
              ) : (
                data.items.map((user) => (
                  <tr key={user.userId}>
                    <td className="sg-caption" style={{ fontFamily: 'var(--sg-font-mono)' }}>
                      {user.userId}
                    </td>
                    <td>{user.fullName}</td>
                    <td className="sg-body-sm">{user.email}</td>
                    <td>
                      <span className={`sg-badge ${user.role === 'Backoffice' ? 'sg-badge-primary' : 'sg-badge-info'}`}>
                        {user.role}
                      </span>
                    </td>
                    <td>
                      <span className={`sg-badge ${user.isActive ? 'sg-badge-success' : 'sg-badge-danger'}`}>
                        <span className="sg-status-dot" />
                        {user.isActive ? 'Active' : 'Inactive'}
                      </span>
                    </td>
                    <td className="sg-caption">
                      {new Date(user.createdAt).toLocaleDateString()}
                    </td>
                    <td>
                      <div className="flex justify-end gap-2">
                        <button
                          className="sg-btn sg-btn-ghost sg-btn-sm"
                          onClick={() => handleEditClick(user)}
                        >
                          Edit
                        </button>
                        <button
                          className={`sg-btn sg-btn-sm ${user.isActive ? 'sg-btn-secondary' : 'sg-btn-success'}`}
                          onClick={() => handleToggleStatus(user)}
                        >
                          {user.isActive ? 'Deactivate' : 'Activate'}
                        </button>
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
              Showing {(page - 1) * pageSize + 1}–{Math.min(page * pageSize, data.total)} of {data.total}
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

      {/* Create / Edit Modal */}
      {showModal && (
        <UserFormModal
          user={editingUser}
          onClose={handleModalClose}
          onSaved={handleModalSaved}
        />
      )}
    </div>
  )
}
