/**
 * Loading Spinner Components
 * Various loading indicators for different use cases
 */

import * as React from "react"
import { Loader2 } from "lucide-react"
import { cn } from "@/lib/utils"

interface LoadingSpinnerProps {
  size?: "sm" | "md" | "lg"
  className?: string
}

export const LoadingSpinner = ({ size = "md", className }: LoadingSpinnerProps) => {
  const sizeClasses = {
    sm: "h-4 w-4",
    md: "h-6 w-6", 
    lg: "h-8 w-8"
  }

  return (
    <Loader2 
      className={cn(
        "animate-spin text-muted-foreground",
        sizeClasses[size],
        className
      )} 
    />
  )
}

interface LoadingOverlayProps {
  isLoading: boolean
  children: React.ReactNode
  loadingText?: string
  className?: string
}

export const LoadingOverlay = ({ 
  isLoading, 
  children, 
  loadingText = "Loading...",
  className 
}: LoadingOverlayProps) => {
  return (
    <div className={cn("relative", className)}>
      {children}
      {isLoading && (
        <div className="absolute inset-0 bg-background/50 backdrop-blur-sm flex items-center justify-center z-10">
          <div className="flex flex-col items-center gap-2">
            <LoadingSpinner size="lg" />
            <p className="text-sm text-muted-foreground">{loadingText}</p>
          </div>
        </div>
      )}
    </div>
  )
}

interface LoadingCardProps {
  title?: string
  description?: string
  className?: string
}

export const LoadingCard = ({ 
  title = "Loading...", 
  description,
  className 
}: LoadingCardProps) => {
  return (
    <div className={cn(
      "flex flex-col items-center justify-center p-8 space-y-4",
      className
    )}>
      <LoadingSpinner size="lg" />
      <div className="text-center space-y-2">
        <h3 className="text-lg font-medium">{title}</h3>
        {description && (
          <p className="text-sm text-muted-foreground">{description}</p>
        )}
      </div>
    </div>
  )
}

interface LoadingDotsProps {
  className?: string
}

export const LoadingDots = ({ className }: LoadingDotsProps) => {
  return (
    <div className={cn("flex space-x-1", className)}>
      <div className="w-2 h-2 bg-current rounded-full animate-bounce [animation-delay:-0.3s]"></div>
      <div className="w-2 h-2 bg-current rounded-full animate-bounce [animation-delay:-0.15s]"></div>
      <div className="w-2 h-2 bg-current rounded-full animate-bounce"></div>
    </div>
  )
}

interface LoadingTextProps {
  text: string
  className?: string
}

export const LoadingText = ({ text, className }: LoadingTextProps) => {
  return (
    <div className={cn("flex items-center gap-2", className)}>
      <LoadingSpinner size="sm" />
      <span className="text-sm text-muted-foreground">{text}</span>
    </div>
  )
}