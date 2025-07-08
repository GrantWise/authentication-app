"use client"

import { useState } from "react"
import { useRouter } from "next/navigation"
import { useForm } from "react-hook-form"
import { zodResolver } from "@hookform/resolvers/zod"
import { ArrowLeft, CheckCircle2 } from "lucide-react"
import { Button } from "@/components/ui/button"
import { LoadingButton } from "@/components/ui/loading-button"
import { LoadingOverlay, LoadingSpinner } from "@/components/ui/loading-spinner"
import { CompactProgressSteps } from "@/components/ui/progress-steps"
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card"
import { UserSelectionGrid } from "@/components/user-selection-grid"
import { PinEntry } from "@/components/pin-entry"
import { useAuthStore } from "@/lib/stores/auth-store"
import { usePinLogin } from "@/lib/api/auth-queries"
import { 
  mobilePinSchema, 
  deviceSelectionSchema, 
  userSelectionSchema,
  type MobilePinFormData, 
  type DeviceSelectionFormData, 
  type UserSelectionFormData 
} from "@/lib/schemas/auth-schemas"
import { authToasts } from "@/lib/utils/toast"

export default function MobileAuth() {
  const router = useRouter()
  const [step, setStep] = useState<"device" | "user" | "pin" | "success">("device")
  const [selectedUser, setSelectedUser] = useState<string | null>(null)
  const [selectedDevice, setSelectedDevice] = useState<string | null>(null)
  const [pinAttempts, setPinAttempts] = useState(0)
  
  const { clearError } = useAuthStore()
  const pinLoginMutation = usePinLogin({
    onSuccess: (data) => {
      if (data.success && data.user) {
        authToasts.loginSuccess(data.user.username)
        setStep("success")
        // Redirect to the app after a short delay
        setTimeout(() => {
          router.push("/dashboard")
        }, 2000)
      }
    },
    onError: (error) => {
      authToasts.loginError((error as Error).message)
      setPinAttempts((prev) => prev + 1)
    }
  })

  // Form hooks for each step
  const deviceForm = useForm<DeviceSelectionFormData>({
    resolver: zodResolver(deviceSelectionSchema),
  })

  const userForm = useForm<UserSelectionFormData>({
    resolver: zodResolver(userSelectionSchema),
  })

  const pinForm = useForm<MobilePinFormData>({
    resolver: zodResolver(mobilePinSchema),
    mode: 'onChange',
  })

  const devices = [
    { id: "1", name: "Warehouse Scanner 1", location: "Main Warehouse" },
    { id: "2", name: "Warehouse Scanner 2", location: "Shipping Area" },
    { id: "3", name: "Warehouse Scanner 3", location: "Receiving Dock" },
  ]

  const users = [
    { id: "1", name: "John Smith", initials: "JS" },
    { id: "2", name: "Maria Garcia", initials: "MG" },
    { id: "3", name: "David Chen", initials: "DC" },
    { id: "4", name: "Sarah Johnson", initials: "SJ" },
    { id: "5", name: "Robert Kim", initials: "RK" },
    { id: "6", name: "Lisa Patel", initials: "LP" },
  ]

  const progressSteps = [
    { id: "device", title: "Device", description: "Select Scanner" },
    { id: "user", title: "User", description: "Select Profile" },
    { id: "pin", title: "PIN", description: "Enter PIN" },
    { id: "success", title: "Complete", description: "Sign In Success" },
  ]

  const handleDeviceSelect = (deviceId: string) => {
    const device = devices.find((d) => d.id === deviceId)
    if (device) {
      setSelectedDevice(device.name)
      deviceForm.setValue("deviceId", deviceId)
      setStep("user")
    }
  }

  const handleUserSelect = (userId: string) => {
    const user = users.find((u) => u.id === userId)
    if (user) {
      setSelectedUser(user.name)
      userForm.setValue("userId", userId)
      setStep("pin")
    }
  }

  const handlePinSubmit = async (pin: string) => {
    clearError()

    // Set form data for validation
    pinForm.setValue("pin", pin)
    pinForm.setValue("deviceId", selectedDevice || "unknown")

    // Validate the form
    const isValid = await pinForm.trigger()
    if (!isValid) {
      setPinAttempts((prev) => prev + 1)
      return
    }

    const formData = pinForm.getValues()
    pinLoginMutation.mutate({
      deviceId: formData.deviceId,
      pin: formData.pin,
      deviceInfo: navigator.userAgent,
    })
  }

  return (
    <main className="flex min-h-screen flex-col items-center justify-center p-4 md:p-6 bg-white">
      <LoadingOverlay 
        isLoading={pinLoginMutation.isPending && step === "pin"}
        loadingText="Verifying PIN..."
        className="w-full max-w-md"
      >
        <Card className="w-full max-w-md border-0 shadow-none">
        <CardHeader className="text-center pb-4">
          <div className="flex justify-center mb-8">
            <div className="text-[#1e4e8c] font-bold text-3xl">TransLution</div>
          </div>
          {step !== "success" && (
            <CardTitle className="text-2xl font-semibold">
              {step === "device" && "Select Device"}
              {step === "user" && "Select User"}
              {step === "pin" && "Enter Your PIN"}
            </CardTitle>
          )}
          {step === "user" && <CardDescription>Choose your user profile</CardDescription>}
          {step === "pin" && selectedUser && (
            <CardDescription>
              Signing in as {selectedUser}
            </CardDescription>
          )}
          
          {/* Progress Steps */}
          {step !== "success" && (
            <div className="mb-6 w-full px-4">
              <CompactProgressSteps steps={progressSteps} currentStepId={step} />
            </div>
          )}
        </CardHeader>
        <CardContent className="flex flex-col items-center">
          {step === "device" && (
            <div className="w-full space-y-4">
              {devices.map((device) => (
                <Button
                  key={device.id}
                  variant="outline"
                  className="w-full h-16 flex flex-col items-start justify-center p-4 text-left bg-transparent"
                  onClick={() => handleDeviceSelect(device.id)}
                >
                  <div className="font-medium">{device.name}</div>
                  <div className="text-sm text-muted-foreground">{device.location}</div>
                </Button>
              ))}
            </div>
          )}

          {step === "user" && (
            <>
              <UserSelectionGrid users={users} onSelect={handleUserSelect} />
              <Button
                variant="ghost"
                className="mt-6 h-14 w-full flex items-center gap-2"
                onClick={() => setStep("device")}
              >
                <ArrowLeft className="h-5 w-5" />
                <span>Back to Device Selection</span>
              </Button>
            </>
          )}

          {step === "pin" && (
            <>
              <PinEntry 
                onComplete={handlePinSubmit} 
                error={!!pinForm.formState.errors.pin || !!pinLoginMutation.error} 
                disabled={pinLoginMutation.isPending}
                value={pinForm.watch("pin")}
                onChange={(pin) => pinForm.setValue("pin", pin)}
              />

              {(pinForm.formState.errors.pin || pinLoginMutation.error) && (
                <div className="mt-4 text-[#dc2626] text-sm flex items-center justify-center">
                  {pinForm.formState.errors.pin?.message || 
                   (pinLoginMutation.error as Error)?.message}
                  {pinAttempts >= 3 && (
                    <div className="ml-2 text-xs text-muted-foreground">
                      Contact supervisor for help
                    </div>
                  )}
                </div>
              )}

              <div className="mt-8 w-full flex justify-between">
                <Button 
                  variant="ghost" 
                  className="h-14 flex items-center gap-2" 
                  onClick={() => {
                    setStep("user")
                    clearError()
                    setPinAttempts(0)
                    pinForm.reset()
                  }}
                  disabled={pinLoginMutation.isPending}
                >
                  <ArrowLeft className="h-5 w-5" />
                  <span>Back</span>
                </Button>

                <Button 
                  variant="outline" 
                  className="h-14 bg-transparent" 
                  disabled={pinLoginMutation.isPending}
                >
                  Get Help
                </Button>
              </div>
            </>
          )}

          {step === "success" && (
            <div className="flex flex-col items-center justify-center py-8">
              <CheckCircle2 className="h-16 w-16 text-[#22c55e] mb-4" />
              <h2 className="text-2xl font-semibold mb-2">Sign In Successful</h2>
              <p className="text-muted-foreground">Redirecting to your workspace...</p>
            </div>
          )}
        </CardContent>
        </Card>
      </LoadingOverlay>
    </main>
  )
}
