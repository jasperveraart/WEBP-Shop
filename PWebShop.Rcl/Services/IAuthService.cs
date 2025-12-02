using PWebShop.Rcl.Dtos;

namespace PWebShop.Rcl.Services;

public interface IAuthService
{
    Task<AuthResultDto> Login(LoginRequestDto loginRequest);
    Task<AuthResultDto> RegisterCustomer(RegisterCustomerRequestDto registerRequest);
    Task<AuthResultDto> RegisterSupplier(RegisterSupplierRequestDto registerRequest);
    Task Logout();
    Task<CurrentUserResponseDto?> GetCurrentUser();
    Task<AuthResultDto> UpdateProfile(UpdateProfileRequestDto request);
    Task<AuthResultDto> ChangePassword(ChangePasswordRequestDto request);
}
