/**
 * Section Drag & Drop avec SortableJS
 * Gère la réorganisation des sections dans les conteneurs par glisser-déposer
 */
window.sectionDragDrop = {
    instances: {},

    /**
     * Initialise le drag & drop pour un conteneur de sections
     * @param {any} dotnetRef - Référence vers l'objet .NET pour les callbacks
     * @param {string} containerId - ID du conteneur (ex: "conteneur-123")
     */
    initialize: function(dotnetRef, containerId) {
        console.log(`[DEBUG] Attempting to initialize drag & drop for container: ${containerId}`);

        // Le conteneur a l'id ET la classe sections-list
        let container = document.querySelector(`#${containerId}.sections-list`);
        if (!container) {
            console.warn(`Container not found: #${containerId}.sections-list`);
            // Essayer aussi l'ancien sélecteur au cas où
            container = document.querySelector(`#${containerId}`);
            if (container) {
                console.log(`[DEBUG] Found container with alternative selector: #${containerId}`);
            } else {
                console.warn(`Container not found with any selector for: ${containerId}`);
                return;
            }
        }

        // Vérifier qu'il y a des éléments à trier
        const items = container.querySelectorAll('.section-item');
        if (items.length === 0) {
            console.warn(`No section items found in container: ${containerId}`);
            return;
        }

        // Vérifier que les poignées drag existent
        const dragHandles = container.querySelectorAll('.drag-handle');
        console.log(`[DEBUG] Found ${dragHandles.length} drag handles in container ${containerId}`);

        // Détruire l'instance existante si elle existe déjà
        if (this.instances[containerId]) {
            console.log(`[DEBUG] Destroying existing instance for ${containerId}`);
            this.instances[containerId].destroy();
        }

        console.log(`[DEBUG] Initializing drag & drop for container: ${containerId} with ${items.length} items`);

        // Créer une nouvelle instance SortableJS
        this.instances[containerId] = Sortable.create(container, {
            // Configuration SortableJS
            animation: 150,                    // Animation fluide de 150ms
            handle: '.drag-handle',            // Seule la poignée ⋮⋮ permet le drag
            ghostClass: 'sortable-ghost',      // Classe pendant le drag
            chosenClass: 'sortable-chosen',    // Classe de l'élément sélectionné
            dragClass: 'sortable-drag',        // Classe de l'élément en cours de drag

            // Événement déclenché à la fin du drag & drop
            onEnd: function(evt) {
                // Récupérer tous les IDs des items dans le nouvel ordre
                const itemIds = Array.from(container.children)
                    .map(el => parseInt(el.dataset.itemId))
                    .filter(id => !isNaN(id));

                console.log(`Items reordered for ${containerId}:`, itemIds);

                // Notifier Blazor du changement d'ordre
                if (dotnetRef && dotnetRef.invokeMethodAsync) {
                    dotnetRef.invokeMethodAsync('OnItemsReorderedJS', containerId, itemIds)
                        .then(() => {
                            console.log('Blazor notified successfully');
                        })
                        .catch(err => {
                            console.error('Error notifying Blazor:', err);
                        });
                }
            },

            // Événement au début du drag (optionnel pour le debug)
            onStart: function(evt) {
                console.log(`[DEBUG] Started dragging item from position ${evt.oldIndex}`);
                console.log(`[DEBUG] Dragged element:`, evt.item);
                console.log(`[DEBUG] Drag handle found:`, evt.item.querySelector('.drag-handle'));
            }
        });

        console.log(`Drag & drop initialized for ${containerId}`);
    },

    /**
     * Détruit l'instance de drag & drop pour un conteneur
     * @param {string} containerId - ID du conteneur
     */
    destroy: function(containerId) {
        if (this.instances[containerId]) {
            this.instances[containerId].destroy();
            delete this.instances[containerId];
            console.log(`Drag & drop destroyed for ${containerId}`);
        }
    },

    /**
     * Détruit toutes les instances (utile lors du nettoyage)
     */
    destroyAll: function() {
        Object.keys(this.instances).forEach(containerId => {
            this.destroy(containerId);
        });
        console.log('All drag & drop instances destroyed');
    }
};