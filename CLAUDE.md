# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

C'est une application Blazor qui sert à gérer et créer des documentations techniques.
L'application doit être modulaire afin d'y ajouter facilement des fonctionnalités.
Un document généré concerne un chantier. La structure du document et son contenu doit être stockée en base de données.
Un document généré doit pouvoir être composé d'une page de garde, une table des matières dynamiquement générée en fonction du contenu, de contenu de type word, de fichiers PDF importés : les fiches techniques.
Les documents seront générés en PDF.

Les documents générés sont principalement de trois sortes : 
    - les DOE = Dossier d'Ouvrages Exécutés : Une compilation des fiches techniques des matériaux installés sur un projet
    - les dossiers techniques : Une compilation des fiches techniques des matériaux prévus d'être installés sur un projet
    - un mémoire technique : Un document qui présente notre société, les méthodologies et les produits qui seront employés

Il doit y avoir une interface conviviale permettant le drag and drop pour l'import de fichiers.

Il doit y avoir une interface de configuration.

La gestion des méthodologies doit avoir une interface de création, modification, suppression.


### Core Models
- "Chantier" - Représente un projet avec un Maitre d'œuvre, un maître d'ouvrage, une adresse, un lot (numéro + intitulé)
- "Fiche Technique" - Représente un produit avec son nom, le nom du fabricant, le type de produit et les documents "ImportPDF" associés.
- `DocumentGenere` - Document generation operations with format support and parameter configuration
- "ImportPDF" - Représente un fichier pdf avec son emplacement, son type 
- "Méthode" - Représente une méthodologie avec un titre, une description. Il doit être possible d'y ajouter des images.
- "SectionLibre" - Représente une section personnalisable dans un document avec titre, contenu HTML et type
- "TypeSection" - Énumération des types de sections disponibles

### Services (Dependency Injected)
- `IFicheTechniqueService` - Manages technical sheets with PDF file operations
- `IMemoireTechniqueService` - Manages technical reports
- `IDocumentGenereService` - Handles document generation and export (singleton)
- `IConfigurationService` - Manages application configuration and settings
- `IFileExplorerService` - Handles file system operations and folder management
- `ITypeProduitService` - Manages product types configuration
- `ITypeDocumentImportService` - Manages document import types
- `ISectionLibreService` - Manages custom sections for documents
- `ITypeSectionService` - Manages section types configuration
- `IImageService` - Handles image upload, storage and management
- `ILoggingService` - Centralized logging service

### Export Capabilities
- **HTML**: Full-featured with CSS styling, table of contents, cover pages
- **Markdown**: Clean markdown with metadata
- **PDF/Word**: Currently simulated (see project-todo.md for implementation needs)
- Export service uses Markdig for Markdown processing

### API Controllers
- `FilesController` - Serves PDF files with secure access (`GET /api/files/{id}`)
- `ImageUploadController` - Handles image upload for HTML editor (`POST /api/images/upload`, `GET /api/images/list`, `DELETE /api/images/{fileName}`)

### Data Storage
Use entity framework.

### UI Structure
- Blazor components in `/Components/` and `/Pages/`
- Uses Bootstrap for styling with Radzen components
- Main pages: 
  - `FichesTechniques.razor` - Technical sheets management with PDF upload/download
  - `SectionsLibres.razor` - Custom sections management with HTML editor
  - `Configuration.razor` - Application configuration and settings
  - `TestEditor.razor` - HTML editor testing page
- Shared components:
  - `AutoComplete` - Reusable autocomplete component with template support
  - `FolderExplorer` - File system navigation component

## Development Guidelines
- Toujours utiliser des noms de variables descriptives (Always use descriptive variable names)
- Follow existing validation patterns using Data Annotations
- Maintain French language for UI text and model properties
- Export service provides comprehensive document generation with configurable parameters
- use context7
- génère et met à jour une todo list pour suivre l'avancement du projet
- utiliser github pour suivre le développement

## Fonctionnalités Récentes

### Système d'Autocomplétion
- Composant `AutoComplete` réutilisable avec support de templates personnalisés
- Utilisé dans les fiches techniques pour les fabricants et types de produits
- Support de la sélection d'items et de la saisie libre

### Gestion d'Images
- Upload d'images via `ImageUploadController` pour l'éditeur HTML
- Stockage sécurisé avec génération de noms uniques
- Support des formats image courants avec validation

### Téléchargement de Fichiers PDF
- API sécurisée `FilesController` pour servir les fichiers PDF
- Vérification d'existence et protection basique contre les attaques path traversal
- Logs détaillés des opérations de téléchargement

### Sections Libres
- Gestion de sections personnalisables avec éditeur HTML Radzen
- Types de sections configurables (Introduction, Conclusion, Technique, etc.)
- Intégration complète avec upload d'images

### Configuration Avancée
- Interface de configuration centralisée
- Gestion des répertoires de stockage (PDF, Images)
- Settings persistent en base de données

## Notes Techniques

### Sécurité
- Validation des types de fichiers pour uploads
- Limitation de taille des fichiers (10MB pour PDF)
- Noms de fichiers sécurisés avec GUID pour éviter les conflits
- Vérification basique des chemins pour prévenir les attaques path traversal

### Performance
- Services injectés en tant que Scoped pour optimiser les ressources
- Lazy loading dans Entity Framework pour les relations
- Pagination et filtrage côté serveur quand nécessaire

## Roadmap Technique

### Phase 1: Implémentation PDF réelle (Semaines 1-2)
**Objectif**: Remplacer la génération PDF simulée par une solution robuste

**Architecture recommandée** (par Software Architect):
- **PuppeteerSharp 15.0.1**: Conversion HTML → PDF (sections libres)
- **PDFSharp-GDI 6.0.0**: Assembly et optimisation PDFs (fiches techniques)
- **MigraDoc 6.0.0**: Documents structurés complémentaires

**Services à implémenter**:
- `IPdfGenerationService`: Génération PDF complète avec assembly
- `IHtmlTemplateService`: Templates HTML professionnels
- Architecture hybride 3 couches (HTML → PDF → Assembly)

**Livrables**:
- [ ] Installation et configuration des packages PDF
- [ ] Implémentation des services de génération
- [ ] Templates HTML pour pages de garde, sections, fiches techniques
- [ ] Tests de génération sur documents d'exemple
- [ ] Migration progressive depuis le système simulé

### Phase 2: Refactoring services (Semaines 3-4)
**Objectif**: Découper DocumentGenereService monolithique en services spécialisés

**Refactoring recommandé** (par Tech Lead):
- **Strategy Pattern**: Différents formats d'export
- **Builder Pattern**: Construction progressive des documents  
- **Factory Pattern**: Types de documents (DOE, Dossiers, Mémoires)

**Services spécialisés**:
- `IDocumentBuilderService`: Construction de documents
- `IExportStrategyService`: Gestion des formats d'export
- `ITemplateRenderingService`: Rendu des templates

**Livrables**:
- [ ] Découpage du DocumentGenereService (600+ lignes)
- [ ] Implémentation des patterns architecturaux
- [ ] Migration des fonctionnalités existantes
- [ ] Tests unitaires pour chaque service
- [ ] Documentation des nouvelles APIs

### Phase 3: Optimisation performances (Semaine 5)
**Objectif**: Optimiser Entity Framework et performances générales

**Optimisations EF**:
- Pagination intelligente pour grandes listes
- Projections pour réduire les transferts de données
- Cache en mémoire pour données fréquemment accédées
- Résolution du problème N+1 queries

**Monitoring et observabilité**:
- Métriques de performance des générations PDF
- Logs détaillés pour debug et monitoring
- Health checks pour les services critiques

**Livrables**:
- [ ] Implémentation de la pagination sur toutes les listes
- [ ] Cache Redis ou in-memory selon besoins
- [ ] Optimisation des requêtes EF avec Include/ThenInclude
- [ ] Métriques et monitoring des performances
- [ ] Documentation des bonnes pratiques

### Phase 4: Tests et qualité (Transverse)
**Objectif**: Assurer la qualité et la stabilité du système

**Stratégie de tests** (recommandée par Tech Lead):
- **Tests unitaires**: Services de génération PDF
- **Tests d'intégration**: Workflow complet de génération
- **Tests de performance**: Génération de gros documents
- **Tests de régression**: Compatibilité avec l'existant

**Quality gates**:
- [ ] Couverture de tests > 80% sur les nouveaux services
- [ ] Tests automatisés dans la CI/CD
- [ ] Validation des PDFs générés (structure, métadonnées)
- [ ] Tests de charge sur génération de documents
- [ ] Documentation technique à jour

## État Actuel (Septembre 2024)

### ✅ Fonctionnalités Terminées
- [x] Refactorisation complète interface SectionsLibres avec modal et filtres
- [x] Architecture modulaire avec conteneurs (SectionConteneur, FTConteneur)
- [x] Système d'autocomplétion avancé avec recherche
- [x] Gestion complète des chantiers et fiches techniques
- [x] Éditeur HTML riche avec upload d'images
- [x] Configuration centralisée et paramètres persistants

### 🔄 En Cours de Développement
- [ ] **Phase 1 PDF**: Implémentation génération PDF réelle
- [ ] **Phase 2 Services**: Refactoring architecture services
- [ ] **Phase 3 Performance**: Optimisations EF et cache
- [ ] **Phase 4 Tests**: Stratégie de tests complète

### 📋 Backlog Priorisé
1. Migration système PDF (PuppeteerSharp + PDFSharp)
2. Découpage DocumentGenereService en services métier
3. Optimisation performances base de données
4. Tests automatisés et CI/CD
5. Templates de documents personnalisables
6. Génération en lot (batch processing)
7. Support formats additionnels (Word, Excel)

---
*Dernière mise à jour: Septembre 2024*
*Roadmap validée par Software Architect et Tech Lead*

