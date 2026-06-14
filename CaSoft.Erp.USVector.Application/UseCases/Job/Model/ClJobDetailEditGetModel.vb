Public Class ClJobDetailEditGetModel
    'Private _entity As Dev_Mob_JobDetailEdit
    Private _phones As List(Of String)
    Private _mails As List(Of String)
    ' Private _selectedContractType As ClContractTypeSelectedModel
    Private _contractTypes As List(Of ClContractTypeGetModel)

    Public Sub New()
        _phones = New List(Of String)()
        _mails = New List(Of String)()
        _contractTypes = New List(Of ClContractTypeGetModel)()
    End Sub

    'Public Sub New(ByVal Entity As Dev_Mob_JobDetailEdit)
    '    _entity = Entity
    '    _phones = New List(Of String)()
    '    AddStringToList(_phones, _entity.CONT_TEL1)
    '    AddStringToList(_phones, _entity.CONT_TEL2)
    '    AddStringToList(_phones, _entity.CONT_TEL3)
    '    _mails = New List(Of String)()
    '    AddStringToList(_mails, _entity.CONT_MAIL)
    '    Dim ContractId As Integer

    '    If Integer.TryParse(_entity.CDE_MOTIF, ContractId) Then
    '        _selectedContractType = New ClContractTypeSelectedModel() With {
    '            .Id = Integer.Parse(_entity.CDE_MOTIF),
    '            .HasSelectedValue = False,
    '            .SelectedValue = _entity.CDE_VALUE
    '        }
    '    Else
    '        _selectedContractType = Nothing
    '    End If

    '    _contractTypes = New List(Of ClContractTypeGetModel)()
    '    _contractTypes.Add(New ClContractTypeGetModel(1, "PSL", False, False, Nothing, Nothing))
    '    _contractTypes.Add(New ClContractTypeGetModel(2, "Autre", False, False, Nothing, Nothing))
    '    _contractTypes.Add(New ClContractTypeGetModel(3, "CPAM", True, False, Nothing, Nothing))
    '    _contractTypes.Add(New ClContractTypeGetModel(4, "Centre 15", False, True, "N° Centaure", Nothing))
    '    _contractTypes.Add(New ClContractTypeGetModel(5, "Art. 80", True, False, Nothing, Nothing))
    '    _contractTypes.Add(New ClContractTypeGetModel(6, "SMUR", False, True, "Smur de", New List(Of String) From {
    '        "Briançon",
    '        "Gap",
    '        "La Ciotat",
    '        "Toulon",
    '        "Hyères",
    '        "Gassin",
    '        "Brignoles",
    '        "Darguignan",
    '        "Frejus",
    '        "Cannes",
    '        "Autre"
    '    }))
    '    _contractTypes.Add(New ClContractTypeGetModel(7, "Assistance", False, True, "N° Dossier", New List(Of String) From {
    '        "IMA",
    '        "AXA",
    '        "Europe",
    '        "Mondiale",
    '        "Fidelia",
    '        "Mutuaide",
    '        "ACTA",
    '        "CNAS",
    '        "Autre"
    '    }))
    '    _contractTypes.Add(New ClContractTypeGetModel(8, "Présence Verte", False, False, Nothing, Nothing))
    '    _contractTypes.Add(New ClContractTypeGetModel(9, "Particulier", False, False, Nothing, Nothing))
    'End Sub

    Public Property JobID As Guid
    Public Property NIR As String
    Public Property DDN As DateTime?
    Public Property Comments As String
    Public Property IsSign As Boolean

    Public ReadOnly Property Phones As List(Of String)
        Get
            Return _phones
        End Get
    End Property

    Public ReadOnly Property Emails As List(Of String)
        Get
            Return _mails
        End Get
    End Property

    Public Property SelectedContractType As ClContractTypeSelectedModel

    Public ReadOnly Property ContractTypes As List(Of ClContractTypeGetModel)
        Get
            Return _contractTypes
        End Get
    End Property

    Public Property IsPmtPresent As Boolean
    Public Property Reference As String

    Public Sub AddStringToList(ByVal MyList As List(Of String), ByVal MyString As String)
        If Not String.IsNullOrWhiteSpace(MyString) Then
            MyList.Add(MyString)
        End If
    End Sub
End Class