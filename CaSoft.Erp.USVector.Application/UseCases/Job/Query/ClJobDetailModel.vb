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
        Public Property CompleteName As String
        Public Property DDN As String
        Public Property Age As String
        Public Property Phones As List(Of String)

    End Class

    ''' <summary>Lieu détaillé multi-lignes. Chaque champ peut être vide → l'UI ne l'affiche pas.</summary>
    Public Class ClJobLocationDto
        Public Property Nom As String = String.Empty
        ''' <summary>Service médical (ex. « Cardiologie »). Vide pour une adresse bénéficiaire.</summary>
        Public Property Service As String = String.Empty
        Public Property Adresse As String = String.Empty
        Public Property Residence As String = String.Empty
        Public Property BatEtage As String = String.Empty
        Public Property Commune As String = String.Empty
        Public Property Complement As String = String.Empty

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

