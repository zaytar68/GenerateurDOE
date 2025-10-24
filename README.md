# 🏗️ Générateur DOE

[![Deploy to Production](https://github.com/zaytar68/GenerateurDOE/actions/workflows/deploy-production.yml/badge.svg)](https://github.com/zaytar68/GenerateurDOE/actions/workflows/deploy-production.yml)
[![Version](https://img.shields.io/badge/version-2.1.3-blue.svg)](https://github.com/zaytar68/GenerateurDOE/releases)
[![License](https://img.shields.io/badge/license-MIT-green.svg)](LICENSE)
[![.NET](https://img.shields.io/badge/.NET-8.0-purple.svg)](https://dotnet.microsoft.com/)

Application Blazor Server pour la génération et la gestion de documents techniques : DOE (Dossier d'Ouvrages Exécutés), Dossiers Techniques et Mémoires Techniques.

---

## 📋 Table des Matières

- [Fonctionnalités](#-fonctionnalités)
- [Technologies](#-technologies)
- [Installation](#-installation)
- [Déploiement](#-déploiement)
- [Architecture](#-architecture)
- [Documentation](#-documentation)
- [Contribuer](#-contribuer)

---

## ✨ Fonctionnalités

### 🎯 Gestion des Documents

- **DOE (Dossier d'Ouvrages Exécutés)** : Compilation des fiches techniques des matériaux installés
- **Dossiers Techniques** : Compilation des fiches techniques des matériaux prévus
- **Mémoires Techniques** : Présentation de la société, méthodologies et produits

### 📄 Génération PDF Avancée

- ✅ Génération PDF réelle avec **PuppeteerSharp** + **PDFSharp**
- ✅ Pages de garde personnalisables
- ✅ Table des matières dynamique
- ✅ Intégration de fiches techniques PDF
- ✅ Sections personnalisables avec éditeur HTML riche
- ✅ Pied de page automatique avec pagination globale

### 🔧 Fonctionnalités Clés

- 📁 Gestion complète des chantiers
- 📑 Bibliothèque de fiches techniques
- 🎨 Éditeur HTML WYSIWYG pour sections personnalisées
- 🖼️ Upload et gestion d'images
- 🔍 Système d'autocomplétion avancé
- 📊 Interface de configuration centralisée
- 🗂️ Explorateur de fichiers intégré
- 🧹 Système de maintenance et nettoyage des fichiers orphelins

---

## 🛠️ Technologies

### Backend

- **Framework** : ASP.NET Core 8.0
- **UI** : Blazor Server
- **Base de données** : Entity Framework Core 8.0
  - SQL Server (développement)
  - PostgreSQL (production)
- **Génération PDF** :
  - PuppeteerSharp 15.1.0 (HTML → PDF)
  - PDFSharp 6.1.1 (Assembly et optimisation)
- **Logging** : Serilog

### Frontend

- **Framework UI** : Radzen Blazor 5.6.4
- **Styling** : Bootstrap 5
- **Éditeur HTML** : Radzen HTML Editor
- **Drag & Drop** : SortableJS 1.15.0

### Déploiement

- **Conteneurisation** : Docker + Docker Compose
- **CI/CD** : GitHub Actions
- **Registry** : GitHub Container Registry (GHCR)
- **Orchestration** : Portainer CE
- **Serveur** : Linux (Debian/Ubuntu)

---

## 🚀 Installation

### Prérequis

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [SQL Server](https://www.microsoft.com/sql-server) ou [PostgreSQL](https://www.postgresql.org/)
- [Git](https://git-scm.com/)

### Installation locale

```bash
# Cloner le dépôt
git clone https://github.com/zaytar68/GenerateurDOE.git
cd GenerateurDOE/GenerateurDOE

# Restaurer les dépendances
dotnet restore

# Appliquer les migrations
dotnet ef database update

# Lancer l'application
dotnet run
```

L'application sera accessible sur `https://localhost:5001`

### Installation avec Docker

```bash
# Cloner le dépôt
git clone https://github.com/zaytar68/GenerateurDOE.git
cd GenerateurDOE/GenerateurDOE

# Lancer avec Docker Compose
docker compose -f docker-compose.postgresql.yml up -d

# Vérifier les logs
docker logs generateur-doe-app -f
```

L'application sera accessible sur `http://localhost:5000`

---

## 🚀 Déploiement

### Déploiement Automatisé (Production)

Le projet utilise un pipeline CI/CD complet :

```
main → production → GitHub Actions → GHCR → Portainer → Production
```

**Documentation complète** :
- 📚 [Guide de déploiement](docs/DEPLOYMENT.md)
- 🐳 [Configuration Portainer](docs/PORTAINER_SETUP.md)

### Déploiement rapide

1. **Créer la branche production**
   ```bash
   git checkout main
   git checkout -b production
   git push -u origin production
   ```

2. **Configurer le webhook Portainer**
   - Suivre le guide : [docs/PORTAINER_SETUP.md](docs/PORTAINER_SETUP.md)

3. **Déployer**
   ```bash
   git checkout production
   git merge main
   git push origin production
   ```

   → GitHub Actions build et déploie automatiquement ! 🎉

---

## 🏗️ Architecture

### Structure du Projet

```
GenerateurDOE/
├── Components/           # Composants Blazor réutilisables
│   ├── Shared/          # Composants partagés (AutoComplete, etc.)
│   └── Layout/          # Layouts de l'application
├── Controllers/         # API Controllers (Files, ImageUpload)
├── Data/                # DbContext et Migrations EF Core
├── Models/              # Modèles de données
├── Pages/               # Pages Blazor
├── Services/            # Services métier
│   ├── Interfaces/      # Interfaces de services
│   └── Implementations/ # Implémentations
├── wwwroot/            # Ressources statiques
├── docs/               # Documentation
├── scripts/            # Scripts utilitaires
└── .github/workflows/  # GitHub Actions CI/CD
```

### Services Principaux

| Service | Description |
|---------|-------------|
| `IDocumentGenereService` | Génération et gestion des documents |
| `IPdfGenerationService` | Génération PDF avec PuppeteerSharp + PDFSharp |
| `IHtmlTemplateService` | Templates HTML pour documents |
| `IFicheTechniqueService` | Gestion des fiches techniques |
| `IChantierService` | Gestion des chantiers |
| `IConfigurationService` | Configuration application |
| `ISectionLibreService` | Sections personnalisables |
| `IDeletionService` | Maintenance et nettoyage |

### Patterns Architecturaux

- ✅ **Repository Pattern** : Optimisations EF Core avec projections DTO
- ✅ **DbContextFactory** : Gestion de la concurrence Blazor Server
- ✅ **Dependency Injection** : Services Scoped pour performances
- ✅ **Strategy Pattern** : Formats d'export multiples

---

## 📚 Documentation

### Guides Utilisateur

- [Guide de démarrage rapide](docs/QUICK_START.md) *(à créer)*
- [Manuel utilisateur](docs/USER_MANUAL.md) *(à créer)*

### Guides Développeur

- [Guide de déploiement](docs/DEPLOYMENT.md) ✅
- [Configuration Portainer](docs/PORTAINER_SETUP.md) ✅
- [Architecture du projet](CLAUDE.md) ✅
- [Changelog](changelog.md) ✅

### Référence Technique

- [Configuration services](CLAUDE.md#services-dependency-injected)
- [Modèles de données](CLAUDE.md#core-models)
- [Génération PDF](CLAUDE.md#export-capabilities)

---

## 🔧 Gestion des Versions

Le projet suit le **Semantic Versioning 2.0.0** : `MAJOR.MINOR.PATCH`

### Mettre à jour la version

```bash
# Script de versioning interactif
./scripts/update-version.sh

# Ou spécifier directement la version
./scripts/update-version.sh 2.2.0
```

Le script synchronise automatiquement :
- ✅ `GenerateurDOE.csproj`
- ✅ `appsettings.json`
- ✅ `docker-compose.postgresql.yml`
- ✅ `changelog.md`
- ✅ Tag Git

---

## 👥 Contribuer

### Workflow de contribution

1. **Fork** le projet
2. Créer une branche de feature (`git checkout -b feature/AmazingFeature`)
3. Commiter les changements (`git commit -m 'feat: add amazing feature'`)
4. Pousser vers la branche (`git push origin feature/AmazingFeature`)
5. Ouvrir une **Pull Request** vers `main`

### Conventions de commit

Le projet utilise [Conventional Commits](https://www.conventionalcommits.org/) :

```
feat: nouvelle fonctionnalité
fix: correction de bug
docs: mise à jour documentation
style: formatage code
refactor: refactorisation sans changement fonctionnel
test: ajout de tests
chore: tâches de maintenance
```

---

## 📝 Licence

Ce projet est sous licence **MIT**. Voir le fichier [LICENSE](LICENSE) pour plus de détails.

---

## 🙏 Remerciements

- [Radzen Blazor](https://blazor.radzen.com/) - Framework UI
- [PuppeteerSharp](https://www.puppeteersharp.com/) - Génération PDF
- [PDFSharp](http://www.pdfsharp.net/) - Manipulation PDF
- [Entity Framework Core](https://docs.microsoft.com/ef/core/) - ORM
- [Serilog](https://serilog.net/) - Logging

---

## 📞 Support

- 🐛 **Issues** : [GitHub Issues](https://github.com/zaytar68/GenerateurDOE/issues)
- 📧 **Email** : cedric.tirolf@multisols.com
- 🏢 **Société** : Multisols

---

## 📊 Statut du Projet

| Statut | Description |
|--------|-------------|
| ✅ **Phase 1** | Génération PDF réelle (PuppeteerSharp + PDFSharp) |
| ✅ **Phase 2** | Migration lots + Repository Pattern |
| ✅ **Phase 2.5** | Migration complète DbContextFactory |
| ⚡ **Phase 3** | Optimisations performances (en cours) |
| 📋 **Phase 4** | Tests automatisés et CI/CD |

Consultez le [CLAUDE.md](CLAUDE.md) pour la roadmap complète.

---

**Développé avec ❤️ par l'équipe Multisols**

**Version actuelle** : 2.1.3
**Dernière mise à jour** : 2025-09-26
