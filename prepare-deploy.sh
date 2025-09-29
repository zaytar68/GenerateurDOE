#!/bin/bash
# ===================================================================
# Script de Préparation Déploiement - Générateur DOE v2.1.3
# ===================================================================
# Bash script pour préparer le package de déploiement serveur Linux
# ===================================================================

OUTPUT_PATH="../GenerateurDOE-Deploy"
CLEAN=false

# Parse arguments
while [[ $# -gt 0 ]]; do
    case $1 in
        --output-path)
            OUTPUT_PATH="$2"
            shift 2
            ;;
        --clean)
            CLEAN=true
            shift
            ;;
        -h|--help)
            echo "Usage: $0 [--output-path PATH] [--clean]"
            echo "  --output-path PATH  : Répertoire de sortie (défaut: ../GenerateurDOE-Deploy)"
            echo "  --clean            : Nettoyer le répertoire existant"
            echo "  -h, --help         : Afficher cette aide"
            exit 0
            ;;
        *)
            echo "Option inconnue: $1"
            exit 1
            ;;
    esac
done

echo "🚀 Préparation du package de déploiement Générateur DOE v2.1.3"

# Nettoyage du répertoire de sortie si demandé
if [ "$CLEAN" = true ] && [ -d "$OUTPUT_PATH" ]; then
    echo "🧹 Nettoyage du répertoire de sortie..."
    rm -rf "$OUTPUT_PATH"
fi

# Création du répertoire de sortie
if [ ! -d "$OUTPUT_PATH" ]; then
    mkdir -p "$OUTPUT_PATH"
fi

echo "📦 Copie des fichiers essentiels..."

# Fichiers Docker essentiels
docker_files=(
    "Dockerfile"
    "docker-compose.postgresql.yml"
    ".dockerignore"
)

for file in "${docker_files[@]}"; do
    if [ -f "$file" ]; then
        cp "$file" "$OUTPUT_PATH/"
        echo "✅ $file"
    else
        echo "❌ Fichier manquant: $file"
    fi
done

# Fichiers de configuration
config_files=(
    "GenerateurDOE.csproj"
    "appsettings.json"
    "appsettings.PostgreSQL.json"
    "appsettings.Production.json"
    "Program.cs"
    "_Imports.razor"
    "App.razor"
)

for file in "${config_files[@]}"; do
    if [ -f "$file" ]; then
        cp "$file" "$OUTPUT_PATH/"
        echo "✅ $file"
    else
        echo "⚠️  Fichier optionnel manquant: $file"
    fi
done

# Documentation
doc_files=(
    "SERVEUR-DEPLOIEMENT.md"
    "DOCKER-PORTAINER.md"
    "DOCKER-QUICK.md"
    "DOCKER-POSTGRESQL-QUICK.md"
    "PGADMIN-QUICK.md"
)

for file in "${doc_files[@]}"; do
    if [ -f "$file" ]; then
        cp "$file" "$OUTPUT_PATH/"
        echo "✅ $file"
    fi
done

# Copie récursive des dossiers source
source_folders=(
    "Components"
    "Controllers"
    "Data"
    "Models"
    "Pages"
    "Services"
    "Shared"
    "wwwroot"
    "Migrations"
    "Scripts"
)

echo "📁 Copie des dossiers source..."

for folder in "${source_folders[@]}"; do
    if [ -d "$folder" ]; then
        cp -r "$folder" "$OUTPUT_PATH/"
        echo "✅ $folder/"
    else
        echo "⚠️  Dossier optionnel manquant: $folder"
    fi
done

# Création d'un fichier de vérification
checklist_path="$OUTPUT_PATH/DEPLOY-CHECKLIST.md"
file_count=$(find "$OUTPUT_PATH" -type f | wc -l)
current_date=$(date)

cat > "$checklist_path" << EOF
# ✅ Checklist Déploiement Serveur

## 📋 Fichiers Inclus
$file_count fichiers copiés

## 🎯 Étapes de Déploiement

### Sur le Serveur de Production :
- [ ] Transférer tous les fichiers vers \`/opt/generateur-doe/\`
- [ ] Construire l'image Docker : \`docker build -t generateur-doe:2.1.3 .\`
- [ ] Créer l'équipe Portainer : \`generateur-doe-team\`
- [ ] Déployer via Portainer ou CLI : \`docker-compose -f docker-compose.postgresql.yml up -d\`
- [ ] Vérifier l'accès : \`http://serveur:80\`

### Sécurité Production :
- [ ] Modifier les mots de passe par défaut
- [ ] Configurer les volumes de sauvegarde
- [ ] Activer les health checks monitoring

## 🌐 URLs Finales
- Application : http://votre-serveur:80
- pgAdmin : http://votre-serveur:8080 (Email: admin@generateur-doe.local)
- Portainer : http://votre-serveur:9000 (optionnel)

Date de création : $current_date
Version : Générateur DOE v2.1.3
EOF

# Résumé
echo ""
echo "🎉 Package de déploiement créé avec succès!"
echo "📂 Emplacement: $OUTPUT_PATH"
echo "📋 Voir DEPLOY-CHECKLIST.md pour les étapes suivantes"

# Affichage du contenu
echo ""
echo "📁 Contenu du package:"
ls -la "$OUTPUT_PATH"

echo ""
echo "🔧 Pour déployer sur Linux, exécutez dans le dossier $OUTPUT_PATH :"
echo "   docker build -t generateur-doe:2.1.3 ."
echo "   docker-compose -f docker-compose.postgresql.yml up -d"