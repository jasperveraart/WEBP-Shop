# Image storage behavior

## Current setup
- **Shared folder**: Both Admin and API write to the same filesystem path, configured via `ImageStorage:PhysicalPath` (defaulting to `../shared-images` relative to each app). Images are served from the shared folder through the `/images` request path.
- **Upload flows**: Admin uploads via the Product Edit screen; the API exposes supplier-protected endpoints that accept `IFormFile` uploads and return the stored URL.

## Operational notes
- Ensure the process accounts have read/write permissions to the `ImageStorage:PhysicalPath` directory (default `../shared-images`).
- Static files are mapped at `ImageStorage:RequestPath` (default `/images`) in both Admin and API. Stored URLs follow `{RequestPath}/products/{productId}/{guid}{ext}`.
- Adjust the `ImageStorage` section in `appsettings.json` for each app if the shared folder or request path differs per environment.
