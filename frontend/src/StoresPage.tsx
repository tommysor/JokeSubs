import { useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Link } from 'react-router-dom'
import { createStore, getStores, type ApiValidationError } from './storesApi'
import type { CreateStoreInput, Store } from './types'

function StoresPage() {
  const queryClient = useQueryClient()
  const [formData, setFormData] = useState<CreateStoreInput>({ id: '', name: '' })
  const [fieldErrors, setFieldErrors] = useState<Partial<Record<keyof CreateStoreInput, string>>>({})
  const [submitError, setSubmitError] = useState<string | null>(null)

  const {
    data: stores = [],
    isPending: isLoading,
    isError: isLoadError,
    refetch,
  } = useQuery({
    queryKey: ['stores'],
    queryFn: getStores,
    select: sortStores,
  })

  const createStoreMutation = useMutation({
    mutationFn: createStore,
    onSuccess: (createdStore) => {
      queryClient.setQueryData<Store[]>(['stores'], (currentStores) => {
        const nextStores = currentStores ? [...currentStores, createdStore] : [createdStore]
        return sortStores(nextStores)
      })
    },
  })

  function handleChange(field: keyof CreateStoreInput, value: string) {
    setFormData((current) => ({ ...current, [field]: value }))
    setFieldErrors((current) => ({ ...current, [field]: undefined }))
    setSubmitError(null)
  }

  function validateForm(values: CreateStoreInput) {
    const errors: Partial<Record<keyof CreateStoreInput, string>> = {}

    if (values.id.trim().length === 0) {
      errors.id = 'Id is required.'
    } else if (values.id.trim().length < 4) {
      errors.id = 'Id must be at least 4 characters long.'
    }

    if (values.name.trim().length === 0) {
      errors.name = 'Name is required.'
    }

    return errors
  }

  async function handleSubmit(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault()

    const nextErrors = validateForm(formData)
    setFieldErrors(nextErrors)

    if (Object.keys(nextErrors).length > 0) {
      return
    }

    setSubmitError(null)

    try { 
      await createStoreMutation.mutateAsync(formData)
      setFormData({ id: '', name: '' })
      setFieldErrors({})
    } catch (error) {
      if (isApiValidationError(error)) {
        const apiErrors = {
          id: error.errors.Id?.[0],
          name: error.errors.Name?.[0],
        }

        setFieldErrors((current) => ({ ...current, ...apiErrors }))
        setSubmitError(error.title ?? 'Please correct the highlighted fields.')
      } else {
        setSubmitError('Unable to save the store right now.')
      }
    }
  }

  return (
    <>
      <header className="hero">
        <div>
          <p className="eyebrow">Operations</p>
          <h1 className="hero-title">Stores</h1>
          <p className="hero-copy">
            Define the stores that own subscription groups and operational workflows.
          </p>
        </div>
        <div className="hero-stat">
          <span className="hero-stat-label">Active stores</span>
          <strong className="hero-stat-value">{stores.length}</strong>
        </div>
      </header>

      <main className="workspace">
        <section className="panel panel-wide" aria-labelledby="stores-heading">
          <div className="panel-header">
            <div>
              <p className="panel-kicker">Directory</p>
              <h2 id="stores-heading" className="panel-title">Store list</h2>
            </div>
            <button className="ghost-button" type="button" onClick={() => void refetch()} disabled={isLoading}>
              Refresh
            </button>
          </div>

          {isLoadError ? <p className="banner banner-error">Unable to load stores right now.</p> : null}

          {isLoading ? (
            <div className="empty-state" role="status" aria-live="polite">
              <p className="empty-state-title">Loading stores...</p>
              <p className="empty-state-copy">Fetching the current in-memory directory from the server.</p>
            </div>
          ) : stores.length === 0 ? (
            <div className="empty-state">
              <p className="empty-state-title">No stores yet</p>
              <p className="empty-state-copy">Add the first store to start organizing groups by site.</p>
            </div>
          ) : (
            <div className="list" role="list" aria-label="Stores">
              {stores.map((store) => (
                <Link className="list-row store-link" role="listitem" key={store.id} to={`/stores/${encodeURIComponent(store.id)}`}>
                  <div>
                    <p className="store-name">{store.name}</p>
                    <p className="store-meta">Store identifier</p>
                  </div>
                  <code className="store-id">{store.id}</code>
                </Link>
              ))}
            </div>
          )}
        </section>

        <section className="panel panel-narrow" aria-labelledby="create-heading">
          <div className="panel-header">
            <div>
              <p className="panel-kicker">Create</p>
              <h2 id="create-heading" className="panel-title">Add store</h2>
            </div>
          </div>

          <form className="store-form" onSubmit={handleSubmit} noValidate>
            <label className="field">
              <span className="field-label">Id</span>
              <input
                className="field-input"
                name="id"
                value={formData.id}
                onChange={(event) => handleChange('id', event.target.value)}
                placeholder="e.g. north-hub"
                autoComplete="off"
                aria-invalid={fieldErrors.id ? 'true' : 'false'}
                aria-describedby={fieldErrors.id ? 'store-id-error' : undefined}
              />
              <span className="field-hint">At least 4 characters, unique across all stores.</span>
              {fieldErrors.id ? <span id="store-id-error" className="field-error">{fieldErrors.id}</span> : null}
            </label>

            <label className="field">
              <span className="field-label">Name</span>
              <input
                className="field-input"
                name="name"
                value={formData.name}
                onChange={(event) => handleChange('name', event.target.value)}
                placeholder="Northern Distribution"
                autoComplete="off"
                aria-invalid={fieldErrors.name ? 'true' : 'false'}
                aria-describedby={fieldErrors.name ? 'store-name-error' : undefined}
              />
              {fieldErrors.name ? <span id="store-name-error" className="field-error">{fieldErrors.name}</span> : null}
            </label>

            {submitError ? <p className="banner banner-error">{submitError}</p> : null}

            <button className="primary-button" type="submit" disabled={createStoreMutation.isPending}>
              {createStoreMutation.isPending ? 'Saving...' : 'Add store'}
            </button>
          </form>
        </section>
      </main>
    </>
  )
}

function isApiValidationError(error: unknown): error is ApiValidationError {
  return typeof error === 'object' && error !== null && 'errors' in error
}

function sortStores(stores: Store[]) {
  return [...stores].sort((left, right) => {
    const nameComparison = left.name.localeCompare(right.name)
    if (nameComparison !== 0) {
      return nameComparison
    }

    return left.id.localeCompare(right.id)
  })
}

export default StoresPage