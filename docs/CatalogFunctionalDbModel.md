# Catalog Functional Data Model

This document describes the functional structure of the **Catalog module** for the PWebShop project.  
It defines all entities, their relationships, and the main API endpoints that expose catalog-related data.

---

## 1. Overview

The catalog is responsible for organizing and presenting products to users.  
It includes categories, subcategories, products, availability methods, and product images.

Main entities:

- **Category** — Top-level product grouping.
- **SubCategory** — Nested grouping within a category.
- **Product** — The core item being sold.
- **AvailabilityMethod** — How a product can be obtained.
- **ProductAvailability** — Linking table between Product and AvailabilityMethod.
- **ProductImage** — Images belonging to a product.

---

## 2. Entity Details

### 2.1 Category

**Purpose:**  
Represents a top-level grouping of products in the catalog.

| Field | Type | Description |
|-------|------|-------------|
| Id | int | Primary key |
| Name | string | Technical name (e.g. `"electronics"`) |
| DisplayName | string | Name shown in the UI (e.g. `"Electronics"`) |
| Description | string | Optional description of the category |
| SortOrder | int | Determines display order |
| IsActive | bool | Indicates whether the category is visible in the catalog |

**Relationships**
- One `Category` has many `SubCategories`.

---

### 2.2 SubCategory

**Purpose:**  
Represents a specific grouping of products within a category.

| Field | Type | Description |
|-------|------|-------------|
| Id | int | Primary key |
| CategoryId | int | Foreign key to Category |
| Name | string | Technical name |
| DisplayName | string | UI display name |
| Description | string | Optional |
| SortOrder | int | Display order within the category |
| IsActive | bool | Whether it is currently visible |

**Relationships**
- Each `SubCategory` belongs to one `Category`.
- Each `SubCategory` contains many `Products`.

---

### 2.3 Product

**Purpose:**  
Represents a sellable item in the system.

| Field | Type | Description |
|-------|------|-------------|
| Id | int | Primary key |
| SubCategoryId | int | Foreign key to SubCategory |
| SupplierId | int | References the supplier (User) |
| Name | string | Product name |
| ShortDescription | string | Short summary shown in listings |
| LongDescription | string | Full product details |
| BasePrice | decimal | Base cost |
| MarkupPercentage | decimal | Price markup |
| FinalPrice | decimal | Final calculated price (optional to store) |
| Status | string | E.g. `PendingApproval`, `Active`, `Inactive` |
| IsFeatured | bool | Indicates if this is a featured product |
| IsActive | bool | Product visibility toggle |
| CreatedAt | DateTime | Creation timestamp |
| UpdatedAt | DateTime | Last modification timestamp |

**Relationships**
- One `Product` belongs to one `SubCategory`.
- One `Product` belongs to one `Supplier`.
- One `Product` has many `ProductImages`.
- One `Product` has many `ProductAvailability` records.

---

### 2.4 AvailabilityMethod

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

### 2.5 ProductAvailability

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

### 2.6 ProductImage

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

### 3.1 Catalog Helper Endpoints

#### `GET /api/catalog/menu`
Returns all active categories with their subcategories for building navigation menus.

#### `GET /api/catalog/featured`
Returns a featured product for homepage display.

---

### 3.2 Product Endpoints

#### `GET /api/products`
Fetches a paginated and filterable list of products.

#### `GET /api/products/{id}`
Returns full product details.

---

### 3.3 Category and SubCategory Endpoints

#### `GET /api/categories`
Returns all categories, optionally including their subcategories.

#### `GET /api/subcategories`
Returns all subcategories.  
Supports `categoryId` as a query parameter for filtering.

---

### 3.4 Availability Endpoints

#### `GET /api/availabilitymethods`
Lists all availability methods that can be assigned to products.

---

## 4. Notes

- All catalog entities include the `IsActive` flag to control visibility in the frontend.
- Sorting within the frontend is typically driven by `SortOrder`.
- DTOs should avoid circular references to prevent serialization loops.
- Product pricing can initially be calculated using `BasePrice + (BasePrice * MarkupPercentage / 100)`.

---

## 5. Next Steps

1. Implement these entities in the `Domain` project.
2. Add `DbSet` properties to `AppDbContext`.
3. Seed a few sample records for categories, subcategories, and products.
4. Create the corresponding API controllers and DTOs.
5. Expand Swagger documentation accordingly.

---

*End of Document*
