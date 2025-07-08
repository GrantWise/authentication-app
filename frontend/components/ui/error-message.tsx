/**
 * Enhanced Error Message Component
 * Provides detailed error messages with recovery suggestions and actionable guidance
 */

import * as React from "react"
import { AlertTriangle, RefreshCw, HelpCircle, Mail, Phone, Shield } from "lucide-react"
import { Button } from "@/components/ui/button"
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card"
import { cn } from "@/lib/utils"

interface ErrorAction {
  label: string
  onClick: () => void
  variant?: "default" | "outline" | "ghost"
  icon?: React.ReactNode
}

interface ErrorMessageProps {
  title?: string
  message: string
  type?: "error" | "warning" | "network" | "validation" | "authentication" | "authorization"
  actions?: ErrorAction[]
  className?: string
  showIcon?: boolean
  suggestion?: string
  technicalDetails?: string
}

const getErrorConfig = (type: string) => {
  switch (type) {
    case "network":
      return {
        icon: <RefreshCw className="h-5 w-5" />,
        title: "Connection Problem",
        suggestion: "Check your internet connection and try again. If the problem persists, contact support.",
        color: "text-orange-600",
        bgColor: "bg-orange-50",
        borderColor: "border-orange-200"
      }
    case "authentication":
      return {
        icon: <Shield className="h-5 w-5" />,
        title: "Authentication Failed",
        suggestion: "Please check your credentials and try again. If you forgot your password, use the reset link.",
        color: "text-red-600",
        bgColor: "bg-red-50",
        borderColor: "border-red-200"
      }
    case "authorization":
      return {
        icon: <Shield className="h-5 w-5" />,
        title: "Access Denied",
        suggestion: "You don't have permission to access this resource. Contact your administrator for help.",
        color: "text-yellow-600",
        bgColor: "bg-yellow-50",
        borderColor: "border-yellow-200"
      }
    case "validation":
      return {
        icon: <HelpCircle className="h-5 w-5" />,
        title: "Invalid Input",
        suggestion: "Please check the highlighted fields and correct any errors before continuing.",
        color: "text-blue-600",
        bgColor: "bg-blue-50",
        borderColor: "border-blue-200"
      }
    case "warning":
      return {
        icon: <AlertTriangle className="h-5 w-5" />,
        title: "Warning",
        suggestion: "Please review the information below and take appropriate action.",
        color: "text-yellow-600",
        bgColor: "bg-yellow-50",
        borderColor: "border-yellow-200"
      }
    default:
      return {
        icon: <AlertTriangle className="h-5 w-5" />,
        title: "Error",
        suggestion: "An unexpected error occurred. Please try again or contact support if the issue persists.",
        color: "text-red-600",
        bgColor: "bg-red-50",
        borderColor: "border-red-200"
      }
  }
}

export const ErrorMessage = ({
  title,
  message,
  type = "error",
  actions = [],
  className,
  showIcon = true,
  suggestion,
  technicalDetails
}: ErrorMessageProps) => {
  const config = getErrorConfig(type)
  const displayTitle = title || config.title
  const displaySuggestion = suggestion || config.suggestion

  return (
    <Card className={cn(
      "border-l-4",
      config.borderColor,
      config.bgColor,
      className
    )}>
      <CardHeader className="pb-3">
        <CardTitle className={cn("flex items-center gap-2 text-base", config.color)}>
          {showIcon && config.icon}
          {displayTitle}
        </CardTitle>
        <CardDescription className="text-gray-700">
          {message}
        </CardDescription>
      </CardHeader>
      
      {(displaySuggestion || actions.length > 0 || technicalDetails) && (
        <CardContent className="pt-0">
          {displaySuggestion && (
            <div className="mb-4">
              <p className="text-sm text-gray-600 font-medium mb-1">What to do next:</p>
              <p className="text-sm text-gray-600">{displaySuggestion}</p>
            </div>
          )}

          {actions.length > 0 && (
            <div className="flex flex-wrap gap-2 mb-4">
              {actions.map((action, index) => (
                <Button
                  key={index}
                  variant={action.variant || "outline"}
                  size="sm"
                  onClick={action.onClick}
                  className="flex items-center gap-1"
                >
                  {action.icon}
                  {action.label}
                </Button>
              ))}
            </div>
          )}

          {technicalDetails && (
            <details className="text-xs text-gray-500">
              <summary className="cursor-pointer hover:text-gray-700">
                Technical Details
              </summary>
              <pre className="mt-2 p-2 bg-gray-100 rounded text-xs overflow-x-auto">
                {technicalDetails}
              </pre>
            </details>
          )}
        </CardContent>
      )}
    </Card>
  )
}

/**
 * Inline Error Message for forms
 */
interface InlineErrorProps {
  message: string
  suggestion?: string
  className?: string
}

export const InlineError = ({ message, suggestion, className }: InlineErrorProps) => {
  return (
    <div className={cn("text-red-600 text-sm space-y-1", className)}>
      <p className="flex items-center gap-1">
        <AlertTriangle className="h-3 w-3" />
        {message}
      </p>
      {suggestion && (
        <p className="text-xs text-gray-600 ml-4">
          💡 {suggestion}
        </p>
      )}
    </div>
  )
}

/**
 * Common error recovery suggestions
 */
export const getRecoverySuggestion = (errorType: string, context?: string): string => {
  switch (errorType) {
    case "invalid_credentials":
      return "Double-check your username and password. Make sure Caps Lock is off. If you still can't sign in, try resetting your password."
    
    case "account_locked":
      return "Your account has been temporarily locked for security. Wait 30 minutes and try again, or contact your supervisor for immediate assistance."
    
    case "network_error":
      return "Check your internet connection. Try refreshing the page or switching to a different network if available."
    
    case "session_expired":
      return "Your session has expired for security reasons. Please sign in again to continue."
    
    case "invalid_pin":
      return "Make sure you're entering the correct 4-digit PIN. If you've forgotten it, contact your supervisor to reset it."
    
    case "device_not_found":
      return "The selected device may be offline or unavailable. Try selecting a different device or contact IT support."
    
    case "validation_error":
      return "Please check all required fields are filled correctly. Look for any highlighted errors above."
    
    case "rate_limited":
      return "Too many attempts detected. Please wait a few minutes before trying again to ensure account security."
    
    case "email_not_found":
      return "If you entered the correct email, check your spam folder. The email should arrive within a few minutes."
    
    case "server_error":
      return "This is a temporary server issue. Please wait a moment and try again. If it continues, contact support."
    
    default:
      return "If this problem continues, please contact support with details about what you were trying to do."
  }
}

/**
 * Create error actions based on error type
 */
export const getErrorActions = (
  errorType: string, 
  onRetry?: () => void,
  onContactSupport?: () => void,
  onGoBack?: () => void
): ErrorAction[] => {
  const actions: ErrorAction[] = []

  // Common retry action
  if (onRetry && ["network_error", "server_error", "timeout"].includes(errorType)) {
    actions.push({
      label: "Try Again",
      onClick: onRetry,
      variant: "default",
      icon: <RefreshCw className="h-3 w-3" />
    })
  }

  // Go back action for certain errors
  if (onGoBack && ["authorization", "invalid_access"].includes(errorType)) {
    actions.push({
      label: "Go Back",
      onClick: onGoBack,
      variant: "outline"
    })
  }

  // Contact support for serious errors
  if (onContactSupport && ["account_locked", "server_error", "unknown"].includes(errorType)) {
    actions.push({
      label: "Contact Support",
      onClick: onContactSupport,
      variant: "outline",
      icon: <Mail className="h-3 w-3" />
    })
  }

  return actions
}