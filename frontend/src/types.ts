export type StoreGroup = {
  name: string
}

export type Store = {
  id: string
  name: string
  groups: StoreGroup[]
}

export type CreateStoreInput = {
  id: string
  name: string
}

export type AddStoreGroupInput = {
  name: string
}