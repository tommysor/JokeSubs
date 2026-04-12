import { useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useParams } from 'react-router-dom'
import { addStoreGroup, getStoreById, StoreNotFoundError, type ApiValidationError } from './storesApi'

function StoreDetailPage() {
  const { storeId } = useParams<{ storeId: string }>()
  const queryClient = useQueryClient()
  const [groupName, setGroupName] = useState('')
  const [groupNameError, setGroupNameError] = useState<string | null>(null)
  const [submitError, setSubmitError] = useState<string | null>(null)

  const { data: store, isPending, error } = useQuery({
    queryKey: ['store', storeId],
    queryFn: () => getStoreById(storeId!),
    enabled: !!storeId,
    retry: (_, err) => !(err instanceof StoreNotFoundError),
  })

  const addGroupMutation = useMutation({
    mutationFn: async (inputName: string) => addStoreGroup(storeId!, { name: inputName }),
    onSuccess: (updatedStore) => {
      queryClient.setQueryData(['store', storeId], updatedStore)
      setGroupName('')
      setGroupNameError(null)
      setSubmitError(null)
    },
  })

  const isMissing = !storeId || error instanceof StoreNotFoundError
  const isError = !!error && !isMissing

  async function handleAddGroup(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault()

    const trimmedName = groupName.trim()
    if (trimmedName.length === 0) {
      setGroupNameError('Name is required.')
      setSubmitError('Please correct the highlighted fields.')
      return
    }

    setGroupNameError(null)
    setSubmitError(null)

    try {
      await addGroupMutation.mutateAsync(trimmedName)
    } catch (mutationError) {
      if (isApiValidationError(mutationError)) {
        setGroupNameError(mutationError.errors.Name?.[0] ?? null)
        setSubmitError(mutationError.title ?? 'Please correct the highlighted fields.')
      } else if (mutationError instanceof StoreNotFoundError) {
        setSubmitError('Store not found.')
      } else {
        setSubmitError('Unable to add the group right now.')
      }
    }
  }

  return (
    <>
      <header className="store-page-header">
        <div className="store-page-identity" aria-live="polite">
          <p className="store-page-kicker">Store</p>
          <h1 className="store-page-name">{getStoreName(isPending, isMissing, isError, store?.name)}</h1>
          <p className="store-page-id">{store?.id ?? (isMissing ? 'Unknown store' : storeId)}</p>
        </div>
      </header>

      <main className="store-page-content">
        <section className="panel" aria-labelledby="groups-heading">
          <div className="panel-header">
            <div>
              <p className="panel-kicker">Administration</p>
              <h2 id="groups-heading" className="panel-title">Groups</h2>
            </div>
          </div>

          {isMissing ? (
            <p className="empty-state-copy">This store does not exist, so groups cannot be managed.</p>
          ) : isError ? (
            <p className="banner banner-error">Unable to load groups right now.</p>
          ) : isPending ? (
            <p className="empty-state-copy" role="status" aria-live="polite">Loading groups...</p>
          ) : store && store.groups.length > 0 ? (
            <ul className="group-list" aria-label="Groups">
              {store.groups.map((group) => (
                <li key={group.name} className="group-pill">{group.name}</li>
              ))}
            </ul>
          ) : (
            <p className="empty-state-copy">No groups yet. Add one to begin organizing subscriptions.</p>
          )}

          <form className="store-form group-form" onSubmit={handleAddGroup} noValidate>
            <label className="field" htmlFor="group-name-input">
              <span className="field-label">Group name</span>
              <input
                id="group-name-input"
                className="field-input"
                name="groupName"
                value={groupName}
                onChange={(event) => {
                  setGroupName(event.target.value)
                  setGroupNameError(null)
                  setSubmitError(null)
                }}
                placeholder="Monthly subscribers"
                autoComplete="off"
                disabled={!store || isPending || isMissing || isError}
                aria-invalid={groupNameError ? 'true' : 'false'}
                aria-describedby={groupNameError ? 'group-name-error' : undefined}
              />
              {groupNameError ? <span id="group-name-error" className="field-error">{groupNameError}</span> : null}
            </label>

            {submitError ? <p className="banner banner-error">{submitError}</p> : null}

            <button
              className="primary-button"
              type="submit"
              disabled={!store || isPending || isMissing || isError || addGroupMutation.isPending}
            >
              {addGroupMutation.isPending ? 'Adding...' : 'Add group'}
            </button>
          </form>
        </section>
      </main>
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

function isApiValidationError(error: unknown): error is ApiValidationError {
  return typeof error === 'object' && error !== null && 'errors' in error
}

export default StoreDetailPage