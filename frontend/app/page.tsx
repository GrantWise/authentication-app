import Link from "next/link"
import { Button } from "@/components/ui/button"
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card"

export default function Home() {
  return (
    <main className="flex min-h-screen flex-col items-center justify-center p-4 md:p-24">
      <Card className="w-full max-w-md">
        <CardHeader className="text-center">
          <CardTitle className="text-3xl">TransLution Authentication</CardTitle>
          <CardDescription>Choose your authentication method</CardDescription>
        </CardHeader>
        <CardContent className="flex flex-col gap-4">
          <Link href="/mobile-auth" className="w-full">
            <Button className="w-full h-14 text-lg" variant="default">
              Mobile Authentication
            </Button>
          </Link>
          <Link href="/desktop-auth" className="w-full">
            <Button className="w-full h-14 text-lg bg-transparent" variant="outline">
              Desktop Authentication
            </Button>
          </Link>
        </CardContent>
      </Card>
    </main>
  )
}
