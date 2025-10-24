# 🎯 Prochaines Étapes - Configuration du Déploiement

## ✅ Ce qui a été fait

- [x] ✅ Workflow GitHub Actions créé (`.github/workflows/deploy-production.yml`)
- [x] ✅ Script de versioning automatique (`scripts/update-version.sh`)
- [x] ✅ Docker Compose production optimisé (`docker-compose.production.yml`)
- [x] ✅ Documentation complète (DEPLOYMENT.md, PORTAINER_SETUP.md)
- [x] ✅ README avec badges de statut
- [x] ✅ Branche `production` créée et poussée vers GitHub
- [x] ✅ Template fichier .env pour secrets production

---

## 🚀 Actions Requises (à faire maintenant)

### 1. Configuration Portainer (30 minutes)

**Sur le serveur de production (192.168.0.8)** :

```bash
# 1. Créer les répertoires de données
sudo mkdir -p /data/generateur-doe-data/{postgres,documents/{pdf,images},logs,dataprotection-keys,temp,backups,pgadmin}

# 2. Définir les permissions
sudo chown -R 1000:1000 /data/generateur-doe-data/documents
sudo chown -R 1000:1000 /data/generateur-doe-data/logs
sudo chown -R 1000:1000 /data/generateur-doe-data/dataprotection-keys
sudo chown -R 1000:1000 /data/generateur-doe-data/temp
sudo chown -R 1001:1001 /data/generateur-doe-data/postgres
sudo chown -R 1001:1001 /data/generateur-doe-data/backups

# 3. Vérifier
ls -la /data/generateur-doe-data/
```

**Dans Portainer (http://192.168.0.8:9000)** :

1. **Créer la stack** :
   - Aller dans **Stacks** → **+ Add stack**
   - Nom : `generateur-doe-production`
   - Build method : **Web editor**
   - Copier le contenu de [`docker-compose.production.yml`](../docker-compose.production.yml)
   - Cliquer sur **Deploy the stack**

2. **Créer le webhook** :
   - Aller dans **Stacks** → `generateur-doe-production`
   - Cliquer sur **Webhooks**
   - **+ Add webhook**
   - Name : `github-auto-deploy`
   - Webhook type : **Redeploy service**
   - Service : `generateur-doe`
   - **Copier l'URL générée** ⚠️ IMPORTANT

   Exemple d'URL :
   ```
   http://192.168.0.8:9000/api/webhooks/xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx
   ```

3. **Activer auto-pull** :
   - Dans la stack, aller en mode **Advanced**
   - Activer **Enable auto-update** ✅
   - Polling interval : `5 minutes`
   - Re-pull image : `Always`

### 2. Configuration GitHub Secrets (5 minutes)

**Sur GitHub (https://github.com/zaytar68/GenerateurDOE)** :

1. Aller dans **Settings** → **Secrets and variables** → **Actions**
2. Cliquer sur **New repository secret**
3. Ajouter le secret :
   - **Name** : `PORTAINER_WEBHOOK_URL`
   - **Secret** : Coller l'URL du webhook Portainer (étape précédente)
4. Cliquer sur **Add secret**

### 3. Permissions GitHub Actions (2 minutes)

**Sur GitHub** :

1. Aller dans **Settings** → **Actions** → **General**
2. Scrollez jusqu'à **Workflow permissions**
3. Sélectionner **Read and write permissions** ✅
4. Activer **Allow GitHub Actions to create and approve pull requests** ✅
5. Cliquer sur **Save**

### 4. Test du Déploiement (10 minutes)

**Sur votre machine de développement** :

```bash
# 1. Créer un fichier de test
echo "# Test déploiement CI/CD" > TEST_DEPLOY.md
git add TEST_DEPLOY.md
git commit -m "test: vérification pipeline de déploiement"

# 2. Pousser vers production pour déclencher le workflow
git push origin main
git checkout production
git merge main
git push origin production
```

**Vérifier le déploiement** :

1. **GitHub Actions** :
   - Aller sur `https://github.com/zaytar68/GenerateurDOE/actions`
   - Vérifier que le workflow **🚀 Deploy to Production** est en cours
   - Attendre la fin (environ 5-10 minutes)

2. **Portainer** :
   - Aller dans **Stacks** → `generateur-doe-production`
   - Vérifier que la stack se redéploie

3. **Application** :
   ```bash
   # Vérifier le health check
   curl http://192.168.0.8:5000/health
   # ✅ Attendu: {"status":"Healthy"}
   ```

---

## 📋 Checklist de Validation

Cochez les étapes au fur et à mesure :

### Configuration Serveur
- [ ] Répertoires `/data/generateur-doe-data/` créés
- [ ] Permissions correctes (1000:1000 pour app, 1001:1001 pour postgres)
- [ ] Portainer accessible sur http://192.168.0.8:9000

### Configuration Portainer
- [ ] Stack `generateur-doe-production` créée et déployée
- [ ] Tous les containers en état `Running`
- [ ] Health checks PostgreSQL OK
- [ ] Health checks Application OK
- [ ] Webhook créé et URL copiée
- [ ] Auto-pull activé (Enable auto-update ✅)

### Configuration GitHub
- [ ] Secret `PORTAINER_WEBHOOK_URL` configuré
- [ ] Permissions GitHub Actions configurées (Read and write)
- [ ] Branche `production` visible dans GitHub

### Tests
- [ ] Push sur `production` déclenche GitHub Actions
- [ ] Workflow GitHub Actions réussit (badge vert)
- [ ] Image Docker poussée vers GHCR
- [ ] Portainer pull la nouvelle image automatiquement
- [ ] Application accessible sur http://192.168.0.8:5000
- [ ] Health check répond correctement
- [ ] Logs sans erreurs critiques

---

## 🎯 Workflow de Déploiement Final

Une fois tout configuré, voici le workflow de déploiement :

```bash
# 1. Développement sur main
git checkout main
# ... développer, tester ...
git add .
git commit -m "feat: nouvelle fonctionnalité"
git push origin main

# 2. Merge vers production (quand prêt pour déploiement)
git checkout production
git merge main
git push origin production

# 3. ✨ Magie ! GitHub Actions s'exécute automatiquement :
#    - Build Docker image
#    - Tag avec version + latest
#    - Push vers GHCR
#    - Appel webhook Portainer
#    - Portainer pull et redémarre
#    - Tag Git créé

# 4. Vérifier
curl http://192.168.0.8:5000/health
```

---

## 🔄 Gestion des Versions

### Incrémenter la version

```bash
# Méthode 1 : Menu interactif
./scripts/update-version.sh

# Méthode 2 : Spécifier directement
./scripts/update-version.sh 2.2.0

# Le script met à jour automatiquement :
# - GenerateurDOE.csproj
# - appsettings.json
# - docker-compose.postgresql.yml
# - changelog.md
# - Tag Git
```

### Déployer une nouvelle version

```bash
# 1. Incrémenter la version
./scripts/update-version.sh 2.2.0

# 2. Remplir le CHANGELOG.md avec les détails

# 3. Pousser vers production
git push origin main
git checkout production
git merge main
git push origin production

# ✅ La nouvelle version sera automatiquement déployée !
```

---

## 🐛 Troubleshooting

### ❌ Le workflow GitHub Actions échoue

**Vérifier** :
- Secret `PORTAINER_WEBHOOK_URL` existe et est correct
- Permissions GitHub Actions configurées
- Webhook Portainer accessible depuis internet (ou GitHub Actions)

**Solution** :
1. Vérifier les logs GitHub Actions pour l'erreur exacte
2. Tester le webhook manuellement :
   ```bash
   curl -X POST "http://192.168.0.8:9000/api/webhooks/xxx"
   ```

### ❌ Portainer ne redémarre pas la stack

**Vérifier** :
- Webhook correctement configuré dans Portainer
- Auto-pull activé dans les options de la stack
- Image GHCR accessible depuis le serveur

**Solution** :
```bash
# Sur le serveur, tester le pull manuel
docker pull ghcr.io/zaytar68/generateurdoe:latest

# Si authentication required :
docker login ghcr.io -u zaytar68
# Mot de passe : GitHub Personal Access Token
```

### ❌ Application ne démarre pas

**Vérifier les logs** :
```bash
docker logs generateur-doe-app-prod --tail 100
docker logs generateur-doe-postgres-prod --tail 100
```

**Problèmes courants** :
- Permissions des volumes incorrectes → voir étape 1
- PostgreSQL pas démarré → `docker restart generateur-doe-postgres-prod`
- Chrome/Puppeteer manquant → vérifier l'image Docker

---

## 📚 Ressources

- [Guide de déploiement complet](./DEPLOYMENT.md)
- [Configuration Portainer détaillée](./PORTAINER_SETUP.md)
- [Architecture du projet](../CLAUDE.md)
- [Changelog](../changelog.md)

---

## ✅ Validation Finale

Une fois toutes les étapes complétées avec succès :

1. **Nettoyer le fichier de test** :
   ```bash
   git rm TEST_DEPLOY.md
   git commit -m "chore: suppression fichier de test déploiement"
   git push origin main
   git checkout production
   git merge main
   git push origin production
   ```

2. **Vérifier que le déploiement automatique fonctionne** ✅

3. **C'est prêt ! 🎉** Vous pouvez maintenant déployer automatiquement en production !

---

**Besoin d'aide ?**
- 📖 Consultez la [documentation complète](./DEPLOYMENT.md)
- 🐛 [Ouvrir une issue](https://github.com/zaytar68/GenerateurDOE/issues)

---

**Dernière mise à jour** : 2025-09-26
**Version** : 1.0.0
