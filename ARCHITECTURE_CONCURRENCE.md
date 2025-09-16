# 🔧 ARCHITECTURE ANTI-CONCURRENCE DbContext

## 🚨 PROBLÈME RÉSOLU

**Erreur critique :** `A second operation was started on this context instance before a previous operation completed`

**Source identifiée :** Événements UI simultanés dans Blazor Server déclenchant des opérations DbContext parallèles sur la même instance scoped.

## 🎯 SOLUTION IMPLÉMENTÉE

### 1. **OperationLockService - Protection Anti-Concurrence**

```csharp
// Utilisation dans les composants Blazor
var operationKey = $"operation-type-{entityId}";
var executed = await operationLockService.ExecuteWithLockAsync(operationKey, async () =>
{
    // Opérations DbContext protégées
    await service.SaveAsync();
    await OnChanged.InvokeAsync();
});
```

**Avantages :**
- ✅ Empêche les opérations simultanées sur même entité
- ✅ Timeout configurable (défaut: 10s)
- ✅ Gestion automatique des semaphores
- ✅ Logs détaillés pour debugging
- ✅ Thread-safe avec ConcurrentDictionary

### 2. **Corrections Critiques Appliquées**

#### **SectionConteneurEditor.razor**
- `CreerNouveauConteneur()` - Protection création
- `SupprimerConteneur()` - Protection suppression
- `AjouterSection()` - Protection ajout multiple
- `RetirerSectionItem()` - Protection suppression item
- `DeplacerSection*()` - Protection réorganisation

#### **FTConteneurEditor.razor**
- `SupprimerElement()` - Protection suppression
- `MettreAJourElement()` - Protection mise à jour
- `CalculerNumeroPages()` - Protection calcul
- `SauvegarderConteneur()` - Protection sauvegarde

## 📋 PATTERNS PRÉVENTIFS POUR L'ÉQUIPE

### 🔴 **RÈGLES STRICTES - TOUJOURS APPLIQUER**

#### **1. Événements UI avec DbContext**
```csharp
// ❌ INTERDIT - Concurrence possible
private async Task OnButtonClick()
{
    await service.SaveAsync();
    await OnChanged.InvokeAsync();
}

// ✅ OBLIGATOIRE - Protection systématique
private async Task OnButtonClick()
{
    var operationKey = $"button-click-{entityId}";
    await operationLockService.ExecuteWithLockAsync(operationKey, async () =>
    {
        await service.SaveAsync();
        await OnChanged.InvokeAsync();
    });
}
```

#### **2. Opérations Cascade**
```csharp
// ❌ INTERDIT - Recharge + Invoke simultané
await service.UpdateAsync(item);
var updated = await service.GetByIdAsync(item.Id);  // ← DANGER
await OnChanged.InvokeAsync(updated);

// ✅ OBLIGATOIRE - Dans la même protection
await operationLockService.ExecuteWithLockAsync(operationKey, async () =>
{
    await service.UpdateAsync(item);
    var updated = await service.GetByIdAsync(item.Id);
    await OnChanged.InvokeAsync(updated);
});
```

#### **3. Événements @bind:after**
```csharp
// ❌ RISQUÉ - Binding rapide
<input @bind="value" @bind:after="SaveChanges" />

// ✅ SÉCURISÉ - Debounce + protection
<input @bind="value" @bind:after="SaveChangesProtected" />

private async Task SaveChangesProtected()
{
    var operationKey = $"bind-save-{entityId}";
    await operationLockService.ExecuteWithLockAsync(operationKey, async () =>
    {
        await SaveChanges();
    });
}
```

### 🟡 **PATTERNS RECOMMANDÉS**

#### **1. Nommage des Clés d'Opération**
```csharp
// Pattern: "action-entity-id-context"
$"create-container-{documentId}-{typeId}"
$"delete-section-{sectionId}"
$"reorder-items-{containerId}"
$"update-element-{elementId}"
```

#### **2. Gestion d'Erreurs**
```csharp
var executed = await operationLockService.ExecuteWithLockAsync(operationKey, async () =>
{
    // Opération protégée
});

if (!executed)
{
    NotificationService.Notify(NotificationSeverity.Warning,
        "Opération ignorée", "Une opération similaire est en cours");
}
```

#### **3. Timeout Personnalisé**
```csharp
// Opérations longues (PDF, exports)
await operationLockService.ExecuteWithLockAsync(operationKey, operation, timeout: 30000);

// Opérations standard
await operationLockService.ExecuteWithLockAsync(operationKey, operation); // 10s par défaut
```

## 🔍 **DÉTECTION PRÉCOCE**

### **Logs à Surveiller**
```log
// ✅ Normal - Opération protégée
"Executing operation 'create-container-123' with concurrency protection"

// ⚠️ Attention - Opération ignorée (fréquent = problème UX)
"Operation 'delete-section-456' skipped - another operation in progress"

// 🚨 Critique - Erreur de concurrence (ne devrait plus arriver)
"A second operation was started on this context instance"
```

### **Métriques de Performance**
- Opérations ignorées/heure < 5% du total
- Timeout dépassés = 0
- Erreurs DbContext = 0

## 🚀 **ÉVOLUTIONS FUTURES**

### **Phase 3D - Optimisations Avancées**
1. **Debouncing automatique** pour événements rapides
2. **Queue d'opérations** pour traitement séquentiel
3. **Cache intelligent** pour réduire les requêtes
4. **Metrics dashboard** pour monitoring temps réel

### **Monitoring Proactif**
```csharp
// Service de monitoring intégré
services.AddScoped<IConcurrencyMonitoringService>();

// Dashboard temps réel des opérations
/admin/concurrency-monitoring
```

---

## 📝 **CHECKLIST DÉVELOPPEUR**

Avant chaque commit avec opérations DbContext :

- [ ] Tous les événements UI sont protégés par OperationLockService
- [ ] Clés d'opération uniques et descriptives
- [ ] Gestion des opérations ignorées avec notification utilisateur
- [ ] Tests de charge sur boutons/événements rapides
- [ ] Vérification logs sans erreurs de concurrence

**Contact Tech Lead :** En cas de doute sur l'implémentation des protections anti-concurrence.