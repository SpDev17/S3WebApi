namespace S3WebApi.GlobalLayer
{
    public static class Constants
    {
        public const string PendingMarker = "PENDING";
        public const string defaultSecurityCache = "defaultSecurityCache";
        public const string countryCache = "Cache";
        public const string securityConfigElement = "SecurityConfiguration";
        public const string webAppPrefix = "StaticConfigurations:webAppPrefix";
        public const string secuirtyConfigXml = "XMLPaths:SECURITY_CONFIG_XML";
        public const string yes = "Y";
        public const string no = "N";
        public const string restrictKey = "DynamicConfigurations:RestrictKey";
        public const string securityKey = "StaticConfigurations:Security_Attribute";
        public const string webAppUrl = "DynamicConfigurations:WEB_APP_URL";
        public const string MShareKeyWord = "StaticConfigurations:MShareKeyWord";
        public const string accountList = "StaticConfigurations:ACCOUNT_LIST_NAME";
        public const string segmentCode = "StaticConfigurations:SEGMENT_CODE";
        public const string index = "Index";
        public const string billingDocument = "BillingDocument";
        public const string certificateDocument = "CertificateDocument";
        public const string autoIDDocument = "AutoIDDocument";
        public const string docLibClaims = "Claims";
        public const string docLibPolicy = "Policies";
        public const string CONST_TRUE = "True";
        public const string docLibAccManagement = "Account Management";
        public const string docLibFiduciary = "Fiduciary";
        public const string docLibPlacement = "Placements";
        public const string docLibProject = "Projects";
        public const string docLibTransaction = "Transactions";
        public const string segmentKey = "StaticConfigurations:segment";
        public const string entityKey = "StaticConfigurations:office";
        public const string ClaimDefaultCTName = "Claim Document Set_INST";
        public const string PlacementDefaultCTName = "Placement Document Set_INST";
        public const string PolicyDefaultCTName = "Policy Document Set_INST";
        public const string ProjectDefaultCTName = "Project Document Set_INST";
        public const string TransactionDefaultCTName = "Transaction Document Set_INST";
        public const string FiduciaryDefaultCTName = "Fiduciary Document Set_INST";
        public const string AccountManagementDefaultCTNames = "Contract Document Set_INST,Account Management Document Set_INST,Correspondence Document Set_INST";
        public const string AccountManagementDSServerRelPath = "/Account Management/" + AccountManagementStaticDocset;
        public const string ContractDSServerRelPath = "/Account Management/" + ContractStaticDocset;
        public const string CorrespondenceDSServerRelPath = "/Account Management/" + CorrespondenceStaticDocset;
        public const string FiduciaryDSServerRelPath = "/Fiduciary/" + FiduciaryStaticDocset;
        public const string ClaimsConfidentialDSServerRelPath = "/Projects/" + ClaimsConfidentialStaticDocset;
        public const string PlacementsConfidentialDSServerRelPath = "/Projects/" + PlacementsConfidentialStaticDocset;
        public const string BusinessConfidentialDSServerRelPath = "/Projects/" + BusinessConfidentialStaticDocset;
        public const string OperationsConfidentialDSServerRelPath = "/Projects/" + OperationsConfidentialStaticDocset;
        public const string AccountManagementStaticDocset = "Account Management";
        public const string ContractStaticDocset = "Contract";
        public const string CorrespondenceStaticDocset = "Correspondence";
        public const string FiduciaryStaticDocset = "Fiduciary";
        public const string ClaimsConfidentialStaticDocset = "Claims Confidential";
        public const string PlacementsConfidentialStaticDocset = "Placements Confidential";
        public const string BusinessConfidentialStaticDocset = "Business Confidential";
        public const string OperationsConfidentialStaticDocset = "Operations Confidential";
        public const int StaticLibraryCount = 7;
        public const string CreateManualClaimNav = "Create Manual Claim";
        public const string CreateManualPlacementNav = "Create Manual Placement";
        public const string CreateManualPolicyNav = "Create Manual Policy";
        public const string CreateManualProjectNav = "Create Manual Project";
        public const string CreateManualTransactionsNav = "Create Manual Transactions";
        public const string SetSecurityNav = "Set Security";
        public const string SetOfficeSegmentNav = "Set Office Segment";
        public const string SetSubsiteOfficeSecurityListName = "SetSubsiteOfficeSecurity";
        public const string SetSubsiteSecurityListName = "SetSubsiteSecurity";
        public const string CreateManualTransactionFolderListName = "CreateManualTransactionFolder";
        public const string CreateManualProjectFolderListName = "CreateManualProjectFolder";
        public const string CreateManualPolicyFolderListName = "CreateManualPolicyFolder";
        public const string CreateManualPlacementFolderListName = "CreateManualPlacementFolder";
        public const string CreateManualClaimFolderListName = "CreateManualClaimFolder";
        public const string NavigationSeparator = "______________________";
        public const string CheckFolder_IsFirstFolder = "IsFirstFolder";
        public const string CheckFolder_IsFolderExist = "IsFolderExist";
        public const string CheckFolder_IsFolderBeingCreated = "IsFolderBeingCreated";
        public const string FolderRenameList = "FolderRename";
        public const string FileRenameList = "FileRename";
        public const string JobStatusPending = "Pending";
        public const string ExceptionNotificationsListName = "StaticConfigurations:EXCEPTION_NOTIFICATION_LISTNAME";
        public const string HomeSite = "DynamicConfigurations:HOME_SITE";
        public const string SecureFolderList = "SecureFolder";
        public const string GroupMemberManagmentList = "GroupMemberManagement";
        public const string UrlPrefix = "https://";
        public const string GroupNamePrefix = "DMS_D";
        public const string delimiter = ";";
        public const string ADGroupPrincipalType = "SecurityGroup";
        public const string ObjectIdSearchColumn = "StaticConfigurations:OBJECTID_SEARCH_COLUMN";
        public const string DlcDocIdSearchColumn = "StaticConfigurations:DLCDOCID_SEARCH_COLUMN";
        public const string ManagedPathSites = "DynamicConfigurations:ManagedPathSites";
        public const string DeleteAllUserIndicator = "DeleteAllUser";
        public const string SmtpServerName = "DynamicConfigurations:SmtpServerName";
        public const string SmtpServerPort = "DynamicConfigurations:SmtpServerPort";
        public const string FromEmailAddress = "DynamicConfigurations:FromEmailAddress";
        public const string GuestLinkFromEmailAddress = "GuestLinkFromEmailAddress";
        public const string GuestLinkDocumentLink = "GuestLinks:GuestLinkDocumentLink";
        public const string GuestLinkExternalAppUrlPrefix = "GuestLinks:GuestLinkExternalAppUrlPrefix";
        public const string GuestLinkListName = "GuestLinks:GuestLinkListName";
        public const string GuestLinkDoclinkSubject = "DoclinkSubject";
        public const string GuestLinkDoclinkNewSubject = "DoclinkNewSubject";
        public const string GuestLinkDoclinkBody = "DoclinkBody";
        public const string GuestLinkTermsAndConditionsText = "TermsAndConditionsText";
        public const string LegalHoldsLibraryName = "LegalHolds:LegalHoldsLibrary";
        public const string LegalHoldsFieldValuesUnderscoreWhiteList = "LegalHolds:FieldValuesUnderscoreWhiteList";
        public const string LegalHoldsFieldValuesToExclude = "LegalHolds:FieldValuesToExclude";

        public const string MoveCopyListName = "MoveCopy:MoveCopyListName";
        public const string UseMultiGeo = "DynamicConfigurations:UseMultiGeo";
        public const string MDocsCountryList = "StaticConfigurations:MDocsCountries";
        public const string CountryListWithNoContractId = "StaticConfigurations:Country_Use_PolicyNumber_For_FolderName_With_No_ContractId";
        public const string ContractIdDefaultValue = "StaticConfigurations:ContractId_Default_value";

        public const string Config_VaultAuthentication_TokenEndpoint = "IntegrationAuthentication:TokenEndpoint";
        public const string Config_VaultAuthentication_SecretsEndpoint = "IntegrationAuthentication:SecretsEndpoint";
        public const string Config_VaultAuthentication_SecretsNamespace = "IntegrationAuthentication:SecretsNamespace";

        public const string BrokasureSourceSystemCode = "Brokasure:SourceSystemCode";

        public const string GuestLinkAuditlistName = "GuestLinks:AuditlistName";

        public const string ScanDocumentMetadata = "ScanDocumentMetadata";
        public const string BlackBoxDocumentMetadata = "BlackBoxDocumentMetadata";
        public const string ClaimDocumentMetadata = "ClaimDocumentMetadata";
        public const string PolicyDocumentMetadata = "PolicyDocumentMetadata";
        public const string PlacementDocumentMetadata = "PlacementDocumentMetadata";
        public const string FiduciaryDocumentMetadata = "FiduciaryDocumentMetadata";
        public const string CertificateDocumentMetadata = "CertificateDocumentMetadata";
        public const string BillingDocumentMetadata = "BillingDocumentMetadata";
        public const string AutoIDDocumentMetadata = "AutoIDDocumentMetadata";
        public const string InboxDocumentMetadata = "InboxDocumentMetadata";
        public const string SystemCdUseKeywordForPolicyFolder = "StaticConfigurations:SystemCdUseKeywordForPolicyFolder";
        public const string DepartmentLevelSecurityCountryList = "StaticConfigurations:DepartmentLevelSecurityCountryList";
        public const string HiddenTransactionsFolderSecurityGroupRuleCountryList = "StaticConfigurations:HiddenTransactionsFolderSecurityGroupRuleCountryList";
        public const string ViewAliasDocumentUrl = "StaticConfigurations:ViewAliasDocumentUrl";

        public const string EsignObjectIdSearchColumn = "EsignResponseMetadata:ESignObjectIdSearchColumn";
        public const string ProjectDocumentMetadata = "ProjectDocumentMetadata";

        public const string SoftDeleteEmailTemplate = @"<html>
                        <head></head>
                        <body style='font-family: Calibri, Arial, sans-serif; font-size: 12pt; width:100%; word-wrap: break-word;'>
                            <p>{Salutation} {Name},</p>
                            <p>{EmailBody}</p>
                            <ul><li><strong>{FolderTitle}</strong> {FolderName}</li></ul>
                            <p><strong>{FolderDetails}</strong></p>
                                <ul>
                                    <li><strong>{ClientTitle}</strong> {ClientName}</li>
                                    <li><strong>{LibraryTitle}</strong> {LibraryName}</li>
                                </ul>
                            <p><strong>{EmailNote} </strong>{Note} <a href='mailto:{SupportEmail}'>{SupportName}</a></p>
                            <hr>
                            <p>{TermsAndCondition}</p><br>
                        </body>
                    </html>";

        public const string SoftDeleteKeyword = "SoftDelete";
        public const string SoftDeleteFromEmailAddress = "FolderDeleteFromEmailAddress";
        public const string SoftDeleteFolderSubject = "FolderDeleteSubject";
        public const string SoftDeleteBody = "FolderDeleteBody";
        public const string SoftDeleteFolderName = "FolderName";
        public const string SoftDeleteFolderDetails = "FolderDetails";
        public const string SoftDeleteFolderClientName = "ClientName";
        public const string SoftDeleteFolderLibraryName = "LibraryName";
        public const string SoftDeleteFolderNote = "FolderNote";
        public const string SoftDeleteTermsAndConditionsText = "FolderDeleteTermsAndConditionsText";
        public const string SoftDeleteSupportName = "SoftDelete:SupportName";
        public const string SoftDeleteSupportEmail = "SoftDelete:SupportEmail";
        public const string EmailSalutation = "Dear";
        public const string EmailNote = "Note:";

        public class GenerateLinks
        {
            public const string GenerateLinksList = "GenerateLinkEvents";
            public const string GenerateLinksInboxIndexingList = "InboxIndexing";
            public const string GenerateLinksInboxList = "Inbox";
            public const string AccountNewLink = "DMS/Document/Account/NewLink";
            public const string ClaimNewLink = "DMS/Document/Claim/NewLink";
            public const string PolicyNewLink = "DMS/Document/Policy/NewLink";
            public const string PlacementNewLink = "DMS/Document/Placement/NewLink";
            public const string FiduciaryNewLink = "DMS/Document/Fiduciary/NewLink";
            public const string BillingNewLink = "DMS/Document/Billing/NewLink";
            public const string CertificateNewLink = "DMS/Document/Certificate/NewLink";
            public const string ApiIdentifier = "document/newLink";

        }

        public class PublishNewDocument
        {

            public const string AccountNewDocument = "DMS/Document/Account/NewDocument";
            public const string ClaimNewDocument = "DMS/Document/Claim/NewDocument";
            public const string PolicyNewDocument = "DMS/Document/Policy/NewDocument";
            public const string PlacementNewDocument = "DMS/Document/Placement/NewDocument";
            public const string FiduciaryNewDocument = "DMS/Document/Fiduciary/NewDocument";
            public const string BillingNewDocument = "DMS/Document/Billing/NewDocument";
            public const string CertificateNewDocument = "DMS/Document/Certificate/NewDocument";
            public const string ApiIdentifier = "document/newDocument";

        }

        public class PublishNewVersion
        {

            public const string AccountNewDocumentVersion = "DMS/Document/Account/NewDocumentVersion";
            public const string ClaimNewDocumentVersion = "DMS/Document/Claim/NewDocumentVersion";
            public const string PolicyNewDocumentVersion = "DMS/Document/Policy/NewDocumentVersion";
            public const string PlacementNewDocumentVersion = "DMS/Document/Placement/NewDocumentVersion";
            public const string FiduciaryNewDocumentVersion = "DMS/Document/Fiduciary/NewDocumentVersion";
            public const string BillingNewDocumentVersion = "DMS/Document/Billing/NewDocumentVersion";
            public const string CertificateNewDocumentVersion = "DMS/Document/Certificate/NewDocumentVersion";
            public const string ApiIdentifier = "document/newDocumentVersion";

        }

        public class PublishEmailGenerateLinks
        {
            public const string GenerateLinksList = "GenerateEmailLinkEvents";
            public const string AccountNewLink = "DMS/Email/Account/NewLink";
            public const string ClaimNewLink = "DMS/Email/Claim/NewLink";
            public const string PolicyNewLink = "DMS/Email/Policy/NewLink";
            public const string PlacementNewLink = "DMS/Email/Placement/NewLink";
            public const string FiduciaryNewLink = "DMS/Email/Fiduciary/NewLink";
            public const string BillingNewLink = "DMS/Email/Billing/NewLink";
            public const string CertificateNewLink = "DMS/Email/Certificate/NewLink";
            public const string ApiIdentifier = "email/newLink";
            public const string InboxNewLink = "DMS/Document/Inbox/NewDocument";
        }

        public class PublishEmailNewDocument
        {

            public const string AccountNewDocument = "DMS/Email/Account/NewDocument";
            public const string ClaimNewDocument = "DMS/Email/Claim/NewDocument";
            public const string InboxNewDocument = "DMS/Email/Inbox/NewDocument";
            public const string PolicyNewDocument = "DMS/Email/Policy/NewDocument";
            public const string PlacementNewDocument = "DMS/Email/Placement/NewDocument";
            public const string FiduciaryNewDocument = "DMS/Email/Fiduciary/NewDocument";
            public const string BillingNewDocument = "DMS/Email/Billing/NewDocument";
            public const string CertificateNewDocument = "DMS/Email/Certificate/NewDocument";
            public const string ApiIdentifier = "email/newDocument";
        }


        public class PublishEmailNewVersion
        {

            public const string AccountNewDocumentVersion = "DMS/Email/Account/NewDocumentVersion";
            public const string ClaimNewDocumentVersion = "DMS/Email/Claim/NewDocumentVersion";
            public const string PolicyNewDocumentVersion = "DMS/Email/Policy/NewDocumentVersion";
            public const string PlacementNewDocumentVersion = "DMS/Email/Placement/NewDocumentVersion";
            public const string FiduciaryNewDocumentVersion = "DMS/Email/Fiduciary/NewDocumentVersion";
            public const string BillingNewDocumentVersion = "DMS/Email/Billing/NewDocumentVersion";
            public const string CertificateNewDocumentVersion = "DMS/Email/Certificate/NewDocumentVersion";
            public const string ApiIdentifier = "email/newDocumentVersion";
        }

        public class SecureEndpointByADGroup
        {
            public const string InternalSvcAccounts = "HeaderValidation:InternalSvcAccounts";
            public const string HC_KeyNme = "HeaderValidation:HC_KeyNme";
            public const string ValidateLifetime = "HeaderValidation:ValidateLifetime";
            public const string ValidateAudience = "HeaderValidation:ValidateAudience";
            public const string ValidateIssuer = "HeaderValidation:ValidateIssuer";
            public const string ValidateByADGroup = "HeaderValidation:ValidateByADGroup";
            public const string AllowedADGroup = "HeaderValidation:AllowedADGroup";
            public const string ADLookUpDomain = "HeaderValidation:ADLookUpDomain";
            public const string LDAPSearchBase = "HeaderValidation:LDAPSearchBase";
            public const string MemberOfAttribute = "HeaderValidation:MemberOfAttribute";
            public const string DisplayNameAttribute = "HeaderValidation:DisplayNameAttribute";
            public const string SAMAccountNameAttribute = "HeaderValidation:SAMAccountNameAttribute";
            public const string LDAPSearchFilter = "HeaderValidation:LDAPSearchFilter";
            public const string LDAPRegexFilter = "HeaderValidation:LDAPRegexFilter";
            public const string UtilitiesCredentialsCacheKey = "HeaderValidation:UtilitiesCredentialsCacheKey";
        }

        public class MoveCopy
        {
            public const string MoveCopystatusPending = "Pending";
            public const string ActionStatusMove = "Move";
            public const string ActionStatusCopy = "Copy";

            public const string ItemTypeFile = "Document";
            public const string ItemTypeFolder = "Folder";
        }

        public class Scanning
        {
            public const string KofaxUrl = "Kofax:RescanVoucherUrl";
            public const string ScanStatusRequested = "Rescan Requested";
            public const string ScanStatusProcessed = "Rescan Processed";
            public const string ScanStatusBatchSelected = "MShare Rescan";
        }
    }

    public static class ConfigurationPaths
    {
        public const string HeaderValidation_InternalSvcAccounts = "HeaderValidation:InternalSvcAccounts";
        public const string HeaderValidation_HC_KeyNme = "HeaderValidation:HC_KeyNme";
        public const string HeaderValidation_Issuer = "HeaderValidation:Issuer";
        public const string HeaderValidation_Audience = "HeaderValidation:Audience";
        public const string HeaderValidation_ExpiresIn = "HeaderValidation:ExpiresIn";
        public const string HeaderValidation_ValidateLifetime = "HeaderValidation:ValidateLifetime";
        public const string HeaderValidation_ValidateAudience = "HeaderValidation:ValidateAudience";
        public const string HeaderValidation_ValidateIssuer = "HeaderValidation:ValidateIssuer";
        public const string Env_Default = "Env:Default";

        public const string FileContent_Truncated = "Truncated";

        public const string DynamicConfigurations_SpringAccountDomain = "DynamicConfigurations:SpringAccountDomain";
        public const string DynamicConfigurations_ldapHost = "DynamicConfigurations:ldapHost";
        public const string DynamicConfigurations_EnableSecurity = "DynamicConfigurations:EnableSecurity";
        public const string DynamicConfigurations_WEB_APP_URL = "DynamicConfigurations:WEB_APP_URL";
        public const string DynamicConfigurations_SpringMultiMessageKeyword = "DynamicConfigurations:SpringMultiMessageKeyword";

        public const string XMLPaths_SECURITY_MAPPING_XML = "XMLPaths:SECURITY_MAPPING_XML";

        public const string HeathCheckProperties_HashicorpUrl = "HeathCheckProperties:HashicorpUrl";
        public const string HeathCheckProperties_HashicorpRespCode = "HeathCheckProperties:HashicorpRespCode";

        public const string IntegrationAuthentication_TokenEndpoint = "IntegrationAuthentication:TokenEndpoint";
        public const string IntegrationAuthentication_CertificateEndpoint = "IntegrationAuthentication:CertificateEndpoint";
        public const string IntegrationAuthentication_SecretEndpoint = "IntegrationAuthentication:SecretsEndpoint";
        public const string IntegrationAuthentication_SecretsNamespace = "IntegrationAuthentication:SecretsNamespace";

        public const string StaticConfigurations_ServiceStartupTimeOut = "StaticConfigurations:ServiceStartupTimeOut";
        public const string StaticConfigurations_confidentialProjectFolderCountry = "StaticConfigurations:confidentialProjectFolderCountry";
        public const string StaticConfigurations_country_level_security = "StaticConfigurations:country_level_security";
        public const string StaticConfigurations_Hub_Client_Level_Security = "StaticConfigurations:Hub_Client_Level_Security";
        public const string StaticConfigurations_set_office_segment_link_country = "StaticConfigurations:set_office_segment_link_country";
        public const string StaticConfigurations_SleepForFolderCheck = "StaticConfigurations:SleepForFolderCheck";
        public const string StaticConfigurations_FolderCacheExpiration = "StaticConfigurations:FolderCacheExpiration";
        public const string AppPrincipal_CertSelector = "AppPrincipal:CertSelector";
    }
}
