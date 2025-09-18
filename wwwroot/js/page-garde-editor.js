// Fonctions JavaScript pour l'éditeur de page de garde

window.insertTextAtCursor = function(text) {
    try {
        // Obtenir l'élément actif (l'éditeur HTML)
        const activeElement = document.activeElement;

        // Pour RadzenHtmlEditor, chercher l'iframe contenant l'éditeur
        let targetElement = activeElement;

        // Si on est dans un iframe (éditeur HTML riche)
        if (activeElement.tagName === 'IFRAME') {
            try {
                const iframeDoc = activeElement.contentDocument || activeElement.contentWindow.document;
                targetElement = iframeDoc.body;

                // Utiliser l'API de sélection pour l'iframe
                const selection = activeElement.contentWindow.getSelection();
                if (selection.rangeCount > 0) {
                    const range = selection.getRangeAt(0);
                    range.deleteContents();

                    const textNode = iframeDoc.createTextNode(text);
                    range.insertNode(textNode);

                    // Placer le curseur après le texte inséré
                    range.setStartAfter(textNode);
                    range.collapse(true);
                    selection.removeAllRanges();
                    selection.addRange(range);

                    return;
                }
            } catch (e) {
                console.warn('Cannot access iframe content, trying alternative methods:', e);
            }
        }

        // Pour les éléments de texte normaux (input, textarea)
        if (activeElement.tagName === 'INPUT' || activeElement.tagName === 'TEXTAREA') {
            const start = activeElement.selectionStart;
            const end = activeElement.selectionEnd;
            const value = activeElement.value;

            activeElement.value = value.substring(0, start) + text + value.substring(end);
            activeElement.selectionStart = activeElement.selectionEnd = start + text.length;

            // Déclencher l'événement input pour notifier Blazor
            activeElement.dispatchEvent(new Event('input', { bubbles: true }));
            return;
        }

        // Méthode alternative : utiliser execCommand (dépréciée mais fonctionne)
        if (document.queryCommandSupported && document.queryCommandSupported('insertText')) {
            document.execCommand('insertText', false, text);
            return;
        }

        // Dernière alternative : insérer dans la sélection courante
        const selection = window.getSelection();
        if (selection.rangeCount > 0) {
            const range = selection.getRangeAt(0);
            range.deleteContents();
            range.insertNode(document.createTextNode(text));
            range.collapse(false);
        }

    } catch (error) {
        console.error('Erreur lors de l\'insertion du texte:', error);
        // Fallback : copier dans le presse-papiers
        navigator.clipboard.writeText(text).then(() => {
            alert(`Variable copiée dans le presse-papiers: ${text}`);
        }).catch(() => {
            alert(`Impossible d'insérer automatiquement. Variable: ${text}`);
        });
    }
};

// Fonction pour obtenir l'éditeur HTML actif
window.getActiveHtmlEditor = function() {
    // Chercher tous les iframes de RadzenHtmlEditor
    const iframes = document.querySelectorAll('iframe');
    for (let iframe of iframes) {
        try {
            if (iframe.contentDocument && iframe.contentDocument.designMode === 'on') {
                return iframe;
            }
        } catch (e) {
            // Ignorer les erreurs d'accès iframe cross-origin
        }
    }
    return null;
};

// Fonction pour définir le focus sur l'éditeur HTML
window.focusHtmlEditor = function() {
    const editor = getActiveHtmlEditor();
    if (editor) {
        try {
            editor.contentWindow.focus();
            return true;
        } catch (e) {
            console.warn('Cannot focus HTML editor:', e);
        }
    }
    return false;
};

// Initialisation après chargement de la page
document.addEventListener('DOMContentLoaded', function() {
    console.log('Page Garde Editor JavaScript loaded');

    // Améliorer l'accessibilité des variables
    document.addEventListener('click', function(e) {
        if (e.target.closest('.variable-item')) {
            // S'assurer que l'éditeur HTML a le focus avant l'insertion
            setTimeout(() => {
                focusHtmlEditor();
            }, 100);
        }
    });
});