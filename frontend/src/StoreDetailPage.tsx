import { useParams } from 'react-router-dom'
import { useQuery } from '@tanstack/react-query'
import { getStoreById, StoreNotFoundError } from './storesApi'

function StoreDetailPage() {
  const { storeId } = useParams<{ storeId: string }>()

  const { data: store, isPending, error } = useQuery({
    queryKey: ['store', storeId],
    queryFn: () => getStoreById(storeId!),
    enabled: !!storeId,
    retry: (_, err) => !(err instanceof StoreNotFoundError),
  })

  const isMissing = !storeId || error instanceof StoreNotFoundError
  const isError = !!error && !isMissing

  return (
    <>
      <header className="store-page-header">
        <div className="store-page-identity" aria-live="polite">
          <p className="store-page-kicker">Store</p>
          <h1 className="store-page-name">{getStoreName(isPending, isMissing, isError, store?.name)}</h1>
          <p className="store-page-id">{store?.id ?? (isMissing ? 'Unknown store' : storeId)}</p>
        </div>
      </header>

      <main className="store-page-content" />
    </>
  )
}

function getStoreName(isPending: boolean, isMissing: boolean, isError: boolean, name?: string) {
  if (name) return name
  if (isMissing) return 'Store not found'
  if (isError) return 'Unable to load store'
  if (isPending) return 'Loading store...'
  return ''
}

export default StoreDetailPage