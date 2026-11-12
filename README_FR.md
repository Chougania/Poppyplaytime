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

## Dossier `autres`

Le dossier `autres/GrabPack-Unity-Project-1.1-main` est conservé comme **référence de migration**. Il contient un projet Unity distinct (sa propre version Unity et ses propres dépendances). Ne déplacez pas tout son contenu directement dans `Assets` : cela peut introduire des scripts dupliqués, des conflits de classes, des shaders incompatibles et des références cassées.

Les systèmes à évaluer et intégrer progressivement sont notamment le câble physique, le lancement de main, l'interaction, le scanner de main et l'IA. Chaque intégration doit être adaptée à l'architecture réseau et aux composants déjà utilisés par ce projet. Vérifiez également les licences et conditions de redistribution des ressources avant de publier une copie.

## Vérification

Les modifications de cette branche ont été relues statiquement à partir des fichiers source. **La compilation Unity, les scènes en Play Mode et les tests multijoueurs ne sont pas encore validés** : ils doivent être exécutés dans Unity 2022.3.62f3 avec au moins deux clients.
