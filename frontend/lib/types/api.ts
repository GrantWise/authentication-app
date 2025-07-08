/**
 * API Response Types and Utilities
 * Generic types for API communication
 */

// Generic API Response Wrapper
export interface ApiResponse<T = any> {
  success: boolean
  data?: T
  error?: string
  message?: string
  statusCode?: number
  timestamp?: string
  correlationId?: string
}

// Success Response
export interface SuccessResponse<T = any> extends ApiResponse<T> {
  success: true
  data: T
  message?: string
}

// Error Response
export interface ErrorResponse extends ApiResponse {
  success: false
  error: string
  message: string
  statusCode: number
  timestamp: string
  details?: Record<string, string[]>
}

// Pagination Types
export interface PaginationParams {
  page: number
  limit: number
  sortBy?: string
  sortOrder?: 'asc' | 'desc'
}

export interface PaginatedResponse<T> {
  data: T[]
  pagination: {
    page: number
    limit: number
    total: number
    totalPages: number
    hasNextPage: boolean
    hasPreviousPage: boolean
  }
}

// HTTP Methods
export type HttpMethod = 'GET' | 'POST' | 'PUT' | 'DELETE' | 'PATCH'

// Request Configuration
export interface RequestConfig {
  method: HttpMethod
  url: string
  data?: any
  params?: Record<string, any>
  headers?: Record<string, string>
  timeout?: number
  retries?: number
  retryDelay?: number
}

// Response Interceptor Types
export interface ResponseInterceptor {
  onResponse?: (response: any) => any
  onError?: (error: any) => any
}

// Request Interceptor Types
export interface RequestInterceptor {
  onRequest?: (config: RequestConfig) => RequestConfig
  onError?: (error: any) => any
}

// API Client Configuration
export interface ApiClientConfig {
  baseURL: string
  timeout: number
  retries: number
  retryDelay: number
  headers?: Record<string, string>
  requestInterceptors?: RequestInterceptor[]
  responseInterceptors?: ResponseInterceptor[]
}

// Rate Limiting Types
export interface RateLimitInfo {
  limit: number
  remaining: number
  reset: number
  retryAfter?: number
}

// Network Error Types
export enum NetworkErrorType {
  TIMEOUT = 'TIMEOUT',
  NETWORK_ERROR = 'NETWORK_ERROR',
  CORS_ERROR = 'CORS_ERROR',
  SERVER_ERROR = 'SERVER_ERROR',
  CLIENT_ERROR = 'CLIENT_ERROR',
  UNKNOWN_ERROR = 'UNKNOWN_ERROR'
}

export interface NetworkError {
  type: NetworkErrorType
  message: string
  statusCode?: number
  originalError?: any
}

// Query Options for TanStack Query
export interface QueryOptions {
  enabled?: boolean
  staleTime?: number
  cacheTime?: number
  refetchOnWindowFocus?: boolean
  refetchOnReconnect?: boolean
  retry?: boolean | number
  retryDelay?: number
}

// Mutation Options for TanStack Query
export interface MutationOptions {
  onSuccess?: (data: any) => void
  onError?: (error: any) => void
  onSettled?: (data: any, error: any) => void
  retry?: boolean | number
  retryDelay?: number
}

// Generic API Hook Return Type
export interface ApiHookReturn<T> {
  data: T | undefined
  error: any
  isLoading: boolean
  isError: boolean
  isSuccess: boolean
  refetch: () => void
}

// Generic Mutation Hook Return Type
export interface MutationHookReturn<T, V> {
  mutate: (variables: V) => void
  mutateAsync: (variables: V) => Promise<T>
  data: T | undefined
  error: any
  isLoading: boolean
  isError: boolean
  isSuccess: boolean
  reset: () => void
}

// Optimistic Update Types
export interface OptimisticUpdate<T> {
  type: 'add' | 'update' | 'remove'
  data: T
  rollback: () => void
}

// Cache Management Types
export interface CacheConfig {
  staleTime: number
  cacheTime: number
  maxAge: number
  invalidateOnError: boolean
}

// Offline Support Types
export interface OfflineConfig {
  enabled: boolean
  storageKey: string
  maxRetries: number
  retryDelay: number
}

// Performance Monitoring Types
export interface PerformanceMetrics {
  requestStart: number
  requestEnd: number
  responseTime: number
  endpoint: string
  method: HttpMethod
  statusCode: number
  cacheHit: boolean
}