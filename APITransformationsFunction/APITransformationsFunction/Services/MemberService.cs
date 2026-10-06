using APITransformationsFunction.Common;
using APITransformationsFunction.Mappers;
using APITransformationsFunction.Model.Member;
using APITransformationsFunction.Model.Shared;
using APITransformationsFunction.Services.Interfaces;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace APITransformationsFunction.Services
{
    public partial class MemberService(HttpClient httpClient, ILogger<MemberService> logger) : IMemberService
    {
        private readonly HttpClient _httpClient = httpClient;
        private readonly ILogger<MemberService> _logger = logger;
        private static readonly JsonSerializerOptions JsonIndentedOptions = new() { WriteIndented = true };
        public async Task<CommonResponseDto<OTAGetMemberResponse>> GetMemberByIdAsync(string memberId)
        {
            var result = new CommonResponseDto<OTAGetMemberResponse>();

            try
            {
                _logger.LogInformation("[Service][MemberService] [START] GetMemberByIdAsync for {MemberId}", memberId);
                var backendResponse = await _httpClient.GetAsync($"{ApiConstants.Version}/{ApiConstants.MemberEndpoint}/{memberId}");
                var backendContent = await backendResponse.Content.ReadAsStringAsync();
                _logger.LogInformation("[Service][MemberService] [Backend][RESPONSE] Payload for {MemberId}:\n{Payload}", memberId, backendContent);
                if (!backendResponse.IsSuccessStatusCode)
                {
                    result.Error = MemberMapper.MapBackendError(backendResponse: backendResponse);
                    result.Success = false;
                    _logger.LogError("[Service][MemberService] [Backend][ERROR] Backend returned error for {MemberId}. Status: {StatusCode}",
                     memberId, backendResponse.StatusCode);
                    return result;
                }

                var sourceResponse = JsonSerializer.Deserialize<OnexGetMemberResponse>(backendContent, JsonOptionsProvider.Options);
                if (sourceResponse?.Data == null || (sourceResponse.Data.Errors?.Count ?? 0) > 0)
                {
                    var error = sourceResponse?.Data?.Errors?.FirstOrDefault();
                    result.Error = MemberMapper.MapBackendError(error);
                    result.Success = false;
                    _logger.LogError("[Service][MemberService] [Backend][ERROR] Backend returned error in body for {MemberId}. ErrorCode: {ErrorCode}, Message: {ErrorMessage}",
                    memberId, error?.ErrorCode, error?.ErrorMessage);
                    return result;
                }

                result.Data = MemberMapper.MapToOTAResponse(sourceResponse);
                result.Success = true;
                _logger.LogInformation("[Service][MemberService] [END]Returning details for {MemberId}", memberId);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Service][MemberService] [EXCEPTION] Unexpected error fetching member data for {MemberId}", memberId);

                result.Error = MemberMapper.MapBackendError(defaultStatusCode: HttpStatusCode.InternalServerError, validationMessages: [ex.Message]);
                result.Success = false;
                return result;
            }
        }
        public async Task<CommonResponseDto<OTAPostMemberResponse>> CreateMemberAsync(OTAPostMemberRequest otaRequest)
        {
            var correlationId = otaRequest.CorrelationID;
            using (_logger.BeginScope(new Dictionary<string, object> { ["CorrelationId"] = correlationId }))

            {
                var result = new CommonResponseDto<OTAPostMemberResponse>();
                try
                {
                    _logger.LogInformation("[Service][MemberService] [START] CreateMemberAsync called.");
                    var requestJson = JsonSerializer.Serialize(otaRequest, JsonIndentedOptions);
                    _logger.LogInformation("[Service][MemberService] [REQUEST] OTAPostMemberRequest payload:\n{Payload}", requestJson);

                    var errors = ValidateMemberRequest(otaRequest);
                    if (errors.Count > 0)
                    {
                        _logger.LogError("[Service][MemberService] [VALIDATION] Validation failed.\nErrors:\n{Errors}", string.Join("\n - ", errors));

                        result.Error = MemberMapper.MapBackendError(defaultStatusCode: HttpStatusCode.BadRequest, validationMessages: errors);
                        result.Success = false;
                        return result;
                    }

                    // 1. Map OTA to Onex                
                    var onexRequest = MemberMapper.MapToOnexRequest(otaRequest);
                    _logger.LogInformation("{Prefix} - CreateMember: New member details are mapped to Onex request", ApiConstants.APITransformationFunctionPrefix);

                    // 2. Call backend API
                    var content = new StringContent(
                        JsonSerializer.Serialize(onexRequest), Encoding.UTF8, "application/json");
                    var response = await _httpClient.PostAsync($"{ApiConstants.Version}/{ApiConstants.MemberEndpoint}", content);

                    if (!response.IsSuccessStatusCode)
                    {
                        _logger.LogError("{Prefix} - CreateMember: Member service returned error while creating new member.", ApiConstants.APITransformationFunctionPrefix);
                        result.Error = MemberMapper.MapBackendError(backendResponse: response);
                        result.Success = false;
                        return result;
                    }

                    // 3. Deserialize backend response

                    var backendJson = await response.Content.ReadAsStringAsync();
                    var sourceResponse = JsonSerializer.Deserialize<MemberResponse>(
                        backendJson, JsonOptionsProvider.Options);

                    if (sourceResponse?.Data == null || (sourceResponse.Data.Errors?.Count ?? 0) > 0)
                    {
                        var firstError = sourceResponse?.Data?.Errors?.FirstOrDefault();
                        _logger.LogError("[Service][MemberService] [Backend][ERROR] Member service returned error while creating new member. ErrorCode: {ErrorCode}, ErrorMessage: {ErrorMessage}",
                        firstError?.ErrorCode, firstError?.ErrorMessage);
                        result.Error = MemberMapper.MapBackendError(firstError);
                        result.Success = false;
                        return result;
                    }

                    // 4. Map Onex to OTA
                    var otaResponse = MemberMapper.MapOnexToOtaResponse(sourceResponse);
                    _logger.LogInformation("[Service][MemberService] [END] Created new member");
                    result.Data = otaResponse;
                    result.Success = true;
                    return result;

                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "{Prefix} - CreateMember: Error creating member", ApiConstants.APITransformationFunctionPrefix);
                    result.Error = MemberMapper.MapBackendError(defaultStatusCode: HttpStatusCode.InternalServerError, validationMessages: [ex.Message]);
                    result.Success = false;
                    return result;

                }
            }
        }
        public async Task<CommonResponseDto<OTAPostMemberResponse>> UpdateMemberAsync(string memberId, JsonPatchDocument<OTAGetMemberResponse> patchDoc)
        {
            var result = new CommonResponseDto<OTAPostMemberResponse>();
            try
            {
                _logger.LogInformation("[Service][MemberService] [START] UpdateMemberAsync for {MemberId}", memberId);
                var patchJson = JsonSerializer.Serialize(patchDoc, JsonIndentedOptions);
                _logger.LogInformation("[Service][MemberService] [REQUEST] PatchDocument payload for {MemberId}:\n{Payload}", memberId, patchJson);
                var requestType = GetPatchRequestType(patchDoc);
                List<string> validationErrors = null;
                string endpoint = null;
                object onexUpdateRequest = null;
                switch (requestType)
                {
                    case PatchRequestType.Activate:
                    {
                        validationErrors = ValidateActivatePatchDocument(patchDoc);
                        endpoint = $"{ApiConstants.Version}/{ApiConstants.MemberEndpoint}/{ApiConstants.ActivateMember}";
                        break;

                    }
                    case PatchRequestType.Deactivate:
                        validationErrors = ValidateDeActivatePatchDocument(patchDoc);
                        endpoint = $"{ApiConstants.Version}/{ApiConstants.MemberEndpoint}/{ApiConstants.DeActivateMember}";
                        break;

                    case PatchRequestType.GeneralUpdate:
                        validationErrors = ValidatePatchDocument(patchDoc);
                        endpoint = $"{ApiConstants.Version}/{ApiConstants.MemberEndpoint}/{memberId}";
                        break;
                    case PatchRequestType.Invalid:
                        break;
                    default:
                        break;
                }

                if (validationErrors.Any())
                {
                    _logger.LogError("[Service][MemberService] [Validation] PatchDocument validation failed {ValidationErrors}", ApiConstants.APITransformationFunctionPrefix, string.Join(", ", validationErrors));

                    result.Error = MemberMapper.MapBackendError(defaultStatusCode: HttpStatusCode.BadRequest, validationMessages: validationErrors);
                    result.Success = false;
                    return result;
                }

                // 2. Fetch member data from backend
                var (otaResponse, error) = await GetMemberFromBackend(memberId);
                if (error != null)
                {
                    _logger.LogError("[Service][MemberService] [Backend][ERROR] Error while fetching member details for update for {MemberId}", memberId);
                    result.Error = error;
                    result.Success = false;
                    return result;
                }

                // 3. Apply the patch
                if (requestType == PatchRequestType.GeneralUpdate)
                {
                    EnsurePatchable(otaResponse, patchDoc);
                }

                patchDoc.ApplyTo(otaResponse);
                _logger.LogInformation("[Service][MemberService] [PATCH] Applied patch on member details for {MemberId}", memberId);

                // 4. Send PATCH to backend
                onexUpdateRequest = requestType switch
                {
                    PatchRequestType.Activate => MemberMapper.MapToActivateDeActivateOnexRequest(otaResponse),
                    PatchRequestType.Deactivate => MemberMapper.MapToActivateDeActivateOnexRequest(otaResponse),
                    PatchRequestType.GeneralUpdate => MemberMapper.MapToUpdateOnexRequest(otaResponse),
                    PatchRequestType.Invalid => throw new NotImplementedException(),
                    _ => null
                };

                var updatedJson = JsonSerializer.Serialize(onexUpdateRequest, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    WriteIndented = false
                });
                _logger.LogInformation("[Service][MemberService] [REQUEST] Backend update payload for {MemberId}:\n{Payload}", memberId, updatedJson);
                var content = new StringContent(updatedJson, Encoding.UTF8, "application/json");
                var response = requestType switch
                {
                    PatchRequestType.Activate or PatchRequestType.Deactivate => await _httpClient.PostAsync(endpoint, content),
                    PatchRequestType.GeneralUpdate => await _httpClient.PutAsync(endpoint, content),
                    PatchRequestType.Invalid => throw new NotImplementedException(),
                    _ => new HttpResponseMessage(HttpStatusCode.BadRequest)
                };

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("[Service][MemberService] [Backend][ERROR] Member service returned error while updating member details for {MemberId}", memberId);
                    result.Error = MemberMapper.MapBackendError(backendResponse: response);
                    result.Success = false;
                    return result;
                }

                // 5. Deserialize backend response and Map backend response to OTA response

                var backendJson = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("[Service][MemberService] [Backend][RESPONSE] Payload for {MemberId}:\n{Payload}", memberId, backendJson);
                switch (requestType)
                {
                    case PatchRequestType.Activate:
                    case PatchRequestType.Deactivate:
                    {
                        var sourceResponse = JsonSerializer.Deserialize<MemberActResponse>(backendJson, JsonOptionsProvider.Options);

                        var actError = sourceResponse?.Data?.Errors?.FirstOrDefault();
                        if (actError != null)
                        {
                            result.Error = MemberMapper.MapBackendError(actError);
                            result.Success = false;
                            return result;
                        }
                        _logger.LogInformation("[Service][MemberService] [END] Updated member details for {MemberId}", memberId);
                        result.Data = MemberMapper.MapOnexToUpdateOtaResponse(sourceResponse, otaResponse, isActivate: requestType == PatchRequestType.Activate);
                        result.Success = true;
                        return result;
                    }

                    case PatchRequestType.GeneralUpdate:
                    {
                        var sourceResponse = JsonSerializer.Deserialize<MemberResponse>(backendJson, JsonOptionsProvider.Options);
                        var firstError = sourceResponse?.Data?.Errors?.FirstOrDefault();
                        if (firstError != null)
                        {
                            result.Error = MemberMapper.MapBackendError(firstError);
                            result.Success = false;
                            return result;
                        }
                        _logger.LogInformation("[Service][MemberService] [END] Updated member details for {MemberId}", memberId);

                        result.Data = MemberMapper.MapOnexToUpdateOtaResponse(sourceResponse);
                        result.Success = true;
                        return result;
                    }
                    case PatchRequestType.Invalid:
                    {
                        _logger.LogError("[Service][MemberService] [Validation] Invalid patch request type for {MemberId}", memberId);
                        result.Error = MemberMapper.MapBackendError(
                            defaultStatusCode: HttpStatusCode.BadRequest,
                            validationMessages: ["Invalid patch request type."]
                        );
                        result.Success = false;
                        return result;
                    }
                    default:
                        throw new InvalidOperationException($"Unsupported patch request type: {requestType}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{Prefix} - UpdateMember: Error while updating member details", ApiConstants.APITransformationFunctionPrefix);
                result.Error = MemberMapper.MapBackendError(defaultStatusCode: HttpStatusCode.InternalServerError, validationMessages: [ex.Message]);
                result.Success = false;
                return result;
            }
        }

        private async Task<(OTAGetMemberResponse? otaResponse, OTAErrorResponse? error)> GetMemberFromBackend(string memberId)
        {
            var backendResponse = await _httpClient.GetAsync($"{ApiConstants.Version}/{ApiConstants.MemberEndpoint}/{memberId}");
            var backendContent = await backendResponse.Content.ReadAsStringAsync();

            if (!backendResponse.IsSuccessStatusCode)
            {
                _logger.LogError("{Prefix} - GetMemberFromBackend: Failed to fetch member {MemberId}. StatusCode: {StatusCode}",
                    ApiConstants.APITransformationFunctionPrefix, memberId, backendResponse.StatusCode);

                return (null, MemberMapper.MapBackendError(backendResponse: backendResponse));
            }

            var sourceGetResponse = JsonSerializer.Deserialize<OnexGetMemberResponse>(backendContent, JsonOptionsProvider.Options);

            if (sourceGetResponse?.Data == null || (sourceGetResponse.Data.Errors?.Count ?? 0) > 0)
            {
                var error = sourceGetResponse?.Data?.Errors?.FirstOrDefault();
                _logger.LogError("{Prefix} - GetMemberFromBackend: Backend returned error for {MemberId}. ErrorCode: {ErrorCode}, ErrorMessage: {ErrorMessage}",
                    ApiConstants.APITransformationFunctionPrefix, memberId, error?.ErrorCode, error?.ErrorMessage);

                return (null, MemberMapper.MapBackendError(error));
            }

            // Map to OTA model
            var otaResponse = MemberMapper.MapToOTAResponse(sourceGetResponse);
            return (otaResponse, null);
        }

        private void EnsurePatchable(OTAGetMemberResponse ota, JsonPatchDocument<OTAGetMemberResponse> patchDoc)
        {
            ota.MemberProfile ??= new MemberProfile();
            ota.ContactInfo ??= new ContactInfo();

            ota.MemberProfile.AdditionalIdentifiers ??= [];
            ota.ContactInfo.Emails ??= [];
            ota.ContactInfo.Phones ??= [];
            ota.ContactInfo.Addresses ??= [];
            ota.LoyaltyPrograms ??= [];

            // Local helper to check if patchDoc contains a path            
            bool HasPatchFor(string pathSegment, params string[] allowedOps) =>
                    patchDoc.Operations.Any(op =>
                        op.path.StartsWith(pathSegment, StringComparison.OrdinalIgnoreCase) &&
                        (allowedOps.Length == 0 || allowedOps.Contains(op.op, StringComparer.OrdinalIgnoreCase)));


            // For replace or remove, ensure the path exists
            if (ota.MemberProfile.AdditionalIdentifiers.Count == 0 && HasPatchFor("/memberProfile/additionalIdentifiers", "replace", "remove"))
            {
                ota.MemberProfile.AdditionalIdentifiers.Add(new AdditionalIdentifier());
            }

            if (ota.ContactInfo.Emails.Count == 0 && HasPatchFor("/contactInfo/emails", "replace", "remove"))
            {
                ota.ContactInfo.Emails.Add(new Emails());
            }

            if (ota.ContactInfo.Phones.Count == 0 && HasPatchFor("/contactInfo/phones", "replace", "remove"))
            {
                ota.ContactInfo.Phones.Add(new Phone());
            }

            if (ota.ContactInfo.Addresses.Count == 0 && HasPatchFor("/contactInfo/addresses", "replace", "remove"))
            {
                ota.ContactInfo.Addresses.Add(new Address());
            }

            if (ota.LoyaltyPrograms.Count == 0 && HasPatchFor("/loyaltyPrograms", "replace", "remove"))
            {
                ota.LoyaltyPrograms.Add(new LoyaltyProgram());
            }

        }

        private static List<string> ValidateMemberRequest(OTAPostMemberRequest otaRequest)
        {
            var errors = new List<string>();

            if (otaRequest?.MemberProfile == null)
            {
                errors.Add("MemberProfile is required.");
                return errors;
            }

            var member = otaRequest.MemberProfile;

            // 1. CorrelationID
            var uuidPattern = @"^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$";
            if (string.IsNullOrWhiteSpace(otaRequest.CorrelationID) ||
                otaRequest.CorrelationID.Length != 36 ||
                !Regex.IsMatch(otaRequest.CorrelationID, uuidPattern))
            {
                errors.Add("CorrelationID is missing or invalid UUID format.");
            }

            // 2. TradingAgreementCode
            if (!string.IsNullOrWhiteSpace(otaRequest.TradingAgreementCode) && otaRequest.TradingAgreementCode.Length > 20)
            {
                errors.Add("TradingAgreementCode exceeds max length 20.");
            }

            // 3. SourceSystemIds
            if (member.SourceSystemIds == null || member.SourceSystemIds.Count < 1)
            {
                errors.Add("At least one SourceSystemId is required.");
            }
            // 3. SourceSystem
            if (member.SourceSystemIds.Any(x => string.IsNullOrEmpty(x.SourceSystem)))
            {
                errors.Add(" SourceSystem is required.");
            }
            else
            {
                var hasPartner = false;
                var allowedSourceSystems = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                {
                    "PARTNER", "EXCHANGE", "INTERNAL", "EXTERNAL", "LEGACY", "VOICE_REFERENCE_NUMBER"
                };

                foreach (var s in member.SourceSystemIds)
                {
                    if (string.IsNullOrWhiteSpace(s.SourceSystem) || s.SourceSystem.Length > 50)
                    {
                        errors.Add("SourceSystem is required and max 50 characters.");
                    }
                    else if (!allowedSourceSystems.Contains(s.SourceSystem))
                    {
                        errors.Add($"SourceSystem is not supported: {s.SourceSystem}");
                    }

                    if (string.IsNullOrWhiteSpace(s.Id) || s.Id.Length > 50)
                    {
                        errors.Add("SourceSystemId.Id is required and max 50 characters.");
                    }

                    if (s.SourceSystem.Equals("PARTNER", StringComparison.OrdinalIgnoreCase))
                    {
                        hasPartner = true;
                    }
                }

                if (!hasPartner)
                {
                    errors.Add("At least one SourceSystemId must have SourceSystem = 'PARTNER'.");
                }
            }

            // 4. Names
            var namePattern = @"^[A-Za-z\s-]{1,50}$";
            if (string.IsNullOrWhiteSpace(member.FirstName) || !Regex.IsMatch(member.FirstName, namePattern))
            {
                errors.Add("FirstName is required and max 50 characters (letters, spaces, hyphen).");
            }

            if (string.IsNullOrWhiteSpace(member.LastName) || !Regex.IsMatch(member.LastName, namePattern))
            {
                errors.Add("LastName is required and max 50 characters (letters, spaces, hyphen).");
            }

            // 5. BirthDate
            if (!string.IsNullOrWhiteSpace(member.BirthDate) &&
                (member.BirthDate.Length > 10 ||
                 !DateTime.TryParseExact(member.BirthDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _)))
            {
                errors.Add("BirthDate must be in yyyy-MM-dd format.");
            }

            // 6. Gender
            var genderPattern = @"^(M|F|O|U)$";
            if (!string.IsNullOrWhiteSpace(member.Gender) && !Regex.IsMatch(member.Gender, genderPattern))
            {
                errors.Add("Gender must be one of M, F, O, U.");
            }

            // 7. ProfileType
            if (!string.IsNullOrWhiteSpace(member.ProfileType))
            {
                var validProfileTypes = new[] { "Individual", "Corporate", "Family", "Group", "Guest", "Employee" };
                if (!validProfileTypes.Contains(member.ProfileType.Trim(), StringComparer.OrdinalIgnoreCase))
                {
                    errors.Add($"ProfileType is not supported: {member.ProfileType}");
                }
            }

            // 8. MemberTier
            if (!string.IsNullOrWhiteSpace(member.MemberTier))
            {
                var validMemberTiers = new[] { "GOLD", "PLATINUM", "DIAMOND", "SILVER", "CHARTER", "BRONZE" };
                if (!validMemberTiers.Contains(member.MemberTier.Trim(), StringComparer.OrdinalIgnoreCase))
                {
                    errors.Add($"MemberTier is not supported: {member.MemberTier}");
                }
            }

            // 9. AdditionalIdentifiers
            if (member.AdditionalIdentifiers != null)
            {
                if (member.AdditionalIdentifiers.Count > 5)
                {
                    errors.Add("Maximum 5 AdditionalIdentifiers allowed.");
                }

                var validTypes = new[] { "DL", "SSN", "PASSPORT", "OTHER", "NATIONAL_ID", "EMPLOYEE_ID" };
                foreach (var id in member.AdditionalIdentifiers)
                {
                    if (!string.IsNullOrWhiteSpace(id.Type) && !validTypes.Contains(id.Type.Trim(), StringComparer.OrdinalIgnoreCase))
                    {
                        errors.Add($"AdditionalIdentifier Type is not supported: {id.Type}");
                    }

                    if (!string.IsNullOrWhiteSpace(id.Value) && id.Value.Length > 50)
                    {
                        errors.Add("AdditionalIdentifier Value exceeds max length 50.");
                    }
                }
            }

            // 10. Emails
            if (otaRequest.ContactInfo?.Emails != null)
            {
                if (otaRequest.ContactInfo.Emails.Count > 5)
                {
                    errors.Add("Maximum 5 Emails allowed.");
                }

                if (otaRequest.ContactInfo.Emails.Count(e => e.IsPrimary) > 1)
                {
                    errors.Add("Only one primary email is allowed.");
                }

                var emailValidator = new System.ComponentModel.DataAnnotations.EmailAddressAttribute();
                var validEmailTypes = new[] { "CUS", "RES", "INFO", "MKT", "BILL", "SUPPORT", "PROMO", "IND" };

                foreach (var e in otaRequest.ContactInfo.Emails)
                {
                    if (string.IsNullOrWhiteSpace(e.Email) || e.Email.Length > 100 || !emailValidator.IsValid(e.Email))
                    {
                        errors.Add($"Email is not in a valid format: {e.Email}");
                    }

                    if (!string.IsNullOrWhiteSpace(e.EmailType) && !validEmailTypes.Contains(e.EmailType.Trim(), StringComparer.OrdinalIgnoreCase))
                    {
                        errors.Add($"EmailType is not supported: {e.EmailType}");
                    }
                }
            }

            // 11. Phones
            if (otaRequest.ContactInfo?.Phones != null)
            {
                if (otaRequest.ContactInfo.Phones.Count > 5)
                {
                    errors.Add("Maximum 5 Phones allowed.");
                }

                if (otaRequest.ContactInfo.Phones.Count(p => p.IsPrimary) > 1)
                {
                    errors.Add("Only one primary phone is allowed.");
                }

                var phonePattern = @"^\+\d{1,3}-\d{1,5}-\d{4,10}$"; // e.g., +1-202-5551234
                var validPhoneTypes = new[] { "VOICE", "FAX", "MOBILE", "PAGER", "VIDEO", "HOME", "WORK" };

                foreach (var p in otaRequest.ContactInfo.Phones)
                {
                    if (string.IsNullOrWhiteSpace(p.PhoneNumber) || p.PhoneNumber.Length > 20 || !Regex.IsMatch(p.PhoneNumber, phonePattern))
                    {
                        errors.Add($"PhoneNumber is not in a valid format: {p.PhoneNumber}");
                    }

                    if (!string.IsNullOrWhiteSpace(p.PhoneTechType) && !validPhoneTypes.Contains(p.PhoneTechType.Trim(), StringComparer.OrdinalIgnoreCase))
                    {
                        errors.Add($"PhoneTechType is not supported: {p.PhoneTechType}");
                    }

                    if (!string.IsNullOrWhiteSpace(p.CountryCode) && p.CountryCode.Length > 4)
                    {
                        errors.Add("CountryCode exceeds max length 4.");
                    }

                    if (!string.IsNullOrWhiteSpace(p.AreaCode) && p.AreaCode.Length > 5)
                    {
                        errors.Add("AreaCode exceeds max length 5.");
                    }
                }
            }

            // 12. Addresses
            if (otaRequest.ContactInfo?.Addresses != null)
            {
                if (otaRequest.ContactInfo.Addresses.Count > 5)
                {
                    errors.Add("Maximum 5 Addresses allowed.");
                }

                if (otaRequest.ContactInfo.Addresses.Count(a => a.IsPrimary) > 1)
                {
                    errors.Add("Only one primary address is allowed.");
                }

                var validAddressTypes = new[] { "Home", "Business", "Billing", "Shipping", "Temporary", "Vacation", "Mailing" };
                foreach (var a in otaRequest.ContactInfo.Addresses)
                {
                    if (!string.IsNullOrWhiteSpace(a.AddressType) && !validAddressTypes.Contains(a.AddressType.Trim(), StringComparer.OrdinalIgnoreCase))
                    {
                        errors.Add($"AddressType is not supported: {a.AddressType}");
                    }

                    if (!string.IsNullOrWhiteSpace(a.AddressLine1) && a.AddressLine1.Length > 300)
                    {
                        errors.Add("AddressLine1 exceeds max length 300.");
                    }

                    if (!string.IsNullOrWhiteSpace(a.AddressLine2) && a.AddressLine2.Length > 300)
                    {
                        errors.Add("AddressLine2 exceeds max length 300.");
                    }

                    if (!string.IsNullOrWhiteSpace(a.CityName) && a.CityName.Length > 50)
                    {
                        errors.Add("CityName exceeds max length 50.");
                    }

                    if (!string.IsNullOrWhiteSpace(a.PostalCode) && a.PostalCode.Length > 10)
                    {
                        errors.Add("PostalCode exceeds max length 10.");
                    }

                    if (!string.IsNullOrWhiteSpace(a.StateProv) && a.StateProv.Length > 2)
                    {
                        errors.Add("StateProv exceeds max length 2.");
                    }

                    if (!string.IsNullOrWhiteSpace(a.State) && a.State.Length > 50)
                    {
                        errors.Add("State exceeds max length 50.");
                    }

                    if (!string.IsNullOrWhiteSpace(a.CountryCode) && a.CountryCode.Length > 3)
                    {
                        errors.Add("CountryCode exceeds max length 3.");
                    }

                    if (!string.IsNullOrWhiteSpace(a.Country) && a.Country.Length > 50)
                    {
                        errors.Add("Country exceeds max length 50.");
                    }

                    if (a.Latitude.HasValue && (a.Latitude < -90 || a.Latitude > 90))
                    {
                        errors.Add("Latitude must be between -90 and 90.");
                    }

                    if (a.Longitude.HasValue && (a.Longitude < -180 || a.Longitude > 180))
                    {
                        errors.Add("Longitude must be between -180 and 180.");
                    }
                }
            }

            // 13. Loyalty Programs
            if (otaRequest.LoyaltyPrograms != null)
            {
                var validProgramCodes = new[] { "GP", "PLATINUM", "GOLD", "SILVER", "HONORS" };
                var validOwnTypes = new[] { "Individual", "Corporate", "Family" };

                foreach (var p in otaRequest.LoyaltyPrograms)
                {
                    if (!string.IsNullOrWhiteSpace(p.ProgramCode) && !validProgramCodes.Contains(p.ProgramCode.Trim(), StringComparer.OrdinalIgnoreCase))
                    {
                        errors.Add($"ProgramCode is not supported: {p.ProgramCode}");
                    }

                    if (!string.IsNullOrWhiteSpace(p.OwnType) && !validOwnTypes.Contains(p.OwnType.Trim(), StringComparer.OrdinalIgnoreCase))
                    {
                        errors.Add($"OwnType is not supported: {p.OwnType}");
                    }

                    if (!string.IsNullOrWhiteSpace(p.MembershipId) && p.MembershipId.Length > 50)
                    {
                        errors.Add("MembershipId exceeds max length 50.");
                    }
                }
            }

            return errors;
        }


        /// <summary>
        /// Validates a JsonPatchDocument.
        /// 
        /// Rules applied:
        /// - Object roots like /memberProfile and /contactInfo
        /// - Primitive roots like /memberStatus
        /// - arrays inside object (like additionalIdentifiers, emails, phones, addresses)
        /// - Standalone array roots like /loyaltyPrograms
        /// - Validation of array indices (0, 1, etc.) or "-" wildcard
        /// - Ensures no extra segments beyond allowed structure
        /// </summary>       
        private List<string> ValidatePatchDocument(JsonPatchDocument<OTAGetMemberResponse> patchDoc)
        {
            var errors = new List<string>();

            var allowedOps = new[] { "replace", "add", "remove" };
            var objectRoots = new[] { "/memberprofile", "/contactinfo" };
            var primitiveRoots = new[] { "/memberstatus" };

            var objectArrayFields = new Dictionary<string, string[]>
            {
                { "/memberprofile", new[] { "additionalidentifiers" } },
                { "/contactinfo", new[] { "emails", "phones", "addresses" } }
            };
            var arrayRoots = new[] { "/loyaltyprograms" };

            foreach (var op in patchDoc.Operations)
            {
                // 1. Validate operation type
                if (string.IsNullOrWhiteSpace(op.op) || !allowedOps.Contains(op.op.ToLowerInvariant()))
                {
                    errors.Add($"Operation '{op.op}' is not allowed.");
                }

                // 2. Validate path
                if (string.IsNullOrWhiteSpace(op.path))
                {
                    errors.Add($"Path is required for operation '{op.op}'.");
                    continue;
                }

                if (!op.path.StartsWith("/"))
                {
                    errors.Add($"Path '{op.path}' is not allowed.");
                    continue;
                }

                var pathLower = op.path.ToLowerInvariant();
                var segments = pathLower.Split('/', StringSplitOptions.RemoveEmptyEntries);

                if (segments.Length == 0)
                {
                    errors.Add($"Path '{op.path}' is not allowed.");
                    continue;
                }

                var root = "/" + segments[0];

                // 3. Object roots
                if (objectRoots.Contains(root))
                {
                    if (segments.Length == 1)
                    {
                        // Root object itself - must be object for add/replace
                        if (op.op.ToLowerInvariant() != "remove" && (op.value is not JsonElement je || je.ValueKind != JsonValueKind.Object))
                        {
                            errors.Add($"Value for '{op.path}' must be an object. Primitive not allowed.");
                            continue;
                        }
                    }
                    else if (segments.Length == 2)
                    {
                        var second = segments[1];
                        // Check if this is an object array field
                        if (objectArrayFields.TryGetValue(root, out var arrays) && arrays.Contains(second))
                        {
                            errors.Add($"Path '{op.path}' is not allowed.");
                            continue;
                        }
                    }
                    else if (segments.Length == 3)
                    {
                        var second = segments[1];
                        var third = segments[2];
                        if (objectArrayFields.TryGetValue(root, out var arrays) && arrays.Contains(second))
                        {
                            if (third != "-" && !int.TryParse(third, out _))
                            {
                                errors.Add($"Path '{op.path}' is not allowed.");
                                continue;
                            }
                        }
                        else
                        {
                            errors.Add($"Path '{op.path}' is not allowed.");
                            continue;
                        }
                    }
                    else
                    {
                        errors.Add($"Path '{op.path}' is not allowed.");
                        continue;
                    }
                }
                // 4. Array roots
                else if (arrayRoots.Contains(root))
                {
                    if (segments.Length != 2 || (segments[1] != "-" && !int.TryParse(segments[1], out _)))
                    {
                        errors.Add($"Path '{op.path}' is not allowed.");
                        continue;
                    }
                }
                // 5. Primitive roots
                else if (primitiveRoots.Contains(root))
                {
                    if (segments.Length != 1)
                    {
                        errors.Add($"Path '{op.path}' is not allowed.");
                        continue;
                    }
                }
                else
                {
                    errors.Add($"Path '{op.path}' is not allowed.");
                    continue;
                }

                // 6. Validate value for required operations
                if (op?.op?.ToLowerInvariant() != "remove")
                {
                    if (op.value == null)
                    {
                        errors.Add($"Value is required for operation '{op.op}' at path '{op.path}'.");
                    }
                    else
                    {
                        // 7. Field-specific validation
                        errors.AddRange(ValidatePatchField(op.path, op.value, op.op));
                    }
                }
            }

            return errors;
        }

        private List<string> ValidatePatchField(string path, object value, string operation)
        {
            var errors = new List<string>();
            var val = value?.ToString();
            var pathLower = path.ToLowerInvariant();

            // Skip validation for "remove" operations
            if (operation == "remove")
            {
                return errors;
            }

            // ==== MemberProfile ====
            if (pathLower.StartsWith("/memberprofile/"))
            {
                if (pathLower.EndsWith("/firstname") || pathLower.EndsWith("/lastname"))
                {
                    var namePattern = @"^[A-Za-z\s-]{1,50}$";
                    if (!string.IsNullOrWhiteSpace(val) && !Regex.IsMatch(val, namePattern))
                    {
                        errors.Add($"Name format for '{path}' is incorrect. Only letters, spaces, and '-' allowed (max 50).");
                    }
                }
                else if (pathLower.EndsWith("/birthdate"))
                {
                    if (!string.IsNullOrWhiteSpace(val) &&
                        (!DateTime.TryParseExact(val, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _) || val.Length > 10))
                    {
                        errors.Add($"Birth date format '{val}' is incorrect. Must be yyyy-MM-dd.");
                    }
                }
                else if (pathLower.EndsWith("/gender"))
                {
                    var genderPattern = @"^(M|F|O|U)$";
                    if (!Regex.IsMatch(val ?? "", genderPattern))
                    {
                        errors.Add($"Gender '{val}' is not allowed.");
                    }
                }
                else if (pathLower.EndsWith("/profiletype"))
                {
                    var validProfileTypes = new[] { "Individual", "Corporate", "Family", "Group", "Guest", "Employee" };
                    if (!validProfileTypes.Contains(val ?? "", StringComparer.OrdinalIgnoreCase))
                    {
                        errors.Add($"ProfileType '{val}' is not supported.");
                    }
                }
                else if (pathLower.EndsWith("/membertier"))
                {
                    var validTiers = new[] { "GOLD", "PLATINUM", "DIAMOND", "SILVER", "CHARTER", "BRONZE" };
                    if (!validTiers.Contains(val ?? "", StringComparer.OrdinalIgnoreCase))
                    {
                        errors.Add($"MemberTier '{val}' is not supported.");
                    }
                }
                else if (pathLower.Contains("/additionalidentifiers"))
                {
                    errors.AddRange(ValidatePatchArrayObject(
                        path,
                        value,
                        validTypes: ["DL", "SSN", "PASSPORT", "OTHER", "NATIONAL_ID", "EMPLOYEE_ID"],
                        primaryField: "type",
                        secondaryField: "value",
                        maxLengthField: "value",
                        maxLength: 50
                    ));
                }

            }

            // ==== ContactInfo/Emails ====
            else if (pathLower.StartsWith("/contactinfo/emails"))
            {
                ValidateContactInfoItem(path, value, "Email", errors);
            }

            // ==== ContactInfo/Phones ====
            else if (pathLower.StartsWith("/contactinfo/phones"))
            {
                ValidateContactInfoItem(path, value, "Phone", errors);
            }
            else if (pathLower.StartsWith("/contactinfo/addresses"))
            {
                ValidateContactInfoItem(path, value, "Address", errors);
            }

            // ==== Member Status ====
            else if (pathLower.EndsWith("/memberstatus"))
            {
                var validStatuses = new[] { "Active", "Inactive" };

                if (string.IsNullOrWhiteSpace(val) ||
                    !validStatuses.Contains(val, StringComparer.OrdinalIgnoreCase))
                {
                    errors.Add($"Invalid MemberStatus '{val}'. Only Active or Inactive is allowed.");
                }
            }


            // ==== LoyaltyPrograms ====
            else if (pathLower.StartsWith("/loyaltyprograms"))
            {

                errors.AddRange(ValidatePatchArrayObject(
                        path, value,
                        validTypes: ["GP", "PLATINUM", "GOLD", "SILVER", "HONORS"],
                        validSecondary: ["Individual", "Corporate", "Family"],
                        primaryField: "programCode",
                        secondaryField: "ownType",
                        maxLengthField: "membershipId",
                        maxLength: 50
                ));

            }

            return errors;
        }

        private static List<string> ValidatePatchArrayObject(string path, object value, string[] validTypes, string[] validSecondary = null, string primaryField = "type", string secondaryField = "value", string maxLengthField = null, int maxLength = 50)
        {
            var errors = new List<string>();
            var pathLower = path.ToLowerInvariant();
            JsonElement je;

            // Convert value to JsonElement
            if (value is JsonElement e)
            {
                je = e;
            }
            else
            {
                try
                {
                    var jsonString = value?.ToString()?.Trim();
                    if (string.IsNullOrWhiteSpace(jsonString))
                    {
                        errors.Add($"{primaryField} value is empty.");
                        return errors;
                    }

                    using var doc = JsonDocument.Parse(jsonString);
                    je = doc.RootElement.Clone();
                }
                catch
                {
                    errors.Add($"JSON format for {primaryField} is incorrect.");
                    return errors;
                }
            }

            // Whole object
            if (je.ValueKind == JsonValueKind.Object)
            {
                var primary = je.TryGetProperty(primaryField, out var pProp) ? pProp.GetString()?.Trim() : null;
                var secondary = (validSecondary != null && je.TryGetProperty(secondaryField, out var sProp)) ? sProp.GetString()?.Trim() : null;
                var lengthCheck = (maxLengthField != null && je.TryGetProperty(maxLengthField, out var lProp)) ? lProp.GetString()?.Trim() : null;

                if (!validTypes.Contains(primary ?? "", StringComparer.OrdinalIgnoreCase))
                {
                    errors.Add($"{primaryField} '{primary}' is not allowed.");
                }

                if (validSecondary != null && !validSecondary.Contains(secondary ?? "", StringComparer.OrdinalIgnoreCase))
                {
                    errors.Add($"{secondaryField} '{secondary}' is not allowed.");
                }

                if (!string.IsNullOrWhiteSpace(lengthCheck) && lengthCheck.Length > maxLength)
                {
                    errors.Add($"{maxLengthField} too long (max {maxLength}).");
                }
            }
            // Single field patch
            else if (je.ValueKind == JsonValueKind.String)
            {
                var valStr = je.GetString()?.Trim();

                if (pathLower.EndsWith("/" + primaryField.ToLower()) &&
                    !validTypes.Contains(valStr ?? "", StringComparer.OrdinalIgnoreCase))
                {
                    errors.Add($"{primaryField} '{valStr}' is not allowed. ");
                }

                if (validSecondary != null && pathLower.EndsWith("/" + secondaryField.ToLower()) &&
                    !validSecondary.Contains(valStr ?? "", StringComparer.OrdinalIgnoreCase))
                {
                    errors.Add($"{secondaryField} '{valStr}' is not allowed. ");
                }

                if (maxLengthField != null && pathLower.EndsWith("/" + maxLengthField.ToLower()) &&
                    !string.IsNullOrWhiteSpace(valStr) && valStr.Length > maxLength)
                {
                    errors.Add($"{maxLengthField} too long (max {maxLength}).");
                }
            }
            else
            {
                errors.Add($"Unsupported {primaryField} value type.");
            }

            return errors;
        }

        private void ValidateContactInfoItem(string path, object value, string itemType, List<string> errors)
        {
            // --- Normalize value to JsonElement ---
            JsonElement obj;
            if (value is JsonElement je && je.ValueKind == JsonValueKind.Object)
            {
                obj = je;
            }
            else if (itemType == "Address" && value is Address addr)
            {
                obj = JsonSerializer.SerializeToElement(addr);
            }
            else if (itemType == "Email" && value is Emails email)
            {
                obj = JsonSerializer.SerializeToElement(email);
            }
            else if (itemType == "Phone" && value is Phone phone)
            {
                obj = JsonSerializer.SerializeToElement(phone);
            }
            else
            {
                var jsonString = value?.ToString()?.Trim();
                if (string.IsNullOrWhiteSpace(jsonString))
                {
                    errors.Add($"Value for '{path}' must be a {itemType} object or valid JSON.");
                    return;
                }

                try
                {
                    using var doc = JsonDocument.Parse(jsonString);
                    obj = doc.RootElement.Clone();
                }
                catch (JsonException ex)
                {
                    errors.Add($"JSON format for '{path}' is incorrect. Error: {ex.Message}");
                    return;
                }
            }

            // --- Field validations ---
            switch (itemType)
            {
                case "Address":
                {
                    var validAddressTypes = new[] { "Home", "Business", "Billing", "Shipping", "Temporary", "Vacation", "Mailing" };
                    if (!obj.TryGetProperty("addressType", out var typeProp) || typeProp.ValueKind != JsonValueKind.String)
                    {
                        errors.Add($"AddressType is required at '{path}'.");
                    }
                    else
                    {
                        var addressType = typeProp.GetString()?.Trim();
                        if (!validAddressTypes.Contains(addressType, StringComparer.OrdinalIgnoreCase))
                        {
                            errors.Add($"AddressType '{addressType}' at '{path}' is not allowed.");
                        }
                    }

                    // String fields length validation
                    void CheckString(string field, int max)
                    {
                        if (obj.TryGetProperty(field, out var prop) &&
                            prop.ValueKind == JsonValueKind.String)
                        {
                            var fieldValue = prop.GetString()?.Trim();
                            if (!string.IsNullOrEmpty(fieldValue) && fieldValue.Length > max)
                            {
                                errors.Add($"{field} too long (max {max}) at '{path}'.");
                            }
                        }
                    }


                    CheckString("addressLine1", 300);
                    CheckString("addressLine2", 300);
                    CheckString("cityName", 50);
                    CheckString("postalCode", 10);
                    CheckString("stateProv", 2);
                    CheckString("state", 50);
                    CheckString("countryCode", 3);
                    CheckString("country", 50);

                    // Latitude/Longitude
                    if (obj.TryGetProperty("latitude", out var latProp) && latProp.ValueKind == JsonValueKind.Number)
                    {
                        var lat = latProp.GetDouble();
                        if (lat is < (-90) or > 90)
                        {
                            errors.Add($"Latitude '{lat}' at '{path}' must be between -90 and 90.");
                        }
                    }
                    if (obj.TryGetProperty("longitude", out var lonProp) && lonProp.ValueKind == JsonValueKind.Number)
                    {
                        var lon = lonProp.GetDouble();
                        if (lon is < (-180) or > 180)
                        {
                            errors.Add($"Longitude '{lon}' at '{path}' must be between -180 and 180.");
                        }
                    }

                    // isPrimary boolean
                    if (obj.TryGetProperty("isPrimary", out var primaryProp) &&
                        primaryProp.ValueKind != JsonValueKind.True && primaryProp.ValueKind != JsonValueKind.False)
                    {
                        errors.Add($"isPrimary must be boolean at '{path}'.");
                    }
                }
                break;

                case "Email":
                {
                    var validEmailTypes = new[] { "CUS", "RES", "INFO", "MKT", "BILL", "SUPPORT", "PROMO", "IND" };


                    if (!obj.TryGetProperty("emailType", out var typeProp) || typeProp.ValueKind != JsonValueKind.String)
                    {
                        errors.Add($"EmailType is required at '{path}'.");
                    }
                    else
                    {
                        var emailType = typeProp.GetString()?.Trim();
                        if (!validEmailTypes.Contains(emailType, StringComparer.OrdinalIgnoreCase))
                        {
                            errors.Add($"EmailType '{emailType}' at '{path}' is not allowed.");
                        }
                    }

                    if (obj.TryGetProperty("email", out var emailProp) && emailProp.ValueKind == JsonValueKind.String)
                    {
                        var emailStr = emailProp.GetString()?.Trim();
                        var validator = new System.ComponentModel.DataAnnotations.EmailAddressAttribute();
                        if (string.IsNullOrWhiteSpace(emailStr) || !validator.IsValid(emailStr) || emailStr.Length > 100)
                        {
                            errors.Add($"Email at '{path}' is invalid or too long (max 100).");
                        }
                    }

                    if (obj.TryGetProperty("isPrimary", out var primaryProp) && primaryProp.ValueKind != JsonValueKind.True && primaryProp.ValueKind != JsonValueKind.False)
                    {
                        errors.Add($"isPrimary must be a boolean at '{path}'.");
                    }
                }
                break;

                case "Phone":
                {
                    var validTypes = new[] { "VOICE", "FAX", "MOBILE", "PAGER", "VIDEO", "HOME", "WORK" };

                    if (!obj.TryGetProperty("phoneTechType", out var techProp) || techProp.ValueKind != JsonValueKind.String)
                    {
                        errors.Add($"PhoneTechType is required at '{path}'.");
                    }
                    else
                    {
                        var phoneType = techProp.GetString()?.Trim();
                        if (!validTypes.Contains(phoneType, StringComparer.OrdinalIgnoreCase))
                        {
                            errors.Add($"PhoneTechType '{phoneType}' at '{path}' is not allowed.");
                        }
                    }

                    if (obj.TryGetProperty("phoneNumber", out var numProp) && numProp.ValueKind == JsonValueKind.String)
                    {
                        var num = numProp.GetString()?.Trim();
                        var pattern = @"^\+\d{1,3}-\d{1,5}-\d{4,10}$";
                        if (string.IsNullOrWhiteSpace(num) || !Regex.IsMatch(num, pattern))
                        {
                            errors.Add($"Phone number '{num}' at '{path}' must match +CC-AAA-NNNNN format.");
                        }
                    }

                    if (obj.TryGetProperty("countryCode", out var ccProp) && ccProp.ValueKind == JsonValueKind.String)
                    {
                        var cc = ccProp.GetString()?.Trim();
                        if (!string.IsNullOrEmpty(cc) && cc.Length > 4)
                        {
                            errors.Add($"CountryCode too long (max 4) at '{path}'.");
                        }
                    }

                    if (obj.TryGetProperty("areaCode", out var acProp) && acProp.ValueKind == JsonValueKind.String)
                    {
                        var ac = acProp.GetString()?.Trim();
                        if (!string.IsNullOrEmpty(ac) && ac.Length > 5)
                        {
                            errors.Add($"AreaCode too long (max 5) at '{path}'.");
                        }
                    }

                    if (obj.TryGetProperty("isPrimary", out var primaryProp) && primaryProp.ValueKind != JsonValueKind.True && primaryProp.ValueKind != JsonValueKind.False)
                    {
                        errors.Add($"isPrimary must be a boolean at '{path}'.");
                    }
                }
                break;
                default:
                    break;
            }
        }

        private static List<string> ValidateActivatePatchDocument(JsonPatchDocument<OTAGetMemberResponse> patchDoc)
        {
            var errors = new List<string>();

            if (patchDoc.Operations.Count != 1)
            {
                errors.Add("Only one operation is allowed.");
                return errors;
            }

            var operation = patchDoc.Operations.First();

            if (!string.Equals(operation.op, "replace", StringComparison.OrdinalIgnoreCase))
            {
                errors.Add("Only 'replace' operation is allowed.");
            }

            if (!string.Equals(operation.path, "/memberStatus", StringComparison.OrdinalIgnoreCase))
            {
                errors.Add("Only '/memberStatus' path is allowed.");
            }

            if (operation.value == null || !string.Equals(operation.value.ToString(), "Active", StringComparison.OrdinalIgnoreCase))
            {
                errors.Add("The 'value' must be 'Active'.");
            }

            return errors;
        }
        private static List<string> ValidateDeActivatePatchDocument(JsonPatchDocument<OTAGetMemberResponse> patchDoc)
        {
            var errors = new List<string>();

            if (patchDoc.Operations.Count != 1)
            {
                errors.Add("Only one operation is allowed.");
                return errors;
            }

            var operation = patchDoc.Operations.First();

            if (!string.Equals(operation.op, "replace", StringComparison.OrdinalIgnoreCase))
            {
                errors.Add("Only 'replace' operation is allowed.");
            }

            if (!string.Equals(operation.path, "/memberStatus", StringComparison.OrdinalIgnoreCase))
            {
                errors.Add("Only '/memberStatus' path is allowed.");
            }

            if (operation.value == null || !string.Equals(operation.value.ToString(), "Inactive", StringComparison.OrdinalIgnoreCase))
            {
                errors.Add("The 'value' must be 'Inactive'.");
            }

            return errors;
        }

        private enum PatchRequestType
        {
            Activate,
            Deactivate,
            GeneralUpdate,
            Invalid
        }

        private static PatchRequestType GetPatchRequestType(JsonPatchDocument<OTAGetMemberResponse> patchDoc)
        {
            if (patchDoc == null || !patchDoc.Operations.Any())
            {
                return PatchRequestType.Invalid;
            }

            if (patchDoc.Operations.Count == 1)
            {
                var op = patchDoc.Operations.First();
                if (string.Equals(op.op, "replace", StringComparison.OrdinalIgnoreCase)
                    && string.Equals(op.path, "/memberStatus", StringComparison.OrdinalIgnoreCase))
                {
                    var value = op.value?.ToString();
                    if (string.Equals(value, "Active", StringComparison.OrdinalIgnoreCase))
                    {
                        return PatchRequestType.Activate;
                    }

                    if (string.Equals(value, "Inactive", StringComparison.OrdinalIgnoreCase))
                    {
                        return PatchRequestType.Deactivate;
                    }
                }
            }

            return PatchRequestType.GeneralUpdate;
        }

    }
}
