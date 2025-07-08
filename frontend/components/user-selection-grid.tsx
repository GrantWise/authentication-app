"use client"

import { useState } from "react"
import { Button } from "@/components/ui/button"

interface User {
  id: string
  name: string
  initials: string
}

interface UserSelectionGridProps {
  users: User[]
  onSelect: (userId: string) => void
}

export function UserSelectionGrid({ users, onSelect }: UserSelectionGridProps) {
  const [selectedUserId, setSelectedUserId] = useState<string | null>(null)

  const handleSelect = (userId: string) => {
    setSelectedUserId(userId)
    onSelect(userId)
  }

  return (
    <div className="grid grid-cols-3 gap-4 w-full">
      {users.map((user) => (
        <Button
          key={user.id}
          variant="outline"
          className={`h-[104px] w-full flex flex-col items-center justify-center p-2 ${
            selectedUserId === user.id ? "border-[#1e4e8c] border-4" : ""
          }`}
          onClick={() => handleSelect(user.id)}
        >
          <div className="bg-muted rounded-full w-12 h-12 flex items-center justify-center mb-2">
            <span className="text-lg font-medium">{user.initials}</span>
          </div>
          <span className="text-sm text-center line-clamp-1">{user.name}</span>
        </Button>
      ))}
    </div>
  )
}
