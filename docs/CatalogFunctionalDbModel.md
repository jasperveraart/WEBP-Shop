# Catalog Functional Data Model

This document describes the current functional structure of the **Catalog module** for the PWebShop project.
It reflects the implemented entities, their relationships, and the API endpoints that expose catalog-related data.

---

## 1. Overview

The catalog is responsible for organizing and presenting products to users.
It includes hierarchical categories, products, availability methods, availability assignments, and product images.

Main entities:

- **Category** — Hierarchical product grouping (parent/child tree).
- **Product** — The core item being sold.
- **AvailabilityMethod** — How a product can be obtained.
- **ProductAvailability** — Linking table between Product and AvailabilityMethod.
- **ProductImage** — Images belonging to a product.

---

## 2. Entity Details

### 2.1 Category

**Purpose:**
Represents a node in the catalog tree. Categories can have parent and child categories, enabling multi-level navigation.

| Field | Type | Description |
|-------|------|-------------|
| Id | int | Primary key |
| ParentId | int? | Optional foreign key to another Category |
| Name | string | Technical name (e.g. `"electronics"`) |
| DisplayName | string | Name shown in the UI (e.g. `"Electronics"`) |
| Description | string | Optional description of the category |
| SortOrder | int | Determines display order |
| IsActive | bool | Indicates whether the category is visible in the catalog |

**Relationships**
- Optional self-referencing relationship via `ParentId` / `Parent`.
- One `Category` has many child categories through `Children`.
- One `Category` has many `Products`.

---

### 2.2 Product

**Purpose:**
Represents a sellable item in the system.

| Field | Type | Description |
|-------|------|-------------|
| Id | int | Primary key |
| CategoryId | int | Foreign key to Category |
| SupplierId | int | References the supplier (User) |
| Name | string | Product name |
| ShortDescription | string | Short summary shown in listings |
| LongDescription | string | Full product details |
| BasePrice | decimal | Base cost |
| MarkupPercentage | decimal | Price markup |
| FinalPrice | decimal? | Persisted selling price (optional, defaults to calculated value) |
| Status | string | E.g. `PendingApproval`, `Active`, `Inactive` |
| IsFeatured | bool | Indicates if this is a featured product |
| IsActive | bool | Product visibility toggle |
| CreatedAt | DateTime | Creation timestamp |
| UpdatedAt | DateTime | Last modification timestamp |

**Relationships**
- One `Product` belongs to one `Category`.
- One `Product` belongs to one `Supplier` (outside of catalog scope).
- One `Product` has many `ProductImages`.
- One `Product` has many `ProductAvailability` records.

---

### 2.3 AvailabilityMethod

**Purpose:**  
Defines the available ways for a customer to obtain a product.

| Field | Type | Description |
|-------|------|-------------|
| Id | int | Primary key |
| Name | string | Technical name (e.g. `"HomeDelivery"`, `"StorePickup"`) |
| DisplayName | string | Display name for UI |
| Description | string | Optional explanation |
| IsActive | bool | Whether this method is currently available |

**Relationships**
- Many-to-many with `Product` via `ProductAvailability`.

---

### 2.4 ProductAvailability

**Purpose:**  
Link table connecting products and their available delivery methods.

| Field | Type | Description |
|-------|------|-------------|
| ProductId | int | References a Product |
| AvailabilityMethodId | int | References an AvailabilityMethod |

**Relationships**
- Each `ProductAvailability` links one `Product` to one `AvailabilityMethod`.
- Together, these two fields form the composite primary key.

---

### 2.5 ProductImage

**Purpose:**  
Stores all images belonging to a product.

| Field | Type | Description |
|-------|------|-------------|
| Id | int | Primary key |
| ProductId | int | Foreign key to Product |
| Url | string | Path or URL to the image |
| AltText | string | Accessibility text for the image |
| IsMain | bool | Marks the main image for the product |
| SortOrder | int | Determines image display order |

**Relationships**
- Each `Product` has many `ProductImages`.

---

## 3. API Endpoints

### 3.1 Catalog helper endpoints

- `GET /api/catalog/menu` — Returns all active categories ordered by `SortOrder`, assembled into a hierarchy suitable for navigation menus. Only active categories are included.
- `GET /api/catalog/featured` — Returns the most recently updated featured product (with category, availability methods, and images). Responds with `404` when no featured product exists.

---

### 3.2 Category endpoints

- `GET /api/categories?parentId={id}` — Returns categories filtered by `parentId` (defaults to root categories). Results are ordered by `SortOrder` then `DisplayName`.
- `GET /api/categories/{id}` — Returns a single category.
- `GET /api/categories/tree` — Returns the full category hierarchy as a nested tree DTO.
- `POST /api/categories` — Creates a category. Validates that the optional parent exists.
- `PUT /api/categories/{id}` — Updates a category. Prevents cycles by ensuring a category cannot be assigned to one of its descendants.
- `DELETE /api/categories/{id}` — Removes a category when it has no children and no products assigned.

---

### 3.3 Product endpoints

- `GET /api/products?page={page}&pageSize={pageSize}&categoryId={id}&isActive={bool}` — Returns a paginated list of product summaries with optional filters.
- `GET /api/products/{id}` — Returns full product details, including availability methods and images.
- `POST /api/products` — Creates a product. Validates the category and availability methods, and calculates `FinalPrice` when it is not provided.
- `PUT /api/products/{id}` — Updates an existing product, synchronising availability methods and images.
- `DELETE /api/products/{id}` — Removes a product.

---

### 3.4 Product image endpoints

- `GET /api/products/{productId}/images` — Lists images for a product, ordered by `SortOrder` and `IsMain`.
- `GET /api/products/{productId}/images/{id}` — Returns a single product image.
- `POST /api/products/{productId}/images` — Adds a new image to a product (product must exist).
- `PUT /api/products/{productId}/images/{id}` — Updates an existing image.
- `DELETE /api/products/{productId}/images/{id}` — Deletes an image.

---

### 3.5 Availability method endpoints

- `GET /api/availabilitymethods` — Lists availability methods ordered by `DisplayName`.
- `GET /api/availabilitymethods/{id}` — Returns a single availability method.
- `POST /api/availabilitymethods` — Creates an availability method.
- `PUT /api/availabilitymethods/{id}` — Updates an availability method.
- `DELETE /api/availabilitymethods/{id}` — Deletes an availability method.

---

## 4. Notes

- All catalog entities expose the `IsActive` flag to control visibility in the frontend.
- `SortOrder` drives display ordering for categories and images. The EF Core configuration sets default values of `0` where applicable.
- Product pricing defaults to `BasePrice + (BasePrice * MarkupPercentage / 100)` when `FinalPrice` is not provided and is rounded to two decimals.
- Category and product entities track `CreatedAt` and `UpdatedAt` timestamps. `UpdatedAt` is refreshed whenever products are modified.
- EF Core enforces maximum lengths for key text fields and restricts deleting categories with children to preserve hierarchy integrity.

---

*End of Document*
