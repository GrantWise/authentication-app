"use client"

import type React from "react"

import { useState } from "react"
import Link from "next/link"
import { ArrowLeft, CheckCircle2, Mail } from "lucide-react"
import { Button } from "@/components/ui/button"
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"

export default function ForgotPassword() {
  const [email, setEmail] = useState("")
  const [isLoading, setIsLoading] = useState(false)
  const [success, setSuccess] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    setIsLoading(true)
    setError(null)

    try {
      const response = await fetch("http://localhost:5097/api/auth/forgot-password", {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
        },
        body: JSON.stringify({ email }),
      })

      if (response.ok) {
        setSuccess(true)
      } else {
        const data = await response.json()
        setError(data.message || "Failed to send reset email. Please try again.")
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
          {!success ? (
            <>
              <CardTitle className="text-2xl font-bold">Reset Password</CardTitle>
              <CardDescription>Enter your email address and we'll send you a reset link</CardDescription>
            </>
          ) : (
            <>
              <CheckCircle2 className="h-16 w-16 text-[#22c55e] mx-auto mb-4" />
              <CardTitle className="text-2xl font-bold">Check Your Email</CardTitle>
              <CardDescription>We've sent a password reset link to {email}</CardDescription>
            </>
          )}
        </CardHeader>
        <CardContent>
          {!success ? (
            <form onSubmit={handleSubmit} className="space-y-6">
              <div className="space-y-2">
                <Label htmlFor="email" className="text-sm font-medium">
                  Email Address
                </Label>
                <Input
                  id="email"
                  type="email"
                  value={email}
                  onChange={(e) => setEmail(e.target.value)}
                  placeholder="Enter your email address"
                  className="h-11"
                  required
                  disabled={isLoading}
                />
              </div>

              {error && <div className="text-[#dc2626] text-sm flex items-center gap-2">{error}</div>}

              <Button type="submit" className="w-full h-11" disabled={isLoading}>
                {isLoading ? "Sending..." : "Send Reset Link"}
              </Button>

              <div className="text-center">
                <Link
                  href="/desktop-auth"
                  className="inline-flex items-center gap-2 text-sm text-[#1e4e8c] hover:underline"
                >
                  <ArrowLeft className="h-4 w-4" />
                  Back to Sign In
                </Link>
              </div>
            </form>
          ) : (
            <div className="space-y-6">
              <div className="text-center space-y-2">
                <Mail className="h-12 w-12 text-[#1e4e8c] mx-auto" />
                <p className="text-sm text-muted-foreground">
                  If an account with that email exists, you'll receive a password reset link shortly.
                </p>
              </div>

              <div className="text-center">
                <Link
                  href="/desktop-auth"
                  className="inline-flex items-center gap-2 text-sm text-[#1e4e8c] hover:underline"
                >
                  <ArrowLeft className="h-4 w-4" />
                  Back to Sign In
                </Link>
              </div>
            </div>
          )}
        </CardContent>
      </Card>
    </main>
  )
}
