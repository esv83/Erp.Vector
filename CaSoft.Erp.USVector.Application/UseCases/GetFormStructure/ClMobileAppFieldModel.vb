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

End Class
