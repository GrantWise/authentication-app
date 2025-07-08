/**
 * Zod validation schemas for authentication forms
 */

import { z } from 'zod'

// Common validation patterns
const passwordRequirements = z
  .string()
  .min(8, 'Password must be at least 8 characters')
  .regex(/[A-Z]/, 'Password must contain at least one uppercase letter')
  .regex(/[a-z]/, 'Password must contain at least one lowercase letter')
  .regex(/\d/, 'Password must contain at least one number')
  .regex(/[!@#$%^&*]/, 'Password must contain at least one special character (!@#$%^&*)')

const emailValidation = z
  .string()
  .min(1, 'Email is required')
  .email('Please enter a valid email address')

const usernameValidation = z
  .string()
  .min(1, 'Username is required')
  .min(3, 'Username must be at least 3 characters')
  .max(50, 'Username must be less than 50 characters')
  .regex(/^[a-zA-Z0-9_.-]+$/, 'Username can only contain letters, numbers, dots, hyphens, and underscores')

const pinValidation = z
  .string()
  .min(4, 'PIN must be exactly 4 digits')
  .max(4, 'PIN must be exactly 4 digits')
  .regex(/^\d{4}$/, 'PIN must be exactly 4 digits')

// Desktop Login Form Schema
export const desktopLoginSchema = z.object({
  username: usernameValidation,
  password: z.string().min(1, 'Password is required'),
  rememberMe: z.boolean().optional().default(false),
})

export type DesktopLoginFormData = z.infer<typeof desktopLoginSchema>

// Mobile PIN Login Form Schema
export const mobilePinSchema = z.object({
  pin: pinValidation,
  deviceId: z.string().min(1, 'Device ID is required'),
})

export type MobilePinFormData = z.infer<typeof mobilePinSchema>

// Device Selection Schema
export const deviceSelectionSchema = z.object({
  deviceId: z.string().min(1, 'Please select a device'),
})

export type DeviceSelectionFormData = z.infer<typeof deviceSelectionSchema>

// User Selection Schema
export const userSelectionSchema = z.object({
  userId: z.string().min(1, 'Please select a user'),
})

export type UserSelectionFormData = z.infer<typeof userSelectionSchema>

// Forgot Password Form Schema
export const forgotPasswordSchema = z.object({
  email: emailValidation,
})

export type ForgotPasswordFormData = z.infer<typeof forgotPasswordSchema>

// Reset Password Form Schema
export const resetPasswordSchema = z.object({
  token: z.string().min(1, 'Reset token is required'),
  password: passwordRequirements,
  confirmPassword: z.string().min(1, 'Please confirm your password'),
}).refine((data) => data.password === data.confirmPassword, {
  message: "Passwords don't match",
  path: ['confirmPassword'],
})

export type ResetPasswordFormData = z.infer<typeof resetPasswordSchema>

// Registration Form Schema
export const registrationSchema = z.object({
  username: usernameValidation,
  email: emailValidation,
  password: passwordRequirements,
  confirmPassword: z.string().min(1, 'Please confirm your password'),
}).refine((data) => data.password === data.confirmPassword, {
  message: "Passwords don't match",
  path: ['confirmPassword'],
})

export type RegistrationFormData = z.infer<typeof registrationSchema>

// Change Password Form Schema
export const changePasswordSchema = z.object({
  currentPassword: z.string().min(1, 'Current password is required'),
  newPassword: passwordRequirements,
  confirmNewPassword: z.string().min(1, 'Please confirm your new password'),
}).refine((data) => data.newPassword === data.confirmNewPassword, {
  message: "New passwords don't match",
  path: ['confirmNewPassword'],
}).refine((data) => data.currentPassword !== data.newPassword, {
  message: "New password must be different from current password",
  path: ['newPassword'],
})

export type ChangePasswordFormData = z.infer<typeof changePasswordSchema>

// MFA Setup Form Schema
export const mfaSetupSchema = z.object({
  method: z.enum(['totp', 'sms', 'email'], {
    required_error: 'Please select an MFA method',
  }),
  phoneNumber: z.string().optional(),
  email: z.string().email().optional(),
  code: z.string().min(6, 'Verification code must be at least 6 characters').optional(),
}).superRefine((data, ctx) => {
  if (data.method === 'sms' && !data.phoneNumber) {
    ctx.addIssue({
      code: z.ZodIssueCode.custom,
      message: 'Phone number is required for SMS verification',
      path: ['phoneNumber'],
    })
  }
  
  if (data.method === 'email' && !data.email) {
    ctx.addIssue({
      code: z.ZodIssueCode.custom,
      message: 'Email is required for email verification',
      path: ['email'],
    })
  }
})

export type MfaSetupFormData = z.infer<typeof mfaSetupSchema>

// MFA Verification Form Schema
export const mfaVerificationSchema = z.object({
  code: z.string()
    .min(6, 'Verification code must be at least 6 characters')
    .max(8, 'Verification code must be no more than 8 characters')
    .regex(/^\d+$/, 'Verification code must contain only numbers'),
})

export type MfaVerificationFormData = z.infer<typeof mfaVerificationSchema>

// Profile Update Form Schema
export const profileUpdateSchema = z.object({
  username: usernameValidation.optional(),
  email: emailValidation.optional(),
  currentPassword: z.string().min(1, 'Current password is required for profile updates'),
})

export type ProfileUpdateFormData = z.infer<typeof profileUpdateSchema>

// Session Management Form Schema
export const sessionManagementSchema = z.object({
  sessionIds: z.array(z.string()).min(1, 'Please select at least one session'),
  action: z.enum(['terminate', 'extend'], {
    required_error: 'Please select an action',
  }),
})

export type SessionManagementFormData = z.infer<typeof sessionManagementSchema>

// Form validation helpers
export const validateForm = <T>(schema: z.ZodSchema<T>, data: unknown): { success: boolean; data?: T; errors?: z.ZodFormattedError<T> } => {
  const result = schema.safeParse(data)
  
  if (result.success) {
    return { success: true, data: result.data }
  } else {
    return { 
      success: false, 
      errors: result.error.format() 
    }
  }
}

// Custom validation functions for specific use cases
export const validatePasswordStrength = (password: string): { score: number; feedback: string[] } => {
  const feedback: string[] = []
  let score = 0
  
  if (password.length >= 8) score += 1
  else feedback.push('Use at least 8 characters')
  
  if (/[A-Z]/.test(password)) score += 1
  else feedback.push('Add uppercase letters')
  
  if (/[a-z]/.test(password)) score += 1
  else feedback.push('Add lowercase letters')
  
  if (/\d/.test(password)) score += 1
  else feedback.push('Add numbers')
  
  if (/[!@#$%^&*]/.test(password)) score += 1
  else feedback.push('Add special characters (!@#$%^&*)')
  
  if (password.length >= 12) score += 1
  if (/[!@#$%^&*()_+\-=\[\]{};':"\\|,.<>\?]/.test(password)) score += 1
  
  return { score, feedback }
}

export const validatePinFormat = (pin: string): boolean => {
  return /^\d{4}$/.test(pin)
}

export const validateUsernameAvailability = async (username: string): Promise<boolean> => {
  // This would typically make an API call to check username availability
  // For now, return true as a placeholder
  return new Promise((resolve) => {
    setTimeout(() => resolve(true), 500)
  })
}

export const validateEmailAvailability = async (email: string): Promise<boolean> => {
  // This would typically make an API call to check email availability
  // For now, return true as a placeholder
  return new Promise((resolve) => {
    setTimeout(() => resolve(true), 500)
  })
}