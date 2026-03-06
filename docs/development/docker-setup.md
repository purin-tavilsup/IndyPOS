# Docker Setup for Local Development

## Prerequisites

- Docker Desktop installed
- Docker Compose (included with Docker Desktop)

## Quick Start

```bash
# From project root
docker-compose up -d

# Check status
docker-compose ps

# View logs
docker-compose logs -f postgres
```

## Services

### PostgreSQL

- **Host**: localhost
- **Port**: 5432
- **Database**: indypos_storehub
- **Username**: indypos_app
- **Password**: indypos_dev_password

### pgAdmin (Optional)

- **URL**: http://localhost:5050
- **Email**: admin@indypos.local
- **Password**: admin

## Connection Strings

### For StoreHub Development

```json
{
  "ConnectionStrings": {
    "StoreHubDb": "Host=127.0.0.1;Port=5432;Database=indypos_storehub;Username=indypos_app;Password=indypos_dev_password"
  }
}
```

### For EF Core Migrations

```bash
dotnet ef database update --connection "Host=127.0.0.1;Port=5432;Database=indypos_storehub;Username=indypos_app;Password=indypos_dev_password"
```

## Commands

```bash
# Start services
docker-compose up -d

# Stop services
docker-compose down

# Stop and remove volumes (reset data)
docker-compose down -v

# Rebuild from scratch
docker-compose down -v && docker-compose up -d --build

# Connect to PostgreSQL CLI
docker exec -it indypos-postgres psql -U indypos_app -d indypos_storehub
```

## Useful SQL Commands

```sql
-- List tables
\dt

-- Describe table
\d table_name

-- Show outbox events
SELECT * FROM outbox ORDER BY created_utc DESC LIMIT 10;

-- Count invoices per day
SELECT DATE(created_utc), COUNT(*)
FROM invoice
GROUP BY DATE(created_utc)
ORDER BY 1 DESC;
```

## Troubleshooting

### Port Already in Use

```bash
# Check what's using port 5432
netstat -ano | findstr :5432

# Kill the process or change the port in docker-compose.yml
```

### Connection Refused

1. Ensure Docker is running
2. Check container status: `docker-compose ps`
3. Check logs: `docker-compose logs postgres`

### Reset Everything

```bash
docker-compose down -v
docker system prune -f
docker-compose up -d
```
