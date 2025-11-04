# ⚡ Migration Express PostgreSQL - Résumé 5 Minutes

**Version**: 2.2.0 | **Date**: 2025-10-28 | **Urgence**: 🔴 Critique (Production cassée)

---

## 🎯 Problème en 1 Phrase
> **La base PostgreSQL production n'a pas les 3 colonnes de personnalisation ajoutées récemment.**

---

## ⚡ Solution en 3 Étapes (5 minutes)

### 1️⃣ Ouvrir pgAdmin
- Lancer **pgAdmin 4**
- Naviguer : `Servers > PostgreSQL > Databases > GenerateurDOE_Prod`
- **Clic droit** sur `GenerateurDOE_Prod` → **"Query Tool"**

### 2️⃣ Exécuter le Script
- Ouvrir le fichier : [`AddPersonnalisationColumns_PostgreSQL.sql`](./AddPersonnalisationColumns_PostgreSQL.sql)
- **Copier tout le contenu** (`Ctrl+A`, `Ctrl+C`)
- **Coller dans Query Tool** (`Ctrl+V`)
- **Exécuter** (`F5` ou bouton ▶️)

### 3️⃣ Vérifier le Résultat
Vous devez voir dans l'onglet **Messages** :
```
✅ NOTICE: Colonne TitrePersonnalise ajoutée avec succès
✅ NOTICE: Colonne ContenuHtmlPersonnalise ajoutée avec succès
✅ NOTICE: Colonne DateModificationPersonnalisation ajoutée avec succès
```

**Et dans l'onglet Data Output**, un tableau avec 3 lignes affichant les colonnes.

---

## 🧪 Test Final

1. **Redémarrer l'application** (conteneur Docker)
2. **Vérifier les logs** : Plus d'erreur `42703`
3. **Tester la fonctionnalité** : Ajouter une section libre dans un document

---

## 📚 Documentation Détaillée

- **Instructions complètes** : [INSTRUCTIONS_MIGRATION_POSTGRESQL.md](./INSTRUCTIONS_MIGRATION_POSTGRESQL.md)
- **Guide visuel** : [GUIDE_VISUEL_PGADMIN.md](./GUIDE_VISUEL_PGADMIN.md)
- **Script SQL** : [AddPersonnalisationColumns_PostgreSQL.sql](./AddPersonnalisationColumns_PostgreSQL.sql)

---

## 🔍 Détails Techniques (Optionnel)

### Colonnes Ajoutées
```sql
ALTER TABLE "SectionConteneurItems"
ADD COLUMN "TitrePersonnalise" VARCHAR(200) NULL;

ALTER TABLE "SectionConteneurItems"
ADD COLUMN "ContenuHtmlPersonnalise" TEXT NULL;

ALTER TABLE "SectionConteneurItems"
ADD COLUMN "DateModificationPersonnalisation" TIMESTAMP NULL;
```

### Cause Racine
La migration `InitialCreate` (20251027170705) a été appliquée sur SQLite (dev) mais jamais sur PostgreSQL (prod). Le script SQL corrige cette désynchronisation.

### Sécurité
- ✅ Script **idempotent** (peut être exécuté plusieurs fois)
- ✅ Vérification automatique de l'existence des colonnes
- ✅ Colonnes **nullable** (compatibilité données existantes)
- ✅ Transaction automatique (rollback en cas d'erreur)

---

## 🚨 En Cas de Problème

### Erreur: "permission denied"
```sql
-- Se connecter en tant que postgres (admin)
GRANT ALL PRIVILEGES ON ALL TABLES IN SCHEMA public TO generateur_user;
```

### Erreur: "table does not exist"
La table `SectionConteneurItems` n'existe pas. Il faut d'abord appliquer la migration `InitialCreate` :
```bash
cd "c:\Users\cedric\Générateur DOE\GenerateurDOE"
dotnet ef database update --connection "VOTRE_CONNECTION_STRING"
```

### Script Déjà Exécuté ?
Si vous voyez :
```
NOTICE: Colonne TitrePersonnalise existe déjà
```
C'est **normal**, la migration est déjà appliquée. Pas d'action requise.

---

## ✅ Checklist Post-Migration

- [ ] Script exécuté sans erreur
- [ ] 3 NOTICE affichés (ou "existe déjà")
- [ ] Application redémarrée
- [ ] Erreur `42703` disparue des logs
- [ ] Fonctionnalité testée (ajout section)

---

**🎉 C'est tout !** En 5 minutes, votre base PostgreSQL est synchronisée.

**💡 Astuce** : Sauvegardez ces fichiers pour d'autres environnements (staging, backup, etc.).
