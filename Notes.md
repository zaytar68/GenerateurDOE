# Processus de création d'un document

Utilise l'agent software-architect.

L'utilisateur crée un document à générer dans la page @Pages/ChantierDetail.
Le document comporte ou non des sections libres.

Le DocumentGenere comporte un champ "EnCours" pour indiquer que le document n'est pas finalisé.

Il faut créer un nouvel objet/model SectionConteneur lié à SectionLibre.
Il ne peut y avoir que 1 SectionConteneur de chaque TypedeSection par DocumentGenere.
Dans un SectionConteneur, il ne peut y avoir que des SectionLibre du même TypedeSection.

Il existera une section particulière pour les fiches techniques : FTConteneur.
Il faut un écran pour selectionner quelles fiches techniques sont à intégrer dans le document ainsi que la "PositionMarché" qu'elle représente. Dans cet écran, il faudar aussi choisir quel ImportPdf appartenant à la FicheTechnique il faut intégrer au DocumentGenere.
La section FTConteneur débutera par un tableau récupitulatif indiquant :

- La position marché
- La marque et le nom du produit
- le type de produit
- les types de document associés (TypeDocumentImports)
- la page afférente dans le document

Elle agrégera ensuite le contenu des fichiers PDF ImportPDF.

A réaliser plus tard:
Lorsque l'utilisateur clique sur "Générer le document", nous aurons un module qui va générer la page de garde avec les champs de Chantier, le sommaire dynamiquement généré en fonction du contenu entier du document. Intégrer les SectionConteneur et la FTConteneur.

# A vérifier
Une SectionLibre peut appartenir à plusieurs SectionConteneur