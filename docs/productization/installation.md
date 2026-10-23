# Fresh installation

The CMS supports a fresh MySQL or MariaDB database through the interactive `BolNews.Installer` utility. It creates the schema from the EF Core migrations shipped with the application, creates the first CMS Admin account, and writes the application connection string to `appsettings.Production.json`.

## Requirements

- .NET 8 runtime (or publish the installer self-contained for the target operating system).
- A reachable MySQL/MariaDB server and a database administrator account allowed to create databases and users, grant permissions, and apply schema changes.
- The installer and application configuration path must be protected. The generated production settings file contains the application database password.

## Install steps

1. Publish the CMS and installer for the server's operating system. From the repository root, for example:

   ```sh
   dotnet publish BolNews.Web/BolNews.Web.csproj -c Release -o ./publish
   dotnet publish BolNews.Installer/BolNews.Installer.csproj -c Release -o ./installer
   ```

   Copy both output folders to the server. The installer needs the `BolNews.Persistence` migrations assembly and its dependencies; its publish output includes them.

2. Run the installer interactively:

   ```sh
   cd /var/www/aspnetapp
   dotnet /path/to/installer/BolNews.Installer.dll
   ```

   The prompts ask for the database server/port, new database name, a database administrator account, an application database username/password, and the first CMS administrator's email/password. Passwords are entered without echoing them to the terminal. The database administrator credential is used only during installation; it is not written to the application config.

3. At the final prompt, enter the full path to the deployed app's `appsettings.Production.json`, normally `/var/www/aspnetapp/appsettings.Production.json`. The installer writes `ConnectionStrings:DefaultConnection` there and restricts the file to owner read/write on Linux. If you ran the installer as root, give the file to the web service account:

   ```sh
   sudo chown www-data:www-data /var/www/aspnetapp/appsettings.Production.json
   sudo chmod 600 /var/www/aspnetapp/appsettings.Production.json
   ```

   Substitute the actual service account and application path. Do not place the file in source control or a public download directory.

4. The installer creates the database and application DB user, applies all included EF migrations, then creates the CMS Admin account. It grants the application database user data access only; database schema changes are performed by the installer using the database administrator connection. Configure the service with `ASPNETCORE_ENVIRONMENT=Production`, ensure the application process can read the config file, and restart the service.

5. Sign in using the administrator email/password chosen during installation. Complete the publisher branding, policy, ad placement, email, and other environment-specific settings before opening the site to readers.

## Connection string

The installer generates the MySQL connection string from the entered host, port, database, and dedicated application user. It stores it at:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=...;Port=3306;Database=...;User ID=...;Password=...;..."
  }
}
```

Use this same value for future deployments of that installation, or move it to a protected secret store/environment variable named `ConnectionStrings__DefaultConnection`. Environment variables override the JSON file. Do not copy production credentials into customer-specific source branches.

## Recovery and reruns

The installer uses `CREATE DATABASE IF NOT EXISTS` and can rerun migrations. It resets the selected application database user's password to the value entered in that run. If the selected database contains data, do not run a fresh-install flow against it without a verified backup. The initial Admin account is reused if the entered email already exists; its password is not reset by a rerun.

If installation stops after creating the database, rerun using the same database and application user details. If it fails while applying migrations, inspect the error and database migration history before manually changing schema. Back up the database before retrying after any schema migration has started.

## Operational security

- Restrict database network access to the CMS hosts and use TLS when the DB server is remote.
- Use a unique, high-entropy password for the DB application user and CMS administrator.
- The DB application user is created for host `%` for compatibility with separately hosted web/database servers. Restrict its permitted source hosts in MySQL/MariaDB firewall or account configuration after install.
- Keep the installer available only to deployment operators; remove temporary installer copies after installation if they are not part of the release process.
