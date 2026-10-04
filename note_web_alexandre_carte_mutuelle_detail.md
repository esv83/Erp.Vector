# 💳 Note UI Web Vector — La carte mutuelle annoncée dans le détail de la mission (pour Alexandre)

> **Date** : 2026-10-04 · **Pour** : Alexandre, dev web de l'UI Vector.
> **Objet** : dire à l'équipage, à l'ouverture de la mission, que la carte du patient est **déjà
> connue** et de quand date la dernière photo — pour qu'il ne la reprenne que si elle a changé.
> **Champs ajoutés** au détail : rien de ce qui existe ne change. **JSON** : PascalCase.

Salut Alexandre 👋

Aujourd'hui, l'app ne sait qu'une carte existe qu'en appelant `GET api/missions/{jobId}/mutuelle-card`
et en recevant un `404` quand il n'y en a pas — **5 390 fois en deux semaines**. Le détail de la mission
porte désormais l'information directement.

## Ce que `GET api/JobDetail/{jobId}` ajoute

| Champ | Type | Sens |
|---|---|---|
| `MutuelleCardKnown` | bool | une carte est déjà connue pour ce patient |
| `MutuelleCardCapturedAt` | date ou `null` | date de la dernière photo → « carte connue, photo du 20/09 » |
| `MutuelleCardFieldsInheritedFrom` | date ou `null` | les champs (mutuelle, AMC…) ont été **repris** d'une photo plus ancienne, pas encore revérifiés → « repris de la photo du 02/06 » |
| `MutuelleCardId` | Guid ou `null` | la carte courante — pour la saisie `PATCH api/mutuelle-card/{Id}` |
| `MutuelleCardImageUrl` | texte ou `null` | la photo, à la demande (relative) |

## Ce que l'écran en fait

- **Carte connue** : « Carte mutuelle connue — photo du 20/09 », un bouton **« Voir la carte »**, et le
  bouton photo reste disponible (la carte a pu changer).
- **Pas de carte** (`MutuelleCardKnown = false`) : le bouton photo, comme aujourd'hui.
- **« Voir la carte »** : l'image se charge **avec le jeton** — `fetch` puis `URL.createObjectURL` ; une
  balise `<img src>` nue reçoit `401`.

## Une nouvelle photo ne fait plus perdre la saisie

Quand l'équipage reprend la photo d'une carte déjà connue, la nouvelle carte **reprend les champs
validés** de la précédente (au lieu de repartir vide), avec `FieldsInheritedFrom`. S'il les corrige ou
les confirme par le `PATCH`, la mention disparaît.

## La saisie des quatre champs refuse désormais l'enregistrement vide

`PATCH api/mutuelle-card/{Id}` remplace toujours les quatre champs (rien ne change si tu les envoies
tous). Deux cas, qui passaient, répondent maintenant **`400` avec une phrase à afficher** :
- **les quatre champs vides** — « Saisie vide : renseignez au moins un des quatre champs. Rien n'a été
  enregistré. » (avant, la carte était effacée et marquée validée) ;
- **un champ trop long** — mutuelle 200, code AMC 50, concentrateur 100, télétransmission 50
  caractères au plus (avant, un `500`).

## ✅ Récap

- [ ] Afficher « carte connue, photo du … » depuis le détail, sans attendre le `404` de la route carte
- [ ] « Voir la carte » : `fetch` avec le jeton + blob
- [ ] Afficher « repris de la photo du … » quand `MutuelleCardFieldsInheritedFrom` est renseigné
- [ ] Saisie des champs : afficher le motif d'un `400` ; borner les zones de saisie aux longueurs ci-dessus
