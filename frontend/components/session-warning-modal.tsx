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
import { useAuthStore } from "@/lib/stores/auth-store"
import { getTokenTimeRemaining } from "@/utils/auth"

interface SessionWarningModalProps {
  isOpen: boolean
  onClose: () => void
}

export function SessionWarningModal({ isOpen, onClose }: SessionWarningModalProps) {
  const router = useRouter()
  const { accessToken, refresh, logout } = useAuthStore()
  const [countdown, setCountdown] = useState(300) // 5 minutes in seconds

  useEffect(() => {
    if (isOpen && accessToken) {
      // Initialize countdown with actual token time remaining
      const timeRemaining = getTokenTimeRemaining(accessToken)
      setCountdown(timeRemaining > 0 ? timeRemaining : 300)
    }
  }, [isOpen, accessToken])

  useEffect(() => {
    if (isOpen && countdown > 0) {
      const timer = setTimeout(() => {
        setCountdown(countdown - 1)
      }, 1000)
      return () => clearTimeout(timer)
    } else if (countdown === 0) {
      // Session expired, logout using store
      handleSignOut()
    }
  }, [countdown, isOpen])

  const formatTime = (seconds: number) => {
    const mins = Math.floor(seconds / 60)
    const secs = seconds % 60
    return `${mins}:${secs.toString().padStart(2, "0")}`
  }

  const handleContinue = async () => {
    try {
      await refresh()
      onClose()
    } catch (error) {
      // If refresh fails, sign out
      handleSignOut()
    }
  }

  const handleSignOut = async () => {
    await logout()
    router.push("/")
    onClose()
  }

  const handleOpenChange = (newOpen: boolean) => {
    if (!newOpen) {
      onClose()
    }
  }

  return (
    <Dialog open={isOpen} onOpenChange={handleOpenChange}>
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
