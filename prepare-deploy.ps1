# ===================================================================
# Script de Préparation Déploiement - Générateur DOE v2.1.3
# ===================================================================
# PowerShell script pour préparer le package de déploiement serveur Linux
# ===================================================================

param(
    [string]$OutputPath = "..\GenerateurDOE-Deploy",
    [switch]$Clean,
    [switch]$Help
)

if ($Help) {
    Write-Host "Usage: .\prepare-deploy.ps1 [-OutputPath PATH] [-Clean] [-Help]"
    Write-Host "  -OutputPath PATH  : Répertoire de sortie (défaut: ..\GenerateurDOE-Deploy)"
    Write-Host "  -Clean           : Nettoyer le répertoire existant"
    Write-Host "  -Help            : Afficher cette aide"
    exit 0
}

Write-Host "[INFO] Préparation du package de déploiement Générateur DOE v2.1.3" -ForegroundColor Green

# Nettoyage du répertoire de sortie si demandé
if ($Clean -and (Test-Path $OutputPath)) {
    Write-Host "[CLEAN] Nettoyage du répertoire de sortie..." -ForegroundColor Yellow
    Remove-Item -Path $OutputPath -Recurse -Force
}

# Création du répertoire de sortie
if (-not (Test-Path $OutputPath)) {
    New-Item -ItemType Directory -Path $OutputPath -Force | Out-Null
}

Write-Host "[COPY] Copie des fichiers essentiels..." -ForegroundColor Cyan

# Fichiers Docker essentiels
$dockerFiles = @(
    "Dockerfile",
    "docker-compose.postgresql.yml",
    ".dockerignore"
)

foreach ($file in $dockerFiles) {
    if (Test-Path $file) {
        Copy-Item -Path $file -Destination $OutputPath -Force
        Write-Host "[OK] $file" -ForegroundColor Green
    } else {
        Write-Host "[ERROR] Fichier manquant: $file" -ForegroundColor Red
    }
}

# Fichiers de configuration
$configFiles = @(
    "GenerateurDOE.csproj",
    "appsettings.json",
    "appsettings.PostgreSQL.json",
    "appsettings.Production.json",
    "Program.cs",
    "_Imports.razor",
    "App.razor"
)

foreach ($file in $configFiles) {
    if (Test-Path $file) {
        Copy-Item -Path $file -Destination $OutputPath -Force
        Write-Host "[OK] $file" -ForegroundColor Green
    } else {
        Write-Host "[WARN] Fichier optionnel manquant: $file" -ForegroundColor Yellow
    }
}

# Documentation
$docFiles = @(
    "SERVEUR-DEPLOIEMENT.md",
    "DOCKER-PORTAINER.md",
    "DOCKER-QUICK.md",
    "DOCKER-POSTGRESQL-QUICK.md",
    "PGADMIN-QUICK.md"
)

foreach ($file in $docFiles) {
    if (Test-Path $file) {
        Copy-Item -Path $file -Destination $OutputPath -Force
        Write-Host "[OK] $file" -ForegroundColor Green
    }
}

# Copie récursive des dossiers source
$sourceFolders = @(
    "Components",
    "Controllers",
    "Data",
    "Models",
    "Pages",
    "Services",
    "Shared",
    "wwwroot",
    "Migrations",
    "Scripts"
)

Write-Host "[FOLDER] Copie des dossiers source..." -ForegroundColor Cyan

foreach ($folder in $sourceFolders) {
    if (Test-Path $folder -PathType Container) {
        Copy-Item -Path $folder -Destination $OutputPath -Recurse -Force
        Write-Host "[OK] $folder/" -ForegroundColor Green
    } else {
        Write-Host "[WARN] Dossier optionnel manquant: $folder" -ForegroundColor Yellow
    }
}

# Création d'un fichier de vérification
$checklistPath = Join-Path $OutputPath "DEPLOY-CHECKLIST.md"
$fileCount = (Get-ChildItem -Path $OutputPath -Recurse -File).Count
$currentDate = Get-Date -Format "yyyy-MM-dd HH:mm:ss"

$checklistContent = @"
# ✅ Checklist Déploiement Serveur

## 📋 Fichiers Inclus
$fileCount fichiers copiés

## 🎯 Étapes de Déploiement

### Sur le Serveur de Production :
- [ ] Transférer tous les fichiers vers `/opt/generateur-doe/`
- [ ] Construire l'image Docker : `docker build -t generateur-doe:2.1.3 .`
- [ ] Créer l'équipe Portainer : `generateur-doe-team`
- [ ] Déployer via Portainer ou CLI : `docker-compose -f docker-compose.postgresql.yml up -d`
- [ ] Vérifier l'accès : `http://serveur:80`

### Sécurité Production :
- [ ] Modifier les mots de passe par défaut
- [ ] Configurer les volumes de sauvegarde
- [ ] Activer les health checks monitoring

## 🌐 URLs Finales
- Application : http://votre-serveur:80
- pgAdmin : http://votre-serveur:8080 (Email: admin@generateur-doe.local)
- Portainer : http://votre-serveur:9000 (optionnel)

Date de création : $currentDate
Version : Générateur DOE v2.1.3
"@

Set-Content -Path $checklistPath -Value $checklistContent -Encoding UTF8

# Résumé
Write-Host ""
Write-Host "[SUCCESS] Package de déploiement créé avec succès!" -ForegroundColor Green
Write-Host "[PATH] Emplacement: $OutputPath" -ForegroundColor Cyan
Write-Host "[INFO] Voir DEPLOY-CHECKLIST.md pour les étapes suivantes" -ForegroundColor Cyan

# Affichage du contenu
Write-Host ""
Write-Host "[LIST] Contenu du package:" -ForegroundColor Cyan
Get-ChildItem -Path $OutputPath | Format-Table -Property Mode, LastWriteTime, Length, Name -AutoSize

Write-Host ""
Write-Host "[DEPLOY] Pour déployer sur Linux, exécutez dans le dossier $OutputPath :" -ForegroundColor Yellow
Write-Host "   docker build -t generateur-doe:2.1.3 ." -ForegroundColor White
Write-Host "   docker-compose -f docker-compose.postgresql.yml up -d" -ForegroundColor White