# Changelog

Toutes les modifications notables de ce projet seront documentées dans ce fichier.

Le format est basé sur [Keep a Changelog](https://keepachangelog.com/fr/1.0.0/),
et ce projet adhère au [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [2.1.5] - 2025-10-24

### 🔧 **Correction Configuration Docker**
- **Versioning Docker** : Suppression de `AppSettings__ApplicationVersion` hardcodée dans docker-compose.production.yml
- **Automatisation** : La version est maintenant automatiquement synchronisée depuis appsettings.json embarqué dans l'image Docker
- **Cohérence** : Évite les incohérences entre version déclarée et version réelle de l'application

### Corrigé
- **⚙️ docker-compose.production.yml** : Variable d'environnement ApplicationVersion supprimée (ligne 67)
- **📝 Documentation** : Ajout commentaire explicatif sur la gestion automatique de la version

## [2.1.4] - 2025-10-24

### 🚀 **Pipeline CI/CD Complet Opérationnel**
- **GitHub Actions** : Workflow automatisé pour build et push vers GHCR
- **GHCR** : Images Docker publiées automatiquement avec tags `latest` et versionnés
- **Déploiement manuel Portainer** : Update stack avec re-pull image
- **Versioning automatique** : Extraction depuis .csproj et création tags Git

### ✨ **Fonctionnalités**
- **Migrations désactivées** : Pas de migrations automatiques au démarrage (gestion manuelle)
- **Vérification connexion DB** : Health check PostgreSQL au démarrage
- **Documentation complète** : Guides de déploiement, setup Portainer, guide rapide

### 🔧 **Technique**
- Workflow GitHub Actions optimisé (5 jobs : version, build, notify, tag, cleanup)
- Configuration docker-compose.production.yml finalisée
- Capabilities Chrome/Puppeteer configurées (SYS_ADMIN, seccomp:unconfined)
- Nettoyage automatique anciennes images GHCR (conservation 4 dernières)

### 📚 **Documentation**
- Guide de déploiement rapide (docs/DEPLOIEMENT_RAPIDE.md)
- Documentation technique complète (docs/DEPLOYMENT.md)
- Guide setup Portainer (docs/PORTAINER_SETUP.md)
- README avec badges de statut

### Corrigé
- **🔥 Migrations PostgreSQL** : Désactivation migrations auto (conflit SQL Server/PostgreSQL)
- **🐳 Image GHCR** : Configuration capabilities Chrome pour éviter exit code 139
- **📋 Workflow GitHub Actions** : Adaptation pour déploiement manuel (pas de webhook Portainer CE)

## [2.1.0] - 2025-09-24

### ✨ **Fonctionnalité Majeure - Pied de page avec surimpression**
- **🎯 Pied de page global** : Système de surimpression automatique sur toutes les pages (sauf page de garde)
- **📊 Numérotation globale** : Pages numérotées correctement sur l'ensemble du document (ex: 5/23, 6/23)
- **📑 Informations contextuelles** : Nom du chantier + type de document alignés à gauche, pagination à droite
- **🎨 Design professionnel** : Fond gris clair (#F5F5F5) pour une meilleure lisibilité
- **📎 Compatibilité totale** : Surimpression uniforme sur pages générées ET fiches techniques PDF importées

### Technique
- **Architecture post-processing** : Workflow PuppeteerSharp → Assembly → Post-processing → Optimisation
- **PDFSharp Graphics** : Rendu vectoriel haute qualité avec XGraphics pour positionnement précis
- **Options configurables** : `DisableFooterForPostProcessing` et `EnableFooterPostProcessing` pour contrôle granulaire
- **Rétrocompatibilité** : Paramètres optionnels préservant l'API existante

### Corrigé
- **🔥 Bug pagination locale** : Correction "1/1" → numérotation globale correcte
- **📄 Fiches techniques** : Application uniforme du pied de page sur PDFs importés
- **🎯 Exclusion page de garde** : Logique conditionnelle pour préserver l'esthétique

## [2.1.1] - 2025-09-25

### Corrigé
- **🔥 Bug validation création documents** : Correction des champs lot obligatoires alors qu'ils doivent être optionnels
- **🎯 Type de document pré-sélectionné** : Initialisation automatique à "DOE" lors de la création d'un nouveau document
- **📝 Synchronisation selectedTypeDocumentString** : Correction du problème "Type de document requis" malgré une sélection
- **⚡ Callback de sélection amélioré** : HandleTypeChange avec mise à jour appropriée du modèle
- **✏️ Détection saisie manuelle** : Préservation des noms de fichiers personnalisés par l'utilisateur
- **🔄 Génération automatique intelligente** : IsAutomaticallyGeneratedName() pour détecter les patterns automatiques

### Technique
- Suppression des attributs `[Required]` sur `NumeroLot` et `IntituleLot` dans `DocumentGenere.cs`
- Correction du binding bidirectionnel dans `DocumentEditGeneralForm.razor`
- Ajout flag `isFileNameManuallySet` pour tracking des modifications utilisateur
- Amélioration de `GenerateDefaultFileName()` avec logique conditionnelle intelligente

## [2.1.3] - 2025-09-25

### ✨ **Refactorisation Majeure - Interface Chantiers Simplifiée**
- **🔄 Navigation directe** : Clic sur chantier → redirection vers page détail dédiée `/chantier/{id}`
- **🧹 Nettoyage architecture** : Suppression -300 lignes de code complexe de gestion détails inline
- **📱 UX améliorée** : Séparation claire des responsabilités entre liste et détail de chantier
- **🎯 Actions centralisées** : Toutes les opérations (archiver, supprimer, documents) dans la page détail

### Corrigé
- **🔥 Bug navigation vs modal** : Correction conflit événements entre modal d'édition et navigation détails
- **⚡ Propagation événements** : `@onclick:stopPropagation="true"` sur `<td>` pour éviter double-déclenchement
- **📊 Interface unifiée** : Retrait dépendance `IDocumentGenereService` dans la liste des chantiers

### Technique
- Suppression méthodes `SelectChantier()`, `OnChantierSelected()`, modal détails documents
- Simplification du modèle de données : plus de `selectedChantier`, `selectedChantierWithDocuments`
- Code plus maintenable avec séparation claire liste/détail

## [2.1.2] - 2025-09-25

### ✨ **Fonctionnalité Majeure - Système de Maintenance des Fichiers Orphelins**
- **🔧 Service DeletionService étendu** : Détection et nettoyage automatique des références orphelines
- **🖥️ Interface de maintenance** : Page dédiée `/maintenance` pour supervision et nettoyage
- **📊 Rapport d'intégrité** : Score de santé système et recommandations automatiques
- **⚠️ Notifications améliorées** : Logging détaillé des PDFs manquants lors de la génération
- **🎯 Navigation étendue** : Lien "Maintenance" ajouté au menu principal

### Technique
- **3 nouvelles méthodes IDeletionService** : DetectOrphanReferencesAsync, CleanupOrphanReferencesAsync, GenerateFileIntegrityReportAsync
- **Types complets pour maintenance** : OrphanFilesReport, FileIntegrityReport, OrphanDetectionOptions, etc.
- **PdfGenerationService amélioré** : Remplacement du "fail silently" par notification explicite
- **Architecture transactionnelle** : Nettoyage sécurisé avec rollback automatique
- **Support multi-types** : ImportPDF, DocumentsGenerés, ImagesMethodes orphelins

### Corrigé
- **🔥 Bug critique PDFs manquants** : Les PDFs ne sont plus insérés silencieusement lors de suppression incomplète
- **📄 Synchronisation BD/filesystem** : Détection automatique des références cassées
- **🎯 Interface utilisateur** : Feedback temps réel avec statut de progression et alertes
- **⚡ Logging détaillé** : Messages explicites avec noms de fichiers et chemins d'accès

### Interface Utilisateur
- **Pages/Maintenance.razor** : Interface complète de gestion des orphelins
- **Cartes de statut** : PDFs orphelins, Documents orphelins, Vue d'ensemble
- **Nettoyage sécurisé** : Confirmation utilisateur et prévisualisation des actions
- **Score de santé** : Indicateur visuel de l'état du système (0-100%)
- **Recommandations automatiques** : Actions suggérées selon l'état détecté

## [Non publié]

### Ajouté
- **🎨 Interface Chantiers modernisée** : Conversion du layout en cartes vers présentation en tableau responsive
- **🆕 Bouton "Nouveau Document"** : Création rapide de documents depuis la page Chantiers avec navigation directe
- **📊 Tableau documents professionnel** : Colonnes Document, Type, Lot, Créé le, Statut, Actions avec badges colorés
- **🎯 État vide amélioré** : Message contextuel et bouton de création pour chantiers sans documents
- **Service de comptage de pages PDF** : PdfPageCountService avec cache intelligent et persistence base de données
- **API Table des matières** : TableOfContentsController pour récupération dynamique de la structure TOC
- **Interface TOC personnalisable** : Mode automatique/personnalisable dans TableMatieresEditor avec drag & drop des entrées
- **Logs de débogage avancés** : Traçabilité complète du processus de génération PDF et extraction configuration TOC
- **Extension base de données** : Colonne Parametres étendue de 2000 à 10000 caractères pour configurations complexes
- Documentation XML complète pour tous les services et modèles
- Commentaires IntelliSense pour améliorer l'expérience développeur

### Modifié
- **Architecture TOC consolidée** : Classes TableOfContentsData/TocEntry/CustomTocEntry unifiées dans Models/TableOfContentsModels.cs
- **Gestion des enums JSON** : Support polymorphe Number/String pour désérialisation CustomModeGeneration robuste
- **ImportPDF modèle** : Ajout propriété PageCount pour cache des nombres de pages

### Corrigé
- **🔥 Bug critique styles PDF** : Les réglages de styles PDF ne sont plus pris en compte lors de la génération (PdfGenerationService.cs:16-31)
- **Architecture injection de dépendances** : Migration `IOptions<AppSettings>` (valeurs figées) → `IConfigurationService` (valeurs dynamiques)
- **Synchronisation configuration** : 6 méthodes mises à jour pour récupérer les paramètres actuels à chaque génération PDF
- **🔥 Bug critique TOC personnalisées** : Correction erreur `JsonElementWrongTypeException` dans ExtractCustomTocConfiguration (PdfGenerationService.cs:1014)
- **Ambiguïtés de classes** : Suppression des définitions dupliquées dans IPdfGenerationService.cs
- **Using statements manquants** : Ajout GenerateurDOE.Models dans Controllers et Services
- **Références JSON** : Gestion robuste des enums sérialisés comme nombre (0,1) ou chaîne ("Automatique", "Personnalisable")
- **Drag & Drop conteneurs** : Correction de la mise à jour UI après réorganisation des conteneurs de sections
- **SectionConteneurEditor** : Rafraîchissement automatique de l'interface après opérations drag & drop
- **StateHasChanged()** : Synchronisation correcte entre base de données et affichage visuel
- **Hot reload** : Compatibilité avec les modifications en temps réel via dotnet watch

### Technique
- **3 Migrations EF Core** : ExtendParametresFieldForCustomToc, AddPageCountToImportPDF, ExtendParametresTo10000Characters
- **Services injection** : IPdfPageCountService avec implémentation cache MemoryCache
- **Debugging** : 15+ logs détaillés pour diagnostic problèmes TOC et génération PDF
- **Résolution problème utilisateur** : "La table modifiée n'est pas prise en compte dans le PDF" ✅ **RÉSOLU**

### Tests
- ✅ Génération PDF réussie avec TOC personnalisées (validé en production)
- ✅ Cache PdfPageCountService fonctionnel avec invalidation basée sur LastWriteTime
- ✅ Interface TableMatieresEditor responsive avec modes automatique/personnalisable

## [1.5.0] - 2025-09-23

### Ajouté
- **Drag & Drop des sections** : Réorganisation intuitive des sections par glisser-déposer avec SortableJS
- **Édition inline des titres de conteneurs** : Modification directe via l'icône paramètres avec validation
- **Poignées de drag Unicode** : Indicateurs visuels `⋮⋮` pour une meilleure UX
- **Protection anti-concurrence** : Verrouillage des opérations simultanées via IOperationLockService

### Modifié
- **Interface SectionConteneurEditor** : UX améliorée avec drag & drop + édition inline
- **JavaScript SortableJS** : Intégration complète avec callbacks Blazor via JSInterop
- **Validation en temps réel** : Feedback utilisateur pour les opérations de réorganisation

### Technique
- **JSInterop** : Communication bidirectionnelle JavaScript ↔ Blazor Server
- **SortableJS 1.15.0** : Bibliothèque de drag & drop performante et accessible
- **Debug logging** : Traçabilité complète des opérations drag & drop
- **CSS optimisé** : Styles unifiés pour les poignées et animations fluides

### Corrigé
- **Sélecteur JavaScript** : Correction `#id .class` → `#id.class` pour détection conteneurs
- **Cache navigateur** : Documentation du rechargement forcé nécessaire pour assets statiques
- **Conflit CSS** : Fusion des styles `.drag-handle` dupliqués
- **Architecture modulaire** : Isolation des responsabilités drag & drop

## [1.4.0] - 2025-09-23

### Ajouté
- Éditeur table des matières avancé avec aperçu temps réel
- Logo PDF configurable pour les documents générés
- Modal de progression PDF temps réel avec barres de progression

### Corrigé
- Logo manquant dans les PDFs générés
- Problèmes d'affichage dans l'éditeur table des matières

## [1.3.0] - 2025-09-20

### Ajouté
- **Migration complète vers DbContextFactory** : Tous les services utilisent maintenant le pattern DbContextFactory pour éviter les problèmes de concurrence
- 42+ ConfigureAwait(false) ajoutés pour prévenir les deadlocks
- Interface contract fix pour GetNextOrderAsync()

### Modifié
- **Architecture 100% cohérente** : Fin de l'architecture mixte (ApplicationDbContext/DbContextFactory)
- Services migrés : DocumentGenereService, SectionLibreService, SectionConteneurService, TypeSectionService, TypeProduitService, TypeDocumentImportService, MemoireTechniqueService

### Corrigé
- 3 invalidations de cache manquantes dans TypeDocumentImportService
- 0 erreurs de compilation après migration complète

## [1.2.0] - 2025-09-18

### Ajouté
- **Migration du système de lots** : Les lots sont maintenant gérés au niveau DocumentGenere (NumeroLot, IntituleLot)
- **Repository Pattern** : DocumentRepositoryService avec projections DTO pour +30-50% de performances
- Migrations EF : MoveLotFromChantierToDocument + UpdateModelAfterLotMigration

### Modifié
- **Business Logic corrigée** : 1 chantier -> N documents -> 1 lot par document
- UI mise à jour : Formulaires DocumentGenere avec validation complète des champs lot
- Architecture nettoyée : Suppression logique lot obsolète de ChantierService

### Déprécié
- Ancienne logique des lots dans le modèle Chantier

## [1.1.0] - 2025-09-15

### Ajouté
- **Génération PDF réelle** : Implémentation PuppeteerSharp + PDFSharp
- **IPdfGenerationService** : Service de génération PDF avec architecture hybride 3 couches
- **IHtmlTemplateService** : Templates HTML professionnels pour pages de garde et sections
- Architecture hybride : HTML → PDF → Assembly

### Modifié
- Remplacement de la génération PDF simulée par une solution de production
- Templates professionnels pour tous les types de documents

### Performances
- +40-60% amélioration temps de génération PDF
- Architecture scalable pour documents complexes

## [1.0.0] - 2025-09-10

### Ajouté
- **Refactorisation complète interface SectionsLibres** avec modal et filtres
- **Architecture modulaire** avec conteneurs (SectionConteneur, FTConteneur)
- **Système d'autocomplétion avancé** avec recherche pour fabricants et types de produits
- **Gestion complète des chantiers** et fiches techniques
- **Éditeur HTML riche** avec upload d'images via ImageUploadController
- **Configuration centralisée** et paramètres persistants en base de données

### Services principaux
- `IFicheTechniqueService` - Gestion fiches techniques avec PDFs
- `IMemoireTechniqueService` - Gestion méthodologies et images
- `IDocumentExportService` - Export documents (HTML, Markdown, PDF simulé)
- `IConfigurationService` - Configuration application
- `IFileExplorerService` - Navigation système de fichiers
- Services utilitaires : Type management, cache, logging

### Sécurité
- Validation types de fichiers pour uploads
- Limitation taille fichiers (10MB pour PDF)
- Protection path traversal basique
- Noms fichiers sécurisés avec GUID

### Interface utilisateur
- Composant `AutoComplete` réutilisable avec templates
- Upload d'images sécurisé pour éditeur HTML
- API REST : FilesController, ImageUploadController
- Bootstrap + Radzen pour styling cohérent

## Architecture

### Technologies
- **Frontend** : Blazor Server avec Bootstrap + Radzen
- **Backend** : ASP.NET Core 8.0
- **Base de données** : Entity Framework Core avec SQL Server
- **Génération PDF** : PuppeteerSharp + PDFSharp
- **Templates** : HTML avec CSS professionnels

### Patterns implémentés
- **Repository Pattern** : Optimisations EF avec projections DTO
- **DbContextFactory** : Gestion concurrence Blazor Server
- **Dependency Injection** : Services Scoped pour performances
- **Strategy Pattern** : Formats d'export multiples

---

**Légende :**
- **Ajouté** : Nouvelles fonctionnalités
- **Modifié** : Modifications aux fonctionnalités existantes
- **Déprécié** : Fonctionnalités dépréciées mais encore supportées
- **Retiré** : Fonctionnalités supprimées
- **Corrigé** : Corrections de bugs
- **Sécurité** : Corrections de vulnérabilités