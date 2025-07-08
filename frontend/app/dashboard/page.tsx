"use client"

import { useEffect, useState } from "react"
import { useRouter } from "next/navigation"
import { Button } from "@/components/ui/button"
import { LoadingButton } from "@/components/ui/loading-button"
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card"
import { Badge } from "@/components/ui/badge"
import { SessionWarningModal } from "@/components/session-warning-modal"
import { LogOut, User, Shield, Settings, Monitor, Smartphone, AlertTriangle } from "lucide-react"
import { useAuthStore } from "@/lib/stores/auth-store"
import { useSessions, useTerminateSession, useLogoutAll } from "@/lib/api/auth-queries"
import { formatTimeRemaining } from "@/utils/auth"
import { authToasts, sessionToasts } from "@/lib/utils/toast"

export default function Dashboard() {
  const router = useRouter()
  const [showSessionWarning, setShowSessionWarning] = useState(false)
  
  const { user, accessToken, isAuthenticated, logout, sessionExpiry, checkTokenExpiry, lastActivity } = useAuthStore()
  
  // Session queries
  const { data: sessionsData, isLoading: sessionsLoading, refetch: refetchSessions } = useSessions(accessToken || undefined)
  const terminateSessionMutation = useTerminateSession({
    onSuccess: () => {
      authToasts.sessionTerminated()
      refetchSessions()
    },
    onError: () => {
      authToasts.sessionTerminationError()
    }
  })
  const logoutAllMutation = useLogoutAll({
    onSuccess: () => {
      authToasts.allSessionsTerminated()
      router.push('/')
    },
    onError: () => {
      authToasts.sessionTerminationError()
    }
  })

  useEffect(() => {
    // Check if user is authenticated
    if (!isAuthenticated || !user) {
      router.push("/")
      return
    }

    // Check if token is expired
    if (checkTokenExpiry()) {
      logout()
      router.push("/")
      return
    }

    // Calculate when to show session warning (5 minutes before expiry)
    if (sessionExpiry) {
      const timeUntilExpiry = sessionExpiry - Date.now()
      const warningTime = timeUntilExpiry - (5 * 60 * 1000) // 5 minutes before expiry
      
      if (warningTime > 0) {
        const timer = setTimeout(() => {
          setShowSessionWarning(true)
        }, warningTime)

        return () => clearTimeout(timer)
      } else if (timeUntilExpiry > 0) {
        // Show warning immediately if less than 5 minutes remaining
        setShowSessionWarning(true)
      }
    }
  }, [isAuthenticated, user, sessionExpiry, checkTokenExpiry, logout, router])

  const handleSignOut = async () => {
    await logout()
    router.push("/")
  }

  const getPrimaryRole = () => {
    if (!user?.roles || user.roles.length === 0) return "user"
    // Return the highest privilege role
    if (user.roles.includes("admin")) return "admin"
    if (user.roles.includes("flowcreator")) return "flowcreator"
    return user.roles[0] || "user"
  }

  const getRoleColor = (role: string) => {
    switch (role.toLowerCase()) {
      case "admin":
        return "destructive"
      case "flowcreator":
        return "default"
      case "user":
        return "secondary"
      default:
        return "outline"
    }
  }

  const getRoleIcon = (role: string) => {
    switch (role.toLowerCase()) {
      case "admin":
        return <Shield className="h-4 w-4" />
      case "flowcreator":
        return <Settings className="h-4 w-4" />
      case "user":
        return <User className="h-4 w-4" />
      default:
        return <User className="h-4 w-4" />
    }
  }

  if (!isAuthenticated || !user) {
    return (
      <div className="flex min-h-screen items-center justify-center">
        <div className="text-center">
          <div className="text-lg">Loading...</div>
        </div>
      </div>
    )
  }

  const primaryRole = getPrimaryRole()

  return (
    <main className="flex min-h-screen flex-col p-4 md:p-24">
      <div className="container mx-auto">
        {/* Header */}
        <div className="flex justify-between items-center mb-8">
          <div>
            <h1 className="text-3xl font-bold">TransLution Dashboard</h1>
            <p className="text-muted-foreground">Welcome back, {user.username}</p>
          </div>
          <div className="flex items-center gap-4">
            <Badge variant={getRoleColor(primaryRole)} className="flex items-center gap-1">
              {getRoleIcon(primaryRole)}
              {primaryRole}
            </Badge>
            <Button variant="outline" onClick={handleSignOut} className="flex items-center gap-2 bg-transparent">
              <LogOut className="h-4 w-4" />
              Sign Out
            </Button>
          </div>
        </div>

        {/* Dashboard Content */}
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
          <Card>
            <CardHeader>
              <CardTitle>Authentication Status</CardTitle>
              <CardDescription>Your current session information</CardDescription>
            </CardHeader>
            <CardContent className="space-y-4">
              <div className="flex justify-between">
                <span className="font-medium">Username:</span>
                <span>{user.username}</span>
              </div>
              <div className="flex justify-between">
                <span className="font-medium">Email:</span>
                <span>{user.email || 'Not provided'}</span>
              </div>
              <div className="flex justify-between">
                <span className="font-medium">Roles:</span>
                <div className="flex gap-1">
                  {user.roles.map((role, index) => (
                    <Badge key={index} variant={getRoleColor(role)} className="text-xs">
                      {role}
                    </Badge>
                  ))}
                </div>
              </div>
              <div className="flex justify-between">
                <span className="font-medium">Session:</span>
                <span className="text-green-600">Active</span>
              </div>
              <div className="flex justify-between">
                <span className="font-medium">Last Activity:</span>
                <span>{lastActivity ? new Date(lastActivity).toLocaleTimeString() : 'Unknown'}</span>
              </div>
              <div className="pt-2">
                <p className="text-sm text-muted-foreground">
                  Your session will expire in {sessionExpiry ? Math.max(0, Math.ceil((sessionExpiry - Date.now()) / (1000 * 60))) : 'unknown'} minutes.
                </p>
              </div>
            </CardContent>
          </Card>

          <Card>
            <CardHeader>
              <CardTitle>JWT Token Info</CardTitle>
              <CardDescription>Token details for development</CardDescription>
            </CardHeader>
            <CardContent>
              <div className="space-y-2">
                <div className="text-sm">
                  <span className="font-medium">Token (first 20 chars):</span>
                  <div className="font-mono text-xs bg-muted p-2 rounded mt-1">
                    {accessToken ? accessToken.substring(0, 20) + '...' : 'No token available'}
                  </div>
                </div>
                <div className="text-sm">
                  <span className="font-medium">Backend URL:</span>
                  <div className="font-mono text-xs bg-muted p-2 rounded mt-1">
                    {process.env.NEXT_PUBLIC_API_URL || 'Not configured'}
                  </div>
                </div>
                <div className="text-sm">
                  <span className="font-medium">User ID:</span>
                  <div className="font-mono text-xs bg-muted p-2 rounded mt-1">
                    {user.id || 'Not available'}
                  </div>
                </div>
              </div>
            </CardContent>
          </Card>

          <Card>
            <CardHeader>
              <CardTitle>Quick Actions</CardTitle>
              <CardDescription>Common session management actions</CardDescription>
            </CardHeader>
            <CardContent>
              <div className="space-y-3">
                <Button 
                  variant="outline" 
                  size="sm" 
                  onClick={handleSignOut}
                  className="w-full"
                >
                  <LogOut className="h-4 w-4 mr-2" />
                  Sign Out Current Session
                </Button>
                <Button 
                  variant="outline" 
                  size="sm" 
                  onClick={() => refetchSessions()}
                  disabled={sessionsLoading}
                  className="w-full"
                >
                  <Monitor className="h-4 w-4 mr-2" />
                  Refresh Session Info
                </Button>
                <div className="text-xs text-muted-foreground text-center pt-2">
                  Sessions: {sessionsData?.sessions?.length || 0} active
                </div>
              </div>
            </CardContent>
          </Card>
        </div>

        {/* Enhanced Session Management Card */}
        <div className="mt-8">
          <Card>
            <CardHeader>
              <CardTitle className="flex items-center gap-2">
                <Monitor className="h-5 w-5" />
                Active Sessions
              </CardTitle>
              <CardDescription>
                Manage all your active sessions across devices
              </CardDescription>
            </CardHeader>
            <CardContent>
              {sessionsLoading ? (
                <div className="text-center py-4">
                  <div className="text-sm text-muted-foreground">Loading sessions...</div>
                </div>
              ) : sessionsData?.sessions && sessionsData.sessions.length > 0 ? (
                <div className="space-y-4">
                  {sessionsData.sessions.map((session) => (
                    <div 
                      key={session.sessionId} 
                      className={`flex items-center justify-between p-4 rounded-lg border ${
                        session.isCurrent ? 'border-blue-200 bg-blue-50' : 'border-gray-200'
                      }`}
                    >
                      <div className="flex items-center gap-3">
                        <div className="flex-shrink-0">
                          {session.deviceInfo.toLowerCase().includes('mobile') ? (
                            <Smartphone className="h-5 w-5 text-gray-500" />
                          ) : (
                            <Monitor className="h-5 w-5 text-gray-500" />
                          )}
                        </div>
                        <div>
                          <div className="flex items-center gap-2">
                            <span className="font-medium">
                              {session.deviceInfo.length > 50 
                                ? session.deviceInfo.substring(0, 50) + '...' 
                                : session.deviceInfo}
                            </span>
                            {session.isCurrent && (
                              <Badge variant="default" className="text-xs">
                                Current
                              </Badge>
                            )}
                          </div>
                          <div className="text-sm text-muted-foreground">
                            IP: {session.ipAddress} • 
                            Last active: {new Date(session.lastActivity).toLocaleString()}
                          </div>
                          <div className="text-xs text-muted-foreground">
                            Expires: {new Date(session.expiresAt).toLocaleString()}
                          </div>
                        </div>
                      </div>
                      <div className="flex items-center gap-2">
                        {!session.isCurrent && (
                          <LoadingButton
                            variant="outline"
                            size="sm"
                            onClick={() => {
                              if (accessToken) {
                                terminateSessionMutation.mutate({ sessionId: session.sessionId, accessToken })
                              }
                            }}
                            loading={terminateSessionMutation.isPending}
                            loadingText="Ending..."
                          >
                            <LogOut className="h-4 w-4 mr-1" />
                            Terminate
                          </LoadingButton>
                        )}
                      </div>
                    </div>
                  ))}
                  <div className="flex gap-2 pt-4 border-t">
                    <LoadingButton
                      variant="outline"
                      onClick={() => {
                        if (accessToken) {
                          logoutAllMutation.mutate(accessToken)
                        }
                      }}
                      loading={logoutAllMutation.isPending}
                      loadingText="Signing out..."
                      className="flex items-center gap-2"
                    >
                      <AlertTriangle className="h-4 w-4" />
                      Sign Out All Sessions
                    </LoadingButton>
                    <Button
                      variant="ghost"
                      onClick={() => refetchSessions()}
                      disabled={sessionsLoading}
                    >
                      Refresh
                    </Button>
                  </div>
                </div>
              ) : (
                <div className="text-center py-8">
                  <Monitor className="h-12 w-12 text-muted-foreground mx-auto mb-4" />
                  <div className="text-sm text-muted-foreground">
                    No active sessions found
                  </div>
                </div>
              )}
            </CardContent>
          </Card>
        </div>

        {/* Session Warning Modal */}
        {showSessionWarning && (
          <SessionWarningModal 
            isOpen={showSessionWarning} 
            onClose={() => setShowSessionWarning(false)} 
          />
        )}
      </div>
    </main>
  )
}
