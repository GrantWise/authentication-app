"use client"

import { useState } from "react"
import { useRouter } from "next/navigation"
import { ArrowLeft, CheckCircle2 } from "lucide-react"
import { Button } from "@/components/ui/button"
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card"
import { UserSelectionGrid } from "@/components/user-selection-grid"
import { PinEntry } from "@/components/pin-entry"

export default function MobileAuth() {
  const router = useRouter()
  const [step, setStep] = useState<"device" | "user" | "pin" | "success">("device")
  const [selectedUser, setSelectedUser] = useState<string | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [pinAttempts, setPinAttempts] = useState(0)

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

  const handleDeviceSelect = (deviceId: string) => {
    // In a real app, this would set the selected device
    setStep("user")
  }

  const handleUserSelect = (userId: string) => {
    const user = users.find((u) => u.id === userId)
    if (user) {
      setSelectedUser(user.name)
      setStep("pin")
    }
  }

  const handlePinSubmit = async (pin: string) => {
    setError(null)

    // Map PINs to test users for demo purposes
    const pinToUser: Record<string, { username: string; password: string }> = {
      "1234": { username: "testuser", password: "TestPassword123!" },
      "5678": { username: "admin", password: "AdminPassword123!" },
      "9999": { username: "flowcreator", password: "FlowPassword123!" },
    }

    const userCredentials = pinToUser[pin]

    if (!userCredentials) {
      setPinAttempts((prev) => prev + 1)
      if (pinAttempts >= 2) {
        setError("Account locked. See supervisor")
      } else {
        setError(`Incorrect PIN. ${2 - pinAttempts} attempts remaining`)
      }
      return
    }

    try {
      const response = await fetch("http://localhost:5097/api/auth/login", {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
        },
        body: JSON.stringify({
          username: userCredentials.username,
          password: userCredentials.password,
        }),
      })

      const data = await response.json()

      if (response.ok) {
        // Store the JWT token
        localStorage.setItem("jwt_token", data.token)
        localStorage.setItem("user_role", data.role)
        localStorage.setItem("username", data.username)

        setStep("success")
        // Redirect to the app after a short delay
        setTimeout(() => {
          router.push("/dashboard")
        }, 1000)
      } else {
        setPinAttempts((prev) => prev + 1)
        if (pinAttempts >= 2) {
          setError("Account locked. See supervisor")
        } else {
          setError(`Authentication failed. ${2 - pinAttempts} attempts remaining`)
        }
      }
    } catch (error) {
      setError("Connection lost. Try again")
    }
  }

  return (
    <main className="flex min-h-screen flex-col items-center justify-center p-4 md:p-6 bg-white">
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
              <br />
              <span className="text-xs text-muted-foreground">
                Test PINs: 1234 (User), 5678 (Admin), 9999 (FlowCreator)
              </span>
            </CardDescription>
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
              <PinEntry onComplete={handlePinSubmit} error={!!error} />

              {error && <div className="mt-4 text-[#dc2626] text-sm flex items-center justify-center">{error}</div>}

              <div className="mt-8 w-full flex justify-between">
                <Button variant="ghost" className="h-14 flex items-center gap-2" onClick={() => setStep("user")}>
                  <ArrowLeft className="h-5 w-5" />
                  <span>Back</span>
                </Button>

                <Button variant="outline" className="h-14 bg-transparent">
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
    </main>
  )
}
