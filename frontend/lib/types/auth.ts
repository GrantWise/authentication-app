/**
 * Authentication Type Definitions
 * Matches backend API structure for JWT authentication
 */

// User Types
export interface User {
  id: string
  username: string
  email: string
  roles: string[]
  isLocked?: boolean
  lastLoginAt?: string
  createdAt?: string
}

// Authentication Request Types
export interface LoginRequest {
  username: string
  password: string
  deviceInfo?: string
  rememberMe?: boolean
}

export interface PinLoginRequest {
  deviceId: string
  pin: string
  deviceInfo?: string
}

export interface RefreshTokenRequest {
  refreshToken: string
}

export interface LogoutRequest {
  refreshToken?: string
}

export interface ForgotPasswordRequest {
  email: string
}

export interface ResetPasswordRequest {
  token: string
  newPassword: string
  confirmPassword: string
}

export interface RegisterRequest {
  username: string
  email: string
  password: string
  confirmPassword: string
}

// Authentication Response Types
export interface AuthResponse {
  success: boolean
  accessToken: string
  refreshToken: string
  accessTokenExpiry: string
  refreshTokenExpiry: string
  user: User
  requiresMfa: boolean
  mfaChallenge?: string
  message?: string
}

export interface RefreshTokenResponse {
  success: boolean
  accessToken: string
  refreshToken: string
  accessTokenExpiry: string
  refreshTokenExpiry: string
  message?: string
}

export interface LogoutResponse {
  success: boolean
  message: string
}

export interface ForgotPasswordResponse {
  success: boolean
  message: string
}

export interface ResetPasswordResponse {
  success: boolean
  message: string
}

export interface RegisterResponse {
  success: boolean
  user: User
  message?: string
}

// Error Response Types
export interface ApiError {
  success: false
  error: string
  message: string
  statusCode: number
  timestamp: string
  details?: Record<string, string[]>
}

export interface ValidationError {
  field: string
  message: string
}

// Authentication State Types
export interface AuthState {
  user: User | null
  accessToken: string | null
  refreshToken: string | null
  isAuthenticated: boolean
  isLoading: boolean
  error: string | null
  lastActivity: number | null
  sessionExpiry: number | null
}

// Session Types
export interface Session {
  sessionId: string
  userId: string
  deviceInfo: string
  ipAddress: string
  createdAt: string
  lastActivity: string
  expiresAt: string
  isCurrent: boolean
}

export interface SessionResponse {
  success: boolean
  sessions: Session[]
  message?: string
}

// Device Types
export interface Device {
  deviceId: string
  deviceName: string
  deviceType: 'mobile' | 'desktop' | 'tablet'
  lastUsed: string
  isActive: boolean
}

// API Configuration Types
export interface ApiConfig {
  baseUrl: string
  timeout: number
  retryAttempts: number
  retryDelay: number
}

// HTTP Status Codes
export enum HttpStatusCode {
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
  SERVICE_UNAVAILABLE = 503
}

// Authentication Event Types
export enum AuthEventType {
  LOGIN_SUCCESS = 'LOGIN_SUCCESS',
  LOGIN_FAILED = 'LOGIN_FAILED',
  LOGOUT = 'LOGOUT',
  TOKEN_REFRESH = 'TOKEN_REFRESH',
  SESSION_EXPIRED = 'SESSION_EXPIRED',
  ACCOUNT_LOCKED = 'ACCOUNT_LOCKED',
  PASSWORD_RESET = 'PASSWORD_RESET'
}

// Form Types
export interface LoginFormData {
  username: string
  password: string
  rememberMe: boolean
}

export interface PinFormData {
  pin: string
}

export interface ForgotPasswordFormData {
  email: string
}

export interface ResetPasswordFormData {
  password: string
  confirmPassword: string
}

export interface RegisterFormData {
  username: string
  email: string
  password: string
  confirmPassword: string
}

// Token Types
export interface DecodedToken {
  sub: string
  username: string
  email: string
  roles: string[]
  iat: number
  exp: number
  jti: string
  iss: string
  aud: string
}

export interface TokenPair {
  accessToken: string
  refreshToken: string
  accessTokenExpiry: string
  refreshTokenExpiry: string
}

// Authentication Store Actions
export interface AuthActions {
  login: (credentials: LoginRequest) => Promise<AuthResponse>
  pinLogin: (credentials: PinLoginRequest) => Promise<AuthResponse>
  logout: () => Promise<void>
  refresh: () => Promise<void>
  forgotPassword: (email: string) => Promise<ForgotPasswordResponse>
  resetPassword: (data: ResetPasswordRequest) => Promise<ResetPasswordResponse>
  register: (data: RegisterRequest) => Promise<RegisterResponse>
  clearError: () => void
  setLoading: (loading: boolean) => void
  updateLastActivity: () => void
  checkTokenExpiry: () => boolean
  getSessions: () => Promise<SessionResponse>
  terminateSession: (sessionId: string) => Promise<void>
  terminateAllSessions: () => Promise<void>
}

// Combined Auth Store Type
export interface AuthStore extends AuthState, AuthActions {}

// API Client Types
export interface ApiClient {
  login: (credentials: LoginRequest) => Promise<AuthResponse>
  pinLogin: (credentials: PinLoginRequest) => Promise<AuthResponse>
  refresh: (refreshToken: string) => Promise<RefreshTokenResponse>
  logout: (refreshToken?: string) => Promise<LogoutResponse>
  logoutAll: () => Promise<LogoutResponse>
  forgotPassword: (email: string) => Promise<ForgotPasswordResponse>
  resetPassword: (data: ResetPasswordRequest) => Promise<ResetPasswordResponse>
  register: (data: RegisterRequest) => Promise<RegisterResponse>
  verify: () => Promise<{ success: boolean; user: User }>
  getSessions: () => Promise<SessionResponse>
  terminateSession: (sessionId: string) => Promise<LogoutResponse>
}

// Environment Configuration
export interface EnvConfig {
  apiUrl: string
  apiBaseUrl: string
  appEnv: string
  appVersion: string
  secureCookies: boolean
  cookieDomain: string
  sessionWarningMinutes: number
  accessTokenExpiresMinutes: number
  refreshTokenExpiresMinutes: number
  enableDevTools: boolean
  logLevel: string
}

// Query Keys for TanStack Query
export const AUTH_QUERY_KEYS = {
  auth: ['auth'] as const,
  user: () => [...AUTH_QUERY_KEYS.auth, 'user'] as const,
  sessions: () => [...AUTH_QUERY_KEYS.auth, 'sessions'] as const,
  verify: () => [...AUTH_QUERY_KEYS.auth, 'verify'] as const,
} as const

// Mutation Keys for TanStack Query
export const AUTH_MUTATION_KEYS = {
  login: ['auth', 'login'] as const,
  pinLogin: ['auth', 'pin-login'] as const,
  logout: ['auth', 'logout'] as const,
  refresh: ['auth', 'refresh'] as const,
  forgotPassword: ['auth', 'forgot-password'] as const,
  resetPassword: ['auth', 'reset-password'] as const,
  register: ['auth', 'register'] as const,
} as const