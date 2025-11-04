# 🔧 Instructions de Migration PostgreSQL - Ajout des Colonnes de Personnalisation

**Date**: 2025-10-28
**Version Application**: 2.2.0
**Base de données**: GenerateurDOE_Prod (PostgreSQL)

---

## 📋 Problème Identifié

### Erreur PostgreSQL
```
42703: column s0.ContenuHtmlPersonnalise does not exist
POSITION: 1588
```

### Cause Racine
La base de données PostgreSQL en production **n'a pas reçu les migrations** qui ajoutent les colonnes de personnalisation dans la table `SectionConteneurItems`. Ces colonnes ont été ajoutées dans la migration `InitialCreate` (20251027170705) pour SQLite en développement, mais jamais appliquées sur PostgreSQL en production.

### Colonnes Manquantes
- `TitrePersonnalise` (VARCHAR 200, nullable)
- `ContenuHtmlPersonnalise` (TEXT, nullable)
- `DateModificationPersonnalisation` (TIMESTAMP, nullable)

---

## 🎯 Solution: Application du Script SQL

### Fichier à Utiliser
📁 **Fichier**: `AddPersonnalisationColumns_PostgreSQL.sql`
📂 **Emplacement**: `c:\Users\cedric\Générateur DOE\GenerateurDOE\Migrations\`

---

## 🚀 Procédure d'Application via pgAdmin

### Étape 1: Ouvrir pgAdmin 4
1. Lancez **pgAdmin 4** sur votre poste
2. Connectez-vous au serveur PostgreSQL (localhost:5432)
3. Naviguez dans l'arborescence :
   ```
   Servers
   └── PostgreSQL 13 (ou version)
       └── Databases
           └── GenerateurDOE_Prod
   ```

### Étape 2: Ouvrir l'Outil de Requête
1. **Clic droit** sur la base `GenerateurDOE_Prod`
2. Sélectionnez **"Query Tool"** (ou appuyez sur `Alt+Shift+Q`)
3. Une nouvelle fenêtre d'éditeur SQL s'ouvre

### Étape 3: Charger le Script
**Option A - Copier/Coller** (Recommandé):
1. Ouvrez le fichier `AddPersonnalisationColumns_PostgreSQL.sql` dans un éditeur de texte
2. Sélectionnez **tout le contenu** (`Ctrl+A`)
3. Copiez (`Ctrl+C`)
4. Collez dans la fenêtre Query Tool de pgAdmin (`Ctrl+V`)

**Option B - Ouverture de fichier**:
1. Dans Query Tool, cliquez sur l'icône **"Open File"** (📂)
2. Naviguez vers `c:\Users\cedric\Générateur DOE\GenerateurDOE\Migrations\`
3. Sélectionnez `AddPersonnalisationColumns_PostgreSQL.sql`

### Étape 4: Exécuter le Script
1. Vérifiez que le script complet est bien affiché dans l'éditeur
2. Cliquez sur le bouton **"Execute/Run"** (▶️ Play) ou appuyez sur `F5`
3. Attendez la fin de l'exécution (quelques secondes)

### Étape 5: Vérifier les Résultats

#### Onglet "Messages"
Vous devriez voir 3 messages `NOTICE` confirmant l'ajout des colonnes :
```
NOTICE: Colonne TitrePersonnalise ajoutée avec succès
NOTICE: Colonne ContenuHtmlPersonnalise ajoutée avec succès
NOTICE: Colonne DateModificationPersonnalisation ajoutée avec succès
```

#### Onglet "Data Output"
Un tableau affichant les 3 colonnes ajoutées :
```
| Colonne                         | Type                        | Longueur Max | Nullable |
|---------------------------------|-----------------------------|--------------|----------|
| ContenuHtmlPersonnalise         | text                        | NULL         | YES      |
| DateModificationPersonnalisation| timestamp without time zone | NULL         | YES      |
| TitrePersonnalise               | character varying           | 200          | YES      |
```

---

## ✅ Vérification Post-Migration

### Test 1: Vérifier la Structure de la Table
Exécutez cette requête dans pgAdmin :
```sql
SELECT column_name, data_type, character_maximum_length, is_nullable
FROM information_schema.columns
WHERE table_name = 'SectionConteneurItems'
ORDER BY ordinal_position;
```

**Attendu**: Vous devez voir les 3 nouvelles colonnes à la fin de la liste.

### Test 2: Redémarrer l'Application
1. Arrêtez le conteneur Docker de l'application
2. Redémarrez le conteneur
3. Vérifiez les logs pour confirmer l'absence d'erreurs `42703`

### Test 3: Tester la Fonctionnalité
1. Connectez-vous à l'application (Portainer WebUI ou directement)
2. Naviguez vers les **Sections Libres**
3. Essayez d'ajouter une section à un document
4. **Résultat attendu** : Aucune erreur, opération réussie

---

## 🔄 Si le Script a Déjà Été Exécuté

Le script est **idempotent**, c'est-à-dire qu'il peut être exécuté plusieurs fois sans erreur. Si les colonnes existent déjà, vous verrez :
```
NOTICE: Colonne TitrePersonnalise existe déjà
NOTICE: Colonne ContenuHtmlPersonnalise existe déjà
NOTICE: Colonne DateModificationPersonnalisation existe déjà
```

Cela signifie que la migration est déjà appliquée et tout est OK.

---

## 🚨 Dépannage

### Erreur: "La table SectionConteneurItems n'existe pas !"
**Cause**: La table n'a jamais été créée en PostgreSQL.
**Solution**: Il faut appliquer la migration `InitialCreate` complète :
```bash
cd "c:\Users\cedric\Générateur DOE\GenerateurDOE"
dotnet ef database update --connection "Host=localhost;Port=5432;Database=GenerateurDOE_Prod;Username=generateur_user;Password=VOTRE_PASSWORD"
```

### Erreur: "relation does not exist"
**Cause**: Problème de casse dans les noms de table (PostgreSQL est sensible à la casse).
**Solution**: Vérifiez que le script utilise bien les guillemets doubles autour des noms : `"SectionConteneurItems"`.

### Erreur: Permission refusée
**Cause**: L'utilisateur `generateur_user` n'a pas les droits `ALTER TABLE`.
**Solution**: Connectez-vous avec un utilisateur admin (postgres) et donnez les droits :
```sql
GRANT ALL PRIVILEGES ON ALL TABLES IN SCHEMA public TO generateur_user;
```

---

## 📊 Informations Techniques

### Configuration PostgreSQL (appsettings.PostgreSQL.json)
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=GenerateurDOE_Prod;Username=generateur_user;Password=REPLACE_WITH_PASSWORD;SSL Mode=Prefer;Trust Server Certificate=true;Command Timeout=300;"
  },
  "DatabaseProvider": "PostgreSQL",
  "Npgsql": {
    "EnableLegacyTimestampBehavior": true
  }
}
```

### Modèle C# (SectionConteneurItem.cs)
```csharp
public class SectionConteneurItem
{
    public int Id { get; set; }
    public int Ordre { get; set; }
    public DateTime DateAjout { get; set; }
    public int SectionConteneursId { get; set; }
    public int SectionLibreId { get; set; }

    // 🆕 Colonnes de personnalisation
    [StringLength(200)]
    public string? TitrePersonnalise { get; set; }

    [StringLength(int.MaxValue)]
    public string? ContenuHtmlPersonnalise { get; set; }

    public DateTime? DateModificationPersonnalisation { get; set; }
}
```

---

## 📞 Support

Si vous rencontrez des problèmes lors de l'application du script :
1. Vérifiez les logs PostgreSQL dans pgAdmin (Tools > Server Status)
2. Vérifiez les logs de l'application Blazor (Logs/app-*.log)
3. Capturez le message d'erreur complet et consultez la documentation EF Core

---

**✅ Migration testée et validée**
**🔒 Script idempotent (peut être exécuté plusieurs fois)**
**📝 Conservez ce fichier pour référence future**
