# Practical Work — Web Programming 2025/2026
### Theme A

## Objective
Develop an **integrated system**, consisting of **three applications**, following all the rules and requirements described in this document.

Each pair of students must build the system **from scratch**, **in English**, using **.NET 8**, **C# 12**, **Entity Framework Core**, **ASP.NET Core Identity**, and **Blazor** technologies.

---

## General Rules
- Work must be done in **pairs**.  
- The project must strictly follow the **technologies and architecture** specified here.  
- Deliverables that **do not comply with these rules will not be accepted**.  
- The final grade is **individual**, considering:  
  - System completeness and correctness  
  - Presentation and defense (oral discussion)  
  - Written report quality  
  - Code structure and comments  

---

## 1. System Structure
The system consists of **three applications** that must share the same database and follow this structure:

1. **Public Application**  
   - Built with **Blazor WebAssembly** and **Blazor Hybrid** (desktop/mobile).  
   - Both must use a **shared Razor Class Library (RCL)** for the UI and logic.  
   - Communication with the backend is done via **a public API**.

2. **Administration Application**  
   - Built with **Blazor Server or Blazor Web**.  
   - This app connects **directly to the database**, without going through the API.  
   - Used for **management and configuration** tasks.

3. **API Application**  
   - Built with **ASP.NET Core Web API**.  
   - Responsible for communication between the frontend applications and the database.  
   - Must use **ASP.NET Identity** and **JWT Authentication**.  
   - Must include **Swagger** documentation and a **Dev Tunnel** for testing.

All three applications share:
- A **single SQL Server LocalDB database**.  
- **Entity Framework Core** with **Migrations**.  
- The **ASP.NET Identity Framework** for user authentication and role management.  

---

## 2. Required Technologies
- .NET 8  
- C# 12  
- Blazor (Web and Hybrid)  
- Razor Class Library (RCL) for shared UI  
- ASP.NET Identity and JWT authentication  
- Entity Framework Core (with Migrations)  
- SQL Server LocalDB  
- Swagger for API testing  
- Dev Tunnel for external access  
- Shared CSS and layout for all applications

---

## 3. Database and Entities
A **single database** must contain all the necessary entities and relationships.

### Required entities (minimum)
- **Product**
- **Category**
- **Subcategory**
- **Availability method**
- **Image**
- **Price**  
  - Contains base price and a percentage for final price calculation.
- **Stock**
- **Order**
- **Order line**
- **Payment (simulation)**
- **Shipment (simulation)**

Each product belongs to a category and a subcategory and can have multiple images and availability methods.

When a product is activated, its final price is calculated as:

> Final Price = Base Price + (Base Price × Percentage)

---

## 4. User Profiles and Roles
The system must support **five user roles** with specific permissions.

| Role | Description | Access |
|------|--------------|--------|
| **Anonymous** | Can browse and view products, add to cart, checkout as guest (requires registration at checkout). | Public Frontend |
| **Customer** | Can manage their profile, view order history, and make purchases. | Public Frontend |
| **Supplier** | Can manage their own products (CRUD) and view sales history. | Public Frontend |
| **Employee** | Can manage products, users, and sales (same as administrator, except role management). | Admin App |
| **Administrator** | Full control, including managing employee roles. | Admin App |

**Registration flow**
- Customers and Suppliers can register via the frontend.
- Their accounts are created with status **“Pending”** and require activation by an **Employee** or **Administrator**.

**Role assignment rules**
- Employees and Administrators can assign roles to other users.
- A user **cannot remove their own account**.

---

## 5. Functional Requirements

### 5.1 Anonymous Users
- Browse products by category and subcategory.  
- Search and filter by price, name, or availability.  
- View product details with images, price, and availability.  
- Add items to cart.  
- Register or log in only when checking out.  
- View a **featured product** chosen randomly by the system.

### 5.2 Customers
- Same as anonymous users, plus:
  - Access to **order history**.
  - Manage **profile information**.
  - Proceed to **checkout and simulated payment**.

### 5.3 Suppliers
- Manage their own products:
  - Add, edit, or remove products.  
  - After creation or modification, the product is set to **“Pending Approval”**.  
  - Only after activation by an administrator will it become visible in the public catalog.
- View their own **sales history**.

### 5.4 Employees
- Manage **products**, **categories**, **availability methods**, and **users**.
- Can:
  - Activate, deactivate, or delete items.
  - Edit prices and stock levels.
  - Approve or reject pending products and users.
  - Manage sales, confirm or reject orders, simulate payments and shipments.

### 5.5 Administrators
- Same permissions as Employees, plus:
  - Manage user **roles and privileges**.
  - Assign or revoke **Employee** and **Administrator** roles.

---

## 6. Management Application (Blazor Server/Web)

### Functionalities
1. **Product Management**
   - CRUD operations with filtering, sorting, and pagination.
   - Activate, deactivate, or permanently delete (only if no sales exist).
   - Update price and stock.
   - Approve products submitted by suppliers.

2. **Category and Availability Management**
   - CRUD operations similar to products.
   - Only active categories appear in the public app.

3. **User Management**
   - View all users with filters by role and status.
   - Activate/deactivate users.
   - Manage roles (only administrators).
   - Prevent self-deletion.

4. **Sales Management**
   - View all sales with filters and pagination.
   - Approve or reject sales.
   - Simulate shipment and payment.
   - Adjust stock accordingly.

---

## 7. Public Frontend (Blazor Web + Hybrid)

### Functionalities
- Catalog with **category and subcategory ribbons** (consistent across all apps).  
- Product detail view with images, description, and price.  
- Shopping cart management.  
- Checkout and simulated payment.  
- User registration and authentication.  
- Same CSS and layout as the admin app via the shared RCL.  
- Featured product section.  

---

## 8. API Application

### Requirements
- Built with **ASP.NET Core Web API**.  
- Uses **ASP.NET Identity** and **JWT Authentication**.  
- Provides endpoints for:
  - Product listing (with filters, sorting, pagination).
  - Category listing.
  - Orders, payments, shipments.
  - Customer and Supplier registration and login.
- Includes **Swagger** documentation.  
- Must be configured with a **Dev Tunnel** for frontend testing.

---

## 9. Shared Razor Class Library (RCL)
All applications must share:
- The same **UI layout**.  
- The same **CSS styles**.  
- The same **header and footer ribbons** (for categories, subcategories, and sub-subcategories).  

Minor color or image changes are allowed, but **the overall structure must remain identical**.

[IMAGE PLACEHOLDER: Example layout with ribbons and sections]

---

## 10. Validation and Business Rules
- Input validation required in all forms.  
- Domain rules must be enforced both server-side and client-side.  
- Business logic examples:
  - A Supplier cannot manage products owned by another supplier.  
  - A Customer can only see their own orders.  
  - Final price calculation must use the configured markup percentage.  
  - Products pending approval cannot appear in the public catalog.  
  - Only Administrators can manage roles.

---

## 11. Testing
Each pair must prepare a **demo scenario** including:
- One user for each role.  
- At least one example of each functional flow:  
  - Customer purchase  
  - Supplier adding product pending approval  
  - Employee activating and managing items  
  - Administrator managing roles

The database must be **pre-populated** with enough data to demonstrate every feature.

---

## 12. Report
A written **report** must be included with the submission.  
The report must contain:

1. **Project Title and Team Members**  
2. **Objectives**  
3. **Feasibility Analysis**  
4. **Implementation Summary**  
5. **Main Problems Encountered and Solutions**  
6. **Critical Self-Evaluation**  
7. **Login Credentials for Each Example User**  
8. **List of Business Rules (Domain Rules)**  
9. **Screenshots of the Applications**

The report must be written in **English** and included in the ZIP submission.

## 13. Submission Rules
- Submit a **ZIP file** named: '''bash PracticalWork_WebProgramming_A_FirstName1_LastName1_FirstName2_LastName2.zip'''
- The ZIP must contain:
- All **source code** of the solution.
- The **database file** for LocalDB (`.mdf` and related files).
- All **images used**.
- The **report** in PDF format.

- Submission is done through **Moodle**.

- **Deadline:** December 14, 2025 (Saturday).

After submission, students must **schedule a defense session** to present and explain their work.

---

## 14. Evaluation Criteria
| Component | Weight |
|----------|--------|
| Implementation quality and completeness | 50% |
| Report and documentation | 25% |
| Oral defense | 25% |

Failure to follow any **technical or structural requirement** may result in **automatic rejection** of the project.

---

## 15. Penalties and Warnings
- Late submissions are **not accepted**.
- Projects using other frameworks or databases will be **rejected**.
- All three applications must share the **same database**.
- Each pair must be able to **compile and run** all applications during the defense.
- **Code copying** or **unadapted AI generated work** is not allowed.

---

## 16. Visual Reference
[IMAGE PLACEHOLDER: Example home page layout]  
[IMAGE PLACEHOLDER: Example product details view]  
[IMAGE PLACEHOLDER: Example administration dashboard]  
[IMAGE PLACEHOLDER: Example user management screen]

---

## 17. Final Notes
- The system must be **fully functional** and **consistent** across all applications.
- Identity roles, UI, and domain rules must be **implemented exactly as described**.
- You may:
- Use different colors or branding.
- Add minor enhancements, as long as all required functionality remains intact.

Focus on **completeness**, **correct logic**, and **clarity** of your implementation.

---

