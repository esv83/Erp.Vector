Imports CaSoft.Framework

Public Class ClAttributValue
    Inherits ClBusinessBase(Of ClAttributValue)

    Public Sub New(intId As Integer, strAttributName As String, objValue As Object)
        LoadProperty(IdProperty, intId)
        LoadProperty(attributNameProperty, strAttributName)
        LoadProperty(valueProperty, objValue)

    End Sub

    Public Sub New(strAttributName As String, objValue As Object)
        LoadProperty(attributNameProperty, strAttributName)
        LoadProperty(valueProperty, objValue)

        MarkAsNew()
    End Sub

    Private IdProperty = MyBase.RegisterProperty(Of Integer)(Function(f) f.Id)
    Public Shadows ReadOnly Property Id As Integer
        Get
            Return GetProperty(IdProperty)
        End Get
    End Property

    Private valueProperty = MyBase.RegisterProperty(Of Object)(Function(f) f.Value)
    Public Property Value As Object
        Get
            Return GetProperty(valueProperty)
        End Get
        Set(value As Object)
            SetProperty(valueProperty, value)
        End Set
    End Property

    Private attributNameProperty = RegisterProperty(Of String)(Function(f) f.Name)
    Public Property Name As String
        Get
            Return GetProperty(attributNameProperty)
        End Get
        Set(value As String)
            SetProperty(attributNameProperty, value)
        End Set
    End Property

End Class
