# 🚀 Guide Rapide de Déploiement - Générateur DOE

## 📋 Workflow Simple en 5 Étapes

### 1️⃣ Développer sur `main`

```bash
cd "C:\Users\cedric\Générateur DOE\GenerateurDOE"
git checkout main

# Développer, modifier, tester...
git add .
git commit -m "feat: nouvelle fonctionnalité"
git push origin main
```

### 2️⃣ Déployer vers `production`

```bash
git checkout production
git merge main
git push origin production
git checkout main
```

**✅ GitHub Actions se déclenche automatiquement**

### 3️⃣ Attendre le Build (5-10 minutes)

Surveillez : `https://github.com/zaytar68/GenerateurDOE/actions`

**Attendez que ces jobs soient verts ✅** :
- 📦 Extract Version
- 🏗️ Build & Push Docker
- 📢 Notify Image Ready
- 🏷️ Create Git Tag
- 🧹 Cleanup Old Images

### 4️⃣ Déployer dans Portainer

1. **Ouvrez** : `http://192.168.0.8:9000`
2. **Naviguez** : Stacks → `generateur-doe-production`
3. **Cliquez** : **⟳ Update the stack**
4. **Cochez** : ☑️ **Re-pull image**
5. **Cliquez** : **Update**

**⏱️ Attendre 1-2 minutes**

### 5️⃣ Vérifier

```bash
# Health check
curl http://192.168.0.8:5000/health

# Réponse attendue: {"status":"Healthy"}
```

**Ouvrez dans le navigateur** : `http://192.168.0.8:5000`

---

## 🔢 Gestion des Versions

### Incrémenter automatiquement

```bash
# Menu interactif
./scripts/update-version.sh

# Choisir:
# 1) PATCH (bug fixes)     → 2.1.4 → 2.1.5
# 2) MINOR (new features)  → 2.1.4 → 2.2.0
# 3) MAJOR (breaking)      → 2.1.4 → 3.0.0
```

**Le script met à jour automatiquement** :
- ✅ `GenerateurDOE.csproj`
- ✅ `appsettings.json`
- ✅ `docker-compose.production.yml`
- ✅ `changelog.md`
- ✅ Tag Git

### Ou manuellement

Modifier directement dans `.csproj` :
```xml
<Version>2.1.4</Version>
```

---

## ⚡ Déploiement Rapide (Résumé 1 Ligne)

```bash
git push origin production && echo "Attendre 10 min → Portainer: Update stack → Re-pull image ✅"
```

---

## 🐛 Problèmes Courants

### ❌ L'image ne se met pas à jour dans Portainer

**Solution** : Toujours cocher **"Re-pull image"** lors de l'update

```bash
# Ou forcer le pull sur le serveur
ssh root@192.168.0.8
docker pull ghcr.io/zaytar68/generateurdoe:latest
# Puis update dans Portainer
```

### ❌ GitHub Actions échoue

**Vérifier** :
1. Les tests passent localement ?
2. Le `.csproj` compile ?
3. Les logs GitHub Actions pour l'erreur exacte

### ❌ Application ne démarre pas après déploiement

**Logs à consulter** :
```bash
docker logs generateur-doe-app --tail 100
docker logs generateur-doe-postgres --tail 50
```

**Problème fréquent** : PostgreSQL pas démarré
```bash
# Vérifier
docker ps | grep postgres
# Doit être "Up" et "healthy"
```

---

## 📊 Architecture CI/CD

```
┌────────────────────────────────────────────────┐
│  Developer Machine (Windows)                   │
│                                                 │
│  git push origin production                    │
└──────────────────┬─────────────────────────────┘
                   │
                   ↓
┌────────────────────────────────────────────────┐
│  GitHub Actions (Automatique)                  │
│  - Build Docker Image                          │
│  - Push vers GHCR                              │
│  - Tag Git (v2.1.4)                            │
│  - Cleanup anciennes images                    │
└──────────────────┬─────────────────────────────┘
                   │
                   ↓ (5-10 minutes)
┌────────────────────────────────────────────────┐
│  Image prête sur GHCR                          │
│  ghcr.io/zaytar68/generateurdoe:latest         │
└──────────────────┬─────────────────────────────┘
                   │
                   ↓ (Action manuelle)
┌────────────────────────────────────────────────┐
│  Portainer (192.168.0.8:9000)                  │
│  Clic "Update stack" + "Re-pull image"         │
└──────────────────┬─────────────────────────────┘
                   │
                   ↓ (1-2 minutes)
┌────────────────────────────────────────────────┐
│  Application en Production                     │
│  http://192.168.0.8:5000                       │
└────────────────────────────────────────────────┘
```

---

## 📖 Documentation Complète

- **Guide technique détaillé** : [DEPLOYMENT.md](./DEPLOYMENT.md)
- **Configuration Portainer** : [PORTAINER_SETUP.md](./PORTAINER_SETUP.md)
- **Vue d'ensemble projet** : [../README.md](../README.md)

---

## ✅ Checklist de Déploiement

Avant chaque déploiement :

- [ ] Code testé localement
- [ ] Tests passent (`dotnet test`)
- [ ] Commit avec message clair
- [ ] Push vers `main` réussi
- [ ] Merge vers `production`
- [ ] GitHub Actions job vert ✅
- [ ] Portainer update avec re-pull
- [ ] Health check OK
- [ ] Application accessible

---

**Version actuelle** : 2.1.4
**Dernière mise à jour** : 2025-10-24
