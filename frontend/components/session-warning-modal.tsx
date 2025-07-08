"use client"

import { useState, useEffect } from "react"
import { useRouter } from "next/navigation"
import { Button } from "@/components/ui/button"
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog"

interface SessionWarningModalProps {
  onClose: () => void
}

export function SessionWarningModal({ onClose }: SessionWarningModalProps) {
  const router = useRouter()
  const [open, setOpen] = useState(true)
  const [countdown, setCountdown] = useState(300) // 5 minutes in seconds

  useEffect(() => {
    if (open && countdown > 0) {
      const timer = setTimeout(() => {
        setCountdown(countdown - 1)
      }, 1000)
      return () => clearTimeout(timer)
    } else if (countdown === 0) {
      // Session expired, redirect to login
      localStorage.removeItem("jwt_token")
      localStorage.removeItem("username")
      localStorage.removeItem("user_role")
      router.push("/")
    }
  }, [countdown, open, router])

  const formatTime = (seconds: number) => {
    const mins = Math.floor(seconds / 60)
    const secs = seconds % 60
    return `${mins}:${secs.toString().padStart(2, "0")}`
  }

  const handleContinue = async () => {
    // In a real app, this would call the backend to extend the session
    try {
      const token = localStorage.getItem("jwt_token")
      const response = await fetch("http://localhost:5097/api/auth/refresh", {
        method: "POST",
        headers: {
          Authorization: `Bearer ${token}`,
          "Content-Type": "application/json",
        },
      })

      if (response.ok) {
        const data = await response.json()
        localStorage.setItem("jwt_token", data.token)
        setOpen(false)
        onClose()
      } else {
        // If refresh fails, sign out
        handleSignOut()
      }
    } catch (error) {
      // If there's an error, just close the modal for now
      setOpen(false)
      onClose()
    }
  }

  const handleSignOut = () => {
    localStorage.removeItem("jwt_token")
    localStorage.removeItem("username")
    localStorage.removeItem("user_role")
    router.push("/")
  }

  const handleOpenChange = (newOpen: boolean) => {
    setOpen(newOpen)
    if (!newOpen) {
      onClose()
    }
  }

  return (
    <Dialog open={open} onOpenChange={handleOpenChange}>
      <DialogContent className="sm:max-w-md">
        <DialogHeader>
          <DialogTitle>Session Expiring Soon</DialogTitle>
          <DialogDescription>
            Your session will expire in {formatTime(countdown)}. Would you like to continue working?
          </DialogDescription>
        </DialogHeader>
        <DialogFooter className="flex flex-col sm:flex-row sm:justify-between gap-2">
          <Button variant="outline" onClick={handleSignOut}>
            Sign Out
          </Button>
          <Button onClick={handleContinue}>Continue Working</Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}
