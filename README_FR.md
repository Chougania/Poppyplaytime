# Project Playtime REMADE — Unity 2022.3.62f3

Projet Unity de fan-game multijoueur inspiré de Project Playtime.

## Ouverture du projet

1. Clonez ou téléchargez le dépôt.
2. Dans Unity Hub, cliquez sur **Ajouter** et sélectionnez le dossier du projet.
3. Ouvrez-le avec **Unity 2022.3.62f3**.
4. Laissez Unity restaurer les dépendances indiquées dans `Packages/manifest.json`.
5. Si nécessaire, importez **TextMesh Pro > Import TMP Essentials**.

## Dépendances principales

- Unity Netcode for GameObjects 1.12.2
- Unity Transport 1.4.1
- TextMesh Pro 3.0.9
- Unity Services (Authentication, Lobby et Relay)

## Améliorations récentes

La branche `upgrade/gameplay-polish` prépare les améliorations suivantes :

- **GrabPack :** contrôle des références manquantes, portée maximale appliquée au tir, attachement plus fiable, corde mieux configurée et remise en place propre de la main.
- **Physique du GrabPack :** vérifications des références caméra, CharacterController et points de sortie avant les calculs physiques.
- **Personnalisation des mains :** vérification des références de configuration, des index de matériaux et des entrées de palette pour éviter les erreurs d'index et les NullReferenceException.
- **Gestion de partie :** chronomètre écrit uniquement par le serveur, durée minimale validée, gestion plus sûre du cycle réseau et chronomètre indépendant de Time.timeScale.
- **Connexion en ligne :** initialisation Unity Services partagée entre les boutons Host/Join, authentification vérifiée même si les services étaient déjà initialisés, contrôles NetworkManager/UnityTransport et arrêt plus propre de la session Lobby.
- **Pseudo joueur :** nom répliqué et borné, affichage du tag au-dessus du joueur avec orientation vers la caméra.
- **Rigidbody réseau :** autorité de simulation recalculée à chaque changement de propriétaire, au lieu de basculer la physique chaque frame.
- **Anti-triche de base :** validation côté serveur du rôle du monstre, de la cible, de la distance et de la zone d'attaque avant une élimination.

## Dossier `autres`

Le dossier `autres/GrabPack-Unity-Project-1.1-main` est conservé comme **référence de migration**. Il contient un projet Unity distinct (sa propre version Unity et ses propres dépendances). Ne déplacez pas tout son contenu directement dans `Assets` : cela peut introduire des scripts dupliqués, des conflits de classes, des shaders incompatibles et des références cassées.

Les systèmes à évaluer et intégrer progressivement sont notamment le câble physique, le lancement de main, l'interaction, le scanner de main et l'IA. Chaque intégration doit être adaptée à l'architecture réseau et aux composants déjà utilisés par ce projet. Vérifiez également les licences et conditions de redistribution des ressources avant de publier une copie.


### GrabPack : progression et mains spécialisées

Le prefab `Assets/Prefabs/Player.prefab` reçoit maintenant `GrabpackProgression`. Les mains bleue et rouge restent disponibles au départ. Les capacités supplémentaires se débloquent par stations de progression et leur état est répliqué par Netcode, puis sauvegardé localement pour le joueur :

| Capacité | Utilisation prévue dans les puzzles |
|---|---|
| Verte | alimenter un récepteur / transférer une charge |
| Magnétique | activer un mécanisme magnétique |
| Flare | déclencher un récepteur sensible à la chaleur |
| Blacklight | révéler un indice ou un symbole caché |

**Configurer un déblocage :**
1. Ajoutez `GrabpackUnlockStation` à un objet de la carte avec un Collider en mode **Is Trigger**.
2. Choisissez `Green`, `Magnet`, `Flare` ou `Blacklight` dans `Ability To Unlock`.
3. Le joueur doit entrer dans le volume avec son personnage réseau. Le serveur accorde la capacité une seule fois.

**Configurer un puzzle :**
1. Ajoutez un `NetworkObject` et `GrabpackPuzzleInteractable` à l'objet qui doit être activé.
2. Choisissez la capacité attendue dans `Required Ability`.
3. Reliez `On Activated` à l'action du puzzle (porte, lumière, ascenseur, alimentation, etc.) et assignez éventuellement `Activated Visual`.
4. Le joueur vise l'objet et appuie sur **E**. Le serveur vérifie la capacité débloquée et la distance avant d'activer le puzzle. L'état activé est répliqué.

**Modèles des mains :** le prefab expose les emplacements `greenHandVisual`, `magnetHandVisual`, `flareHandVisual` et `blacklightHandVisual`. Ils sont laissés vides par défaut : glissez-y les modèles/objets correspondants dans Unity pour afficher les mains. Les capacités et les puzzles fonctionnent sans ces références visuelles, mais les nouveaux modèles 3D, leurs animations et leurs effets spécifiques doivent encore être assignés/créés dans le prefab et les scènes. Les scripts de l'ancien projet dans `autres` ne sont pas copiés tels quels, afin d'éviter les dépendances et conflits avec le Netcode actuel.

## Vérification

Les modifications de cette branche ont été relues statiquement à partir des fichiers source. **La compilation Unity, les scènes en Play Mode et les tests multijoueurs ne sont pas encore validés** : ils doivent être exécutés dans Unity 2022.3.62f3 avec au moins deux clients.


## Nouvelle expérience : menu, boutique et ennemi IA

### Menu creepy
Le script `PoppyMenuBootstrap` installe automatiquement le menu dans la scène nommée `Lobby`.
Il construit un couloir sombre à l'exécution, avec des néons qui clignotent, une interface sombre, les commandes Host/Join, un accès à la boutique et un aperçu Huggy si le modèle de la scène est trouvé.
Le menu masque les anciens canvases de connexion et se ferme dès que le NetworkManager démarre une session.

**Références de scène :** le bootstrap recherche `ConnectionManafer`, la caméra active, un objet tagué `Player` et le modèle dont le nom est `Poppy_Playtime__Box_-_Huggy_Wuggy`. Si le personnage ou Huggy n'apparaît pas dans l'aperçu, assignez ces références dans le composant `PoppyMainMenu` après avoir créé un objet qui porte ce composant.

### Boutique de couleurs
- 8 couleurs de GrabPack/personnage.
- Couleurs gratuites et couleurs déblocables avec des crédits.
- Sauvegarde locale des crédits, achats et couleur équipée via PlayerPrefs.
- Le choix de couleur est envoyé au serveur au spawn du joueur puis répliqué aux autres joueurs.
- Une manche terminée donne 100 crédits à chaque client connecté.

Les crédits sont actuellement une économie locale de prototype, pas un portefeuille anti-triche lié au compte. Pour une économie compétitive, il faudra stocker les achats et soldes côté serveur (Cloud Save / service backend) et valider les transactions à distance.

### Mode ennemi IA
Le bouton `ENEMY: PLAYER / ENEMY: AI` mémorise le choix sur la machine de l'hôte.
En mode AI, GameManager fait apparaître `aiMonsterPrefab` et tous les joueurs humains deviennent des survivants. Une partie solo est autorisée dans ce mode si le prefab et le point d'apparition IA sont configurés. Si aucun prefab AI n'est assigné, le jeu revient au mode monstre-joueur.

**Créer le prefab Huggy :**
1. Ouvrez la scène `Lobby` dans Unity 2022.3.62f3.
2. Sélectionnez le GameObject racine du modèle Huggy dans la Hierarchy.
3. Lancez `Tools > Poppy Playtime > Create AI Network Prefab From Selected Huggy`.
4. Le wizard crée `Assets/Prefabs/Huggy AI Network.prefab` et renseigne `GameManager.aiMonsterPrefab` si un GameManager est présent dans la scène.
5. Sur le GameObject `NetworkManager`, ajoutez le prefab à `NetworkConfig > Network Prefabs`.
6. Sur la carte jouable, ajoutez/configurez un NavMeshSurface (le package AI Navigation 1.1.5 est ajouté au manifest), marquez le sol comme navigable puis faites **Bake**.
7. Vérifiez que `monsterSpawnPoint` se trouve sur le NavMesh. Le prefab utilise `NetworkObject`, `NetworkTransform`, `NavMeshAgent` et `HuggyHunterAI`.

L'IA patrouille, détecte les survivants dans son champ de vision, poursuit leur dernière position connue, recherche puis déclenche une attaque à courte portée. Le serveur est responsable de son déplacement et de la validation de l'élimination.

### Nouveautés : skins de monstres et difficulté IA
- **6 apparences Huggy** dans la boutique : Classic, Nightmare, Toxic Experiment, Frostbite, Shadow Huggy et Molten Prototype.
- Les skins de monstres utilisent les crédits de la boutique, sont sauvegardés localement et le choix de l'hôte est répliqué aux clients lorsque Huggy IA apparaît.
- **3 difficultés IA** accessibles depuis le menu : Easy (vision et vitesse réduites, attaques moins fréquentes), Normal et Nightmare (vision et vitesse augmentées, attaques plus rapprochées).
- La boutique présente maintenant les couleurs de joueur et les skins de monstres dans deux colonnes distinctes.
- Les skins sont des variantes de teinte du modèle Huggy déjà présent : pour obtenir des monstres avec des silhouettes, accessoires ou animations réellement différents, il faudra ajouter des modèles 3D et les configurer dans Unity.

### Correctifs complémentaires (revue statique)
- La détection visuelle de Huggy examine désormais les collisions sans supposer que les Raycast sont triés, et réutilise un tampon de résultats pour limiter les allocations répétées.
- Lorsqu'un survivant disparaît de la vue, Huggy conserve sa dernière position réellement observée au lieu de lire sa position à travers les murs.
- Les Gizmos de Huggy affichent la portée de vision, l'angle de détection et la distance d'attaque lorsque le prefab est sélectionné.
- Correction d'une incompatibilité de type dans le bouton de sélection du mode (le bouton fournit maintenant son composant TMP enfant).
- Ajout d'un shader de secours pour le décor du menu si le shader Standard / URP n'est pas disponible.

### Validation obligatoire
Ces fonctions ont été ajoutées au code source, mais elles n'ont pas encore été ouvertes, compilées ou testées dans Unity dans cet environnement. Il faut notamment vérifier la génération du prefab, l'enregistrement Network Prefabs, le bake NavMesh, le rendu du menu et les achats/synchronisations en Host + Client.
