import { useEffect, useState } from 'react'
import { useParams } from 'react-router-dom'
import { getStoreById, StoreNotFoundError } from './storesApi'
import type { Store } from './types'

type StoreDetailState =
  | { status: 'loading' }
  | { status: 'ready'; store: Store }
  | { status: 'missing' }
  | { status: 'error' }

function StoreDetailPage() {
  const { storeId } = useParams<{ storeId: string }>()
  const [state, setState] = useState<StoreDetailState>(storeId ? { status: 'loading' } : { status: 'missing' })

  useEffect(() => {
    if (!storeId) {
      return
    }

    const routeStoreId = storeId

    let isDisposed = false

    async function loadStore() {
      setState({ status: 'loading' })

      try {
        const store = await getStoreById(routeStoreId)

        if (!isDisposed) {
          setState({ status: 'ready', store })
        }
      } catch (error) {
        if (isDisposed) {
          return
        }

        if (error instanceof StoreNotFoundError) {
          setState({ status: 'missing' })
          return
        }

        setState({ status: 'error' })
      }
    }

    void loadStore()

    return () => {
      isDisposed = true
    }
  }, [storeId])

  return (
    <>
      <header className="store-page-header">
        <div className="store-page-identity" aria-live="polite">
          <p className="store-page-kicker">Store</p>
          <h1 className="store-page-name">{getStoreName(state)}</h1>
          <p className="store-page-id">{getStoreId(state, storeId)}</p>
        </div>
      </header>

      <main className="store-page-content" />
    </>
  )
}

function getStoreName(state: StoreDetailState) {
  switch (state.status) {
    case 'ready':
      return state.store.name
    case 'missing':
      return 'Store not found'
    case 'error':
      return 'Unable to load store'
    default:
      return 'Loading store...'
  }
}

function getStoreId(state: StoreDetailState, routeStoreId?: string) {
  if (state.status === 'ready') {
    return state.store.id
  }

  if (!routeStoreId) {
    return 'Unknown store'
  }

  return routeStoreId
}

export default StoreDetailPage