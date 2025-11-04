/**
 * File Drop Handler pour Blazor InputFile
 * Gère le drag & drop de fichiers pour upload dans les composants Blazor
 *
 * @author Claude Code
 * @date 2025-11-04
 * @version 1.0
 */

window.fileDropHandler = {
    /**
     * Initialise le drag & drop sur une zone spécifique
     *
     * @param {HTMLElement} dropZone - Élément HTML de la zone de drop
     * @param {string} inputFileId - ID du InputFile Blazor associé
     * @returns {boolean} True si initialisation réussie, false sinon
     */
    initialize: function (dropZone, inputFileId) {
        // Validation des paramètres
        if (!dropZone) {
            console.error('[FileDropHandler] Drop zone element not found');
            return false;
        }

        const inputFile = document.getElementById(inputFileId);
        if (!inputFile) {
            console.error(`[FileDropHandler] InputFile element with id '${inputFileId}' not found`);
            return false;
        }

        // Vérifier que l'élément n'a pas déjà été initialisé
        if (dropZone.dataset.dropInitialized === 'true') {
            console.warn('[FileDropHandler] Drop zone already initialized');
            return false;
        }

        console.log(`[FileDropHandler] Initializing drag & drop for input '${inputFileId}'`);

        // Événement DROP : transférer les fichiers vers InputFile
        const dropHandler = (e) => {
            e.preventDefault();
            e.stopPropagation();

            if (!e.dataTransfer || !e.dataTransfer.files || e.dataTransfer.files.length === 0) {
                console.warn('[FileDropHandler] No files in dataTransfer');
                return;
            }

            console.log(`[FileDropHandler] Dropped ${e.dataTransfer.files.length} file(s)`);

            try {
                // Créer un nouvel objet DataTransfer pour contourner readonly
                const dataTransfer = new DataTransfer();

                // Copier tous les fichiers droppés
                Array.from(e.dataTransfer.files).forEach(file => {
                    dataTransfer.items.add(file);
                    console.log(`[FileDropHandler] - ${file.name} (${file.type}, ${file.size} bytes)`);
                });

                // Assigner les fichiers à l'InputFile (propriété 'files' est settable)
                inputFile.files = dataTransfer.files;

                // Déclencher l'événement 'change' pour notifier Blazor
                const changeEvent = new Event('change', { bubbles: true });
                inputFile.dispatchEvent(changeEvent);

                console.log('[FileDropHandler] Files transferred to InputFile successfully');
            } catch (error) {
                console.error('[FileDropHandler] Error transferring files:', error);
            }
        };

        // Événement DRAGOVER : empêcher le comportement par défaut (important !)
        const dragOverHandler = (e) => {
            e.preventDefault();
            e.stopPropagation();
            // Indiquer au navigateur qu'on accepte le drop
            e.dataTransfer.dropEffect = 'copy';
        };

        // Attacher les event listeners
        dropZone.addEventListener('drop', dropHandler);
        dropZone.addEventListener('dragover', dragOverHandler);

        // Marquer comme initialisé pour éviter les doublons
        dropZone.dataset.dropInitialized = 'true';

        // Stocker les handlers pour cleanup ultérieur
        dropZone._dropHandler = dropHandler;
        dropZone._dragOverHandler = dragOverHandler;

        console.log('[FileDropHandler] Initialization complete');
        return true;
    },

    /**
     * Détruit l'instance et nettoie les event listeners
     *
     * @param {HTMLElement} dropZone - Élément HTML de la zone de drop
     */
    destroy: function (dropZone) {
        if (!dropZone) {
            return;
        }

        console.log('[FileDropHandler] Destroying drag & drop handlers');

        // Supprimer les event listeners si ils existent
        if (dropZone._dropHandler) {
            dropZone.removeEventListener('drop', dropZone._dropHandler);
            delete dropZone._dropHandler;
        }

        if (dropZone._dragOverHandler) {
            dropZone.removeEventListener('dragover', dropZone._dragOverHandler);
            delete dropZone._dragOverHandler;
        }

        // Nettoyer les données
        delete dropZone.dataset.dropInitialized;

        console.log('[FileDropHandler] Cleanup complete');
    }
};

// Log de chargement du module
console.log('[FileDropHandler] Module loaded successfully');
