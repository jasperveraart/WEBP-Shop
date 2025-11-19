using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace PWebShop.Admin.Data;

public class AdminDbContext(DbContextOptions<AdminDbContext> options) : IdentityDbContext<ApplicationUser>(options)
{
}
