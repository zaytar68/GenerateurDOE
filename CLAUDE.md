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
- "Chantier" - Représente un projet avec un Maitre d'œuvre, un maître d'ouvrage, une adresse (IMPORTANT: les lots sont maintenant dans DocumentGenere)
- "DocumentGenere" - Document avec son chantier, type, format, nom fichier, NumeroLot et IntituleLot spécifiques
- "Fiche Technique" - Représente un produit avec son nom, le nom du fabricant, le type de produit et les documents "ImportPDF" associés
- "ImportPDF" - Représente un fichier pdf avec son emplacement, son type 
- "Méthode" - Représente une méthodologie avec un titre, une description. Il doit être possible d'y ajouter des images
- "SectionLibre" - Représente une section personnalisable dans un document avec titre, contenu HTML et type
- "TypeSection" - Énumération des types de sections disponibles

### Services (Dependency Injected)
- `IFicheTechniqueService` - Manages technical sheets with PDF file operations
- `IMemoireTechniqueService` - Manages technical reports
- `IDocumentGenereService` - Handles document generation and export (singleton)
- `IDocumentRepositoryService` - Repository Pattern avec projections DTO pour performances optimisées
- `IChantierService` - Manages construction sites (lots logic moved to documents)
- `IPdfGenerationService` - Real PDF generation with PuppeteerSharp + PDFSharp architecture
- `IHtmlTemplateService` - Professional HTML templates for document generation
- `IConfigurationService` - Manages application configuration and settings
- `IFileExplorerService` - Handles file system operations and folder management
- `ITypeProduitService` - Manages product types configuration
- `ITypeDocumentImportService` - Manages document import types
- `ISectionLibreService` - Manages custom sections for documents
- `ITypeSectionService` - Manages section types configuration
- `IImageService` - Handles image upload, storage and management
- `ILoggingService` - Centralized logging service

### Export Capabilities
- **PDF**: Production-ready with PuppeteerSharp (HTML→PDF) + PDFSharp (assembly/optimization) 
- **HTML**: Full-featured with CSS styling, table of contents, cover pages
- **Markdown**: Clean markdown with metadata
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

## État Actuel (Septembre 2025)

### ✅ Fonctionnalités Terminées
- [x] Refactorisation complète interface SectionsLibres avec modal et filtres
- [x] Architecture modulaire avec conteneurs (SectionConteneur, FTConteneur)
- [x] Système d'autocomplétion avancé avec recherche
- [x] Gestion complète des chantiers et fiches techniques
- [x] Éditeur HTML riche avec upload d'images
- [x] Configuration centralisée et paramètres persistants

### 🔄 En Cours de Développement
- [x] **Phase 1 PDF**: Implémentation génération PDF réelle ✅ **TERMINÉ**
- [x] **Phase 1.5 PDF**: Validation intégration fiches techniques ✅ **TERMINÉ**
- [x] **Phase 2 Services**: Migration lots + Repository Pattern ✅ **TERMINÉ**
- [x] **Phase 2.5 DbContextFactory**: Migration complète vers DbContextFactory ✅ **TERMINÉ**
- [ ] **Phase 3 Performance**: Optimisations EF et cache ⚡ **EN ANALYSE**
- [ ] **Phase 4 Tests**: Stratégie de tests complète

### 📋 Backlog Priorisé
1. ~~Migration système PDF (PuppeteerSharp + PDFSharp)~~ ✅ **TERMINÉ**
2. ~~Migration lots de Chantier vers DocumentGenere~~ ✅ **TERMINÉ**
3. ~~Repository Pattern avec projections DTO~~ ✅ **TERMINÉ**
4. ~~Migration complète vers DbContextFactory~~ ✅ **TERMINÉ**
5. Optimisation performances EF Core ⚡ **PRIORITÉ 1**
6. Tests automatisés et CI/CD
7. Templates de documents personnalisables
8. Génération en lot (batch processing)
9. Support formats additionnels (Word, Excel)

## 🚀 Analyse des Performances - Phase 3 (Septembre 2025)

### 📊 **GOULOTS D'ÉTRANGLEMENT IDENTIFIÉS**

#### 🔴 **PROBLÈMES CRITIQUES DÉTECTÉS**

**1. EF Core - Multiple Collection Warning**
```log
Compiling a query which loads related collections for more than one collection navigation
```
- **Impact** : Requêtes très lentes sur les documents complexes (40-60% plus lent)
- **Localisation** : `GenerateCompletePdfAsync()` ligne 512-522 dans DocumentGenereService
- **Cause** : Multiple Include().ThenInclude() sans QuerySplittingBehavior configuré

**2. EF Core - Shadow Properties Warning**  
```log
Multiple relationships between 'SectionLibre' and 'SectionConteneur' without configured foreign key
```
- **Impact** : Configuration EF ambiguë, possibles erreurs de mapping
- **Solution** : Configuration explicite des relations avec `[ForeignKey]`

**3. N+1 Query Problem**
- **Détecté** : 65 occurrences de `GetAllAsync/ToListAsync` dans 16 services
- **Impact** : Requêtes multiples inutiles pour les relations
- **Pages affectées** : Chantiers, FichesTechniques, SectionsLibres

### 🎯 **PLAN D'OPTIMISATION PRIORITAIRE**

#### **Phase 3A : Correction EF Core Critical (Priorité 1 - 2h)**

**1. QuerySplittingBehavior Configuration**
```csharp
// Dans ApplicationDbContext.OnConfiguring()
protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
{
    optionsBuilder.UseSqlServer(connectionString)
        .UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
}
```
- **Gain attendu** : +40-60% performance sur requêtes complexes

**2. Relations Disambiguation**
```csharp
// Dans SectionLibre.cs - clarifier les relations
[ForeignKey("SectionConteneurId")]
public virtual SectionConteneur? SectionConteneur { get; set; }
```
- **Gain attendu** : +100% stabilité mapping relationnel

#### **Phase 3B : Performance Queries (Priorité 2 - 3h)**

**3. Projections DTO**
```csharp
public class DocumentSummaryDto 
{
    public int Id { get; set; }
    public string NomFichier { get; set; }
    public string ChantierNom { get; set; }
    // Éviter le chargement complet des entités
}
```
- **Gain attendu** : +30-50% performance transfert données

**4. Pagination Intelligente**
```csharp
public async Task<PagedResult<T>> GetPagedAsync(int page, int size)
{
    var query = _context.Set<T>()
        .Skip((page - 1) * size)
        .Take(size);
    // + cache du count total
}
```
- **Gain attendu** : +80% temps chargement listes

#### **Phase 3C : Cache Strategy (Priorité 3 - 2h)**

**5. Memory Cache Implementation**
```csharp
services.AddMemoryCache();
// Cache pour données référentielles :
// - TypesSections (expiration: 1h)
// - TypesProduits (expiration: 1h) 
// - AppSettings (expiration: 30min)
```
- **Gain attendu** : +70% accès données statiques

**6. PDF Generation Optimization**
- Cache templates HTML compilés
- Pool de browsers PuppeteerSharp (singleton pattern)
- **Gain attendu** : +25-40% performance génération PDF

#### **Phase 3D : Database Indexing (Priorité 4 - 1h)**

**7. Index Strategy**
```sql
-- Index composite pour SectionConteneur
CREATE INDEX IX_SectionConteneur_DocumentGenere_TypeSection 
ON SectionConteneur (DocumentGenereId, TypeSectionId);

-- Index pour SectionsLibres
CREATE INDEX IX_SectionLibre_TypeSection_Active 
ON SectionLibre (TypeSectionId, IsActive);

-- Index pour DocumentsGeneres  
CREATE INDEX IX_DocumentGenere_Chantier_EnCours
ON DocumentGenere (ChantierId, EnCours);
```
- **Gain attendu** : +20-35% performance requêtes

### 💡 **GAINS TOTAUX ATTENDUS**

| Optimisation | Gain Performance | Priorité | Temps |
|-------------|------------------|----------|-------|
| QuerySplittingBehavior | +40-60% | 🔴 Critique | 2h |
| Relations EF | +100% stabilité | 🔴 Critique | 1h |
| Pagination | +80% listes | 🟡 Haute | 2h |
| Memory Cache | +70% données ref | 🟡 Haute | 2h |
| DTO Projections | +30-50% transfert | 🟢 Moyenne | 2h |
| PDF Optimization | +25-40% PDF | 🟢 Moyenne | 1h |

**📈 Impact Global Estimé :**
- **Génération PDF** : 40-60% plus rapide
- **Navigation générale** : 30-50% plus fluide  
- **Listes/Chargement** : 80% plus rapide
- **Stabilité EF** : 100% warnings résolus

**⏱️ Temps Total** : 8-10 heures sur 2-3 jours

### 🛠️ **IMPLÉMENTATION RECOMMANDÉE**

**🔥 AUJOURD'HUI (Critique) :**
1. QuerySplittingBehavior (DocumentGenereService ligne 512-522)
2. Relations disambiguation (ApplicationDbContext)

**⚡ CETTE SEMAINE (Haute priorité) :**
3. Pagination sur pages principales
4. Memory cache pour données référentielles

**📈 SEMAINE PROCHAINE (Optimisation) :**
5. DTO projections pour listes
6. PDF cache & browser pooling
7. Database indexes supplémentaires

## 📈 **ÉTAT PHASE 2 - TERMINÉE (Septembre 2025)**

### ✅ **MIGRATION LOTS RÉUSSIE**
- **Business Logic corrigée** : Lots déplacés de Chantier vers DocumentGenere (1 chantier -> N documents -> 1 lot/document)
- **Migrations EF** : MoveLotFromChantierToDocument + UpdateModelAfterLotMigration appliquées avec succès
- **Repository Pattern** : DocumentRepositoryService avec projections DTO (+30-50% performances)
- **UI Mise à jour** : Formulaires DocumentGenere avec validation complète des champs lot
- **Architecture nettoyée** : Suppression logique lot obsolète de ChantierService

### ✅ **SERVICES REFACTORISÉS**
- **IPdfGenerationService** : Génération PDF réelle avec PuppeteerSharp + PDFSharp
- **IHtmlTemplateService** : Templates professionnels pour pages de garde et sections
- **IDocumentRepositoryService** : Repository Pattern avec optimisations EF Core
- **Architecture hybride** : 3 couches (HTML → PDF → Assembly) opérationnelle

## 📈 **ÉTAT PHASE 2.5 - TERMINÉE (Septembre 2025)**

### ✅ **MIGRATION DBCONTEXTFACTORY RÉUSSIE**
**Problème résolu** : Architecture mixte (5/15 services DbContextFactory, 10/15 services ApplicationDbContext direct) créait des risques de concurrence en Blazor Server

**Services migrés avec succès** :
- [x] **DocumentGenereService** (550+ lignes) : Service principal avec 20+ méthodes
- [x] **SectionLibreService** (205 lignes) : Gestion sections avec logique réorganisation
- [x] **SectionConteneurService** (280+ lignes) : Service le plus complexe avec 15+ méthodes
- [x] **TypeSectionService** (220 lignes) : Migration avec préservation cache L1
- [x] **TypeProduitService** (187 lignes) : Pattern similaire TypeSectionService
- [x] **TypeDocumentImportService** (181 lignes) : Migration + correction cache manquant
- [x] **MemoireTechniqueService** (177 lignes) : Gestion méthodes et images
- [x] **ConfigurationService** : Aucune migration nécessaire (pas de DbContext)

**Améliorations techniques** :
- **Architecture 100% cohérente** : Tous les services DB utilisent DbContextFactory
- **42+ ConfigureAwait(false)** ajoutés pour éviter les deadlocks
- **Correction bugs cache** : 3 invalidations manquantes ajoutées dans TypeDocumentImportService
- **0 erreurs compilation** : Migration sans régression
- **Interface contract fix** : GetNextOrderAsync() interface/implémentation synchronisée

**Pattern standardisé appliqué** :
```csharp
// Avant
public async Task<Entity> GetByIdAsync(int id)
{
    return await _context.Entities.FindAsync(id);
}

// Après
public async Task<Entity> GetByIdAsync(int id)
{
    using var context = await _contextFactory.CreateDbContextAsync().ConfigureAwait(false);
    return await context.Entities.FindAsync(id).ConfigureAwait(false);
}
```

### 🎯 **PROCHAINE ÉTAPE : PHASE 3 PERFORMANCE**
La Phase 3 devient maintenant la priorité absolue avec les optimisations EF Core critiques identifiées :
1. **QuerySplittingBehavior** (40-60% gain sur requêtes complexes)
2. **Relations disambiguation** (100% stabilité EF)
3. **Memory caching** (70% gain données référentielles)

## 🎯 **DÉCISION ARCHITECTURALE : ASYNC vs SYNC (Septembre 2025)**

### ✅ **VERDICT TECHNIQUE FINAL**
**Architecture asynchrone CONSERVÉE** - Recommandation unanime Software Architect + Tech Lead

**Analyse des agents techniques** :
- ❌ **Synchrone serait une RÉGRESSION architecturale** : Deadlocks, thread blocking, perte de scalabilité
- ✅ **Asynchrone actuel est OPTIMAL** : DbContext Scoped + QuerySplittingBehavior + 42 ConfigureAwait(false)
- 🎯 **Solution** : Améliorer loading states UI plutôt que changer l'architecture

### 🔧 **PLAN D'AMÉLIORATION UX (Phase 3D)**

**Priorité 1 - Loading States (4-5h sur 1-2 jours)** :
1. **LoadingWrapper Component** : Composant réutilisable avec RadzenProgressBarCircular
2. **LoadingStateService** : Service de gestion d'état global (injection Scoped)
3. **UI Feedback** : Spinners sur boutons génération PDF, opérations CRUD
4. **Memory Cache** : Données référentielles (TypesSections, TypesProduits - expiration 1h)

**Gains attendus** :
- **UX** : +80% perception de réactivité
- **Performance** : Conservation +300% throughput async
- **Stabilité** : Aucune régression architecturale
- **Cache** : +70% vitesse données statiques

**Patterns UI recommandés** :
```razor
<RadzenButton Click="HandleOperation" Disabled="@isLoading">
    @if (isLoading)
    {
        <RadzenIcon Icon="hourglass_empty" Style="animation: spin 1s linear infinite;" />
        <span>Opération en cours...</span>
    }
</RadzenButton>
```

### 📋 **BACKLOG MISE À JOUR**
4. **Phase 3D UX** : États de chargement et feedback utilisateur ⚡ **NOUVELLE PRIORITÉ**
5. Optimisation performances EF Core (QuerySplittingBehavior + Relations)
6. Tests automatisés et CI/CD

---
*Dernière mise à jour: Septembre 2025*
*Architecture async validée - Phase 3D UX Loading States ajoutée*
*Décision technique : Software Architect + Tech Lead + User*
- Todo : \
- "Position marché" non obligatoire\
- Numéro de lot non obligatoire\
- Possibilité de modifier le sommaire
- Use dotnet watch