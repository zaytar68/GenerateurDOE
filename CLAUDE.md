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
- `DocumentExport` - Export operations with format support and parameter configuration
- "ImportPDF" - Représente un fichier pdf avec son emplacement, son type 
- "Méthode" - Représente une méthodologie avec un titre, une description. Il doit être possible d'y ajouter des images.
- "SectionLibre" - Représente une section personnalisable dans un document avec titre, contenu HTML et type
- "TypeSection" - Énumération des types de sections disponibles

### Services (Dependency Injected)
- `IFicheTechniqueService` - Manages technical sheets with PDF file operations
- `IMemoireTechniqueService` - Manages technical reports
- `IDocumentExportService` - Handles document generation and export (singleton)
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

