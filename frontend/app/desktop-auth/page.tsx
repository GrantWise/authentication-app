"use client"

import { useState } from "react"
import Link from "next/link"
import { useRouter } from "next/navigation"
import { useForm } from "react-hook-form"
import { zodResolver } from "@hookform/resolvers/zod"
import { CheckCircle2, Eye, EyeOff, Lock } from "lucide-react"
import { Button } from "@/components/ui/button"
import { LoadingButton } from "@/components/ui/loading-button"
import { LoadingOverlay } from "@/components/ui/loading-spinner"
import { Card, CardContent, CardDescription, CardFooter, CardHeader, CardTitle } from "@/components/ui/card"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { Checkbox } from "@/components/ui/checkbox"
import { useAuthStore } from "@/lib/stores/auth-store"
import { useLogin } from "@/lib/api/auth-queries"
import { 
  desktopLoginSchema, 
  type DesktopLoginFormData 
} from "@/lib/schemas/auth-schemas"
import { authToasts, formToasts } from "@/lib/utils/toast"

export default function DesktopAuth() {
  const router = useRouter()
  const [showPassword, setShowPassword] = useState(false)
  const [loginAttempts, setLoginAttempts] = useState(0)
  const [success, setSuccess] = useState(false)
  
  const { clearError } = useAuthStore()
  const loginMutation = useLogin({
    onSuccess: (data) => {
      if (data.success && data.user) {
        authToasts.loginSuccess(data.user.username)
        setSuccess(true)
        // Redirect to the app after a short delay
        setTimeout(() => {
          router.push("/dashboard")
        }, 1000)
      }
    },
    onError: (error) => {
      authToasts.loginError((error as Error).message)
      setLoginAttempts((prev) => prev + 1)
    }
  })

  // Form hook
  const form = useForm<DesktopLoginFormData>({
    resolver: zodResolver(desktopLoginSchema),
    defaultValues: {
      username: "",
      password: "",
      rememberMe: false,
    },
    mode: 'onChange',
  })

  const onSubmit = (data: DesktopLoginFormData) => {
    clearError()
    
    loginMutation.mutate({
      username: data.username.trim(),
      password: data.password,
      deviceInfo: navigator.userAgent,
      rememberMe: data.rememberMe,
    })
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
            <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-6">
              <div className="space-y-2">
                <Label htmlFor="username" className="text-sm font-medium">
                  Username
                </Label>
                <Input
                  id="username"
                  type="text"
                  {...form.register("username")}
                  placeholder="Enter your username"
                  className="h-11"
                  aria-label="Username or email address, required"
                  disabled={loginMutation.isPending}
                />
                {form.formState.errors.username && (
                  <p className="text-sm text-red-600">{form.formState.errors.username.message}</p>
                )}
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
                    {...form.register("password")}
                    placeholder="Enter your password"
                    className="h-11 pr-10"
                    aria-label="Password, required"
                    disabled={loginMutation.isPending}
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
                {form.formState.errors.password && (
                  <p className="text-sm text-red-600">{form.formState.errors.password.message}</p>
                )}
              </div>

              {loginMutation.error && (
                <div className="text-[#dc2626] text-sm flex items-center gap-2">
                  {(loginMutation.error as Error).message}
                </div>
              )}

              <div className="flex items-center space-x-2">
                <Checkbox 
                  id="remember" 
                  checked={form.watch("rememberMe")}
                  onCheckedChange={(checked) => form.setValue("rememberMe", !!checked)}
                />
                <Label htmlFor="remember" className="text-sm font-medium">
                  Remember me for 60 minutes
                </Label>
              </div>

              <LoadingButton 
                type="submit" 
                className="w-full h-11" 
                loading={loginMutation.isPending}
                loadingText="Signing in..."
                disabled={!form.formState.isValid}
              >
                Sign In
              </LoadingButton>
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
