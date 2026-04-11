import type { CreateStoreInput, Store } from './types'

export type ApiValidationError = {
  title?: string
  errors: Record<string, string[]>
}

export async function getStores(): Promise<Store[]> {
  const response = await fetch('/api/stores')

  if (!response.ok) {
    throw new Error('Failed to load stores.')
  }

  return response.json() as Promise<Store[]>
}

export async function createStore(input: CreateStoreInput): Promise<Store> {
  const response = await fetch('/api/stores', {
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
    return response.json() as Promise<Store>
  }

  if (response.status === 400) {
    throw (await response.json()) as ApiValidationError
  }

  throw new Error('Failed to create store.')
}