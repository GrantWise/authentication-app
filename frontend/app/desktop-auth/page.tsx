"use client"

import type React from "react"

import { useState } from "react"
import Link from "next/link"
import { useRouter } from "next/navigation"
import { CheckCircle2, Eye, EyeOff, Lock } from "lucide-react"
import { Button } from "@/components/ui/button"
import { Card, CardContent, CardDescription, CardFooter, CardHeader, CardTitle } from "@/components/ui/card"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { Checkbox } from "@/components/ui/checkbox"

export default function DesktopAuth() {
  const router = useRouter()
  const [username, setUsername] = useState("")
  const [password, setPassword] = useState("")
  const [showPassword, setShowPassword] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [isLoading, setIsLoading] = useState(false)
  const [loginAttempts, setLoginAttempts] = useState(0)
  const [success, setSuccess] = useState(false)

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    setIsLoading(true)
    setError(null)

    try {
      const response = await fetch("http://localhost:5097/api/auth/login", {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
        },
        body: JSON.stringify({
          username,
          password,
        }),
      })

      const data = await response.json()

      if (response.ok) {
        // Store the JWT token
        localStorage.setItem("jwt_token", data.token)
        localStorage.setItem("user_role", data.role)
        localStorage.setItem("username", data.username)

        setSuccess(true)
        // Redirect to the app after a short delay
        setTimeout(() => {
          router.push("/dashboard")
        }, 1000)
      } else {
        setLoginAttempts((prev) => prev + 1)
        if (loginAttempts >= 2) {
          setError("Account locked for 30 minutes. Please contact your administrator.")
        } else {
          setError(data.message || "Incorrect username or password. Please try again.")
        }
      }
    } catch (error) {
      setError("Unable to connect to server. Please check your network connection.")
    } finally {
      setIsLoading(false)
    }
  }

  return (
    <main className="flex min-h-screen flex-col items-center justify-center p-4 md:p-24 bg-white">
      <Card className="w-full max-w-md">
        <CardHeader className="text-center pb-4">
          <div className="flex justify-center mb-8">
            <div className="text-[#1e4e8c] font-bold text-3xl">TransLution</div>
          </div>
          {!success && (
            <>
              <CardTitle className="text-3xl font-bold">Sign In</CardTitle>
              <CardDescription>Access your TransLution dashboard</CardDescription>
            </>
          )}
        </CardHeader>
        <CardContent>
          {!success ? (
            <form onSubmit={handleSubmit} className="space-y-6">
              <div className="space-y-2">
                <Label htmlFor="username" className="text-sm font-medium">
                  Username
                </Label>
                <Input
                  id="username"
                  type="text"
                  value={username}
                  onChange={(e) => setUsername(e.target.value)}
                  placeholder="Try: testuser, admin, or flowcreator"
                  className="h-11"
                  required
                  aria-label="Username or email address, required"
                  disabled={isLoading}
                />
              </div>

              <div className="space-y-2">
                <div className="flex justify-between items-center">
                  <Label htmlFor="password" className="text-sm font-medium">
                    Password
                  </Label>
                </div>
                <div className="relative">
                  <Input
                    id="password"
                    type={showPassword ? "text" : "password"}
                    value={password}
                    onChange={(e) => setPassword(e.target.value)}
                    placeholder="Try: TestPassword123!, AdminPassword123!, or FlowPassword123!"
                    className="h-11 pr-10"
                    required
                    aria-label="Password, required"
                    disabled={isLoading}
                  />
                  <Button
                    type="button"
                    variant="ghost"
                    size="icon"
                    className="absolute right-0 top-0 h-11 w-11"
                    onClick={() => setShowPassword(!showPassword)}
                    aria-label={showPassword ? "Hide password" : "Show password as plain text"}
                  >
                    {showPassword ? <EyeOff className="h-5 w-5" /> : <Eye className="h-5 w-5" />}
                  </Button>
                </div>
              </div>

              {error && <div className="text-[#dc2626] text-sm flex items-center gap-2">{error}</div>}

              <div className="flex items-center space-x-2">
                <Checkbox id="remember" />
                <Label htmlFor="remember" className="text-sm font-medium">
                  Remember me for 60 minutes
                </Label>
              </div>

              <Button type="submit" className="w-full h-11" disabled={isLoading}>
                {isLoading ? "Signing in..." : "Sign In"}
              </Button>
            </form>
          ) : (
            <div className="flex flex-col items-center justify-center py-8">
              <CheckCircle2 className="h-16 w-16 text-[#22c55e] mb-4" />
              <h2 className="text-2xl font-semibold mb-2">Sign In Successful</h2>
              <p className="text-muted-foreground">Redirecting to your dashboard...</p>
            </div>
          )}
        </CardContent>
        <CardFooter className="flex flex-col items-center gap-4">
          {!success && (
            <>
              <div className="flex items-center justify-center w-full">
                <Link href="/forgot-password" className="text-sm text-[#1e4e8c] hover:underline">
                  Forgot password?
                </Link>
              </div>
              <div className="flex items-center gap-2 text-sm text-muted-foreground">
                <Lock className="h-4 w-4" />
                <span>Secure connection</span>
              </div>
            </>
          )}
        </CardFooter>
      </Card>
    </main>
  )
}
