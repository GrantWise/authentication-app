/**
 * HTTP Client with Request/Response Interceptors
 * Handles automatic token attachment, refresh, and error handling
 */

import { useAuthStore } from '@/lib/stores/auth-store'
import { getEnvConfig } from '@/utils/auth'
import type { ApiResponse, ErrorResponse } from '@/lib/types/api'

const config = getEnvConfig()

// HTTP Status Codes
export enum HttpStatus {
  OK = 200,
  CREATED = 201,
  BAD_REQUEST = 400,
  UNAUTHORIZED = 401,
  FORBIDDEN = 403,
  NOT_FOUND = 404,
  CONFLICT = 409,
  LOCKED = 423,
  TOO_MANY_REQUESTS = 429,
  INTERNAL_SERVER_ERROR = 500,
  SERVICE_UNAVAILABLE = 503,
}

// Custom Error Class
export class ApiError extends Error {
  constructor(
    public statusCode: number,
    public message: string,
    public details?: Record<string, string[]>
  ) {
    super(message)
    this.name = 'ApiError'
  }
}

// Request Configuration
interface RequestConfig extends Omit<RequestInit, 'body'> {
  url: string
  data?: any
  params?: Record<string, string | number | boolean>
  skipAuth?: boolean
  skipRefresh?: boolean
}

// Response Type Guard
const isErrorResponse = (response: any): response is ErrorResponse => {
  return response && typeof response.success === 'boolean' && !response.success
}

// HTTP Client Class
class HttpClient {
  private baseURL: string

  constructor(baseURL: string) {
    this.baseURL = baseURL
  }

  // Build URL with query parameters
  private buildURL(url: string, params?: Record<string, string | number | boolean>): string {
    const fullURL = url.startsWith('http') ? url : `${this.baseURL}${url}`
    
    if (!params) return fullURL
    
    const urlObj = new URL(fullURL)
    Object.entries(params).forEach(([key, value]) => {
      urlObj.searchParams.set(key, String(value))
    })
    
    return urlObj.toString()
  }

  // Request Interceptor
  private async interceptRequest(config: RequestConfig): Promise<RequestInit> {
    const headers: Record<string, string> = {
      'Content-Type': 'application/json',
      ...config.headers,
    }

    // Add Authorization header if not skipped
    if (!config.skipAuth) {
      const { accessToken } = useAuthStore.getState()
      if (accessToken) {
        headers.Authorization = `Bearer ${accessToken}`
      }
    }

    // Add correlation ID for tracing
    headers['X-Correlation-Id'] = crypto.randomUUID()

    return {
      ...config,
      headers,
      body: config.data ? JSON.stringify(config.data) : config.body,
    }
  }

  // Response Interceptor
  private async interceptResponse<T>(response: Response, originalConfig: RequestConfig): Promise<T> {
    const contentType = response.headers.get('content-type')
    const isJSON = contentType?.includes('application/json')
    
    let data: any
    try {
      data = isJSON ? await response.json() : await response.text()
    } catch (error) {
      throw new ApiError(
        response.status,
        'Failed to parse response',
        { parseError: ['Response body could not be parsed'] }
      )
    }

    // Handle successful responses
    if (response.ok) {
      return data
    }

    // Handle 401 Unauthorized - attempt token refresh
    if (response.status === HttpStatus.UNAUTHORIZED && !originalConfig.skipRefresh) {
      const { refreshToken, refresh } = useAuthStore.getState()
      
      if (refreshToken) {
        try {
          await refresh()
          // Retry original request with new token
          return this.request({ ...originalConfig, skipRefresh: true })
        } catch (refreshError) {
          // Refresh failed, redirect to login
          useAuthStore.getState().logout()
          throw new ApiError(HttpStatus.UNAUTHORIZED, 'Session expired. Please log in again.')
        }
      }
    }

    // Handle other error responses
    const errorMessage = isErrorResponse(data) ? data.message : 'An error occurred'
    const errorDetails = isErrorResponse(data) ? data.details : undefined
    
    throw new ApiError(response.status, errorMessage, errorDetails)
  }

  // Main request method
  async request<T = any>(config: RequestConfig): Promise<T> {
    const url = this.buildURL(config.url, config.params)
    const requestConfig = await this.interceptRequest(config)

    try {
      const response = await fetch(url, requestConfig)
      return await this.interceptResponse<T>(response, config)
    } catch (error) {
      // Handle network errors
      if (error instanceof ApiError) {
        throw error
      }
      
      if (error instanceof TypeError && error.message.includes('fetch')) {
        throw new ApiError(0, 'Network error. Please check your internet connection.')
      }
      
      throw new ApiError(0, 'An unexpected error occurred.')
    }
  }

  // Convenience methods
  async get<T = any>(url: string, config?: Omit<RequestConfig, 'url' | 'method'>): Promise<T> {
    return this.request<T>({ ...config, url, method: 'GET' })
  }

  async post<T = any>(url: string, data?: any, config?: Omit<RequestConfig, 'url' | 'method' | 'data'>): Promise<T> {
    return this.request<T>({ ...config, url, method: 'POST', data })
  }

  async put<T = any>(url: string, data?: any, config?: Omit<RequestConfig, 'url' | 'method' | 'data'>): Promise<T> {
    return this.request<T>({ ...config, url, method: 'PUT', data })
  }

  async patch<T = any>(url: string, data?: any, config?: Omit<RequestConfig, 'url' | 'method' | 'data'>): Promise<T> {
    return this.request<T>({ ...config, url, method: 'PATCH', data })
  }

  async delete<T = any>(url: string, config?: Omit<RequestConfig, 'url' | 'method'>): Promise<T> {
    return this.request<T>({ ...config, url, method: 'DELETE' })
  }
}

// Create HTTP client instance
export const httpClient = new HttpClient(config.apiBaseUrl)

// Error handling utilities
export const handleApiError = (error: unknown): string => {
  if (error instanceof ApiError) {
    return error.message
  }
  
  if (error instanceof Error) {
    return error.message
  }
  
  return 'An unexpected error occurred'
}

export const isNetworkError = (error: unknown): boolean => {
  return error instanceof ApiError && error.statusCode === 0
}

export const isAuthError = (error: unknown): boolean => {
  return error instanceof ApiError && 
    (error.statusCode === HttpStatus.UNAUTHORIZED || error.statusCode === HttpStatus.FORBIDDEN)
}

export const isValidationError = (error: unknown): boolean => {
  return error instanceof ApiError && error.statusCode === HttpStatus.BAD_REQUEST
}

export const isRateLimitError = (error: unknown): boolean => {
  return error instanceof ApiError && error.statusCode === HttpStatus.TOO_MANY_REQUESTS
}

export const isAccountLockedError = (error: unknown): boolean => {
  return error instanceof ApiError && error.statusCode === HttpStatus.LOCKED
}

// Request timeout utility
export const withTimeout = <T>(promise: Promise<T>, timeoutMs: number): Promise<T> => {
  return Promise.race([
    promise,
    new Promise<never>((_, reject) =>
      setTimeout(() => reject(new ApiError(0, 'Request timeout')), timeoutMs)
    ),
  ])
}

// Retry utility
export const withRetry = async <T>(
  fn: () => Promise<T>,
  maxRetries: number = 3,
  delayMs: number = 1000
): Promise<T> => {
  let lastError: unknown
  
  for (let i = 0; i <= maxRetries; i++) {
    try {
      return await fn()
    } catch (error) {
      lastError = error
      
      // Don't retry on client errors (4xx except 408, 429)
      if (error instanceof ApiError && 
          error.statusCode >= 400 && 
          error.statusCode < 500 && 
          error.statusCode !== 408 && 
          error.statusCode !== 429) {
        throw error
      }
      
      // Don't retry on last attempt
      if (i === maxRetries) {
        throw error
      }
      
      // Exponential backoff
      await new Promise(resolve => setTimeout(resolve, delayMs * Math.pow(2, i)))
    }
  }
  
  throw lastError
}

// Response cache utility (simple in-memory cache)
class ResponseCache {
  private cache = new Map<string, { data: any; timestamp: number; ttl: number }>()

  set(key: string, data: any, ttlMs: number = 5 * 60 * 1000): void {
    this.cache.set(key, {
      data,
      timestamp: Date.now(),
      ttl: ttlMs,
    })
  }

  get(key: string): any | null {
    const entry = this.cache.get(key)
    if (!entry) return null

    if (Date.now() - entry.timestamp > entry.ttl) {
      this.cache.delete(key)
      return null
    }

    return entry.data
  }

  clear(): void {
    this.cache.clear()
  }

  delete(key: string): boolean {
    return this.cache.delete(key)
  }
}

export const responseCache = new ResponseCache()