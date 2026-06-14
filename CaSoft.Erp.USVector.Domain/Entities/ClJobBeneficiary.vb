Imports CaSoft.Framework

Public Class ClJobBeneficiary
    Inherits ClBusinessBase(Of ClJobBeneficiary)

    Private IdProperty = RegisterProperty(Of Guid)(Function(f) f.Id)
    Public Shadows Property Id As Guid
        Get
            Return GetProperty(IdProperty)
        End Get
        Set(value As Guid)
            SetProperty(IdProperty, value)
        End Set
    End Property

    Private nameProperty = RegisterProperty(Of String)(Function(f) f.Name)
    Public Property Name As String
        Get
            Return GetProperty(nameProperty)
        End Get
        Set(value As String)
            SetProperty(nameProperty, value)
        End Set
    End Property

    Private lastNameProperty = RegisterProperty(Of String)(Function(f) f.LastName)
    Public Property LastName As String
        Get
            Return GetProperty(lastNameProperty)
        End Get
        Set(value As String)
            SetProperty(lastNameProperty, value)
        End Set
    End Property

    Private isKnowProperty = RegisterProperty(Of Boolean)(Function(f) f.IsKnown)
    Public Property IsKnown As Boolean
        Get
            Return GetProperty(isKnowProperty)
        End Get
        Set(value As Boolean)
            SetProperty(isKnowProperty, value)
        End Set
    End Property

    Private NirProperty = RegisterProperty(Of String)(Function(f) f.NIR)
    Public Property NIR As String
        Get
            Return GetProperty(NirProperty)
        End Get
        Set(value As String)
            SetProperty(NirProperty, value)
        End Set
    End Property

    Private DdnProperty = RegisterProperty(Of DateTime?)(Function(f) f.DDN)
    Public Property DDN As DateTime?
        Get
            Return GetProperty(DdnProperty)
        End Get
        Set(value As DateTime?)
            SetProperty(DdnProperty, value)
        End Set
    End Property

    Private phonesProperty = RegisterProperty(Of List(Of String))(Function(f) f.Phones)
    Public Property Phones As List(Of String)
        Get
            Return GetProperty(phonesProperty)
        End Get
        Set(value As List(Of String))
            SetProperty(phonesProperty, value)
        End Set
    End Property

    Private emailsProperty = RegisterProperty(Of List(Of String))(Function(f) f.Emails)
    Public Property Emails As List(Of String)
        Get
            Return GetProperty(emailsProperty)
        End Get
        Set(value As List(Of String))
            SetProperty(emailsProperty, value)
        End Set
    End Property

End Class
