"use client"

import { useEffect } from 'react'
import { useAuthStore } from '@/lib/stores/auth-store'
import { isTokenExpired, getTokenTimeRemaining } from '@/utils/auth'

interface AuthProviderProps {
  children: React.ReactNode
}

/**
 * Authentication Provider Component
 * Handles automatic token refresh, session monitoring, and cleanup
 */
export function AuthProvider({ children }: AuthProviderProps) {
  const { accessToken, refreshToken, refresh, logout, updateLastActivity } = useAuthStore()

  useEffect(() => {
    let refreshInterval: NodeJS.Timeout
    let activityInterval: NodeJS.Timeout

    const setupTokenRefresh = () => {
      if (!accessToken || !refreshToken) {
        return
      }

      // Check if access token is expired or will expire soon (within 2 minutes)
      const timeRemaining = getTokenTimeRemaining(accessToken)
      const shouldRefresh = timeRemaining < 120 // 2 minutes

      if (shouldRefresh) {
        // Attempt immediate refresh if token is expired or expiring soon
        refresh().catch(() => {
          // If refresh fails, logout
          logout()
        })
      }

      // Set up automatic refresh interval (check every minute)
      refreshInterval = setInterval(() => {
        if (accessToken && !isTokenExpired(accessToken)) {
          const remaining = getTokenTimeRemaining(accessToken)
          
          // Refresh when 2 minutes remaining
          if (remaining <= 120 && remaining > 0) {
            refresh().catch(() => {
              logout()
            })
          }
        } else if (accessToken && isTokenExpired(accessToken)) {
          // Token is expired, try to refresh
          if (refreshToken) {
            refresh().catch(() => {
              logout()
            })
          } else {
            logout()
          }
        }
      }, 60000) // Check every minute
    }

    const setupActivityTracking = () => {
      // Update last activity every 30 seconds if user is active
      activityInterval = setInterval(() => {
        if (accessToken) {
          updateLastActivity()
        }
      }, 30000)

      // Listen for user activity events
      const activityEvents = ['mousedown', 'keydown', 'scroll', 'touchstart', 'click']
      
      const handleActivity = () => {
        if (accessToken) {
          updateLastActivity()
        }
      }

      activityEvents.forEach(event => {
        document.addEventListener(event, handleActivity, { passive: true })
      })

      // Cleanup function
      return () => {
        activityEvents.forEach(event => {
          document.removeEventListener(event, handleActivity)
        })
      }
    }

    // Initialize if user is authenticated
    if (accessToken && refreshToken) {
      setupTokenRefresh()
      const cleanupActivity = setupActivityTracking()

      return () => {
        if (refreshInterval) clearInterval(refreshInterval)
        if (activityInterval) clearInterval(activityInterval)
        cleanupActivity?.()
      }
    }

    return () => {
      if (refreshInterval) clearInterval(refreshInterval)
      if (activityInterval) clearInterval(activityInterval)
    }
  }, [accessToken, refreshToken, refresh, logout, updateLastActivity])

  // Cleanup on unmount
  useEffect(() => {
    return () => {
      // Cleanup will be handled by the previous useEffect
    }
  }, [])

  return <>{children}</>
}