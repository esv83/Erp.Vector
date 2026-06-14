' Port BD Mobile : sessions / tokens équipage (table MOB_SESSION).
' Remplace le EQ_TOKEN porté par T_EQUIPAGE_EQ dans le legacy — la session vit
' désormais côté BD Mobile, l'équipage côté ERP (CRW_CREW, référencé par Guid).
' Depuis MOB-4a, l'authentification mobile passe par Keycloak (JWT) et l'identité
' est résolue par IMobileIdentityResolver — ce port reste pour la fin de service (MOB-12).
Public Interface ISessionRepository

    ''' <summary>Retourne le token de la session active de l'équipage, en la créant si besoin.</summary>
    Function GetOrCreateToken(crewId As Guid) As Guid

    ''' <summary>Résout l'équipage (Guid ERP) depuis un token de session active. Nothing si token inconnu ou session close.</summary>
    Function GetCrewIdByToken(token As Guid) As Guid?

    ''' <summary>Clôture la session active de l'équipage (fin de service). Sans effet si aucune session active.</summary>
    Sub CloseSession(crewId As Guid)

End Interface
