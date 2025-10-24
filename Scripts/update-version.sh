#!/bin/bash
# ===================================================================
# Script de Synchronisation des Versions - Générateur DOE
# ===================================================================
# Usage: ./scripts/update-version.sh [nouvelle_version]
# Exemple: ./scripts/update-version.sh 2.1.4
#
# Ce script :
# 1. Extrait la version actuelle du .csproj
# 2. Met à jour la version dans tous les fichiers concernés
# 3. Synchronise le CHANGELOG.md
# 4. Crée un commit et un tag Git (optionnel)
# ===================================================================

set -e # Exit on error

# Couleurs pour output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Fonctions d'affichage
info() { echo -e "${BLUE}ℹ️  $1${NC}"; }
success() { echo -e "${GREEN}✅ $1${NC}"; }
warning() { echo -e "${YELLOW}⚠️  $1${NC}"; }
error() { echo -e "${RED}❌ $1${NC}"; exit 1; }

# ===================================================================
# Configuration
# ===================================================================
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(dirname "$SCRIPT_DIR")"
CSPROJ_FILE="$PROJECT_ROOT/GenerateurDOE.csproj"
APPSETTINGS_FILE="$PROJECT_ROOT/appsettings.json"
CHANGELOG_FILE="$PROJECT_ROOT/changelog.md"
DOCKER_COMPOSE_FILE="$PROJECT_ROOT/docker-compose.postgresql.yml"

info "📁 Répertoire du projet: $PROJECT_ROOT"

# ===================================================================
# Extraction de la version actuelle
# ===================================================================
extract_current_version() {
    if [[ ! -f "$CSPROJ_FILE" ]]; then
        error "Fichier .csproj non trouvé: $CSPROJ_FILE"
    fi

    CURRENT_VERSION=$(grep -oP '(?<=<Version>)[^<]+' "$CSPROJ_FILE" | head -1)

    if [[ -z "$CURRENT_VERSION" ]]; then
        error "Impossible d'extraire la version depuis $CSPROJ_FILE"
    fi

    info "Version actuelle: $CURRENT_VERSION"
}

# ===================================================================
# Validation du format de version (semver)
# ===================================================================
validate_version() {
    local version=$1
    if [[ ! "$version" =~ ^[0-9]+\.[0-9]+\.[0-9]+$ ]]; then
        error "Format de version invalide: $version (attendu: X.Y.Z)"
    fi
}

# ===================================================================
# Incrémentation automatique de version
# ===================================================================
increment_version() {
    local version=$1
    local part=${2:-patch} # major, minor, patch

    IFS='.' read -r -a parts <<< "$version"
    local major="${parts[0]}"
    local minor="${parts[1]}"
    local patch="${parts[2]}"

    case $part in
        major)
            major=$((major + 1))
            minor=0
            patch=0
            ;;
        minor)
            minor=$((minor + 1))
            patch=0
            ;;
        patch)
            patch=$((patch + 1))
            ;;
        *)
            error "Type d'incrémentation invalide: $part (attendu: major, minor, patch)"
            ;;
    esac

    echo "$major.$minor.$patch"
}

# ===================================================================
# Mise à jour du fichier .csproj
# ===================================================================
update_csproj() {
    local new_version=$1
    info "Mise à jour de $CSPROJ_FILE..."

    # Sauvegarde
    cp "$CSPROJ_FILE" "$CSPROJ_FILE.bak"

    # Mise à jour des balises de version
    sed -i "s|<Version>.*</Version>|<Version>$new_version</Version>|" "$CSPROJ_FILE"
    sed -i "s|<AssemblyVersion>.*</AssemblyVersion>|<AssemblyVersion>$new_version</AssemblyVersion>|" "$CSPROJ_FILE"
    sed -i "s|<FileVersion>.*</FileVersion>|<FileVersion>$new_version</FileVersion>|" "$CSPROJ_FILE"

    success ".csproj mis à jour vers $new_version"
}

# ===================================================================
# Mise à jour de appsettings.json
# ===================================================================
update_appsettings() {
    local new_version=$1
    info "Mise à jour de $APPSETTINGS_FILE..."

    if [[ ! -f "$APPSETTINGS_FILE" ]]; then
        warning "Fichier appsettings.json non trouvé, ignoré"
        return
    fi

    # Sauvegarde
    cp "$APPSETTINGS_FILE" "$APPSETTINGS_FILE.bak"

    # Mise à jour de la version dans AppSettings
    sed -i "s|\"ApplicationVersion\": \".*\"|\"ApplicationVersion\": \"$new_version\"|" "$APPSETTINGS_FILE"

    success "appsettings.json mis à jour"
}

# ===================================================================
# Mise à jour de docker-compose.postgresql.yml
# ===================================================================
update_docker_compose() {
    local new_version=$1
    info "Mise à jour de $DOCKER_COMPOSE_FILE..."

    if [[ ! -f "$DOCKER_COMPOSE_FILE" ]]; then
        warning "Fichier docker-compose.postgresql.yml non trouvé, ignoré"
        return
    fi

    # Sauvegarde
    cp "$DOCKER_COMPOSE_FILE" "$DOCKER_COMPOSE_FILE.bak"

    # Mise à jour de la version de l'image
    sed -i "s|image: generateur-doe:.*|image: generateur-doe:$new_version|" "$DOCKER_COMPOSE_FILE"

    # Mise à jour des commentaires de version
    sed -i "s|# Docker Compose PostgreSQL - Générateur DOE v.*|# Docker Compose PostgreSQL - Générateur DOE v$new_version|" "$DOCKER_COMPOSE_FILE"

    success "docker-compose.postgresql.yml mis à jour"
}

# ===================================================================
# Mise à jour du CHANGELOG.md
# ===================================================================
update_changelog() {
    local new_version=$1
    local current_date=$(date +%Y-%m-%d)

    info "Mise à jour de $CHANGELOG_FILE..."

    if [[ ! -f "$CHANGELOG_FILE" ]]; then
        warning "Fichier CHANGELOG.md non trouvé, création..."
        echo "# Changelog" > "$CHANGELOG_FILE"
    fi

    # Sauvegarde
    cp "$CHANGELOG_FILE" "$CHANGELOG_FILE.bak"

    # Créer l'entrée de changelog temporaire
    local temp_entry=$(cat <<EOF

## [$new_version] - $current_date

### Ajouté
-

### Modifié
-

### Corrigé
-

EOF
)

    # Insérer la nouvelle entrée après l'en-tête
    sed -i "/^## \[Non publié\]/a\\$temp_entry" "$CHANGELOG_FILE"

    success "CHANGELOG.md mis à jour avec l'entrée v$new_version"
    warning "N'oubliez pas de remplir les détails du CHANGELOG!"
}

# ===================================================================
# Création du commit et du tag Git
# ===================================================================
create_git_commit_and_tag() {
    local new_version=$1

    info "Création du commit Git..."

    # Vérifier si git est disponible
    if ! command -v git &> /dev/null; then
        warning "Git non disponible, commit ignoré"
        return
    fi

    # Ajouter les fichiers modifiés
    git add "$CSPROJ_FILE" "$APPSETTINGS_FILE" "$CHANGELOG_FILE" "$DOCKER_COMPOSE_FILE" 2>/dev/null || true

    # Créer le commit
    git commit -m "chore: bump version to $new_version

- Updated .csproj version
- Updated appsettings.json version
- Updated docker-compose.postgresql.yml
- Added CHANGELOG entry for v$new_version

🤖 Generated with version update script" 2>/dev/null || warning "Aucun changement à commiter"

    # Créer le tag
    info "Création du tag Git v$new_version..."
    git tag -a "v$new_version" -m "Release version $new_version" 2>/dev/null || warning "Tag déjà existant"

    success "Commit et tag créés pour v$new_version"
}

# ===================================================================
# Nettoyage des backups
# ===================================================================
cleanup_backups() {
    info "Nettoyage des fichiers de sauvegarde..."
    rm -f "$CSPROJ_FILE.bak" "$APPSETTINGS_FILE.bak" "$CHANGELOG_FILE.bak" "$DOCKER_COMPOSE_FILE.bak"
    success "Fichiers de sauvegarde supprimés"
}

# ===================================================================
# Rollback en cas d'erreur
# ===================================================================
rollback() {
    error "Erreur détectée, rollback..."
    [[ -f "$CSPROJ_FILE.bak" ]] && mv "$CSPROJ_FILE.bak" "$CSPROJ_FILE"
    [[ -f "$APPSETTINGS_FILE.bak" ]] && mv "$APPSETTINGS_FILE.bak" "$APPSETTINGS_FILE"
    [[ -f "$CHANGELOG_FILE.bak" ]] && mv "$CHANGELOG_FILE.bak" "$CHANGELOG_FILE"
    [[ -f "$DOCKER_COMPOSE_FILE.bak" ]] && mv "$DOCKER_COMPOSE_FILE.bak" "$DOCKER_COMPOSE_FILE"
    error "Rollback effectué, fichiers restaurés"
}

# Trap pour rollback en cas d'erreur
trap rollback ERR

# ===================================================================
# Menu Interactif
# ===================================================================
show_menu() {
    echo ""
    echo "╔═══════════════════════════════════════════════════════════╗"
    echo "║   🔄 Script de Mise à Jour de Version - Générateur DOE   ║"
    echo "╚═══════════════════════════════════════════════════════════╝"
    echo ""
    echo "Version actuelle: $CURRENT_VERSION"
    echo ""
    echo "Choisissez une option:"
    echo "  1) Incrémentation PATCH (bug fixes)      → $(increment_version "$CURRENT_VERSION" patch)"
    echo "  2) Incrémentation MINOR (new features)   → $(increment_version "$CURRENT_VERSION" minor)"
    echo "  3) Incrémentation MAJOR (breaking changes) → $(increment_version "$CURRENT_VERSION" major)"
    echo "  4) Saisir une version personnalisée"
    echo "  5) Quitter"
    echo ""
    read -p "Votre choix [1-5]: " choice

    case $choice in
        1)
            NEW_VERSION=$(increment_version "$CURRENT_VERSION" patch)
            ;;
        2)
            NEW_VERSION=$(increment_version "$CURRENT_VERSION" minor)
            ;;
        3)
            NEW_VERSION=$(increment_version "$CURRENT_VERSION" major)
            ;;
        4)
            read -p "Entrez la nouvelle version (format: X.Y.Z): " NEW_VERSION
            validate_version "$NEW_VERSION"
            ;;
        5)
            info "Opération annulée"
            exit 0
            ;;
        *)
            error "Option invalide"
            ;;
    esac
}

# ===================================================================
# Main
# ===================================================================
main() {
    info "🚀 Démarrage du script de mise à jour de version..."
    echo ""

    # Extraire la version actuelle
    extract_current_version

    # Si une version est fournie en argument
    if [[ -n "$1" ]]; then
        NEW_VERSION="$1"
        validate_version "$NEW_VERSION"
    else
        show_menu
    fi

    # Confirmation
    echo ""
    warning "Vous allez mettre à jour la version de $CURRENT_VERSION vers $NEW_VERSION"
    read -p "Confirmer? [y/N]: " confirm

    if [[ "$confirm" != "y" && "$confirm" != "Y" ]]; then
        info "Opération annulée"
        exit 0
    fi

    echo ""
    info "📝 Mise à jour des fichiers..."

    # Mise à jour de tous les fichiers
    update_csproj "$NEW_VERSION"
    update_appsettings "$NEW_VERSION"
    update_docker_compose "$NEW_VERSION"
    update_changelog "$NEW_VERSION"

    # Création du commit et tag Git
    read -p "Créer un commit et tag Git? [Y/n]: " git_confirm
    if [[ "$git_confirm" != "n" && "$git_confirm" != "N" ]]; then
        create_git_commit_and_tag "$NEW_VERSION"
    fi

    # Nettoyage
    cleanup_backups

    # Résumé
    echo ""
    echo "╔═══════════════════════════════════════════════════════════╗"
    echo "║                  ✅ MISE À JOUR RÉUSSIE                   ║"
    echo "╚═══════════════════════════════════════════════════════════╝"
    echo ""
    success "Version mise à jour: $CURRENT_VERSION → $NEW_VERSION"
    echo ""
    info "📋 Fichiers mis à jour:"
    echo "  • GenerateurDOE.csproj"
    echo "  • appsettings.json"
    echo "  • docker-compose.postgresql.yml"
    echo "  • changelog.md"
    echo ""
    info "🏷️  Tag Git créé: v$NEW_VERSION"
    echo ""
    warning "📝 N'oubliez pas de:"
    echo "  1. Remplir les détails dans CHANGELOG.md"
    echo "  2. Pousser les changements: git push origin main"
    echo "  3. Pousser le tag: git push origin v$NEW_VERSION"
    echo "  4. Merger vers production: git checkout production && git merge main"
    echo ""
}

# Exécution
main "$@"
