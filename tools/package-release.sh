#!/usr/bin/env bash
# Builds a customer-ready bundle: the website and the installer, both self-contained for Linux, so the
# customer's server needs no .NET installation at all.
#
# Usage:  tools/package-release.sh [runtime-id] [output-folder]
#         tools/package-release.sh linux-x64 ./release
set -euo pipefail
RID="${1:-linux-x64}"
OUT="${2:-./release}"
cd "$(dirname "$0")/.."

rm -rf "$OUT"
mkdir -p "$OUT"

echo "Publishing the website ($RID, self-contained)..."
dotnet publish BolNews.Web/BolNews.Web.csproj -c Release -r "$RID" --self-contained true -o "$OUT/website"

echo "Publishing the installer ($RID, self-contained)..."
dotnet publish BolNews.Installer/BolNews.Installer.csproj -c Release -r "$RID" --self-contained true -o "$OUT/installer"

cat > "$OUT/INSTALL.txt" <<'TXT'
Installing the CMS on a Debian or Ubuntu server
================================================
1. Copy this whole folder to the server (for example with scp), then unpack it if it is an archive.
2. Copy the "website" folder to where the site should live, for example:
       sudo mkdir -p /var/www/aspnetapp
       sudo cp -r website/. /var/www/aspnetapp/
3. Start the installer and answer the questions:
       sudo chmod +x installer/cms-installer
       sudo ./installer/cms-installer
   (Add --dry-run to rehearse it first without changing anything.)

The installer installs MariaDB and Apache if they are missing, creates the database and its users, saves the
settings and API keys in a protected file, and creates the Apache site and the service.
TXT

(cd "$OUT" && tar czf ../cms-release.tar.gz . ) && mv "$OUT/../cms-release.tar.gz" "$OUT/cms-release.tar.gz"
echo "Done. Bundle: $OUT/cms-release.tar.gz"
