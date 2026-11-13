# Project Playtime REMADE — Unity 2022.3.62f3

## Ouverture
1. Décompressez l'archive.
2. Dans Unity Hub, cliquez sur **Add** / **Ajouter**, puis sélectionnez le dossier `ProjectPlaytimeRemade`.
3. Ouvrez le projet avec **Unity 2022.3.62f3** (même version que celle déclarée dans `ProjectSettings/ProjectVersion.txt`).
4. Laissez Unity résoudre et télécharger les dépendances déclarées dans `Packages/manifest.json`.
5. Si Unity demande d'importer TextMesh Pro, utilisez **Import TMP Essentials**.

## Vérifications effectuées
- Structure Unity présente : `Assets`, `Packages` et `ProjectSettings`.
- Version déclarée : `2022.3.62f3`.
- `Packages/manifest.json` est un JSON valide.
- Scène `Assets/Scenes/Lobby.unity` présente et inscrite dans les scènes de build.
- Fichiers `.meta` présents pour tous les assets du dossier `Assets`.
- Aucun GUID `.meta` dupliqué détecté.

## À savoir
Cette archive ne contient pas le dossier `Library` : Unity le recréera automatiquement au premier lancement.
La restauration des packages nécessite une connexion Internet. Les scripts utilisent l'ancien Input Manager Unity, qui est conservé dans les paramètres du projet.

Le projet a été vérifié statiquement. Une compilation complète et un test en Play Mode nécessitent l'ouverture dans l'éditeur Unity 2022.3.62f3 ; ils ne peuvent pas être certifiés depuis cette archive seule.
