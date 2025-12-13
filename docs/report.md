# Practical Work — Web Programming 2025/2026
## Theme A: Integrated Web Shop System

### 1. Project Title and Team Members
**Project Title:** PWebShop - Integrated E-Commerce Solution

**Team Members:**
*   [First Name] [Last Name] (Student ID)
*   [First Name] [Last Name] (Student ID)

---

### 2. Objectives
The primary objective of this project was to develop a comprehensive, integrated e-commerce system consisting of three distinct applications sharing a single database and business logic core.

**Key Goals:**
*   **Public Application:** A customer-facing storefront built with **Blazor WebAssembly** (for web) and **Blazor Hybrid** (for desktop/mobile), utilizing a shared Razor Class Library (RCL) for consistent UI and logic.
*   **Administration Application:** A management dashboard built with **Blazor Server/Web** for employees and administrators to manage products, users, and orders, connecting directly to the database.
*   **API Application:** An **ASP.NET Core Web API** serving as the backend for the public frontend, handling authentication (Identity/JWT) and data access.
*   **Technology Stack:** Strict adherence to **.NET 8**, **C# 12**, **Entity Framework Core**, and **ASP.NET Core Identity**.

---

### 3. Feasibility Analysis
The project's architecture was designed to ensure code reusability, scalability, and maintainability.

*   **Shared UI & Logic (RCL):** By moving all Razor components, services, and DTOs into a **Razor Class Library (PWebShop.Rcl)**, we achieved near-100% code sharing between the Blazor WebAssembly and Blazor Hybrid applications. This drastically reduced development time and ensured UI consistency.
*   **Clean Architecture:** The solution is structured into layers:
    *   **Domain:** Entities and core business logic (no dependencies).
    *   **Infrastructure:** Database context, migrations, and identity implementation.
    *   **API:** RESTful endpoints exposing data to the public frontend.
    *   **RCL:** Shared frontend components and client-side services.
    *   **Apps (Web, Admin, Hybrid):** Hosting projects for specific platforms.
*   **Database Strategy:** A single **SQL Server LocalDB** instance serves all applications, ensuring data integrity. The Admin app connects directly for performance and security, while the Public app goes through the API for abstraction and security.

---

### 4. Implementation Summary

#### 4.1. API Application
*   Implemented using **ASP.NET Core Web API**.
*   Secured using **ASP.NET Identity** and **JWT Bearer Authentication**.
*   Exposes endpoints for Products, Categories, Orders, and Authentication.
*   Includes **Swagger** for documentation and testing.

#### 4.2. Administration Application
*   Built with **Blazor Server** for server-side rendering and direct database access.
*   **Features:**
    *   **Product Management:** CRUD operations, approval workflow (Approve/Decline supplier products).
    *   **User Management:** Role assignment (Admin, Employee, Supplier, Customer), blocking/activating users.
    *   **Order Management:** Viewing orders, updating status, simulating shipping/payment.
    *   **Category/Availability Management:** Managing hierarchy and availability methods.

#### 4.3. Public Application (Web & Hybrid)
*   Built with **Blazor WebAssembly** (Web) and **.NET MAUI Blazor Hybrid** (Desktop/Mobile).
*   **Features:**
    *   **Storefront:** Category carousel, product grid with filtering and sorting.
    *   **Shopping Cart:** Client-side cart management with local storage persistence.
    *   **Checkout:** Order placement and simulated payment flow.
    *   **User Account:** Order history and profile management.
    *   **Featured Products:** Randomly selected featured product highlighted in the grid.

---

### 5. Main Problems Encountered and Solutions

#### 5.1. Sharing UI between WebAssembly and Hybrid
*   **Problem:** Ensuring that the same Razor components worked seamlessly in both the browser (WASM) and the native shell (MAUI/Hybrid) without code duplication.
*   **Solution:** We strictly enforced the use of the **Razor Class Library (RCL)**. All pages and components reside in the RCL. The hosting projects (`PWebShop.Web` and `PWebShop.Hybrid`) only contain the `index.html` and startup configuration. We also used dependency injection interfaces (e.g., `IAuthService`) to handle platform-specific differences if necessary (though most logic remained shared).

#### 5.2. Handling Missing Images
*   **Problem:** The UI looked broken when category or product images were missing or failed to load.
*   **Solution:** We implemented a robust `GetImageUrl` helper method in the frontend. It detects missing URLs and generates dynamic placeholder images using `placehold.co`, embedding the category or product name directly into the image for a professional appearance.

#### 5.3. Featured Product Display
*   **Problem:** The initial requirement was a "Hero" section, but it took up too much space and separated the featured item from the browsing flow.
*   **Solution:** We integrated the random featured product directly into the product grid as the **first item**. This logic ensures it appears at the top regardless of the selected category filter, providing visibility without breaking the user experience.

---

### 6. Critical Self-Evaluation
*[This section is to be filled by the students. Reflect on the code quality, what could be improved, and any features that could be more robust.]*

**Example points to consider:**
*   *The separation of concerns in the solution is strong, but more unit tests could have been added.*
*   *The UI is responsive, but accessibility (ARIA labels) could be improved.*
*   *Error handling in the API is present but could be more granular.*

---

### 7. Login Credentials for Each Example User
*[Note: The database seeder will be updated to ensure these users exist with these exact credentials. Details to follow.]*

| Role | Email | Password |
|------|-------|----------|
| **Administrator** | admin@example.com | *[To be confirmed]* |
| **Employee** | employee@example.com | *[To be confirmed]* |
| **Supplier** | label.stone@seed.local | *[To be confirmed]* |
| **Customer** | customer@example.com | *[To be confirmed]* |

---

### 8. List of Business Rules (Domain Rules)

1.  **Product Approval:** Products created by Suppliers are created with `PendingApproval` status. They are not visible in the public catalog until an Employee or Administrator approves them.
2.  **Supplier Isolation:** A Supplier can only manage (edit/delete) their own products. They cannot access products from other suppliers.
3.  **Customer Privacy:** Customers can only view their own order history.
4.  **Role Management:** Only Administrators can assign or revoke `Administrator` and `Employee` roles.
5.  **Self-Protection:** A user cannot delete or deactivate their own account to prevent accidental lockouts.
6.  **Data Integrity:** Users cannot be deleted if they have associated orders or products.
7.  **Pricing:** The Final Price of a product is calculated as `Base Price + (Base Price * Markup Percentage)`.
8.  **Availability:** Only active categories and products are visible in the public storefront.

---

### 9. Screenshots of the Applications

#### 9.1. Public Storefront (Home Page)
SCREENSHOT: [Screenshot of the home page showing the category carousel and product grid with the featured product at the top]

#### 9.2. Product Details & Shopping Cart
SCREENSHOT: [Screenshot of the shopping cart page with items]

#### 9.3. Admin Dashboard (Product Management)
SCREENSHOT: [Screenshot of the Admin app's product list showing status badges (Active, Pending, etc.)]

#### 9.4. User Management (Admin)
SCREENSHOT: [Screenshot of the User management screen showing the role assignment modal]

#### 9.5. Mobile/Hybrid View
SCREENSHOT: [Screenshot of the application running in the mobile/hybrid view]
