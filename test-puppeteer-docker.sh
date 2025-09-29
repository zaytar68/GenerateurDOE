#!/bin/bash

# ===================================================================
# Script de test PuppeteerSharp Docker - Générateur DOE v2.1.3
# ===================================================================
# Teste la correction du problème Chrome dans le conteneur Docker
# ===================================================================

echo "🔥 Test PuppeteerSharp Docker - Générateur DOE"
echo "=================================================="

# Couleurs pour l'output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Nettoyer les anciennes images
echo -e "${YELLOW}🧹 Nettoyage des anciennes images...${NC}"
docker rmi generateur-doe:2.1.3 2>/dev/null || true

# Build de l'image
echo -e "${YELLOW}🔨 Construction de l'image Docker...${NC}"
if docker build -t generateur-doe:2.1.3 . ; then
    echo -e "${GREEN}✅ Image construite avec succès${NC}"
else
    echo -e "${RED}❌ Échec de la construction${NC}"
    exit 1
fi

# Vérification que Chrome est installé dans l'image
echo -e "${YELLOW}🔍 Vérification de l'installation Chrome...${NC}"
if docker run --rm generateur-doe:2.1.3 /usr/bin/google-chrome-stable --version ; then
    echo -e "${GREEN}✅ Chrome installé correctement${NC}"
else
    echo -e "${RED}❌ Chrome non trouvé${NC}"
    exit 1
fi

# Test des variables d'environnement
echo -e "${YELLOW}🔧 Vérification des variables d'environnement...${NC}"
docker run --rm generateur-doe:2.1.3 /bin/bash -c "echo 'PUPPETEER_EXECUTABLE_PATH=' \$PUPPETEER_EXECUTABLE_PATH && echo 'PUPPETEER_SKIP_CHROMIUM_DOWNLOAD=' \$PUPPETEER_SKIP_CHROMIUM_DOWNLOAD"

# Test de démarrage rapide (30 secondes max)
echo -e "${YELLOW}🚀 Test de démarrage de l'application...${NC}"
timeout 30s docker run --rm -p 5001:5000 \
    -e ConnectionStrings__DefaultConnection="Server=localhost;Database=TestDB;Trusted_Connection=true;" \
    generateur-doe:2.1.3 &

DOCKER_PID=$!
sleep 5

# Vérifier que l'application démarre sans erreur PuppeteerSharp
echo -e "${YELLOW}📋 Vérification des logs...${NC}"
if docker logs $(docker ps -q --filter ancestor=generateur-doe:2.1.3) 2>&1 | grep -i "failed to download chrome" ; then
    echo -e "${RED}❌ Erreur PuppeteerSharp détectée${NC}"
    kill $DOCKER_PID 2>/dev/null
    exit 1
else
    echo -e "${GREEN}✅ Aucune erreur PuppeteerSharp détectée${NC}"
fi

# Nettoyer
kill $DOCKER_PID 2>/dev/null
docker stop $(docker ps -q --filter ancestor=generateur-doe:2.1.3) 2>/dev/null

echo -e "${GREEN}🎉 Test terminé avec succès !${NC}"
echo -e "${GREEN}✅ PuppeteerSharp configuré correctement pour Docker${NC}"
echo ""
echo "📋 Prochaines étapes :"
echo "   1. Déployer avec : docker-compose -f docker-compose.postgresql.yml up -d"
echo "   2. Tester la génération PDF dans l'interface"
echo "   3. Vérifier les logs : docker logs generateur-doe-app"