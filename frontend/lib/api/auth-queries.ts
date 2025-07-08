/**
 * TanStack Query hooks for authentication API calls
 */

import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { 
  LoginRequest, 
  PinLoginRequest, 
  AuthResponse, 
  RefreshTokenResponse,
  LogoutResponse,
  ForgotPasswordRequest,
  ForgotPasswordResponse,
  ResetPasswordRequest,
  ResetPasswordResponse,
  RegisterRequest,
  RegisterResponse,
  SessionResponse,
  AUTH_QUERY_KEYS,
  AUTH_MUTATION_KEYS
} from '@/lib/types/auth'
import { getEnvConfig } from '@/utils/auth'

const config = getEnvConfig()

// API Client Functions
const authApi = {
  login: async (credentials: LoginRequest): Promise<AuthResponse> => {
    const response = await fetch(`${config.apiBaseUrl}/auth/login`, {
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

    return response.json()
  },

  pinLogin: async (credentials: PinLoginRequest): Promise<AuthResponse> => {
    const response = await fetch(`${config.apiBaseUrl}/auth/pin-login`, {
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

    return response.json()
  },

  refresh: async (refreshToken: string): Promise<RefreshTokenResponse> => {
    const response = await fetch(`${config.apiBaseUrl}/auth/refresh`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'Authorization': `Bearer ${refreshToken}`,
      },
    })

    if (!response.ok) {
      const errorData = await response.json()
      throw new Error(errorData.message || 'Token refresh failed')
    }

    return response.json()
  },

  logout: async (refreshToken?: string, accessToken?: string): Promise<LogoutResponse> => {
    const headers: Record<string, string> = {
      'Content-Type': 'application/json',
    }

    if (accessToken) {
      headers.Authorization = `Bearer ${accessToken}`
    }

    const response = await fetch(`${config.apiBaseUrl}/auth/logout`, {
      method: 'POST',
      headers,
      body: JSON.stringify({ refreshToken }),
    })

    if (!response.ok) {
      const errorData = await response.json()
      throw new Error(errorData.message || 'Logout failed')
    }

    return response.json()
  },

  logoutAll: async (accessToken: string): Promise<LogoutResponse> => {
    const response = await fetch(`${config.apiBaseUrl}/auth/logout-all`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'Authorization': `Bearer ${accessToken}`,
      },
    })

    if (!response.ok) {
      const errorData = await response.json()
      throw new Error(errorData.message || 'Logout all failed')
    }

    return response.json()
  },

  forgotPassword: async (request: ForgotPasswordRequest): Promise<ForgotPasswordResponse> => {
    const response = await fetch(`${config.apiBaseUrl}/auth/forgot-password`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(request),
    })

    if (!response.ok) {
      const errorData = await response.json()
      throw new Error(errorData.message || 'Password reset failed')
    }

    return response.json()
  },

  resetPassword: async (request: ResetPasswordRequest): Promise<ResetPasswordResponse> => {
    const response = await fetch(`${config.apiBaseUrl}/auth/reset-password`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(request),
    })

    if (!response.ok) {
      const errorData = await response.json()
      throw new Error(errorData.message || 'Password reset failed')
    }

    return response.json()
  },

  register: async (request: RegisterRequest): Promise<RegisterResponse> => {
    const response = await fetch(`${config.apiBaseUrl}/auth/register`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(request),
    })

    if (!response.ok) {
      const errorData = await response.json()
      throw new Error(errorData.message || 'Registration failed')
    }

    return response.json()
  },

  verify: async (accessToken: string): Promise<{ success: boolean; user: any }> => {
    const response = await fetch(`${config.apiBaseUrl}/auth/verify`, {
      headers: {
        'Authorization': `Bearer ${accessToken}`,
      },
    })

    if (!response.ok) {
      const errorData = await response.json()
      throw new Error(errorData.message || 'Token verification failed')
    }

    return response.json()
  },

  getSessions: async (accessToken: string): Promise<SessionResponse> => {
    const response = await fetch(`${config.apiBaseUrl}/auth/sessions`, {
      headers: {
        'Authorization': `Bearer ${accessToken}`,
      },
    })

    if (!response.ok) {
      const errorData = await response.json()
      throw new Error(errorData.message || 'Failed to fetch sessions')
    }

    return response.json()
  },

  terminateSession: async (sessionId: string, accessToken: string): Promise<LogoutResponse> => {
    const response = await fetch(`${config.apiBaseUrl}/auth/sessions/${sessionId}`, {
      method: 'DELETE',
      headers: {
        'Authorization': `Bearer ${accessToken}`,
      },
    })

    if (!response.ok) {
      const errorData = await response.json()
      throw new Error(errorData.message || 'Failed to terminate session')
    }

    return response.json()
  },
}

// Query Hooks
export const useVerifyToken = (accessToken?: string) => {
  return useQuery({
    queryKey: AUTH_QUERY_KEYS.verify(),
    queryFn: () => authApi.verify(accessToken!),
    enabled: !!accessToken,
    staleTime: 1000 * 60 * 5, // 5 minutes
    retry: false,
  })
}

export const useSessions = (accessToken?: string) => {
  return useQuery({
    queryKey: AUTH_QUERY_KEYS.sessions(),
    queryFn: () => authApi.getSessions(accessToken!),
    enabled: !!accessToken,
    staleTime: 1000 * 60 * 2, // 2 minutes
  })
}

// Mutation Hooks
export const useLogin = (options?: {
  onSuccess?: (data: AuthResponse, variables: LoginRequest) => void
  onError?: (error: Error) => void
}) => {
  const queryClient = useQueryClient()

  return useMutation({
    mutationKey: AUTH_MUTATION_KEYS.login,
    mutationFn: authApi.login,
    onSuccess: (data) => {
      // Invalidate and refetch auth-related queries
      queryClient.invalidateQueries({ queryKey: AUTH_QUERY_KEYS.auth })
      options?.onSuccess?.(data, {} as LoginRequest)
    },
    onError: options?.onError,
  })
}

export const usePinLogin = (options?: {
  onSuccess?: (data: AuthResponse, variables: PinLoginRequest) => void
  onError?: (error: Error) => void
}) => {
  const queryClient = useQueryClient()

  return useMutation({
    mutationKey: AUTH_MUTATION_KEYS.pinLogin,
    mutationFn: authApi.pinLogin,
    onSuccess: (data) => {
      // Invalidate and refetch auth-related queries
      queryClient.invalidateQueries({ queryKey: AUTH_QUERY_KEYS.auth })
      options?.onSuccess?.(data, {} as PinLoginRequest)
    },
    onError: options?.onError,
  })
}

export const useRefreshToken = () => {
  const queryClient = useQueryClient()

  return useMutation({
    mutationKey: AUTH_MUTATION_KEYS.refresh,
    mutationFn: authApi.refresh,
    onSuccess: (data) => {
      // Update auth queries without refetching
      queryClient.setQueryData(AUTH_QUERY_KEYS.verify(), data)
    },
  })
}

export const useLogout = () => {
  const queryClient = useQueryClient()

  return useMutation({
    mutationKey: AUTH_MUTATION_KEYS.logout,
    mutationFn: ({ refreshToken, accessToken }: { refreshToken?: string; accessToken?: string }) =>
      authApi.logout(refreshToken, accessToken),
    onSuccess: () => {
      // Clear all auth-related queries
      queryClient.removeQueries({ queryKey: AUTH_QUERY_KEYS.auth })
      queryClient.clear()
    },
  })
}

export const useLogoutAll = (options?: {
  onSuccess?: () => void
  onError?: (error: Error) => void
}) => {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (accessToken: string) => authApi.logoutAll(accessToken),
    onSuccess: () => {
      // Clear all auth-related queries
      queryClient.removeQueries({ queryKey: AUTH_QUERY_KEYS.auth })
      queryClient.clear()
      options?.onSuccess?.()
    },
    onError: options?.onError,
  })
}

export const useForgotPassword = (options?: {
  onSuccess?: (data: ForgotPasswordResponse, variables: ForgotPasswordRequest) => void
  onError?: (error: Error) => void
}) => {
  return useMutation({
    mutationKey: AUTH_MUTATION_KEYS.forgotPassword,
    mutationFn: authApi.forgotPassword,
    ...options,
  })
}

export const useResetPassword = () => {
  return useMutation({
    mutationKey: AUTH_MUTATION_KEYS.resetPassword,
    mutationFn: authApi.resetPassword,
  })
}

export const useRegister = () => {
  return useMutation({
    mutationKey: AUTH_MUTATION_KEYS.register,
    mutationFn: authApi.register,
  })
}

export const useTerminateSession = (options?: {
  onSuccess?: () => void
  onError?: (error: Error) => void
}) => {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: ({ sessionId, accessToken }: { sessionId: string; accessToken: string }) =>
      authApi.terminateSession(sessionId, accessToken),
    onSuccess: () => {
      // Refetch sessions list
      queryClient.invalidateQueries({ queryKey: AUTH_QUERY_KEYS.sessions() })
      options?.onSuccess?.()
    },
    onError: options?.onError,
  })
}

// Convenience hooks that combine mutation with Zustand store
export const useAuthMutations = () => {
  const loginMutation = useLogin()
  const pinLoginMutation = usePinLogin()
  const logoutMutation = useLogout()
  const refreshMutation = useRefreshToken()
  const forgotPasswordMutation = useForgotPassword()
  const resetPasswordMutation = useResetPassword()
  const registerMutation = useRegister()

  return {
    login: loginMutation,
    pinLogin: pinLoginMutation,
    logout: logoutMutation,
    refresh: refreshMutation,
    forgotPassword: forgotPasswordMutation,
    resetPassword: resetPasswordMutation,
    register: registerMutation,
  }
}