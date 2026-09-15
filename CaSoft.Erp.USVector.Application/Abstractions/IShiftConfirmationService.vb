Imports System.Threading

Namespace Port

    ''' <summary>
    ''' Confirmation de prise de service par l'ambulancier, <b>depuis l'application</b> — le pendant
    ''' authentifié du lien reçu par courriel. La source est Order, et elle seule.
    ''' <para>
    ''' L'identité n'est jamais fournie par le mobile : le contrôleur la résout du jeton Keycloak et la
    ''' passe ici. Order vérifie que la demande est bien celle de ce personnel.
    ''' </para>
    ''' </summary>
    Public Interface IShiftConfirmationService

        ''' <summary>
        ''' Ce qui attend la confirmation de ce personnel, <b>tous équipages confondus</b>. Ne part pas
        ''' d'un équipage : une demande arrive souvent la veille, avant que l'équipage soit accessible.
        ''' </summary>
        Function GetMineAsync(personnelId As Guid, ct As CancellationToken) As Task(Of ClMyShiftConfirmationsDtoOut)

        ''' <summary>Confirme la demande au nom de ce personnel (origine « Portail » côté Order).</summary>
        Function ConfirmAsync(crewId As Guid, requestId As Guid, personnelId As Guid,
                              ct As CancellationToken) As Task(Of ClShiftConfirmationConfirmResult)

    End Interface

End Namespace
