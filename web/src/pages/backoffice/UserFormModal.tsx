import { useState } from 'react'
import { X } from 'lucide-react'
import { createUser, updateUser } from '../../api/usersApi'
import type { WebUser, CreateUserRequest, UpdateUserRequest } from '../../types/UserTypes'
import { getErrorMessage } from '../../api/errors'

interface Props {
  user: WebUser | null          // null = create mode
  onClose: () => void
  onSaved: () => void
}

export default function UserFormModal({ user, onClose, onSaved }: Props) {
  const isEdit = !!user

  const [fullName, setFullName] = useState(user?.fullName ?? '')
  const [email, setEmail] = useState(user?.email ?? '')
  const [password, setPassword] = useState('')
  const [role, setRole] = useState<'Backoffice' | 'Operator'>(
    (user?.role as 'Backoffice' | 'Operator') ?? 'Operator'
  )
  const [assignedNodeIds, setAssignedNodeIds] = useState(
    user?.assignedNodeIds.join(', ') ?? ''
  )
  const [isSubmitting, setIsSubmitting] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    setError(null)

    // Client-side validation hints (API still enforces everything).
    if (!fullName.trim()) { setError('Full name is required.'); return }
    if (!email.trim()) { setError('Email is required.'); return }
    if (!isEdit && (!password || password.length < 8)) {
      setError('Password must be at least 8 characters.')
      return
    }
    if (isEdit && password && password.length < 8) {
      setError('Password must be at least 8 characters.')
      return
    }

    const nodeIds = role === 'Operator'
      ? assignedNodeIds.split(',').map((s) => s.trim()).filter(Boolean)
      : []

    if (role === 'Operator' && nodeIds.length === 0) {
      setError('Operator must have at least one assigned node ID.')
      return
    }

    try {
      setIsSubmitting(true)

      if (isEdit) {
        const body: UpdateUserRequest = {
          fullName: fullName.trim(),
          email: email.trim(),
          role,
          assignedNodeIds: nodeIds,
        }
        if (password) body.password = password
        await updateUser(user.userId, body)
      } else {
        const body: CreateUserRequest = {
          fullName: fullName.trim(),
          email: email.trim(),
          password,
          role,
          assignedNodeIds: nodeIds,
        }
        await createUser(body)
      }

      onSaved()
    } catch (err) {
      setError(getErrorMessage(err))
    } finally {
      setIsSubmitting(false)
    }
  }

  return (
    <div className="sg-modal-overlay" onClick={onClose}>
      <div
        className="sg-card sg-modal-content"
        style={{ width: '100%', maxWidth: 520 }}
        onClick={(e) => e.stopPropagation()}
      >
        <div className="sg-card-header">
          <h3 className="sg-h4">{isEdit ? 'Edit User' : 'Add User'}</h3>
          <button className="sg-btn sg-btn-ghost sg-btn-icon sg-btn-sm" onClick={onClose}>
            <X size={18} />
          </button>
        </div>

        <form onSubmit={handleSubmit} autoComplete="off">
          <div className="sg-card-body sg-stack">
            {error && (
              <div className="sg-alert sg-alert-danger">
                <span>{error}</span>
              </div>
            )}

            <div className="sg-form-field">
              <label htmlFor="user-fullname" className="sg-label">Full Name</label>
              <input
                id="user-fullname"
                className="sg-input"
                value={fullName}
                onChange={(e) => setFullName(e.target.value)}
                disabled={isSubmitting}
                placeholder="e.g. Ricky Perera"
              />
            </div>

            <div className="sg-form-field">
              <label htmlFor="user-email" className="sg-label">Email</label>
              <input
                id="user-email"
                type="email"
                className="sg-input"
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                disabled={isSubmitting}
                placeholder="e.g. ricky@example.com"
                autoComplete="off"
              />
            </div>

            <div className="sg-form-field">
              <label htmlFor="user-password" className="sg-label">
                Password {isEdit && <span className="sg-caption">(leave blank to keep current)</span>}
              </label>
              <input
                id="user-password"
                type="password"
                className="sg-input"
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                disabled={isSubmitting}
                placeholder={isEdit ? '••••••••' : 'Min 8 characters'}
                autoComplete="new-password"
              />
            </div>

            <div className="sg-form-field">
              <label htmlFor="user-role" className="sg-label">Role</label>
              <select
                id="user-role"
                className="sg-select"
                value={role}
                onChange={(e) => setRole(e.target.value as 'Backoffice' | 'Operator')}
                disabled={isSubmitting}
              >
                <option value="Operator">Grid Operator</option>
                <option value="Backoffice">Backoffice</option>
              </select>
            </div>

            {role === 'Operator' && (
              <div className="sg-form-field">
                <label htmlFor="user-nodes" className="sg-label">Assigned Node IDs</label>
                <input
                  id="user-nodes"
                  className="sg-input"
                  value={assignedNodeIds}
                  onChange={(e) => setAssignedNodeIds(e.target.value)}
                  disabled={isSubmitting}
                  placeholder="e.g. ND-0001, ND-0002"
                />
                <p className="sg-helper">Comma-separated node IDs</p>
              </div>
            )}
          </div>

          <div className="sg-card-footer">
            <button
              type="button"
              className="sg-btn sg-btn-secondary"
              onClick={onClose}
              disabled={isSubmitting}
            >
              Cancel
            </button>
            <button
              type="submit"
              className={`sg-btn sg-btn-primary ${isSubmitting ? 'sg-btn-loading opacity-70' : ''}`}
              disabled={isSubmitting}
            >
              {isSubmitting ? 'Saving…' : isEdit ? 'Save Changes' : 'Create User'}
            </button>
          </div>
        </form>
      </div>
    </div>
  )
}
