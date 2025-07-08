"use client"

import { useState, useEffect } from "react"
import { Button } from "@/components/ui/button"

interface PinEntryProps {
  onComplete: (pin: string) => void
  error?: boolean
}

export function PinEntry({ onComplete, error = false }: PinEntryProps) {
  const [pin, setPin] = useState<string>("")
  const pinLength = 4

  // Handle pin entry
  const handlePinDigit = (digit: string) => {
    if (pin.length < pinLength) {
      const newPin = pin + digit
      setPin(newPin)

      if (newPin.length === pinLength) {
        onComplete(newPin)
      }
    }
  }

  // Handle delete
  const handleDelete = () => {
    if (pin.length > 0) {
      setPin(pin.slice(0, -1))
    }
  }

  // Handle clear
  const handleClear = () => {
    setPin("")
  }

  // Apply shake animation when error occurs
  useEffect(() => {
    if (error) {
      const timer = setTimeout(() => {
        setPin("")
      }, 300)
      return () => clearTimeout(timer)
    }
  }, [error])

  return (
    <div className="w-full">
      {/* PIN display dots */}
      <div className={`flex justify-center mb-8 ${error ? "animate-shake" : ""}`}>
        <div className="flex gap-2">
          {Array.from({ length: pinLength }).map((_, index) => (
            <div
              key={index}
              className={`w-4 h-4 rounded-full ${
                index < pin.length
                  ? error
                    ? "bg-[#dc2626]"
                    : "bg-[#1e4e8c]"
                  : "bg-transparent border-2 border-gray-300"
              }`}
              aria-label={`PIN digit ${index + 1} of ${pinLength}, ${index < pin.length ? "filled" : "empty"}`}
            />
          ))}
        </div>
      </div>

      {/* PIN pad */}
      <div className="grid grid-cols-3 gap-4">
        {[1, 2, 3, 4, 5, 6, 7, 8, 9].map((digit) => (
          <Button
            key={digit}
            type="button"
            variant="outline"
            className="h-[72px] w-full text-2xl font-medium bg-transparent"
            onClick={() => handlePinDigit(digit.toString())}
          >
            {digit}
          </Button>
        ))}
        <Button
          type="button"
          variant="outline"
          className="h-[72px] w-full text-sm font-medium bg-transparent"
          onClick={handleClear}
        >
          Clear
        </Button>
        <Button
          type="button"
          variant="outline"
          className="h-[72px] w-full text-2xl font-medium bg-transparent"
          onClick={() => handlePinDigit("0")}
        >
          0
        </Button>
        <Button
          type="button"
          variant="outline"
          className="h-[72px] w-full text-sm font-medium bg-transparent"
          onClick={handleDelete}
        >
          Del
        </Button>
      </div>
    </div>
  )
}
