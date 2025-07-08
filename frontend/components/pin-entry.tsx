"use client"

import { useState, useEffect, forwardRef } from "react"
import { Button } from "@/components/ui/button"

interface PinEntryProps {
  onComplete: (pin: string) => void
  error?: boolean
  disabled?: boolean
  value?: string
  onChange?: (pin: string) => void
}

export function PinEntry({ 
  onComplete, 
  error = false, 
  disabled = false, 
  value, 
  onChange 
}: PinEntryProps) {
  const [pin, setPin] = useState<string>(value || "")
  const pinLength = 4

  // Update local state when value prop changes
  useEffect(() => {
    if (value !== undefined) {
      setPin(value)
    }
  }, [value])

  // Handle pin entry
  const handlePinDigit = (digit: string) => {
    if (disabled) return
    
    if (pin.length < pinLength) {
      const newPin = pin + digit
      setPin(newPin)
      onChange?.(newPin)

      if (newPin.length === pinLength) {
        onComplete(newPin)
      }
    }
  }

  // Handle delete
  const handleDelete = () => {
    if (disabled) return
    
    if (pin.length > 0) {
      const newPin = pin.slice(0, -1)
      setPin(newPin)
      onChange?.(newPin)
    }
  }

  // Handle clear
  const handleClear = () => {
    if (disabled) return
    
    setPin("")
    onChange?.("")
  }

  // Apply shake animation when error occurs
  useEffect(() => {
    if (error) {
      const timer = setTimeout(() => {
        setPin("")
        onChange?.("")
      }, 300)
      return () => clearTimeout(timer)
    }
  }, [error, onChange])

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
            disabled={disabled}
          >
            {digit}
          </Button>
        ))}
        <Button
          type="button"
          variant="outline"
          className="h-[72px] w-full text-sm font-medium bg-transparent"
          onClick={handleClear}
          disabled={disabled}
        >
          Clear
        </Button>
        <Button
          type="button"
          variant="outline"
          className="h-[72px] w-full text-2xl font-medium bg-transparent"
          onClick={() => handlePinDigit("0")}
          disabled={disabled}
        >
          0
        </Button>
        <Button
          type="button"
          variant="outline"
          className="h-[72px] w-full text-sm font-medium bg-transparent"
          onClick={handleDelete}
          disabled={disabled}
        >
          Del
        </Button>
      </div>
    </div>
  )
}
