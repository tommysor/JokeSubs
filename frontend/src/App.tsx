import './App.css'
import { useEffect, useState } from 'react'
import { createLocation, getLocations, type ApiValidationError } from './locationsApi'
import type { CreateLocationInput, Location } from './types'

function App() {
  const [locations, setLocations] = useState<Location[]>([])
  const [formData, setFormData] = useState<CreateLocationInput>({ id: '', name: '' })
  const [fieldErrors, setFieldErrors] = useState<Partial<Record<keyof CreateLocationInput, string>>>({})
  const [loadError, setLoadError] = useState<string | null>(null)
  const [submitError, setSubmitError] = useState<string | null>(null)
  const [isLoading, setIsLoading] = useState(true)
  const [isSaving, setIsSaving] = useState(false)

  useEffect(() => {
    void loadLocations()
  }, [])

  async function loadLocations() {
    setIsLoading(true)
    setLoadError(null)

    try {
      const nextLocations = await getLocations()
      setLocations(nextLocations)
    } catch {
      setLoadError('Unable to load locations right now.')
    } finally {
      setIsLoading(false)
    }
  }

  function handleChange(field: keyof CreateLocationInput, value: string) {
    setFormData((current) => ({ ...current, [field]: value }))
    setFieldErrors((current) => ({ ...current, [field]: undefined }))
    setSubmitError(null)
  }

  function validateForm(values: CreateLocationInput) {
    const errors: Partial<Record<keyof CreateLocationInput, string>> = {}

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

    setIsSaving(true)
    setSubmitError(null)

    try {
      const createdLocation = await createLocation(formData)

      setLocations((current) => {
        const nextLocations = [...current, createdLocation]
        return nextLocations.sort((left, right) => {
          const nameComparison = left.name.localeCompare(right.name)
          if (nameComparison !== 0) {
            return nameComparison
          }

          return left.id.localeCompare(right.id)
        })
      })
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
        setSubmitError('Unable to save the location right now.')
      }
    } finally {
      setIsSaving(false)
    }
  }

  return (
    <div className="app-shell">
      <header className="hero">
        <div>
          <p className="eyebrow">Operations</p>
          <h1 className="hero-title">Locations</h1>
          <p className="hero-copy">
            Define the locations that own subscription groups and operational workflows.
          </p>
        </div>
        <div className="hero-stat">
          <span className="hero-stat-label">Active locations</span>
          <strong className="hero-stat-value">{locations.length}</strong>
        </div>
      </header>

      <main className="workspace">
        <section className="panel panel-wide" aria-labelledby="locations-heading">
          <div className="panel-header">
            <div>
              <p className="panel-kicker">Directory</p>
              <h2 id="locations-heading" className="panel-title">Location list</h2>
            </div>
            <button className="ghost-button" type="button" onClick={() => void loadLocations()} disabled={isLoading}>
              Refresh
            </button>
          </div>

          {loadError ? <p className="banner banner-error">{loadError}</p> : null}

          {isLoading ? (
            <div className="empty-state" role="status" aria-live="polite">
              <p className="empty-state-title">Loading locations...</p>
              <p className="empty-state-copy">Fetching the current in-memory directory from the server.</p>
            </div>
          ) : locations.length === 0 ? (
            <div className="empty-state">
              <p className="empty-state-title">No locations yet</p>
              <p className="empty-state-copy">Add the first location to start organizing groups by site.</p>
            </div>
          ) : (
            <div className="list" role="list" aria-label="Locations">
              {locations.map((location) => (
                <article className="list-row" role="listitem" key={location.id}>
                  <div>
                    <p className="location-name">{location.name}</p>
                    <p className="location-meta">Location identifier</p>
                  </div>
                  <code className="location-id">{location.id}</code>
                </article>
              ))}
            </div>
          )}
        </section>

        <section className="panel panel-narrow" aria-labelledby="create-heading">
          <div className="panel-header">
            <div>
              <p className="panel-kicker">Create</p>
              <h2 id="create-heading" className="panel-title">Add location</h2>
            </div>
          </div>

          <form className="location-form" onSubmit={handleSubmit} noValidate>
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
                aria-describedby={fieldErrors.id ? 'location-id-error' : undefined}
              />
              <span className="field-hint">At least 4 characters, unique across all locations.</span>
              {fieldErrors.id ? <span id="location-id-error" className="field-error">{fieldErrors.id}</span> : null}
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
                aria-describedby={fieldErrors.name ? 'location-name-error' : undefined}
              />
              {fieldErrors.name ? <span id="location-name-error" className="field-error">{fieldErrors.name}</span> : null}
            </label>

            {submitError ? <p className="banner banner-error">{submitError}</p> : null}

            <button className="primary-button" type="submit" disabled={isSaving}>
              {isSaving ? 'Saving...' : 'Add location'}
            </button>
          </form>
        </section>
      </main>

      <footer className="app-footer">
        <p>
          Icons by <a target="_blank" href="https://icons8.com" rel="noreferrer">Icons8</a>
        </p>
      </footer>
    </div>
  )
}

function isApiValidationError(error: unknown): error is ApiValidationError {
  return typeof error === 'object' && error !== null && 'errors' in error
}

export default App
