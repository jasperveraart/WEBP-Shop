
# Business Logic Overview

This document defines the **business rules and access logic** for the integrated e-commerce system.  
It describes what each role can and cannot do, and how entities like products, users, orders, and payments interact.

---

## 1. Core Concepts

### Roles
- Anonymous
- Customer
- Supplier
- Employee
- Administrator

### User Status
`ApplicationUser` contains the following status fields that drive behavior:
- `IsActive`
- `IsPendingApproval`
- `IsBlocked`
- Type flags: `IsCustomer`, `IsSupplier`, `IsEmployee`, `IsAdministrator`

### Product Status
- `IsActive`
- `IsPendingApproval`
- `IsRejected`
- `RejectedReason`

Only products that are **active** and **approved** appear in the public catalog.

---

## 2. Access Levels by Role

### 2.1 Anonymous
**Can:**
- View the public product catalog (list, search, filter)
- View product details
- See the featured product
- Build a local (client-side) shopping cart

**Cannot:**
- Place orders
- View or modify user accounts
- Manage products or users

Anonymous users must register or log in during checkout to become Customers.

---

### 2.2 Customer
**Conditions:** `IsCustomer = true`, `IsActive = true`, `IsBlocked = false`

**Can:**
- Everything an anonymous user can
- Manage own profile (name, address, password, contact info)
- Maintain their shopping cart
- Place orders
- View their own order history and details

**Cannot:**
- Modify or manage products
- Access admin features

**Business Rule:**  
All order operations must validate that `Order.CustomerId == CurrentUserId`.

---

### 2.3 Supplier
**Conditions:** `IsSupplier = true`, `IsActive = true`, `IsBlocked = false`

**Can:**
- Manage own profile
- Create and edit **own products**
  - New or edited products are automatically set to `IsPendingApproval = true`
- View sales related to their own products

**Cannot:**
- Access or modify other suppliers’ products
- Edit product prices, markup, or stock
- Approve or activate products
- Manage users or roles

**Business Rules:**
- Every product modification must check `Product.SupplierId == CurrentUserId`
- Suppliers can edit descriptive data only (name, description, images)
- Price and stock are managed by employees or administrators
- Supplier-submitted products must be approved before being visible

---

### 2.4 Employee
**Conditions:** `IsEmployee = true`, `IsActive = true`, `IsBlocked = false`

**Can:**
- Manage all products
  - Create, edit, approve/reject, activate/deactivate
  - Update prices and stock
- Manage categories and availability methods
- Manage all orders and sales
  - View, approve, cancel, or complete orders
  - Simulate payments and shipments
  - Update stock after shipment or cancellation
- Manage users (Customers only)
  - Activate/deactivate, block/unblock

**Cannot:**
- Manage roles
- Create or activate Suppliers
- Modify Administrator accounts

**Rules:**
- Employees cannot alter user roles or privileges
- Employees may manage products globally

---

### 2.5 Administrator
**Conditions:** `IsAdministrator = true`, `IsActive = true`, `IsBlocked = false`

**Can:**
- Everything an Employee can
- Manage **all users and roles**
  - Activate/deactivate/block/unblock any user
  - Approve or reject Suppliers
  - Assign or remove any roles

**Cannot:**
- Delete their own account
- Remove their own Administrator role

**Rules:**
- Role changes must prevent self-removal of Administrator rights

---

## 3. Product and Catalog Logic

### 3.1 Visibility
- Public catalog shows only products where:
  - `IsActive = true`
  - `IsPendingApproval = false`
  - `IsRejected = false`

### 3.2 Creation and Editing

**Supplier:**
- `POST /products`: Allowed only for active suppliers
  - Automatically sets `SupplierId = CurrentUserId`
  - Marks product as pending approval
- `PUT /products/{id}`: Only allowed if product belongs to current supplier
  - Editing resets product to pending approval

**Employee/Administrator:**
- Can manage any product, including activation, rejection, and pricing.

### 3.3 Price Logic
- `FinalPrice = BasePrice + (BasePrice * MarkupPercentage)`
- Only one current price (`IsCurrent = true`) per product
- Suppliers cannot change markup or final price

### 3.4 Stock Logic
- Stock reduced when orders are confirmed, restored when canceled
- Only Employees and Administrators can modify stock

---

## 4. Orders, Cart, Payments, and Shipments

### 4.1 Cart
**Anonymous:** Can add/remove items locally.  
**Customer:** Can checkout and place orders.

---

### 4.2 Orders
**Rules:**
- Orders always reference the current user (`CustomerId`)
- Copy prices and address at the time of purchase

**Status Flow:**
`PendingPayment → Paid → Cancelled → Shipped → Completed`

**Access Control:**
- Customer: view/create own orders only  
- Supplier: may view limited sales data related to their products  
- Employee/Admin: can view and modify all orders

---

### 4.3 Payments
- Each order has one simulated payment
- Customer: initiate payment for own order
- Employee/Admin: simulate payment success/failure

---

### 4.4 Shipments
- Each order has one simulated shipment
- Customer: view shipment of own order
- Employee/Admin: create, edit, mark shipped/delivered

---

## 5. User and Role Management

### 5.1 Activation and Blocking
- New Customer/Supplier accounts are created with:
  - `IsPendingApproval = true`
  - `IsActive = false`
- Login is denied if user is blocked or inactive

**Employee:**
- Can activate/deactivate Customers

**Administrator:**
- Can activate/deactivate any user
- Can approve/reject Suppliers

---

### 5.2 Role Management
**Administrator only:**
- Can assign or remove roles (Customer, Supplier, Employee, Administrator)

**Rules:**
- A user cannot delete their own account
- An Administrator cannot remove their own admin role
- Avoid leaving users with no roles assigned

---

## 6. Security Mapping (Examples)

| Endpoint | Access | Additional Rule |
|-----------|---------|-----------------|
| `GET /api/products` | `[AllowAnonymous]` | Only active and approved products |
| `POST /api/products` | `[Authorize(Roles = Supplier)]` | Supplier must own created product |
| `PUT /api/products/{id}` | `[Authorize(Roles = Supplier)]` | Must own product; sets pending approval |
| `PUT /api/products/{id}/price` | `[Authorize(Roles = Employee,Administrator)]` | Update price and markup |
| `GET /api/orders` | `[Authorize]` | Customer: own only; Employee/Admin: all |
| `POST /api/orders` | `[Authorize(Roles = Customer)]` | Customer must be active |
| `GET /api/admin/users` | `[Authorize(Roles = Administrator)]` | Full user list |
| `PUT /api/admin/users/{id}/roles` | `[Authorize(Roles = Administrator)]` | Cannot modify self |

---

## 7. Summary

- **Customers** buy.  
- **Suppliers** provide products (pending approval).  
- **Employees** manage catalog and customers.  
- **Administrators** manage everything including roles.  
- **Anonymous** users can browse only.

This logic defines all security and authorization rules necessary to protect and organize the API endpoints and application workflow.
