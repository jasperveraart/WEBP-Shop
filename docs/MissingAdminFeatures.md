# Missing Admin Features

Based on the analysis of `docs/ProjectDescription.md` and the current implementation of the Admin application, the following features are missing or incomplete:

## 1. Availability Management
**Requirement:** CRUD operations for Availability Methods (e.g., Physical Shipping, Digital Download).
**Status:** **Missing**.
- There is no UI to manage `AvailabilityMethod` entities.
- Admins cannot create, edit, or delete availability methods.


## 3. User Management
**Requirement:** Activate/deactivate users; Prevent self-deletion.
**Status:** **Partially Missing**.
- **Block/Unblock:** While there is "Approve/Decline" for pending users, there is no explicit "Block" or "Deactivate" toggle for *already active* users in the main list or edit modal.
- **Self-Deletion:** The "Delete" button is disabled for the current user, which satisfies "Prevent self-deletion", but explicit "Deactivate" (soft delete/ban) is missing for active users.

## 4. Product Management
**Requirement:** Permanently delete (only if no sales exist).
**Status:** **Incomplete**.
- **Deletion Logic:** The product list (`Products.razor`) does not have a delete button. The edit page (`ProductEdit.razor`) might have one, but the specific check "only if no sales exist" needs to be verified or implemented to prevent database constraint violations or data loss.
