/**
 * Toast Notification Utilities
 * Provides consistent toast notifications across the application
 */

import toast from 'react-hot-toast'
import { getRecoverySuggestion } from '@/components/ui/error-message'

interface ToastOptions {
  duration?: number
  position?: 'top-left' | 'top-center' | 'top-right' | 'bottom-left' | 'bottom-center' | 'bottom-right'
}

/**
 * Success toast notification
 */
export const showSuccessToast = (message: string, options?: ToastOptions) => {
  return toast.success(message, {
    duration: options?.duration || 4000,
    position: options?.position,
  })
}

/**
 * Error toast notification
 */
export const showErrorToast = (message: string, options?: ToastOptions) => {
  return toast.error(message, {
    duration: options?.duration || 5000,
    position: options?.position,
  })
}

/**
 * Info toast notification
 */
export const showInfoToast = (message: string, options?: ToastOptions) => {
  return toast(message, {
    icon: 'ℹ️',
    duration: options?.duration || 4000,
    position: options?.position,
  })
}

/**
 * Warning toast notification
 */
export const showWarningToast = (message: string, options?: ToastOptions) => {
  return toast(message, {
    icon: '⚠️',
    duration: options?.duration || 4000,
    position: options?.position,
  })
}

/**
 * Loading toast notification
 */
export const showLoadingToast = (message: string) => {
  return toast.loading(message)
}

/**
 * Dismiss a specific toast
 */
export const dismissToast = (toastId: string) => {
  toast.dismiss(toastId)
}

/**
 * Dismiss all toasts
 */
export const dismissAllToasts = () => {
  toast.dismiss()
}

/**
 * Promise-based toast for async operations
 */
export const showPromiseToast = <T>(
  promise: Promise<T>,
  messages: {
    loading: string
    success: string | ((data: T) => string)
    error: string | ((error: any) => string)
  },
  options?: ToastOptions
) => {
  return toast.promise(
    promise,
    {
      loading: messages.loading,
      success: messages.success,
      error: messages.error,
    },
    {
      duration: options?.duration,
      position: options?.position,
    }
  )
}

/**
 * Enhanced authentication-specific toast notifications with recovery suggestions
 */
export const authToasts = {
  loginSuccess: (username: string) => 
    showSuccessToast(`Welcome back, ${username}!`),
  
  loginError: (error: string) => {
    const errorType = error.toLowerCase().includes('credentials') ? 'invalid_credentials' :
                      error.toLowerCase().includes('locked') ? 'account_locked' :
                      error.toLowerCase().includes('network') ? 'network_error' :
                      'unknown'
    
    const suggestion = getRecoverySuggestion(errorType)
    return showErrorToast(`${error}. ${suggestion}`, { duration: 6000 })
  },
  
  logoutSuccess: () => 
    showSuccessToast('Successfully signed out'),
  
  sessionExpired: () => {
    const suggestion = getRecoverySuggestion('session_expired')
    return showWarningToast(`Your session has expired. ${suggestion}`)
  },
  
  sessionRefreshed: () => 
    showInfoToast('Session refreshed successfully'),
  
  sessionRefreshFailed: () => {
    const suggestion = getRecoverySuggestion('session_expired')
    return showErrorToast(`Failed to refresh session. ${suggestion}`)
  },
  
  passwordResetSent: (email: string) => 
    showSuccessToast(`Password reset instructions sent to ${email}. Check your email and spam folder.`),
  
  passwordResetError: (error: string) => {
    const errorType = error.toLowerCase().includes('not found') ? 'email_not_found' : 'unknown'
    const suggestion = getRecoverySuggestion(errorType)
    return showErrorToast(`Password reset failed: ${error}. ${suggestion}`, { duration: 6000 })
  },
  
  registrationSuccess: () => 
    showSuccessToast('Account created successfully! You can now sign in with your credentials.'),
  
  registrationError: (error: string) => {
    const suggestion = getRecoverySuggestion('validation_error')
    return showErrorToast(`Registration failed: ${error}. ${suggestion}`, { duration: 6000 })
  },
  
  accountLocked: () => {
    const suggestion = getRecoverySuggestion('account_locked')
    return showErrorToast(`Account temporarily locked. ${suggestion}`, { duration: 8000 })
  },
  
  rateLimited: () => {
    const suggestion = getRecoverySuggestion('rate_limited')
    return showWarningToast(`Too many attempts detected. ${suggestion}`, { duration: 6000 })
  },
  
  networkError: () => {
    const suggestion = getRecoverySuggestion('network_error')
    return showErrorToast(`Network error. ${suggestion}`, { duration: 6000 })
  },
  
  sessionTerminated: () => 
    showSuccessToast('Session terminated successfully'),
  
  sessionTerminationError: () => 
    showErrorToast('Failed to terminate session. Please try again or refresh the page.'),
  
  allSessionsTerminated: () => 
    showSuccessToast('All sessions terminated successfully. You will be redirected to the login page.'),
  
  invalidPin: () => {
    const suggestion = getRecoverySuggestion('invalid_pin')
    return showErrorToast(`Invalid PIN entered. ${suggestion}`, { duration: 6000 })
  },
  
  deviceNotFound: () => {
    const suggestion = getRecoverySuggestion('device_not_found')
    return showErrorToast(`Device not available. ${suggestion}`, { duration: 6000 })
  },
}

/**
 * Form-specific toast notifications
 */
export const formToasts = {
  validationError: (message: string) => 
    showErrorToast(`Please check your input: ${message}`),
  
  submissionSuccess: (action: string) => 
    showSuccessToast(`${action} completed successfully`),
  
  submissionError: (action: string, error: string) => 
    showErrorToast(`${action} failed: ${error}`),
  
  fieldError: (field: string, error: string) => 
    showErrorToast(`${field}: ${error}`),
}

/**
 * Session management toast notifications
 */
export const sessionToasts = {
  warningShown: (minutes: number) => 
    showWarningToast(`Your session will expire in ${minutes} minutes`),
  
  sessionExtended: () => 
    showSuccessToast('Session extended successfully'),
  
  sessionExpiringSoon: () => 
    showWarningToast('Your session is expiring soon. Activity detected - session extended.'),
}