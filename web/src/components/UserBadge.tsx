interface UserBadgeProps {
  displayName?: string
  role?: string
}

// Read-only identity display for the header (name + avatar initials).
// Sign-out stays in the sidebar footer; this is not a logout control.
export default function UserBadge({ displayName, role }: UserBadgeProps) {
  const initials = displayName
    ? displayName
        .trim()
        .split(/\s+/)
        .map((part) => part[0])
        .slice(0, 2)
        .join('')
        .toUpperCase()
    : '?'

  return (
    <div className="flex items-center gap-3">
      <div className="hidden text-right leading-tight sm:block">
        <p className="max-w-[160px] truncate text-sm font-medium text-sg-text">{displayName}</p>
        <p className="text-xs text-sg-text-muted">{role}</p>
      </div>
      <div className="sg-avatar sg-avatar-sm shrink-0">{initials}</div>
    </div>
  )
}
