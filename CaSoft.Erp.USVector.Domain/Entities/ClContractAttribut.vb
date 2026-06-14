Imports CaSoft.Framework

Public Class ClContractAttribut
    Inherits ClBusinessBase(Of ClContractAttribut)

    Private idProperty = RegisterProperty(Of Integer)(Function(f) f.Id)


    Public Sub New(intId As Integer)
        SetProperty(idProperty, intId)
    End Sub
    Public Shadows ReadOnly Property Id As Integer
        Get
            Return GetProperty(idProperty)
        End Get
    End Property
    Public Property Name As String
    Public Property Label As String
    Public Property Type As String
    Public Property InstantUpdate As Boolean
    Public Property Required As Boolean
    Public Property PlaceHolder As String
    Public Property FieldType As String
    Public Property Index As Integer
    ''' <summary>Liste de choix (FieldType = 'list') : des Options sont fournies.</summary>
    Public Property IsList As Boolean
    ''' <summary>Champ multi-valué (saisie répétable : téléphones, e-mails).</summary>
    Public Property IsMulti As Boolean
    Public Property Options As IDictionary(Of Integer, String)

    Private ValueProperty = RegisterProperty(Of String)(Function(f) f.Value)
    Public Property Value As String
        Get
            Return GetProperty(ValueProperty)
        End Get
        Set(value As String)
            SetProperty(ValueProperty, value)
        End Set
    End Property


End Class
