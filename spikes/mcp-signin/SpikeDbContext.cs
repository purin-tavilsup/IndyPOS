using Microsoft.EntityFrameworkCore;

namespace McpSignIn;

public sealed class SpikeDbContext(DbContextOptions<SpikeDbContext> options) : DbContext(options);
