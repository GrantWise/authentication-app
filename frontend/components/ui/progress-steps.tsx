/**
 * Progress Steps Component
 * Shows progress through multi-step authentication flows
 */

import * as React from "react"
import { Check } from "lucide-react"
import { cn } from "@/lib/utils"

interface Step {
  id: string
  title: string
  description?: string
}

interface ProgressStepsProps {
  steps: Step[]
  currentStepId: string
  className?: string
  size?: "sm" | "md" | "lg"
}

export const ProgressSteps = ({ 
  steps, 
  currentStepId, 
  className,
  size = "md" 
}: ProgressStepsProps) => {
  const currentIndex = steps.findIndex(step => step.id === currentStepId)
  
  const sizeClasses = {
    sm: {
      circle: "h-6 w-6 text-xs",
      line: "h-0.5",
      title: "text-xs",
      description: "text-xs"
    },
    md: {
      circle: "h-8 w-8 text-sm",
      line: "h-1",
      title: "text-sm",
      description: "text-xs"
    },
    lg: {
      circle: "h-10 w-10 text-base",
      line: "h-1",
      title: "text-base",
      description: "text-sm"
    }
  }

  const classes = sizeClasses[size]

  return (
    <div className={cn("w-full", className)}>
      <nav aria-label="Progress">
        <ol className="flex items-center">
          {steps.map((step, index) => {
            const isCompleted = index < currentIndex
            const isCurrent = index === currentIndex
            const isUpcoming = index > currentIndex
            
            return (
              <li key={step.id} className={cn("flex items-center", index < steps.length - 1 && "flex-1")}>
                <div className="flex flex-col items-center">
                  {/* Step Circle */}
                  <div
                    className={cn(
                      "flex items-center justify-center rounded-full border-2 font-medium",
                      classes.circle,
                      {
                        "border-green-500 bg-green-500 text-white": isCompleted,
                        "border-blue-500 bg-blue-500 text-white": isCurrent,
                        "border-gray-300 bg-white text-gray-400": isUpcoming,
                      }
                    )}
                  >
                    {isCompleted ? (
                      <Check className="h-4 w-4" />
                    ) : (
                      <span>{index + 1}</span>
                    )}
                  </div>
                  
                  {/* Step Text */}
                  <div className="mt-2 text-center">
                    <p
                      className={cn(
                        "font-medium",
                        classes.title,
                        {
                          "text-green-600": isCompleted,
                          "text-blue-600": isCurrent,
                          "text-gray-400": isUpcoming,
                        }
                      )}
                    >
                      {step.title}
                    </p>
                    {step.description && (
                      <p
                        className={cn(
                          "text-muted-foreground",
                          classes.description,
                          {
                            "text-green-500": isCompleted,
                            "text-blue-500": isCurrent,
                            "text-gray-400": isUpcoming,
                          }
                        )}
                      >
                        {step.description}
                      </p>
                    )}
                  </div>
                </div>
                
                {/* Connecting Line */}
                {index < steps.length - 1 && (
                  <div className={cn("flex-1 mx-4", classes.line)}>
                    <div
                      className={cn(
                        "h-full rounded-full",
                        {
                          "bg-green-500": index < currentIndex,
                          "bg-gray-300": index >= currentIndex,
                        }
                      )}
                    />
                  </div>
                )}
              </li>
            )
          })}
        </ol>
      </nav>
    </div>
  )
}

interface CompactProgressStepsProps {
  steps: Step[]
  currentStepId: string
  className?: string
}

export const CompactProgressSteps = ({ 
  steps, 
  currentStepId, 
  className 
}: CompactProgressStepsProps) => {
  const currentIndex = steps.findIndex(step => step.id === currentStepId)
  const currentStep = steps[currentIndex]
  const progress = ((currentIndex + 1) / steps.length) * 100

  return (
    <div className={cn("w-full space-y-2", className)}>
      {/* Progress Bar */}
      <div className="w-full bg-gray-200 rounded-full h-2">
        <div
          className="bg-blue-500 h-2 rounded-full transition-all duration-300 ease-in-out"
          style={{ width: `${progress}%` }}
        />
      </div>
      
      {/* Step Info */}
      <div className="flex justify-between items-center text-sm">
        <span className="text-muted-foreground">
          Step {currentIndex + 1} of {steps.length}
        </span>
        <span className="font-medium text-blue-600">
          {currentStep?.title}
        </span>
      </div>
    </div>
  )
}

interface CircularProgressProps {
  steps: Step[]
  currentStepId: string
  className?: string
  size?: number
}

export const CircularProgress = ({ 
  steps, 
  currentStepId, 
  className,
  size = 80 
}: CircularProgressProps) => {
  const currentIndex = steps.findIndex(step => step.id === currentStepId)
  const progress = ((currentIndex + 1) / steps.length) * 100
  const circumference = 2 * Math.PI * (size / 2 - 8)
  const strokeDasharray = circumference
  const strokeDashoffset = circumference - (progress / 100) * circumference

  return (
    <div className={cn("flex flex-col items-center", className)}>
      <div className="relative">
        <svg width={size} height={size} className="transform -rotate-90">
          {/* Background circle */}
          <circle
            cx={size / 2}
            cy={size / 2}
            r={size / 2 - 8}
            stroke="currentColor"
            strokeWidth="4"
            fill="transparent"
            className="text-gray-200"
          />
          {/* Progress circle */}
          <circle
            cx={size / 2}
            cy={size / 2}
            r={size / 2 - 8}
            stroke="currentColor"
            strokeWidth="4"
            fill="transparent"
            strokeDasharray={strokeDasharray}
            strokeDashoffset={strokeDashoffset}
            className="text-blue-500 transition-all duration-500 ease-in-out"
            strokeLinecap="round"
          />
        </svg>
        
        {/* Center content */}
        <div className="absolute inset-0 flex items-center justify-center">
          <div className="text-center">
            <div className="text-lg font-bold text-blue-600">
              {currentIndex + 1}
            </div>
            <div className="text-xs text-muted-foreground">
              of {steps.length}
            </div>
          </div>
        </div>
      </div>
      
      <div className="mt-2 text-center">
        <p className="text-sm font-medium text-blue-600">
          {steps[currentIndex]?.title}
        </p>
      </div>
    </div>
  )
}