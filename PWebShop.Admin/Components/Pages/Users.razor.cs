using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.JSInterop;
using PWebShop.Admin.Models;
using PWebShop.Infrastructure;
using PWebShop.Infrastructure.Identity;

namespace PWebShop.Admin.Components.Pages;

public partial class Users : ComponentBase
{
    [Inject] private IServiceScopeFactory ServiceScopeFactory { get; set; } = default!;
    [Inject] private AuthenticationStateProvider AuthenticationStateProvider { get; set; } = default!;
    [Inject] private IJSRuntime JSRuntime { get; set; } = default!;

    private ApplicationUser? _currentUser;
    private bool _isAdmin;
    private bool _isEmployee;
    private bool _isModalOpen;
    private bool _isEditMode;
    private bool _disableRoleSelection;
    private string? _statusMessage;
    private string? _errorMessage;

    private readonly List<UserListItem> _users = new();
    private readonly List<string> _allRoles = new();
    private List<string> _availableRolesForModal = new();
    private UserEditModel _editModel = new();

    protected override async Task OnInitializedAsync()
    {
        var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
        
        using var scope = ServiceScopeFactory.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        
        _currentUser = await userManager.GetUserAsync(authState.User);

        if (_currentUser is not null)
        {
            _isAdmin = await userManager.IsInRoleAsync(_currentUser, ApplicationRoleNames.Administrator);
            _isEmployee = await userManager.IsInRoleAsync(_currentUser, ApplicationRoleNames.Employee);
        }

        await LoadRolesAsync();
        await LoadUsersAsync();
    }

    private async Task LoadRolesAsync()
    {
        using var scope = ServiceScopeFactory.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        
        var roles = await roleManager.Roles.Select(r => r.Name!).ToListAsync();
        _allRoles.Clear();
        _allRoles.AddRange(roles);
    }

    private async Task LoadUsersAsync()
    {
        using var scope = ServiceScopeFactory.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        
        var users = await userManager.Users
            .AsNoTracking()
            .OrderByDescending(u => u.IsPendingApproval)
            .ThenBy(u => u.DisplayName ?? u.UserName ?? u.Email)
            .ToListAsync();

        _users.Clear();
        foreach (var user in users)
        {
            var roles = await userManager.GetRolesAsync(user);
            _users.Add(new UserListItem
            {
                Id = user.Id,
                Email = user.Email ?? string.Empty,
                DisplayName = string.IsNullOrWhiteSpace(user.DisplayName) ? user.UserName ?? string.Empty : user.DisplayName!,
                Roles = roles.ToList(),
                IsPendingApproval = user.IsPendingApproval,
                IsActive = user.IsActive,
                IsBlocked = user.IsBlocked,
                IsSelf = _currentUser?.Id == user.Id
            });
        }
    }

    private void OpenCreateModal()
    {
        _statusMessage = null;
        _errorMessage = null;
        _isEditMode = false;
        _editModel = new UserEditModel { IsActive = true };
        _availableRolesForModal = GetAvailableRolesForCreate();
        _disableRoleSelection = false;
        _isModalOpen = true;
    }

    private List<string> GetAvailableRolesForCreate()
    {
        if (_isAdmin)
        {
            return _allRoles.ToList();
        }

        return _allRoles.Where(r => r is ApplicationRoleNames.Customer or ApplicationRoleNames.Supplier).ToList();
    }

    private List<string> GetAvailableRolesForEdit()
    {
        if (_isAdmin)
        {
            return _allRoles.ToList();
        }

        // Employees can only work with customer/supplier roles
        return _allRoles.Where(r => r is ApplicationRoleNames.Customer or ApplicationRoleNames.Supplier).ToList();
    }

    private async Task OpenEditModal(string userId)
    {
        _statusMessage = null;
        _errorMessage = null;

        using var scope = ServiceScopeFactory.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var user = await userManager.FindByIdAsync(userId);
        if (user is null)
        {
            _errorMessage = "User not found.";
            return;
        }

        var roles = await userManager.GetRolesAsync(user);

        if (_isEmployee && roles.Any(r => r == ApplicationRoleNames.Administrator || r == ApplicationRoleNames.Employee))
        {
            _errorMessage = "Employees cannot edit Admin or Employee accounts.";
            return;
        }

        _editModel = new UserEditModel
        {
            Id = user.Id,
            Email = user.Email ?? string.Empty,
            DisplayName = user.DisplayName,
            IsActive = user.IsActive,
            IsBlocked = user.IsBlocked,
            IsAdmin = roles.Contains(ApplicationRoleNames.Administrator),
            IsEmployee = roles.Contains(ApplicationRoleNames.Employee),
            IsCustomer = roles.Contains(ApplicationRoleNames.Customer),
            IsSupplier = roles.Contains(ApplicationRoleNames.Supplier)
        };

        _availableRolesForModal = GetAvailableRolesForEdit();
        _disableRoleSelection = _isEmployee && _currentUser?.Id == user.Id;
        _isEditMode = true;
        _isModalOpen = true;
    }

    private void CloseModal()
    {
        _isModalOpen = false;
        _editModel = new UserEditModel();
    }

    private async Task HandleModalSubmit(UserEditModel model)
    {
        if (_isEditMode)
        {
            await UpdateUserAsync(model);
        }
        else
        {
            await CreateUserAsync(model);
        }
    }

    private async Task CreateUserAsync(UserEditModel model)
    {
        _errorMessage = null;
        _statusMessage = null;

        var selectedRoles = GetValidatedRoles(model);
        if (selectedRoles is null)
        {
            return;
        }

        using var scope = ServiceScopeFactory.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var newUser = new ApplicationUser
        {
            UserName = model.Email,
            Email = model.Email,
            DisplayName = model.DisplayName,
            IsActive = model.IsActive,
            IsBlocked = model.IsBlocked
        };
        SetRoleFlags(newUser, selectedRoles);

        var createResult = await userManager.CreateAsync(newUser, model.Password!);
        if (!createResult.Succeeded)
        {
            _errorMessage = FormatErrors(createResult);
            return;
        }

        var roleResult = await userManager.AddToRolesAsync(newUser, selectedRoles);
        if (!roleResult.Succeeded)
        {
            _errorMessage = FormatErrors(roleResult);
            return;
        }

        _statusMessage = "User created successfully.";
        _isModalOpen = false;
        await LoadUsersAsync();
    }

    private bool _isDeleteModalOpen;
    private UserListItem? _userToDelete;

    private bool CanEditUser(UserListItem user)
    {
        if (user.IsSelf)
        {
            return false;
        }

        if (_isAdmin)
        {
            return true;
        }

        if (_isEmployee)
        {
            var hasPrivilegedRole = user.Roles.Any(r => r == ApplicationRoleNames.Administrator || r == ApplicationRoleNames.Employee);
            return !hasPrivilegedRole;
        }

        return false;
    }

    private string GetEditTooltip(UserListItem user)
    {
        if (user.IsSelf)
        {
            return "You cannot edit your own account.";
        }

        if (CanEditUser(user))
        {
            return "Edit user";
        }

        if (_isEmployee && user.Roles.Any(r => r == ApplicationRoleNames.Administrator || r == ApplicationRoleNames.Employee))
        {
            return "Employees cannot edit Administrator or Employee accounts.";
        }

        return "You do not have permission to edit this user.";
    }

    private async Task UpdateUserAsync(UserEditModel model)
    {
        _errorMessage = null;
        _statusMessage = null;

        if (string.IsNullOrWhiteSpace(model.Id))
        {
            _errorMessage = "Invalid user identifier.";
            return;
        }

        using var scope = ServiceScopeFactory.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var user = await userManager.FindByIdAsync(model.Id);
        if (user is null)
        {
            _errorMessage = "User not found.";
            return;
        }

        var currentRoles = await userManager.GetRolesAsync(user);
        var isEditingSelf = _currentUser?.Id == user.Id;

        if (isEditingSelf)
        {
            _errorMessage = "You cannot edit your own account.";
            return;
        }

        if (_isEmployee && currentRoles.Any(r => r == ApplicationRoleNames.Administrator || r == ApplicationRoleNames.Employee))
        {
            _errorMessage = "Employees cannot edit Admin or Employee accounts.";
            return;
        }

        var selectedRoles = GetValidatedRoles(model, isEditingSelf: isEditingSelf, currentRoles: currentRoles);
        if (selectedRoles is null)
        {
            return;
        }

        user.Email = model.Email;
        user.UserName = model.Email;
        user.DisplayName = model.DisplayName;
        user.IsActive = model.IsActive;
        user.IsBlocked = model.IsBlocked;
        SetRoleFlags(user, selectedRoles);

        var updateResult = await userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            _errorMessage = FormatErrors(updateResult);
            return;
        }

        if (!isEditingSelf || _isAdmin)
        {
            var rolesToAdd = selectedRoles.Except(currentRoles).ToList();
            var rolesToRemove = currentRoles.Except(selectedRoles).ToList();

            if (rolesToRemove.Count > 0)
            {
                var removeResult = await userManager.RemoveFromRolesAsync(user, rolesToRemove);
                if (!removeResult.Succeeded)
                {
                    _errorMessage = FormatErrors(removeResult);
                    return;
                }
            }

            if (rolesToAdd.Count > 0)
            {
                var addResult = await userManager.AddToRolesAsync(user, rolesToAdd);
                if (!addResult.Succeeded)
                {
                    _errorMessage = FormatErrors(addResult);
                    return;
                }
            }
        }

        if (!string.IsNullOrWhiteSpace(model.Password))
        {
            var resetToken = await userManager.GeneratePasswordResetTokenAsync(user);
            var resetResult = await userManager.ResetPasswordAsync(user, resetToken, model.Password);
            if (!resetResult.Succeeded)
            {
                _errorMessage = FormatErrors(resetResult);
                return;
            }
        }

        _statusMessage = "User updated successfully.";
        _isModalOpen = false;
        await LoadUsersAsync();
    }

    private async Task ApproveUserAsync(string userId)
    {
        _errorMessage = null;
        _statusMessage = null;

        using var scope = ServiceScopeFactory.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var user = await userManager.FindByIdAsync(userId);
        if (user is null)
        {
            _errorMessage = "User not found.";
            return;
        }

        var roles = await userManager.GetRolesAsync(user);
        if (_isEmployee && roles.Any(r => r == ApplicationRoleNames.Administrator || r == ApplicationRoleNames.Employee))
        {
            _errorMessage = "Employees cannot approve Admin or Employee accounts.";
            return;
        }

        user.IsPendingApproval = false;
        user.IsActive = true;
        user.IsBlocked = false;

        var updateResult = await userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            _errorMessage = FormatErrors(updateResult);
            return;
        }

        _statusMessage = "User approved successfully.";
        await LoadUsersAsync();
    }

    private async Task DeclineUserAsync(string userId)
    {
        _errorMessage = null;
        _statusMessage = null;

        using var scope = ServiceScopeFactory.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var user = await userManager.FindByIdAsync(userId);
        if (user is null)
        {
            _errorMessage = "User not found.";
            return;
        }

        var roles = await userManager.GetRolesAsync(user);
        if (_isEmployee && roles.Any(r => r == ApplicationRoleNames.Administrator || r == ApplicationRoleNames.Employee))
        {
            _errorMessage = "Employees cannot decline Admin or Employee accounts.";
            return;
        }

        user.IsPendingApproval = false;
        user.IsActive = false;
        user.IsBlocked = true;

        var updateResult = await userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            _errorMessage = FormatErrors(updateResult);
            return;
        }

        _statusMessage = "User declined successfully.";
        await LoadUsersAsync();
    }

    private async Task DeleteUserAsync(string userId)
    {
        _errorMessage = null;
        _statusMessage = null;

        if (_currentUser?.Id == userId)
        {
            _errorMessage = "You cannot delete your own account.";
            return;
        }

        using var scope = ServiceScopeFactory.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var user = await userManager.FindByIdAsync(userId);
        if (user is null)
        {
            _errorMessage = "User not found.";
            return;
        }

        var userRoles = await userManager.GetRolesAsync(user);

        if (!_isAdmin)
        {
            if (!_isEmployee)
            {
                _errorMessage = "Only administrators or employees can delete users.";
                return;
            }

            var hasPrivilegedRole = userRoles.Any(r => r == ApplicationRoleNames.Administrator || r == ApplicationRoleNames.Employee);
            if (hasPrivilegedRole)
            {
                _errorMessage = "Employees cannot delete Admin or Employee accounts.";
                return;
            }
        }

        var result = await userManager.DeleteAsync(user);
        if (!result.Succeeded)
        {
            _errorMessage = FormatErrors(result);
            return;
        }

        _statusMessage = "User deleted successfully.";
        await LoadUsersAsync();
    }

    private void ConfirmDeleteAsync(UserListItem user)
    {
        if (!CanDeleteUser(user))
        {
            return;
        }

        _userToDelete = user;
        _isDeleteModalOpen = true;
    }

    [Inject] private IDbContextFactory<AppDbContext> DbContextFactory { get; set; } = default!;

    private async Task HandleDeleteConfirmed()
    {
        if (_userToDelete != null)
        {
            try 
            {
                await using var dbContext = await DbContextFactory.CreateDbContextAsync();
                
                var hasOrders = await dbContext.Orders.AnyAsync(o => o.CustomerId == _userToDelete.Id);
                if (hasOrders)
                {
                    _errorMessage = "Cannot delete user because they have existing orders. Deletion would violate data integrity.";
                    _isDeleteModalOpen = false;
                    _userToDelete = null;
                    return;
                }

                var hasProducts = await dbContext.Products.AnyAsync(p => p.SupplierId == _userToDelete.Id);
                if (hasProducts)
                {
                    _errorMessage = "Cannot delete user because they have existing products. Deletion would violate data integrity.";
                    _isDeleteModalOpen = false;
                    _userToDelete = null;
                    return;
                }

                await DeleteUserAsync(_userToDelete.Id);
            }
            catch (Exception ex)
            {
                _errorMessage = $"An error occurred while verifying user data: {ex.Message}";
            }
        }
        _isDeleteModalOpen = false;
        _userToDelete = null;
    }

    private void HandleDeleteCancelled()
    {
        _isDeleteModalOpen = false;
        _userToDelete = null;
    }

    private List<string>? GetValidatedRoles(UserEditModel model, bool isEditingSelf = false, IEnumerable<string>? currentRoles = null)
    {
        var selectedRoles = model.GetSelectedRoles();

        if (!selectedRoles.Any())
        {
            _errorMessage = "Select at least one role.";
            return null;
        }

        if (_isEmployee)
        {
            if (isEditingSelf && currentRoles is not null)
            {
                // Employees cannot change their own roles
                return currentRoles.ToList();
            }

            var allowedRoles = new[] { ApplicationRoleNames.Customer, ApplicationRoleNames.Supplier };
            if (selectedRoles.Except(allowedRoles).Any())
            {
                _errorMessage = "Employees can only assign Customer or Supplier roles.";
                return null;
            }
        }

        return selectedRoles;
    }

    private static void SetRoleFlags(ApplicationUser user, IEnumerable<string> roles)
    {
        user.IsAdministrator = roles.Contains(ApplicationRoleNames.Administrator);
        user.IsEmployee = roles.Contains(ApplicationRoleNames.Employee);
        user.IsCustomer = roles.Contains(ApplicationRoleNames.Customer);
        user.IsSupplier = roles.Contains(ApplicationRoleNames.Supplier);
    }

    private static string FormatErrors(IdentityResult result) => string.Join(" ", result.Errors.Select(e => e.Description));

    private bool CanDeleteUser(UserListItem user)
    {
        if (user.IsSelf)
        {
            return false;
        }

        if (_isAdmin)
        {
            return true;
        }

        if (_isEmployee)
        {
            var hasPrivilegedRole = user.Roles.Any(r => r == ApplicationRoleNames.Administrator || r == ApplicationRoleNames.Employee);
            return !hasPrivilegedRole;
        }

        return false;
    }

    private string GetDeleteTooltip(UserListItem user)
    {
        if (user.IsSelf)
        {
            return "You cannot delete your own account.";
        }

        if (_isAdmin)
        {
            return "Delete user";
        }

        if (!_isEmployee)
        {
            return "You do not have permission to delete users.";
        }

        if (user.Roles.Any(r => r == ApplicationRoleNames.Administrator || r == ApplicationRoleNames.Employee))
        {
            return "Employees cannot delete Admin or Employee accounts.";
        }

        return "Delete user";
    }

    private class UserListItem
    {
        public string Id { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public List<string> Roles { get; set; } = new();
        public bool IsActive { get; set; }
        public bool IsPendingApproval { get; set; }
        public bool IsBlocked { get; set; }
        public bool IsSelf { get; set; }
    }
}
