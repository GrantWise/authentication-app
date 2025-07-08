// Authentication utility functions
export interface User {
  username: string
  role: string
  token: string
}

export const getStoredUser = (): User | null => {
  if (typeof window === "undefined") return null

  const token = localStorage.getItem("jwt_token")
  const username = localStorage.getItem("username")
  const role = localStorage.getItem("user_role")

  if (!token || !username || !role) {
    return null
  }

  return { username, role, token }
}

export const clearStoredUser = (): void => {
  if (typeof window === "undefined") return

  localStorage.removeItem("jwt_token")
  localStorage.removeItem("username")
  localStorage.removeItem("user_role")
}

export const isAuthenticated = (): boolean => {
  return getStoredUser() !== null
}

export const getAuthHeaders = (): Record<string, string> => {
  const user = getStoredUser()
  if (!user) return {}

  return {
    Authorization: `Bearer ${user.token}`,
    "Content-Type": "application/json",
  }
}

// API base URL
export const API_BASE_URL = "http://localhost:5097"

// API endpoints
export const API_ENDPOINTS = {
  LOGIN: `${API_BASE_URL}/api/auth/login`,
  REFRESH: `${API_BASE_URL}/api/auth/refresh`,
  FORGOT_PASSWORD: `${API_BASE_URL}/api/auth/forgot-password`,
  LOGOUT: `${API_BASE_URL}/api/auth/logout`,
} as const
