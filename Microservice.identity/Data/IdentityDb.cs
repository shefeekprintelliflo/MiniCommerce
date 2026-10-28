using Microservice.identity.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Microservice.identity.Data;

public class IdentityDb(DbContextOptions<IdentityDb> options) : IdentityDbContext<AppUser, IdentityRole<Guid>, Guid>(options);
