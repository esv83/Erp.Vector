Imports System.Text

Public Class ClJobDetailModel

    Public Sub New()
        Beneficiary = New ClPatientDto
        PickupLocation = New ClJobLocationDto
        DropoffLocation = New ClJobLocationDto
        PickupDisplay = New ClLocationDisplayDto
        DropoffDisplay = New ClLocationDisplayDto
    End Sub

    ' ── Champs historiques (compat — seront retirés une fois l'UI basculée) ──────────
    Public Property TransportMode As String = String.Empty
    Public Property IsSerial As Boolean
    Public Property TransportSens As String = String.Empty
    Public Property Schedule As String = String.Empty
    Public Property Appointment As String = String.Empty
    Public Property Departure As String = String.Empty
    Public Property Arrival As String = String.Empty
    Public Property Comments As String = String.Empty
    Public Property IsLastDay As Boolean
    ''' <summary>MOB-8 — Présence d'une signature patient (reflète MI_SIGNATURE_EXISTS).</summary>
    Public Property IsSign As Boolean
    Public Property Beneficiary As ClPatientDto

    ' ── Nouveaux champs (règles côté serveur, UI = affichage seul) ───────────────────
    ''' <summary>Prise en charge formatée : « à HH:mm » le jour même, sinon « dddd dd/MM/yyyy à HH:mm ».</summary>
    Public Property ScheduleLabel As String = String.Empty
    ''' <summary>Mode de transport : sous-catégorie (secondaire) si présente, sinon mode principal.</summary>
    Public Property TransportModeLabel As String = String.Empty
    ''' <summary>Lieu de prise en charge, détaillé (l'UI affiche les champs non vides).</summary>
    Public Property PickupLocation As ClJobLocationDto
    ''' <summary>Lieu de dépose, détaillé (l'UI affiche les champs non vides).</summary>
    Public Property DropoffLocation As ClJobLocationDto

    ''' <summary>DET-2 — prise en charge : affichage piloté serveur (sections de lignes + coords). L'UI rend tel quel.</summary>
    Public Property PickupDisplay As ClLocationDisplayDto
    ''' <summary>DET-2 — dépose : affichage piloté serveur (sections de lignes + coords). L'UI rend tel quel.</summary>
    Public Property DropoffDisplay As ClLocationDisplayDto

    Public Class ClPatientDto

        ''' <summary>
        ''' Identifiant du bénéficiaire côté ERP. <b>Nothing</b> quand la mission n'en résout aucun.
        ''' </summary>
        ''' <remarks>
        ''' <para>
        ''' Ajout additif (D14). Il manquait, et ce manque bloquait un écran entier : la carte
        ''' mutuelle s'attache au <b>patient</b> (décision M4 — elle le suit d'un transport à
        ''' l'autre), donc ses routes sont indexées par bénéficiaire, alors que l'écran qui la
        ''' capture est celui d'une <b>mission</b>. Sans cet identifiant, le front ne pouvait pas
        ''' construire l'URL de <c>POST /api/beneficiaries/{id}/mutuelle-card</c> — ce qui explique
        ''' très probablement que <c>MOB_MUTUELLE_CARD</c> soit restée vide depuis juin, là où l'on
        ''' concluait à un défaut d'adoption.
        ''' </para>
        ''' <para>
        ''' <b>Nullable, et pas <c>Guid.Empty</c>.</b> Une mission dont le bénéficiaire ne se résout
        ''' pas doit faire <b>disparaître</b> le bouton de capture, pas produire une carte orpheline
        ''' rattachée à un identifiant nul que plus rien ne saurait relier à un patient.
        ''' </para>
        ''' </remarks>
        Public Property BeneficiaryId As Guid?

        Public Property CompleteName As String
        Public Property DDN As String
        Public Property Age As String
        Public Property Phones As List(Of String)

    End Class

    ''' <summary>Lieu détaillé multi-lignes. Chaque champ peut être vide → l'UI ne l'affiche pas.</summary>
    Public Class ClJobLocationDto
        Public Property Nom As String = String.Empty
        ''' <summary>Service médical (ex. « Cardiologie »), à afficher après Nom. Vide hors établissement de santé / FreeText.</summary>
        Public Property Service As String = String.Empty
        Public Property Adresse As String = String.Empty
        Public Property Residence As String = String.Empty
        Public Property BatEtage As String = String.Empty
        Public Property Commune As String = String.Empty
        Public Property Complement As String = String.Empty
        ''' <summary>Lignes prêtes à afficher (ordre + vides déjà filtrés), identiques quel que soit le type de
        ''' lieu. <b>Le plus simple pour l'UI</b> : rendre ces lignes une par une (plus de cas « une seule ligne »).</summary>
        Public Property DisplayLines As List(Of String) = New List(Of String)

        ''' <summary>
        ''' Coordonnées du lieu — sous-objet à part, hors des champs-lignes : l'UI affiche
        ''' les champs texte non vides, et consomme celui-ci séparément (carto).
        ''' Nothing si l'ERP n'a pas géocodé le lieu.
        ''' </summary>
        Public Property Coordinates As ClJobCoordinatesDto
    End Class

    ''' <summary>Coordonnées WGS84 d'un lieu.</summary>
    Public Class ClJobCoordinatesDto
        Public Property Latitude As Double
        Public Property Longitude As Double
    End Class

End Class

