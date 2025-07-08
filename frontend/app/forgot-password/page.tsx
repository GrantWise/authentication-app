"use client"

import type React from "react"

import { useState } from "react"
import Link from "next/link"
import { ArrowLeft, CheckCircle2, Mail } from "lucide-react"
import { useForm } from "react-hook-form"
import { zodResolver } from "@hookform/resolvers/zod"
import { Button } from "@/components/ui/button"
import { LoadingButton } from "@/components/ui/loading-button"
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { forgotPasswordSchema } from "@/lib/schemas/auth-schemas"
import { useForgotPassword } from "@/lib/api/auth-queries"
import { handleApiError } from "@/lib/api/http-client"
import type { ForgotPasswordFormData } from "@/lib/types/auth"

export default function ForgotPassword() {
  const [success, setSuccess] = useState(false)
  const [submittedEmail, setSubmittedEmail] = useState("")

  const {
    register,
    handleSubmit,
    formState: { errors },
    setError,
    clearErrors,
  } = useForm<ForgotPasswordFormData>({
    resolver: zodResolver(forgotPasswordSchema),
    mode: 'onBlur',
  })

  const forgotPasswordMutation = useForgotPassword({
    onSuccess: (data, variables) => {
      setSubmittedEmail(variables.email)
      setSuccess(true)
      clearErrors()
    },
    onError: (error) => {
      const errorMessage = handleApiError(error)
      setError('root', {
        type: 'manual',
        message: errorMessage,
      })
    },
  })

  const onSubmit = (data: ForgotPasswordFormData) => {
    forgotPasswordMutation.mutate(data)
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
              <CardDescription>We've sent a password reset link to {submittedEmail}</CardDescription>
            </>
          )}
        </CardHeader>
        <CardContent>
          {!success ? (
            <form onSubmit={handleSubmit(onSubmit)} className="space-y-6">
              <div className="space-y-2">
                <Label htmlFor="email" className="text-sm font-medium">
                  Email Address
                </Label>
                <Input
                  id="email"
                  type="email"
                  {...register('email')}
                  placeholder="Enter your email address"
                  className="h-11"
                  disabled={forgotPasswordMutation.isPending}
                  aria-invalid={errors.email ? 'true' : 'false'}
                />
                {errors.email && (
                  <p className="text-[#dc2626] text-sm">
                    {errors.email.message}
                  </p>
                )}
              </div>

              {errors.root && (
                <div className="text-[#dc2626] text-sm flex items-center gap-2">
                  {errors.root.message}
                </div>
              )}

              <LoadingButton 
                type="submit" 
                className="w-full h-11" 
                loading={forgotPasswordMutation.isPending}
                loadingText="Sending..."
              >
                Send Reset Link
              </LoadingButton>

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
