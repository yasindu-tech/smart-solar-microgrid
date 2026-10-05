import React, { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { useAuth } from '../lib/AuthContext'
import { useLogin } from '../api/hooks'
import { Eye, EyeOff } from 'lucide-react'

export default function Login() {
  const navigate = useNavigate()
  const { login } = useAuth()
  const { loginAction, isLoading, error: apiError } = useLogin()
  
  const [identifier, setIdentifier] = useState('')
  const [password, setPassword] = useState('')
  const [showPassword, setShowPassword] = useState(false)
  const [validationError, setValidationError] = useState<string | null>(null)

  const error = validationError || apiError

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    setValidationError(null)

    if (!identifier || !password) {
      setValidationError('Please enter both identifier and password.')
      return
    }

    const response = await loginAction({ identifier, password })
    
    if (response) {
      login(response.token, {
        subjectId: response.nicOrUserId,
        role: response.role,
        displayName: response.displayName,
      })

      if (response.role === 'Backoffice') {
        navigate('/backoffice')
      } else if (response.role === 'Operator') {
        navigate('/operator')
      } else if (response.role === 'Prosumer') {
        navigate('/prosumer')
      } else {
        navigate('/')
      }
    }
  }

  return (
    <div className="sg-page flex items-center justify-center bg-sg-bg">
      <div className="sg-card w-full max-w-md p-8">
        <div className="text-center mb-8">
          <img src="/logo-navbar.png" alt="SunGrid" className="h-10 w-auto mx-auto mb-6" />
          <h1 className="sg-h3">Welcome back</h1>
          <p className="sg-body-sm text-sg-text-secondary mt-2">
            Sign in to your administration account
          </p>
        </div>

        <form onSubmit={handleSubmit} className="sg-stack" autoComplete="off">
          {error && (
            <div className="p-3 bg-sg-danger-soft text-sg-danger-text text-sm rounded-md border border-sg-danger/20">
              {error}
            </div>
          )}

          <div className="sg-form-field">
            <label htmlFor="identifier" className="text-sm font-medium">
              Email Address
            </label>
            <input
              id="identifier"
              name="identifier"
              type="text"
              value={identifier}
              onChange={(e) => setIdentifier(e.target.value)}
              className="sg-input"
              placeholder="e.g. example@example.com"
              disabled={isLoading}
              autoComplete="off"
              data-lpignore="true"
            />
          </div>

          <div className="sg-form-field">
            <label htmlFor="password" className="text-sm font-medium">
              Password
            </label>
            <div className="relative">
              <input
                id="password"
                name="password"
                type={showPassword ? "text" : "password"}
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                className="sg-input pr-10"
                placeholder="Enter your password"
                disabled={isLoading}
                autoComplete="new-password"
                data-lpignore="true"
              />
              <button
                type="button"
                onClick={() => setShowPassword(!showPassword)}
                className="absolute right-3 top-1/2 -translate-y-1/2 text-sg-text-muted hover:text-sg-text focus:outline-none"
                disabled={isLoading}
                aria-label={showPassword ? "Hide password" : "Show password"}
              >
                {showPassword ? <EyeOff size={18} /> : <Eye size={18} />}
              </button>
            </div>
          </div>

          <button
            type="submit"
            className={`sg-btn sg-btn-primary w-full mt-4 ${isLoading ? 'sg-btn-loading opacity-70' : ''}`}
            disabled={isLoading}
          >
            {isLoading ? 'Signing in...' : 'Sign in'}
          </button>
        </form>
      </div>
    </div>
  )
}
