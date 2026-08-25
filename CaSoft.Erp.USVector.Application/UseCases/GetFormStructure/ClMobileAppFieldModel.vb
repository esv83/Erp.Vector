Public Class ClMobileAppFieldModel
    Public Property Name As String
    Public Property Label As String
    Public Property Index As Integer
    ''' <summary>Type de contrôle web : text | textarea | checkbox | list | phone | email | number | date.</summary>
    Public Property Type As String
    Public Property Required As Boolean
    Public Property InstantUpdate As Boolean
    Public Property PlaceHolder As String
    ''' <summary>Champ multi-valué (saisie répétable : téléphones, e-mails).</summary>
    Public Property IsMulti As Boolean
    ''' <summary>Pour Type = 'list' : valeurs proposées (clé entière -> libellé).</summary>
    Public Property Options As Object
    Public Property Value As String

    ''' <summary>
    ''' OC-5 — Verrou <b>par champ</b> : afficher la valeur, désactiver la saisie. À ne pas confondre
    ''' avec le verrou du <i>type</i> de mission — une mission au type libre peut très bien porter une
    ''' date de naissance déjà connue, donc figée. Ajout <b>additif</b> (D14) : reste à <c>False</c>
    ''' tant que les attributs viennent du catalogue Vector, qui ne connaît pas cette notion.
    ''' </summary>
    Public Property IsReadOnly As Boolean

    ''' <summary>Motif affichable du verrou. Nothing quand le champ est ouvert.</summary>
    Public Property ReadOnlyReason As String

End Class
