# Changelog

Toutes les modifications notables de ce projet seront documentées dans ce fichier.

Le format est basé sur [Keep a Changelog](https://keepachangelog.com/fr/1.0.0/),
et ce projet adhère au [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Non publié]

### Ajouté
- Documentation XML complète pour tous les services et modèles
- Commentaires IntelliSense pour améliorer l'expérience développeur

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