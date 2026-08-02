<#
.SYNOPSIS
  Publie Vector.Api sur le serveur de DEV ou de PROD.

.DESCRIPTION
  La cible = un profil de publication (.pubxml) qui copie la build Release
  sur le partage UNC du serveur :
    dev  -> \\192.168.1.112\dev_api\Vector.Api
    prod -> \\192.168.1.112\prod_api\Vector.Api

  Le web.config (ASPNETCORE_ENVIRONMENT + secrets) est gere MANUELLEMENT sur le
  serveur (csproj : IsTransformWebConfigDisabled=true) -- rien a tamponner ici.

  ATTENTION : appsettings.json EST ecrase par la publication (il fait partie de la
  sortie) et porte les valeurs de dev. Toute valeur specifique a la prod doit vivre
  dans appsettings.Production.json, qui est shippe et surcharge la base.

  La publication vers PROD demande une confirmation explicite (-Force pour la sauter,
  p.ex. en CI). La publication pose app_offline.htm : l'API est coupee le temps de la copie.

  Prerequis (1re fois) : une session ouverte sur le partage cible
    net use \\192.168.1.112\dev_api  /user:192.168.1.112\DeployApi *
    net use \\192.168.1.112\prod_api /user:192.168.1.112\DeployApi *

.EXAMPLE
  .\deploy.ps1            # publie sur le serveur de dev
  .\deploy.ps1 dev
  .\deploy.ps1 prod       # demande confirmation
  .\deploy.ps1 prod -Force
#>
[CmdletBinding()]
param(
    [Parameter(Position = 0)]
    [ValidateSet('dev', 'prod')]
    [string]$Target = 'dev',

    # Saute la confirmation interactive de la cible PROD (usage non-interactif / CI).
    [switch]$Force
)

$ErrorActionPreference = 'Stop'
$proj = Join-Path $PSScriptRoot 'CaSoft.Erp.USVector.Api\CaSoft.Erp.USVector.Api.csproj'

# Cible -> profil pubxml
$profileOf = @{
    dev  = 'IIS-DevServer'
    prod = 'IIS-ProdServer'
}

# Cibles sensibles : confirmation explicite avant de couper l'app et d'ecraser la sortie.
$protected = @('prod')

function Confirm-Target($key, $url) {
    if ($key -notin $protected -or $Force) { return }

    Write-Host ""
    Write-Host "  /!\  Publication en $($key.ToUpper()) : $url" -ForegroundColor Yellow
    Write-Host "       L'API sera coupee (app_offline.htm) le temps de la copie." -ForegroundColor Yellow
    $answer = Read-Host "       Taper $key pour confirmer"
    if ($answer -ne $key) { throw "Publication $key annulee (confirmation non saisie)." }
}

function Publish-Target($key) {
    $profileName = $profileOf[$key]
    Write-Host ""
    Write-Host "-> Publication $($key.ToUpper()) (profil $profileName)" -ForegroundColor Cyan

    # Cible de copie reelle = publishUrl du profil (UNC). Source unique : le .pubxml.
    $pubxml = Join-Path $PSScriptRoot "CaSoft.Erp.USVector.Api\Properties\PublishProfiles\$profileName.pubxml"
    [xml]$x = Get-Content -LiteralPath $pubxml
    $url = ([string]$x.SelectSingleNode('//*[local-name()="publishUrl"]').InnerText).Trim()
    if (-not $url) { throw "publishUrl introuvable dans $pubxml." }

    # Pre-vol : sans session ouverte sur le partage, MSBuild echoue tard et le message est obscur
    # (et sur une cible protegee on aurait deja pose app_offline.htm). On tranche avant.
    if (-not (Test-Path -LiteralPath $url)) {
        # Racine du partage = \\serveur\partage (Split-Path -Qualifier ne gere pas l'UNC).
        $share = if ($url -match '^(\\\\[^\\]+\\[^\\]+)') { $Matches[1] } else { $url }
        throw "Cible $url inaccessible. Ouvrir la session : net use $share /user:192.168.1.112\DeployApi *"
    }

    Confirm-Target $key $url

    dotnet publish $proj -c Release "/p:PublishProfile=$profileName"
    if ($LASTEXITCODE) { throw "Echec de la publication $key (profil $profileName)." }

    # Verification robuste : l'assembly applicatif le plus recent produit par la build (bin local)
    # doit exister a l'IDENTIQUE (meme horodatage) sur l'UNC. La copie de publication preserve
    # l'horodatage source, donc bin == UNC => ce build a bien atteint la cible.
    # Insensible : (a) a quel assembly a change (on prend le plus recent du bin, pas Api.dll seul),
    # (b) a un pre-build (on ne compare pas a un startUtc mais bin<->UNC).
    $binDir = Join-Path $PSScriptRoot 'CaSoft.Erp.USVector.Api\bin\Release'
    $localNewest = Get-ChildItem -LiteralPath $binDir -Recurse -Filter 'CaSoft.*.dll' -ErrorAction SilentlyContinue |
        Sort-Object LastWriteTimeUtc -Descending | Select-Object -First 1
    if (-not $localNewest) { throw "Verif KO : build local introuvable sous $binDir." }

    $uncFile = Join-Path $url $localNewest.Name
    if (-not (Test-Path -LiteralPath $uncFile)) {
        throw "Verif KO : $($localNewest.Name) absent de $url. La publication n'a pas atteint la cible."
    }
    $uncWriteUtc = (Get-Item -LiteralPath $uncFile).LastWriteTimeUtc
    if ([Math]::Abs(($uncWriteUtc - $localNewest.LastWriteTimeUtc).TotalSeconds) -gt 2) {
        throw "Verif KO : $($localNewest.Name) sur $url ($uncWriteUtc) ne correspond pas au build local " +
              "($($localNewest.LastWriteTimeUtc)). La publication est peut-etre partie en local sans atteindre $url."
    }
    # Verification de la CONFIG (KC-1) : depuis que Keycloak:Authority vient de la config, un binaire
    # a jour pose sur un appsettings.json perime fait ECHOUER LE DEMARRAGE (garde-fou Program.cs).
    # MSBuild peut sauter la copie d'un fichier de contenu (incrementalite) : on le verifie donc.
    $cfgLocal = Join-Path $PSScriptRoot 'CaSoft.Erp.USVector.Api\appsettings.json'
    $cfgUnc = Join-Path $url 'appsettings.json'
    if (-not (Test-Path -LiteralPath $cfgUnc)) { throw "Verif KO : appsettings.json absent de $url." }
    if ((Get-FileHash -LiteralPath $cfgLocal).Hash -ne (Get-FileHash -LiteralPath $cfgUnc).Hash) {
        throw "Verif KO : appsettings.json de $url differe du depot. La publication ne l'a pas copie " +
              "(incrementalite MSBuild) : copier le fichier a la main, sinon l'API refusera de demarrer."
    }

    Write-Host "[OK] $($key.ToUpper()) publie et verifie -> $url" -ForegroundColor Green
    Write-Host "     Assembly verifie : $($localNewest.Name) @ $($localNewest.LastWriteTime)" -ForegroundColor DarkGray
    Write-Host "     Config verifiee  : appsettings.json identique au depot" -ForegroundColor DarkGray
}

Publish-Target $Target
