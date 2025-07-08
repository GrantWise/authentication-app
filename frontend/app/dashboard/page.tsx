"use client"

import { useEffect, useState } from "react"
import { useRouter } from "next/navigation"
import { Button } from "@/components/ui/button"
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card"
import { Badge } from "@/components/ui/badge"
import { SessionWarningModal } from "@/components/session-warning-modal"
import { LogOut, User, Shield, Settings } from "lucide-react"

export default function Dashboard() {
  const router = useRouter()
  const [userInfo, setUserInfo] = useState<{
    username: string
    role: string
    token: string
  } | null>(null)
  const [showSessionWarning, setShowSessionWarning] = useState(false)

  useEffect(() => {
    // Check if user is authenticated
    const token = localStorage.getItem("jwt_token")
    const username = localStorage.getItem("username")
    const role = localStorage.getItem("user_role")

    if (!token || !username || !role) {
      router.push("/")
      return
    }

    setUserInfo({ username, role, token })

    // Simulate session warning after 5 seconds for demo
    const timer = setTimeout(() => {
      setShowSessionWarning(true)
    }, 5000)

    return () => clearTimeout(timer)
  }, [router])

  const handleSignOut = () => {
    localStorage.removeItem("jwt_token")
    localStorage.removeItem("username")
    localStorage.removeItem("user_role")
    router.push("/")
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

  if (!userInfo) {
    return (
      <div className="flex min-h-screen items-center justify-center">
        <div className="text-center">
          <div className="text-lg">Loading...</div>
        </div>
      </div>
    )
  }

  return (
    <main className="flex min-h-screen flex-col p-4 md:p-24">
      <div className="container mx-auto">
        {/* Header */}
        <div className="flex justify-between items-center mb-8">
          <div>
            <h1 className="text-3xl font-bold">TransLution Dashboard</h1>
            <p className="text-muted-foreground">Welcome back, {userInfo.username}</p>
          </div>
          <div className="flex items-center gap-4">
            <Badge variant={getRoleColor(userInfo.role)} className="flex items-center gap-1">
              {getRoleIcon(userInfo.role)}
              {userInfo.role}
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
                <span>{userInfo.username}</span>
              </div>
              <div className="flex justify-between">
                <span className="font-medium">Role:</span>
                <Badge variant={getRoleColor(userInfo.role)}>{userInfo.role}</Badge>
              </div>
              <div className="flex justify-between">
                <span className="font-medium">Session:</span>
                <span className="text-green-600">Active</span>
              </div>
              <div className="pt-2">
                <p className="text-sm text-muted-foreground">
                  Your session will expire after 60 minutes of inactivity.
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
                    {userInfo.token.substring(0, 20)}...
                  </div>
                </div>
                <div className="text-sm">
                  <span className="font-medium">Backend URL:</span>
                  <div className="font-mono text-xs bg-muted p-2 rounded mt-1">http://localhost:5097</div>
                </div>
              </div>
            </CardContent>
          </Card>

          <Card>
            <CardHeader>
              <CardTitle>Test Credentials</CardTitle>
              <CardDescription>Available test accounts</CardDescription>
            </CardHeader>
            <CardContent>
              <div className="space-y-3 text-sm">
                <div>
                  <div className="font-medium">User Account:</div>
                  <div className="font-mono text-xs">testuser / TestPassword123!</div>
                  <div className="font-mono text-xs">PIN: 1234</div>
                </div>
                <div>
                  <div className="font-medium">Admin Account:</div>
                  <div className="font-mono text-xs">admin / AdminPassword123!</div>
                  <div className="font-mono text-xs">PIN: 5678</div>
                </div>
                <div>
                  <div className="font-medium">FlowCreator Account:</div>
                  <div className="font-mono text-xs">flowcreator / FlowPassword123!</div>
                  <div className="font-mono text-xs">PIN: 9999</div>
                </div>
              </div>
            </CardContent>
          </Card>
        </div>

        {/* Session Warning Modal */}
        {showSessionWarning && <SessionWarningModal onClose={() => setShowSessionWarning(false)} />}
      </div>
    </main>
  )
}
