# Fresh installation

The `cms-installer` program (project `BolNews.Installer`) sets up a complete website on a **Debian or Ubuntu** server. It asks every question first, then installs and configures everything and checks that the site answers.

## What it does

1. Detects the published website folder (the one that holds the `.dll`, its `.runtimeconfig.json` and `wwwroot`).
2. Checks the .NET runtime. With a self-contained website nothing needs installing; otherwise it installs `aspnetcore-runtime-8.0` through apt.
3. Installs **MariaDB** if no MariaDB/MySQL server is present, and starts it.
4. Creates the database and two users limited to it (no global privileges): `<name>_app` (data access, used by the website) and `<name>_migrator` (all rights on this one database, for upgrades). On the same server it logs in as the system administrator through the local socket, so no manual SQL is needed. Passwords are generated randomly.
5. Creates the tables, seeds defaults, sets the site language and creates the first CMS administrator.
6. Installs **Apache** if missing and enables `proxy`, `proxy_http`, `headers` (and `ssl` when HTTPS is chosen).
7. Writes `/etc/apache2/sites-available/<name>.conf` and enables it. It runs `apache2ctl configtest` first; if Apache rejects it, the new site is removed again and other sites stay untouched. Existing files are never overwritten without asking (a backup is kept).
8. Writes the secure settings file `/etc/<name>/<name>.env` (mode 600, root only): the connection string, `DataProtection__KeyPath`, public address and every API key the user chose to save (Gemini, ChatGPT, RapidAPI, Weather, Forex, optional SMTP). Keys that were skipped appear as commented placeholders.
9. Creates `/etc/systemd/system/<name>.service` on a free local port, starts it and waits until the site answers.

`<name>` is the database name (lower-case, 2 to 24 characters). The product name never appears in anything the installer creates.

## Requirements

- Debian or Ubuntu with internet access (or a local apt mirror), run as root (`sudo`).
- The published website copied to a folder such as `/var/www/aspnetapp/` **before** starting the installer.

## Steps

```sh
tools/package-release.sh linux-x64 ./release      # on the build machine
# copy release/cms-release.tar.gz to the server and unpack it, then:
sudo mkdir -p /var/www/aspnetapp && sudo cp -r website/. /var/www/aspnetapp/
sudo ./installer/cms-installer                    # add --dry-run to rehearse without changing anything
```

Options: `--app-dir /var/www/aspnetapp` skips the folder search; `--dry-run` asks the questions and prints every command and file it would create.

The installer does not close unexpectedly: typing mistakes are asked again, a failed step offers *try again / skip (optional steps) / stop*, Ctrl+C stops cleanly, and every run is logged to `/var/log/<name>-installer.log` (no passwords). It can be run again safely: users and files are updated in place and already-saved API keys can be kept by pressing Enter.

## Encryption keys

`DataProtection__KeyPath` points to `/var/lib/<name>/keys`, owned by the service account. This keeps login cookies and the encrypted TTS provider key valid across restarts. Back this folder up with the database.

## Connection string

The installer stores the connection string in the settings file `/etc/<name>/<name>.env` as `ConnectionStrings__DefaultConnection` (environment variables override `appsettings*.json`). The upgrade user's connection is kept separately in `/etc/<name>/<name>-migrator.env`, which the website never reads. Do not copy production credentials into customer-specific source branches.

## Recovery and reruns

The installer uses `CREATE DATABASE IF NOT EXISTS` and can rerun migrations. If a previous installer run stopped while upgrading a newly created but otherwise empty database, the installer detects the EF migration metadata without CMS tables, removes that incomplete metadata, and retries fresh schema creation. It resets the selected application database user's password to the value entered in that run. If the selected database contains application tables or data, do not run a fresh-install flow against it without a verified backup. The initial Admin account is reused if the entered email already exists; its password is not reset by a rerun.

If installation stops after creating the database, rerun using the same database and application user details. If it fails while applying migrations, inspect the error and database migration history before manually changing schema. Back up the database before retrying after any schema migration has started.

## Operational security

- Restrict database network access to the CMS hosts and use TLS when the DB server is remote.
- Use a unique, high-entropy password for the DB application user and CMS administrator.
- On a local database the users are created only for `localhost` and `127.0.0.1`. When you choose a remote database server you decide the allowed host (default `%`); restrict it where you can.
- Keep the installer available only to deployment operators; remove temporary installer copies after installation if they are not part of the release process.
