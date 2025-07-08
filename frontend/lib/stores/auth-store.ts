/**
 * Authentication Store using Zustand
 * Manages global authentication state and actions
 */

import { create } from 'zustand'
import { persist, createJSONStorage } from 'zustand/middleware'
import { devtools } from 'zustand/middleware'
import { 
  AuthStore, 
  AuthState, 
  User, 
  LoginRequest, 
  PinLoginRequest, 
  AuthResponse,
  TokenPair,
  DecodedToken,
  ForgotPasswordResponse,
  ResetPasswordRequest,
  ResetPasswordResponse,
  RegisterRequest,
  RegisterResponse,
  SessionResponse
} from '../types/auth'

// JWT Token Utilities
const parseJWT = (token: string): DecodedToken | null => {
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

const isTokenExpired = (token: string): boolean => {
  const decoded = parseJWT(token)
  if (!decoded) return true
  
  const currentTime = Math.floor(Date.now() / 1000)
  return decoded.exp < currentTime
}

const getTokenExpiry = (token: string): number | null => {
  const decoded = parseJWT(token)
  return decoded?.exp || null
}

// Initial State
const initialState: AuthState = {
  user: null,
  accessToken: null,
  refreshToken: null,
  isAuthenticated: false,
  isLoading: false,
  error: null,
  lastActivity: null,
  sessionExpiry: null,
}

// Create Auth Store with Zustand
export const useAuthStore = create<AuthStore>()(
  devtools(
    persist(
      (set, get) => ({
        // State
        ...initialState,

        // Actions
        login: async (credentials: LoginRequest): Promise<AuthResponse> => {
          set({ isLoading: true, error: null })
          
          try {
            const response = await fetch(`${process.env.NEXT_PUBLIC_API_BASE_URL}/auth/login`, {
              method: 'POST',
              headers: {
                'Content-Type': 'application/json',
              },
              body: JSON.stringify(credentials),
            })

            if (!response.ok) {
              const errorData = await response.json()
              throw new Error(errorData.message || 'Login failed')
            }

            const data: AuthResponse = await response.json()
            
            if (data.success) {
              const sessionExpiry = getTokenExpiry(data.accessToken)
              
              set({
                user: data.user,
                accessToken: data.accessToken,
                refreshToken: data.refreshToken,
                isAuthenticated: true,
                isLoading: false,
                error: null,
                lastActivity: Date.now(),
                sessionExpiry: sessionExpiry ? sessionExpiry * 1000 : null,
              })
            } else {
              set({ 
                isLoading: false, 
                error: data.message || 'Login failed' 
              })
            }
            
            return data
          } catch (error) {
            const errorMessage = error instanceof Error ? error.message : 'Login failed'
            set({ 
              isLoading: false, 
              error: errorMessage 
            })
            throw error
          }
        },

        pinLogin: async (credentials: PinLoginRequest): Promise<AuthResponse> => {
          set({ isLoading: true, error: null })
          
          try {
            const response = await fetch(`${process.env.NEXT_PUBLIC_API_BASE_URL}/auth/pin-login`, {
              method: 'POST',
              headers: {
                'Content-Type': 'application/json',
              },
              body: JSON.stringify(credentials),
            })

            if (!response.ok) {
              const errorData = await response.json()
              throw new Error(errorData.message || 'PIN login failed')
            }

            const data: AuthResponse = await response.json()
            
            if (data.success) {
              const sessionExpiry = getTokenExpiry(data.accessToken)
              
              set({
                user: data.user,
                accessToken: data.accessToken,
                refreshToken: data.refreshToken,
                isAuthenticated: true,
                isLoading: false,
                error: null,
                lastActivity: Date.now(),
                sessionExpiry: sessionExpiry ? sessionExpiry * 1000 : null,
              })
            } else {
              set({ 
                isLoading: false, 
                error: data.message || 'PIN login failed' 
              })
            }
            
            return data
          } catch (error) {
            const errorMessage = error instanceof Error ? error.message : 'PIN login failed'
            set({ 
              isLoading: false, 
              error: errorMessage 
            })
            throw error
          }
        },

        logout: async (): Promise<void> => {
          set({ isLoading: true })
          
          try {
            const { refreshToken } = get()
            
            if (refreshToken) {
              // Call backend logout endpoint
              await fetch(`${process.env.NEXT_PUBLIC_API_BASE_URL}/auth/logout`, {
                method: 'POST',
                headers: {
                  'Content-Type': 'application/json',
                  'Authorization': `Bearer ${get().accessToken}`,
                },
                body: JSON.stringify({ refreshToken }),
              })
            }
          } catch (error) {
            console.error('Logout API call failed:', error)
            // Continue with client-side logout even if API call fails
          }
          
          // Clear local state
          set({
            ...initialState,
            isLoading: false,
          })
        },

        refresh: async (): Promise<void> => {
          const { refreshToken, accessToken } = get()
          
          if (!refreshToken) {
            throw new Error('No refresh token available')
          }

          if (accessToken && !isTokenExpired(accessToken)) {
            // Token is still valid, no need to refresh
            return
          }

          set({ isLoading: true })

          try {
            const response = await fetch(`${process.env.NEXT_PUBLIC_API_BASE_URL}/auth/refresh`, {
              method: 'POST',
              headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${refreshToken}`,
              },
            })

            if (!response.ok) {
              throw new Error('Token refresh failed')
            }

            const data = await response.json()
            
            if (data.success) {
              const sessionExpiry = getTokenExpiry(data.accessToken)
              
              set({
                accessToken: data.accessToken,
                refreshToken: data.refreshToken,
                isLoading: false,
                lastActivity: Date.now(),
                sessionExpiry: sessionExpiry ? sessionExpiry * 1000 : null,
              })
            } else {
              // Refresh failed, logout user
              await get().logout()
              throw new Error(data.message || 'Token refresh failed')
            }
          } catch (error) {
            // Refresh failed, logout user
            await get().logout()
            throw error
          }
        },

        forgotPassword: async (email: string): Promise<ForgotPasswordResponse> => {
          set({ isLoading: true, error: null })
          
          try {
            const response = await fetch(`${process.env.NEXT_PUBLIC_API_BASE_URL}/auth/forgot-password`, {
              method: 'POST',
              headers: {
                'Content-Type': 'application/json',
              },
              body: JSON.stringify({ email }),
            })

            const data = await response.json()
            
            set({ isLoading: false })
            
            if (!response.ok) {
              set({ error: data.message || 'Password reset failed' })
              throw new Error(data.message || 'Password reset failed')
            }
            
            return data
          } catch (error) {
            const errorMessage = error instanceof Error ? error.message : 'Password reset failed'
            set({ 
              isLoading: false, 
              error: errorMessage 
            })
            throw error
          }
        },

        resetPassword: async (data: ResetPasswordRequest): Promise<ResetPasswordResponse> => {
          set({ isLoading: true, error: null })
          
          try {
            const response = await fetch(`${process.env.NEXT_PUBLIC_API_BASE_URL}/auth/reset-password`, {
              method: 'POST',
              headers: {
                'Content-Type': 'application/json',
              },
              body: JSON.stringify(data),
            })

            const result = await response.json()
            
            set({ isLoading: false })
            
            if (!response.ok) {
              set({ error: result.message || 'Password reset failed' })
              throw new Error(result.message || 'Password reset failed')
            }
            
            return result
          } catch (error) {
            const errorMessage = error instanceof Error ? error.message : 'Password reset failed'
            set({ 
              isLoading: false, 
              error: errorMessage 
            })
            throw error
          }
        },

        register: async (data: RegisterRequest): Promise<RegisterResponse> => {
          set({ isLoading: true, error: null })
          
          try {
            const response = await fetch(`${process.env.NEXT_PUBLIC_API_BASE_URL}/auth/register`, {
              method: 'POST',
              headers: {
                'Content-Type': 'application/json',
              },
              body: JSON.stringify(data),
            })

            const result = await response.json()
            
            set({ isLoading: false })
            
            if (!response.ok) {
              set({ error: result.message || 'Registration failed' })
              throw new Error(result.message || 'Registration failed')
            }
            
            return result
          } catch (error) {
            const errorMessage = error instanceof Error ? error.message : 'Registration failed'
            set({ 
              isLoading: false, 
              error: errorMessage 
            })
            throw error
          }
        },

        clearError: (): void => {
          set({ error: null })
        },

        setLoading: (loading: boolean): void => {
          set({ isLoading: loading })
        },

        updateLastActivity: (): void => {
          set({ lastActivity: Date.now() })
        },

        checkTokenExpiry: (): boolean => {
          const { accessToken } = get()
          return accessToken ? isTokenExpired(accessToken) : true
        },

        getSessions: async (): Promise<SessionResponse> => {
          const { accessToken } = get()
          
          if (!accessToken) {
            throw new Error('No access token available')
          }

          try {
            const response = await fetch(`${process.env.NEXT_PUBLIC_API_BASE_URL}/auth/sessions`, {
              headers: {
                'Authorization': `Bearer ${accessToken}`,
              },
            })

            if (!response.ok) {
              throw new Error('Failed to fetch sessions')
            }

            return await response.json()
          } catch (error) {
            throw error
          }
        },

        terminateSession: async (sessionId: string): Promise<void> => {
          const { accessToken } = get()
          
          if (!accessToken) {
            throw new Error('No access token available')
          }

          try {
            const response = await fetch(`${process.env.NEXT_PUBLIC_API_BASE_URL}/auth/sessions/${sessionId}`, {
              method: 'DELETE',
              headers: {
                'Authorization': `Bearer ${accessToken}`,
              },
            })

            if (!response.ok) {
              throw new Error('Failed to terminate session')
            }
          } catch (error) {
            throw error
          }
        },

        terminateAllSessions: async (): Promise<void> => {
          const { accessToken } = get()
          
          if (!accessToken) {
            throw new Error('No access token available')
          }

          try {
            const response = await fetch(`${process.env.NEXT_PUBLIC_API_BASE_URL}/auth/logout-all`, {
              method: 'POST',
              headers: {
                'Authorization': `Bearer ${accessToken}`,
              },
            })

            if (!response.ok) {
              throw new Error('Failed to terminate all sessions')
            }

            // After terminating all sessions, logout locally
            await get().logout()
          } catch (error) {
            throw error
          }
        },
      }),
      {
        name: 'auth-store',
        storage: createJSONStorage(() => localStorage),
        partialize: (state) => ({
          user: state.user,
          accessToken: state.accessToken,
          refreshToken: state.refreshToken,
          isAuthenticated: state.isAuthenticated,
          lastActivity: state.lastActivity,
          sessionExpiry: state.sessionExpiry,
        }),
        onRehydrateStorage: () => (state) => {
          // Check if tokens are expired on rehydration
          if (state?.accessToken && isTokenExpired(state.accessToken)) {
            // Try to refresh token automatically
            state.refresh().catch(() => {
              // If refresh fails, logout
              state.logout()
            })
          }
        },
      }
    ),
    {
      name: 'auth-store',
      enabled: process.env.NODE_ENV === 'development',
    }
  )
)

// Utility hooks for specific auth state
export const useUser = () => useAuthStore((state) => state.user)
export const useIsAuthenticated = () => useAuthStore((state) => state.isAuthenticated)
export const useAuthLoading = () => useAuthStore((state) => state.isLoading)
export const useAuthError = () => useAuthStore((state) => state.error)
export const useTokens = () => useAuthStore((state) => ({
  accessToken: state.accessToken,
  refreshToken: state.refreshToken,
}))

// Auth actions hooks
export const useAuthActions = () => useAuthStore((state) => ({
  login: state.login,
  pinLogin: state.pinLogin,
  logout: state.logout,
  refresh: state.refresh,
  forgotPassword: state.forgotPassword,
  resetPassword: state.resetPassword,
  register: state.register,
  clearError: state.clearError,
  setLoading: state.setLoading,
  updateLastActivity: state.updateLastActivity,
  checkTokenExpiry: state.checkTokenExpiry,
  getSessions: state.getSessions,
  terminateSession: state.terminateSession,
  terminateAllSessions: state.terminateAllSessions,
}))

// Token refresh middleware for automatic token refresh
export const setupTokenRefresh = () => {
  const store = useAuthStore.getState()
  
  // Check token expiry every minute
  setInterval(() => {
    const { accessToken, refreshToken, isAuthenticated } = store
    
    if (isAuthenticated && accessToken && refreshToken) {
      const decoded = parseJWT(accessToken)
      if (decoded) {
        const currentTime = Math.floor(Date.now() / 1000)
        const timeUntilExpiry = decoded.exp - currentTime
        
        // Refresh token if it expires in less than 5 minutes
        if (timeUntilExpiry < 300) {
          store.refresh().catch(() => {
            console.error('Automatic token refresh failed')
          })
        }
      }
    }
  }, 60000) // Check every minute
}