/**
 * Authentication utility functions
 * @deprecated Use the Zustand auth store instead for state management
 */

import { EnvConfig } from '@/lib/types/auth'

// Environment configuration
export const getEnvConfig = (): EnvConfig => ({
  apiUrl: process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5097',
  apiBaseUrl: process.env.NEXT_PUBLIC_API_BASE_URL || 'http://localhost:5097/api',
  appEnv: process.env.NEXT_PUBLIC_APP_ENV || 'development',
  appVersion: process.env.NEXT_PUBLIC_APP_VERSION || '1.0.0',
  secureCookies: process.env.NEXT_PUBLIC_SECURE_COOKIES === 'true',
  cookieDomain: process.env.NEXT_PUBLIC_COOKIE_DOMAIN || 'localhost',
  sessionWarningMinutes: parseInt(process.env.NEXT_PUBLIC_SESSION_WARNING_MINUTES || '5'),
  accessTokenExpiresMinutes: parseInt(process.env.NEXT_PUBLIC_ACCESS_TOKEN_EXPIRES_MINUTES || '15'),
  refreshTokenExpiresMinutes: parseInt(process.env.NEXT_PUBLIC_REFRESH_TOKEN_EXPIRES_MINUTES || '60'),
  enableDevTools: process.env.NEXT_PUBLIC_ENABLE_DEV_TOOLS === 'true',
  logLevel: process.env.NEXT_PUBLIC_LOG_LEVEL || 'info',
})

// JWT Token utilities
export const parseJWT = (token: string): any | null => {
  try {
    const base64Url = token.split('.')[1]
    const base64 = base64Url.replace(/-/g, '+').replace(/_/g, '/')
    const jsonPayload = decodeURIComponent(
      atob(base64)
        .split('')
        .map((c) => '%' + ('00' + c.charCodeAt(0).toString(16)).slice(-2))
        .join('')
    )
    return JSON.parse(jsonPayload)
  } catch (error) {
    console.error('Failed to parse JWT token:', error)
    return null
  }
}

export const isTokenExpired = (token: string): boolean => {
  const decoded = parseJWT(token)
  if (!decoded || !decoded.exp) return true
  
  const currentTime = Math.floor(Date.now() / 1000)
  return decoded.exp < currentTime
}

export const getTokenExpiry = (token: string): number | null => {
  const decoded = parseJWT(token)
  return decoded?.exp || null
}

export const getTokenTimeRemaining = (token: string): number => {
  const decoded = parseJWT(token)
  if (!decoded || !decoded.exp) return 0
  
  const currentTime = Math.floor(Date.now() / 1000)
  return Math.max(0, decoded.exp - currentTime)
}

// API Configuration
const config = getEnvConfig()

// API endpoints
export const API_ENDPOINTS = {
  // Authentication
  LOGIN: `${config.apiBaseUrl}/auth/login`,
  PIN_LOGIN: `${config.apiBaseUrl}/auth/pin-login`,
  REFRESH: `${config.apiBaseUrl}/auth/refresh`,
  LOGOUT: `${config.apiBaseUrl}/auth/logout`,
  LOGOUT_ALL: `${config.apiBaseUrl}/auth/logout-all`,
  VERIFY: `${config.apiBaseUrl}/auth/verify`,
  
  // Password Management
  FORGOT_PASSWORD: `${config.apiBaseUrl}/auth/forgot-password`,
  RESET_PASSWORD: `${config.apiBaseUrl}/auth/reset-password`,
  
  // User Management
  REGISTER: `${config.apiBaseUrl}/auth/register`,
  
  // Session Management
  SESSIONS: `${config.apiBaseUrl}/auth/sessions`,
  
  // Health & Info
  HEALTH: `${config.apiBaseUrl}/health`,
  INFO: `${config.apiBaseUrl}/info`,
} as const

// Request utilities
export const getAuthHeaders = (token?: string): Record<string, string> => {
  const headers: Record<string, string> = {
    'Content-Type': 'application/json',
  }

  if (token) {
    headers.Authorization = `Bearer ${token}`
  }

  return headers
}

export const createApiRequest = (
  endpoint: string,
  options: RequestInit = {},
  token?: string
): RequestInit => {
  return {
    ...options,
    headers: {
      ...getAuthHeaders(token),
      ...options.headers,
    },
  }
}

// Error handling utilities
export const handleApiError = (error: any): string => {
  if (error.response?.data?.message) {
    return error.response.data.message
  }
  
  if (error.message) {
    return error.message
  }
  
  return 'An unexpected error occurred'
}

// Validation utilities
export const validateEmail = (email: string): boolean => {
  const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/
  return emailRegex.test(email)
}

export const validatePassword = (password: string): {
  isValid: boolean
  errors: string[]
} => {
  const errors: string[] = []
  
  if (password.length < 8) {
    errors.push('Password must be at least 8 characters long')
  }
  
  if (!/[A-Z]/.test(password)) {
    errors.push('Password must contain at least one uppercase letter')
  }
  
  if (!/[a-z]/.test(password)) {
    errors.push('Password must contain at least one lowercase letter')
  }
  
  if (!/\d/.test(password)) {
    errors.push('Password must contain at least one number')
  }
  
  if (!/[!@#$%^&*]/.test(password)) {
    errors.push('Password must contain at least one special character (!@#$%^&*)')
  }
  
  return {
    isValid: errors.length === 0,
    errors
  }
}

export const validatePIN = (pin: string): boolean => {
  return /^\d{4}$/.test(pin)
}

// Device information utilities
export const getDeviceInfo = (): string => {
  if (typeof window === 'undefined') return 'Server'
  
  const userAgent = window.navigator.userAgent
  const platform = window.navigator.platform
  
  return `${platform} - ${userAgent}`
}

export const getBrowserInfo = (): string => {
  if (typeof window === 'undefined') return 'Server'
  
  const userAgent = window.navigator.userAgent
  
  if (userAgent.includes('Chrome')) return 'Chrome'
  if (userAgent.includes('Firefox')) return 'Firefox'
  if (userAgent.includes('Safari')) return 'Safari'
  if (userAgent.includes('Edge')) return 'Edge'
  
  return 'Unknown Browser'
}

// Session utilities
export const calculateSessionExpiry = (tokenExpiry: number): Date => {
  return new Date(tokenExpiry * 1000)
}

export const formatTimeRemaining = (seconds: number): string => {
  const minutes = Math.floor(seconds / 60)
  const remainingSeconds = seconds % 60
  
  if (minutes > 0) {
    return `${minutes}m ${remainingSeconds}s`
  }
  
  return `${remainingSeconds}s`
}
