# Deployment Guide — Azure VM (Windows Server + IIS)

Status: **Agreed** · Owner: **all four members** (this earns group marks, not individual ones)
Target: **4 group marks** — "Hosting the Web API / Service on IIS (4)" + counts toward "MongoDB connection (4)"

This is the step-by-step version of `system-overview.md` §6. Do this once, early — **Day 13 in
`plan/work-breakdown.md` at the latest, ideally sooner** so both clients have a stable hosted URL to
build against instead of `localhost`.

---

## 0. What "deployed" actually means here, and what it doesn't

| Marked | Not marked, don't bother |
|---|---|
| API reachable over the network from both clients, on IIS | Auto-scaling, load balancing |
| MongoDB reachable from the hosted API | A managed CI/CD pipeline that redeploys on every push |
| A repeatable way to redeploy after a code change | A custom domain name |
| Secrets kept out of the repo | A production-grade cert from a paid CA |

This is a manual deploy you repeat by hand a handful of times over 15 days, not a pipeline. Don't over-build it.

## 1. One person owns the VM; everyone else gets the URL

Pick **one member** to hold the Azure account and the VM's admin password. Everyone else needs:
the VM's public IP (or DNS name), and to be added to `appsettings.Production.json`'s CORS origins if
you're testing from your own machine's web build. RDP access is optional — most of the team only
needs the base URL, recorded in `project-config.md` §9 once it exists.

## 2. Create the VM

**Fast path (Azure CLI)** — run this once, from any machine with the CLI installed and `az login` done:

```bash
az group create --name smartsolar-rg --location eastus

az vm create \
  --resource-group smartsolar-rg \
  --name smartsolar-api \
  --image Win2022Datacenter \
  --size Standard_B2s \
  --admin-username smartsolaradmin \
  --admin-password '<set a strong password — you will type this into RDP>' \
  --public-ip-sku Standard

az vm open-port --resource-group smartsolar-rg --name smartsolar-api --port 3389 --priority 900   # RDP
az vm open-port --resource-group smartsolar-rg --name smartsolar-api --port 80   --priority 910   # API (HTTP)
az vm open-port --resource-group smartsolar-rg --name smartsolar-api --port 443  --priority 920   # API (HTTPS, once set up)
```

`Standard_B2s` (2 vCPU, 4 GB) is enough for an API + a small IIS site and stays cheap on student
credit. Note the `publicIpAddress` the command prints — that's your base URL until you set up a name.

**Portal path**, if you'd rather click: Create a resource → Windows Server 2022 Datacenter → same size
→ under Networking, allow inbound RDP (3389), HTTP (80), HTTPS (443).

> **Restrict RDP to your own IP** if you can (`az vm open-port` with `--priority` lower than a
> `--source-address-prefixes` rule, or edit the NSG in the portal). Leaving RDP open to the whole
> internet is the most common way a student Azure VM gets compromised within days.

## 3. Cost control — stop it when you're not using it

Azure charges by the hour while the VM is running, even mid-build. Stop it (deallocate, not just
shut down inside Windows) whenever nobody needs it:

```bash
az vm deallocate --resource-group smartsolar-rg --name smartsolar-api   # stop billing
az vm start      --resource-group smartsolar-rg --name smartsolar-api   # resume — note: public IP may change unless you used --public-ip-sku Standard with a static allocation
```

If the IP changes on restart, every client's base URL breaks until updated — this is the one reason to
set a static public IP (the CLI command above already requests `Standard` SKU, which defaults to static).

## 4. RDP in and prepare the VM

Connect with Remote Desktop to the public IP, using the admin username/password from step 2. Then, in
an elevated PowerShell prompt on the VM:

```powershell
# Enable the IIS role and the management console
Install-WindowsFeature -Name Web-Server,Web-Mgmt-Console -IncludeManagementTools

# Download and install the ASP.NET Core Hosting Bundle (lets IIS run a .NET 8 app)
# Check https://dotnet.microsoft.com/download/dotnet/8.0 for the current exact link if this 404s.
Invoke-WebRequest -Uri "https://dotnet.microsoft.com/download/dotnet/8.0" -OutFile "$env:TEMP\hint.html"
# Get the "ASP.NET Core Runtime 8.0.x - Hosting Bundle" installer link from that page in a browser,
# then on the VM:
Invoke-WebRequest -Uri "<hosting-bundle-download-url>" -OutFile "$env:TEMP\dotnet-hosting-bundle.exe"
Start-Process "$env:TEMP\dotnet-hosting-bundle.exe" -ArgumentList "/quiet /norestart" -Wait

# The hosting bundle installs an IIS module (ASP.NET Core Module v2) - restart IIS so it's picked up
net stop was /y
net start w3svc
```

Confirm it worked: `Get-WebGlobalModule -Name AspNetCoreModuleV2` should return a result, not an error.

## 5. Create the IIS site and app pool

```powershell
Import-Module WebAdministration

New-Item "IIS:\AppPools\SmartSolarApi" -Force
Set-ItemProperty "IIS:\AppPools\SmartSolarApi" -Name managedRuntimeVersion -Value ""   # "No Managed Code" - required for .NET Core

New-Item "C:\inetpub\smartsolar-api" -ItemType Directory -Force

New-Website -Name "SmartSolarApi" -PhysicalPath "C:\inetpub\smartsolar-api" `
  -ApplicationPool "SmartSolarApi" -Port 80 -Force
```

## 6. Publish the API from your dev machine and copy it up

On your **own machine** (not the VM), from the repo root:

```bash
dotnet publish backend/src/SmartSolar.Api/SmartSolar.Api.csproj -c Release -o ./publish-output
```

Copy the contents of `publish-output/` to `C:\inetpub\smartsolar-api\` on the VM — over RDP clipboard
file copy, `scp` if you enable it, or zip it, upload via the Azure Portal's "Upload files" in Bastion/
Serial console, or a shared Azure Files share if you set one up. For a team of four redeploying
repeatedly, the least fiddly option is usually: zip → upload via RDP clipboard → unzip with
`Expand-Archive` on the VM.

**Before copying**, on the VM stop the app pool so files aren't locked:

```powershell
Stop-WebAppPool -Name "SmartSolarApi"
# ... copy/overwrite files in C:\inetpub\smartsolar-api ...
Start-WebAppPool -Name "SmartSolarApi"
```

`scripts/deploy-to-iis.ps1` in this repo automates the stop/copy/start loop — run it **on the VM**
after you've already copied a fresh `publish-output` folder there. See that script's header for usage.

## 7. Secrets — appsettings.Production.json lives only on the VM

**Never commit this file.** On the VM, create
`C:\inetpub\smartsolar-api\appsettings.Production.json` by hand (RDP in, use Notepad), based on
`backend/src/SmartSolar.Api/appsettings.Development.json.example`:

```json
{
  "Mongo": {
    "ConnectionString": "<your MongoDB Atlas connection string>",
    "DatabaseName": "smartsolar"
  },
  "Jwt": {
    "SigningKey": "<a long random value, different from any example you've seen>"
  },
  "Qr": {
    "SigningSecret": "<a different long random value>"
  }
}
```

Set the app pool's environment so ASP.NET Core actually loads this file:

```powershell
Set-ItemProperty "IIS:\AppPools\SmartSolarApi" -Name processModel.environmentVariables `
  -Value @{ ASPNETCORE_ENVIRONMENT = "Production" }
```

## 8. MongoDB — Atlas free tier, and let the VM in

1. [cloud.mongodb.com](https://cloud.mongodb.com) → create an M0 (free) cluster.
2. Database Access → create a user with a strong password, read/write on the `smartsolar` database.
3. Network Access → **Add IP Address** → enter the Azure VM's public IP (from step 2). Do **not** use
   "Allow access from anywhere" (`0.0.0.0/0`) for the actual demo build — that's an easy, avoidable
   security ding.
4. Copy the connection string (Atlas gives you a `mongodb+srv://...` URI) into
   `appsettings.Production.json` from step 7.
5. Run the seed script once against the hosted cluster:
   ```bash
   mongosh "<your connection string>/smartsolar" seed/seed.js
   ```

## 9. Smoke-test the API before touching the clients

From your own machine:

```bash
curl http://<vm-public-ip>/api/health
# expect: {"status":"ok","service":"SmartSolar.Api","utc":"..."}
```

If this fails, check (in order): Windows Firewall on the VM (port 80 inbound allowed — the Azure NSG
rule alone isn't enough, Windows has its own firewall too), the app pool is `Started` in IIS Manager,
`Get-EventLog -LogName Application -Newest 20` on the VM for a startup exception.

## 10. Point both clients at the hosted API

| Client | Change |
|---|---|
| Web app | `web/.env` → `VITE_API_BASE_URL=http://<vm-public-ip>/api`, then `npm run build` again |
| Android | `mobile/app/build.gradle.kts` → `buildConfigField("String", "API_BASE_URL", "\"http://<vm-public-ip>/api/\"")` |

Record the real base URL in `project-config.md` §9 (add it — see the stub below) so nobody hard-codes
a stale IP into a branch.

## 11. HTTPS — do this if you have a spare hour, skip it if you don't

The assignment does not require HTTPS, and `usesCleartextTraffic="true"` is already set in the Android
manifest for exactly this reason. If you want it anyway (it's a legitimate line for the report's
"challenges" section either way — "we noted this and chose HTTP for the demo, here's why"):

```powershell
# On the VM - win-acme gets a free Let's Encrypt cert, but needs a real DNS name pointing at the VM,
# not just a bare IP. Set up an Azure DNS name label on the VM's public IP first (portal: the public
# IP resource -> Configuration -> DNS name label), then:
Invoke-WebRequest -Uri "https://github.com/win-acme/win-acme/releases/latest" -OutFile "$env:TEMP\wacs.zip"
Expand-Archive "$env:TEMP\wacs.zip" -DestinationPath "C:\win-acme"
C:\win-acme\wacs.exe   # interactive - point it at the SmartSolarApi site
```

If you skip this, use `http://` consistently everywhere (clients, CORS origins, the URL you demo from)
— a mix of `http://` and `https://` is a more common source of a broken demo than no HTTPS at all.

## 12. Redeploying after a code change — the loop you'll repeat

```bash
# on your machine
dotnet publish backend/src/SmartSolar.Api/SmartSolar.Api.csproj -c Release -o ./publish-output
# copy publish-output/* to the VM (RDP clipboard, zip+upload, whatever you settled on)
```
```powershell
# on the VM, after the fresh files are in place
./scripts/deploy-to-iis.ps1
```

Do this after every merge to `develop` that touches `backend/`, not just once at the end — an API that
has drifted from what both clients were tested against for a week is how integration breaks on Day 28.

## 13. Troubleshooting

| Symptom | Likely cause |
|---|---|
| `502.5 Process Failure` in the browser | Hosting Bundle not installed, or `ASPNETCORE_ENVIRONMENT` not set to `Production` |
| Works via RDP-local `curl localhost`, not from your own machine | Windows Firewall on the VM, not just the Azure NSG |
| API starts but every request 500s | `appsettings.Production.json` missing, malformed, or the Mongo connection string wrong |
| Mongo connection times out | Atlas Network Access doesn't have the VM's current public IP (it changed after a deallocate/start without a static IP) |
| Android emulator can't reach it, physical device can | You're still using `10.0.2.2` — that only works for a locally-hosted API, not the VM |

---

## Definition of done for this document

- [ ] VM created, public IP recorded in `project-config.md`
- [ ] IIS + Hosting Bundle installed, site + app pool created
- [ ] `appsettings.Production.json` present on the VM only, never committed
- [ ] MongoDB Atlas cluster created, VM's IP allow-listed, seed data loaded
- [ ] `GET /api/health` reachable from a machine that is not the VM itself
- [ ] Web app and Android both rebuilt against the hosted base URL
- [ ] At least one full redeploy has been done using `scripts/deploy-to-iis.ps1`, so the process is proven before you need it under time pressure
