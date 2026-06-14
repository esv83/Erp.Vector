Imports System.Text
Imports CaSoft.Erp.USVector.Application

Public Class ClGeolocTrameSirus
    Inherits ClTrameBase

    Const SystemName As String = "URGENSANTE"  'A limiter a 10 caracteres


    Public Sub New(ObjUser As ClLocationModel)
        MyBase.New(78)

        initTrameId()

        TrameState = TrameState.Pending

        SetTrameData(ObjUser)

    End Sub
    Private Sub initTrameId()

        'initialise l'ID
        Dim rand As New Random()
        Dim NewIntegerId = rand.Next()
        Dim NewByteId As Byte() = BitConverter.GetBytes(NewIntegerId)

        NewByteId.CopyTo(_trameData, 0)

    End Sub


#Region "propertys"
    Public Property Version As Integer = 1
    Public Property TrameState As TrameState

    Private _TransmitDate As DateTime?
    Public Property TransmitDate As DateTime?
        Get
            Return _TransmitDate
        End Get
        Set(value As DateTime?)
            _TransmitDate = value
            If TransmitDate.HasValue Then
                TrameState = TrameState.Transmitted
            End If
        End Set
    End Property
    Public Property Acknoledge As Boolean = False
    Public ReadOnly Property ContentAsByte As Byte()
        Get
            Return _trameData
        End Get

    End Property
    Public ReadOnly Property ContentAsString As String
        Get
            Return TrameToString()  'Text.Encoding.UTF8.GetString(_trame) ''
        End Get

    End Property
#End Region
    Private Sub SetTrameData(objUser As ClLocationModel)

        SetTrameField(Id, 0, 4)
        SetTrameField(objUser.IMEI, 4, 20)
        SetTrameField(Math.Floor(objUser.Latitude * 1000000), 24, 4)
        SetTrameField(Math.Floor(objUser.Longitude * 1000000), 28, 4)
        SetTrameField(objUser.State, 32, 1)  'etat
        SetTrameField(objUser.Odometer, 33, 3)   'Km
        Dim strdate = objUser.LocationUtcDate.Value.ToString("yyyyMMddhhmmss")
        SetTrameField(strdate, 36, 14)
        SetTrameField(DateTime.Now.ToString("yyyyMMddhhmmss"), 50, 14)
        SetTrameField(Math.Floor(objUser.Speed), 64, 1)
        SetTrameField(Math.Floor(objUser.Heading), 65, 2)
        SetTrameField(Version.ToString, 67, 2)
        SetTrameField(SystemName, 69, 10)

    End Sub
    Private Sub SetTrameField(objData As Object, intPosition As Integer, intLength As Integer)

        Dim dataByteArray(intLength - 1) As Byte

        If objData Is Nothing Then
            Throw New ArgumentNullException(NameOf(objData), "Le paramètre objData ne peut pas être null.")
        End If

        If objData.GetType() = GetType(String) Then
            Dim adjustedString As String = AdjustStringToLength(CStr(objData), intLength)
            dataByteArray = System.Text.Encoding.UTF8.GetBytes(adjustedString)
        ElseIf TypeOf objData Is Integer OrElse TypeOf objData Is Double OrElse TypeOf objData Is Boolean OrElse TypeOf objData Is Decimal Then
            dataByteArray = BitConverter.GetBytes(CDbl(objData))
        Else
            Dim t = objData.GetType
            Throw New ArgumentException("Type non pris en charge pour objData.")
        End If

        ' Vérification de la taille de _trame
        If _trameData Is Nothing OrElse _trameData.Length < intPosition + dataByteArray.Length Then
            Throw New ArgumentOutOfRangeException("Le tableau _trame est trop petit pour contenir les données.")
        End If

        dataByteArray.CopyTo(_trameData, intPosition)

    End Sub
    Private Function AdjustStringToLength(dataString As String, Length As Integer) As String
        Dim result As String


        'reduit la taille de la chaine si necessaire
        If dataString.Count > Length Then
            result = dataString.Substring(0, Length)

            'Dim errorString = String.Format("la chaine {0} devrait se composer de {1} caracteres et elle en comprend {2}", data, Length.ToString, dataBytesCount.ToString)
            'NLog.LogManager.GetCurrentClassLogger.Error(errorString)
            'Debug.WriteLine(errorString)
        ElseIf dataString.Count < Length Then
            Dim NbWhiteSpace = Length - dataString.Count
            Dim sb As New StringBuilder(dataString)
            For i = 1 To NbWhiteSpace
                sb.Append(" ")
            Next

            result = sb.ToString
        Else
            result = dataString
        End If


        Return result

    End Function
    Public Function TrameToString() As String

        ' Encoding.RegisterProvider(CodePagesEncodingProvider.Instance)

        Dim sb As New StringBuilder
        '   Dim TrameArray = TrameToCharArray()
        For Each b In _trameData
            Dim myChar = Convert.ToChar(b)
            sb.Append(myChar)
        Next

        Dim result As String = sb.ToString

        Return result

    End Function






End Class
