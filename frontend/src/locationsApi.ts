import type { CreateLocationInput, Location } from './types'

export type ApiValidationError = {
  title?: string
  errors: Record<string, string[]>
}

export async function getLocations(): Promise<Location[]> {
  const response = await fetch('/api/locations')

  if (!response.ok) {
    throw new Error('Failed to load locations.')
  }

  return response.json() as Promise<Location[]>
}

export async function createLocation(input: CreateLocationInput): Promise<Location> {
  const response = await fetch('/api/locations', {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
    },
    body: JSON.stringify({
      id: input.id.trim(),
      name: input.name.trim(),
    }),
  })

  if (response.ok) {
    return response.json() as Promise<Location>
  }

  if (response.status === 400) {
    throw (await response.json()) as ApiValidationError
  }

  throw new Error('Failed to create location.')
}