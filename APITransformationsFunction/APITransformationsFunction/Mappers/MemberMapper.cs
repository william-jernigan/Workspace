
using APITransformationsFunction.Model.Member;
using APITransformationsFunction.Model.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;

namespace APITransformationsFunction.Mappers
{
    public static class MemberMapper
    {
        public static MemberRequest MapToOnexRequest(OTAPostMemberRequest ota)
        {
            if (ota == null)
            {
                return null;
            }

            var partnerSourceSystem = ota.MemberProfile?.SourceSystemIds?.FirstOrDefault(s => s.SourceSystem?.Equals("PARTNER", StringComparison.OrdinalIgnoreCase) == true);
            var otherSourceSystems = ota.MemberProfile?.SourceSystemIds?.Where(s => !s.SourceSystem?.Equals("PARTNER", StringComparison.OrdinalIgnoreCase) == true).ToList();

            // ExternalReference uses the PARTNER source system
            var externalReference = partnerSourceSystem != null
                ? new ExternalReference
                {
                    SourceSystem = partnerSourceSystem.SourceSystem ?? string.Empty,
                    ReferenceId = partnerSourceSystem.Id ?? string.Empty
                }
                : new ExternalReference
                {
                    SourceSystem = string.Empty,
                    ReferenceId = string.Empty
                };

            // Assigning other SourceSystemIds to otherAttributes
            var otherAttributes = new List<OtherAttribute>();

            if (otherSourceSystems != null)
            {
                otherAttributes.AddRange(otherSourceSystems.Select(s => new OtherAttribute
                {
                    Attribute = s.SourceSystem ?? string.Empty,
                    Value = s.Id ?? string.Empty
                }));
            }
            if (ota.MemberProfile?.AdditionalIdentifiers != null)
            {
                otherAttributes.AddRange(ota.MemberProfile.AdditionalIdentifiers.Select(ai => new OtherAttribute
                {
                    Attribute = ai.Type ?? string.Empty,
                    Value = ai.Value ?? string.Empty
                }));
            }
            // Add static attributes
            otherAttributes.AddRange(
            [
                new OtherAttribute { Attribute = "birthDate", Value = ota.MemberProfile?.BirthDate ?? string.Empty },
                new OtherAttribute { Attribute = "gender", Value = ota.MemberProfile?.Gender ?? string.Empty },
                new OtherAttribute { Attribute = "profileType", Value = ota.MemberProfile?.ProfileType ?? string.Empty },
                new OtherAttribute { Attribute = "tradingAgreementCode", Value = ota.TradingAgreementCode ?? string.Empty },
                new OtherAttribute { Attribute = "marketingKeyCode", Value = ota.MemberProfile?.MarketingKeyCode ?? string.Empty }
            ]);

            //Map Honors program to OtherAttributes
            if (ota.LoyaltyPrograms != null)
            {
                var honorsPrograms = ota.LoyaltyPrograms
                    .Where(l => l.ProgramCode?.Equals("Honors", StringComparison.OrdinalIgnoreCase) == true)
                    .ToList();

                foreach (var honors in honorsPrograms)
                {
                    otherAttributes.Add(new OtherAttribute
                    {
                        Attribute = "Honors",
                        Value = honors.MembershipId ?? string.Empty
                    });
                }
            }

            var firstLoyalty = ota.LoyaltyPrograms?.FirstOrDefault();

            var memberRequest = new MemberRequest
            {
                Id = "1004",
                Source = "CLARITY",
                SpecVersion = "1.0",
                Type = "MemberCreated",
                CorrelationId = ota.CorrelationID ?? string.Empty,
                Data = new Data
                {
                    SystemMeta = new SystemMeta(),
                    PersonName = new PersonName(),
                    ExternalReference = null,
                    DigitalAddresses = [],
                    PhoneNumbers = [],
                    PostalAddresses = [],
                    MembershipInformation = new Model.Shared.MembershipInformation(),
                    OtherAttributes = null
                }
            };

            memberRequest.Data.SystemMeta.TenantID = "HGV";
            memberRequest.Data.SystemMeta.InsertedBy = "System User";
            memberRequest.Data.SystemMeta.InsertedDateTime = DateTime.UtcNow.ToString();
            memberRequest.Data.SystemMeta.UpdatedBy = "System User";
            memberRequest.Data.SystemMeta.UpdatedDateTime = DateTime.UtcNow.ToString();
            memberRequest.Data.SystemMeta.Status = true;
            memberRequest.Data.SystemMeta.StatusReason = "Active";

            memberRequest.Data.PersonName.FirstName = ota.MemberProfile?.FirstName ?? string.Empty;
            memberRequest.Data.PersonName.LastName = ota.MemberProfile?.LastName ?? string.Empty;

            memberRequest.Data.ExternalReference = externalReference;

            if (ota.ContactInfo?.Emails != null)
            {
                foreach (var e in ota.ContactInfo.Emails)
                {
                    var da = new DigitalAddresses
                    {
                        DigitalAddress = e?.Email ?? string.Empty,
                        DigitalAddressType = e?.EmailType ?? string.Empty,
                        Status = e?.IsPrimary ?? false
                    };
                    memberRequest.Data.DigitalAddresses.Add(da);
                }
            }

            if (ota.ContactInfo?.Phones != null)
            {
                foreach (var p in ota.ContactInfo.Phones)
                {
                    var phone = new PhoneNumber
                    {
                        Number = p?.PhoneNumber ?? string.Empty,
                        PhoneType = p?.PhoneTechType ?? string.Empty,
                        Extension = p?.AreaCode ?? string.Empty,
                        Status = p?.IsPrimary ?? false
                    };
                    memberRequest.Data.PhoneNumbers.Add(phone);
                }
            }

            if (ota.ContactInfo?.Addresses != null)
            {
                foreach (var a in ota.ContactInfo.Addresses)
                {
                    var address = new PostalAddress
                    {
                        Line1 = a?.AddressLine1 ?? string.Empty,
                        Line2 = a?.AddressLine2 ?? string.Empty,
                        City = a?.CityName ?? string.Empty,
                        State = a?.State ?? string.Empty,
                        StateCode = a?.StateProv ?? string.Empty,
                        PostalCode = a?.PostalCode ?? string.Empty,
                        Country = a?.Country ?? string.Empty,
                        CountryCode = a?.CountryCode ?? string.Empty,
                        Latitude = a?.Latitude ?? 0,
                        Longitude = a?.Longitude ?? 0,
                        AddressType = a?.AddressType ?? string.Empty,
                        Status = a?.IsPrimary ?? false
                    };
                    memberRequest.Data.PostalAddresses.Add(address);
                }
            }

            memberRequest.Data.MembershipInformation.PremierGenCode =
                int.TryParse(firstLoyalty?.ProgramCode, out var code) ? code : 0;

            memberRequest.Data.MembershipInformation.AccountNumber =
                firstLoyalty?.MembershipId ?? string.Empty;

            memberRequest.Data.MembershipInformation.OwnerType =
                firstLoyalty?.OwnType ?? string.Empty;

            memberRequest.Data.MembershipInformation.MembershipLevel =
                ota?.MemberProfile?.MemberTier ?? string.Empty;

            memberRequest.Data.OtherAttributes = otherAttributes;

            return memberRequest;

        }

        public static MemberActRequest MapToActivateDeActivateOnexRequest(OTAGetMemberResponse ota)
        {
            if (ota == null)
            {
                return null;
            }

            var partnerSourceSystem = ota.MemberProfile?.SourceSystemIds?.FirstOrDefault(s => s.SourceSystem?.Equals("PARTNER", StringComparison.OrdinalIgnoreCase) == true);
            var otherSourceSystems = ota.MemberProfile?.SourceSystemIds?.Where(s => !s.SourceSystem?.Equals("PARTNER", StringComparison.OrdinalIgnoreCase) == true).ToList();
            var exchangeSourceSystem = ota.MemberProfile?.SourceSystemIds?.FirstOrDefault(s => s.SourceSystem?.Equals("EXCHANGE", StringComparison.OrdinalIgnoreCase) == true);

            var externalReference = partnerSourceSystem != null
                    ? new ExternalReference
                    {
                        SourceSystem = partnerSourceSystem.SourceSystem ?? string.Empty,
                        ReferenceId = partnerSourceSystem.Id ?? string.Empty
                    }
                    : new ExternalReference
                    {
                        SourceSystem = string.Empty,
                        ReferenceId = string.Empty
                    };



            // Assigning other SourceSystemIds to otherAttributes
            var otherAttributes = new List<OtherAttribute>();

            if (otherSourceSystems != null)
            {
                otherAttributes.AddRange(otherSourceSystems.Select(s => new OtherAttribute
                {
                    Attribute = s.SourceSystem ?? string.Empty,
                    Value = s.Id ?? string.Empty
                }));
            }
            if (ota.MemberProfile?.AdditionalIdentifiers != null)
            {
                otherAttributes.AddRange(ota.MemberProfile.AdditionalIdentifiers.Select(ai => new OtherAttribute
                {
                    Attribute = ai.Type ?? string.Empty,
                    Value = ai.Value ?? string.Empty
                }));
            }
            // Add static attributes
            otherAttributes.AddRange(
            [
                new OtherAttribute { Attribute = "birthDate", Value = ota.MemberProfile?.BirthDate ?? string.Empty },
                new OtherAttribute { Attribute = "gender", Value = ota.MemberProfile?.Gender ?? string.Empty },
                new OtherAttribute { Attribute = "profileType", Value = ota.MemberProfile?.ProfileType ?? string.Empty },
                new OtherAttribute { Attribute = "tradingAgreementCode", Value = ota.TradingAgreementCode ?? string.Empty },
                new OtherAttribute { Attribute = "marketingKeyCode", Value = ota.MemberProfile?.MarketingKeyCode ?? string.Empty }
            ]);


            var firstLoyalty = ota.LoyaltyPrograms?.FirstOrDefault();

            return new MemberActRequest
            {
                Id = "1004",
                Source = "CLARITY",
                SpecVersion = "1.0",
                Type = "MemberUpdation",
                CorrelationId = string.IsNullOrWhiteSpace(ota.CorrelationID) ? Guid.NewGuid().ToString() : ota.CorrelationID,
                Datacontenttype = "application/json",
                Dataschema = "https://json-schema.org/draft/2020-12/schema",
                Ispartial = false,
                Subject = "ActivateMember",
                Time = DateTime.UtcNow.ToString(),
                Data = new ActData
                {
                    SystemMeta = new SystemMeta
                    {
                        TenantID = "HGV",
                        InsertedBy = "System User",
                        InsertedDateTime = DateTime.UtcNow.ToString(),
                        UpdatedBy = "System User",
                        UpdatedDateTime = DateTime.UtcNow.ToString(),
                        Status = true,
                        StatusReason = "Active"
                    },
                    MembershipInformation = new Model.Member.MembershipInformation
                    {
                        PremierGenCode = int.TryParse(firstLoyalty?.ProgramCode, out var code) ? code : 0,
                        // TODO- For Activate and DeActivate to work, member Id should be present in Membership stub and in Clarity.
                        // As it is still under development by Clariy team, using the member id "7881332140" which exists in membership stub.
                        // Uncomment the below code once it is developed
                        // AccountNumber = exchangeSourceSystem?.Id,
                        AccountNumber = "7881332140",
                        OwnerType = firstLoyalty?.OwnType ?? string.Empty,
                        MembershipLevel = ota?.MemberProfile?.MemberTier ?? "PLATINUM",
                        PastDue = 1,
                        Points = 2,
                        TravelersPlus = new Model.Member.TravelersPlus
                        {
                            ExpirationDate = DateTime.UtcNow,
                        }
                    }
                }
            };
        }

        public static MemberRequest MapToUpdateOnexRequest(OTAGetMemberResponse ota)
        {
            if (ota == null)
            {
                return null;
            }

            var partnerSourceSystem = ota.MemberProfile?.SourceSystemIds?.FirstOrDefault(s => s.SourceSystem?.Equals("PARTNER", StringComparison.OrdinalIgnoreCase) == true);
            var otherSourceSystems = ota.MemberProfile?.SourceSystemIds?.Where(s => !s.SourceSystem?.Equals("PARTNER", StringComparison.OrdinalIgnoreCase) == true).ToList();
            var exchangeSourceSystem = ota.MemberProfile?.SourceSystemIds?.FirstOrDefault(s => s.SourceSystem?.Equals("EXCHANGE", StringComparison.OrdinalIgnoreCase) == true);

            var externalReference = partnerSourceSystem != null
                    ? new ExternalReference
                    {
                        SourceSystem = partnerSourceSystem.SourceSystem ?? string.Empty,
                        ReferenceId = partnerSourceSystem.Id ?? string.Empty
                    }
                    : new ExternalReference
                    {
                        SourceSystem = string.Empty,
                        ReferenceId = string.Empty
                    };



            // Assigning other SourceSystemIds to otherAttributes
            var otherAttributes = new List<OtherAttribute>();

            if (otherSourceSystems != null)
            {
                otherAttributes.AddRange(otherSourceSystems.Select(s => new OtherAttribute
                {
                    Attribute = s.SourceSystem ?? string.Empty,
                    Value = s.Id ?? string.Empty
                }));
            }
            if (ota.MemberProfile?.AdditionalIdentifiers != null)
            {
                otherAttributes.AddRange(ota.MemberProfile.AdditionalIdentifiers.Select(ai => new OtherAttribute
                {
                    Attribute = ai.Type ?? string.Empty,
                    Value = ai.Value ?? string.Empty
                }));
            }
            // Add static attributes          
            otherAttributes.AddRange(
                [
                    new OtherAttribute { Attribute = "birthDate", Value = string.IsNullOrWhiteSpace(ota.MemberProfile?.BirthDate) ? "" : ota.MemberProfile.BirthDate },
                    new OtherAttribute { Attribute = "gender", Value = string.IsNullOrWhiteSpace(ota.MemberProfile?.Gender) ? "" : ota.MemberProfile.Gender },
                    new OtherAttribute { Attribute = "ProfileType", Value = string.IsNullOrWhiteSpace(ota.MemberProfile?.ProfileType) ? "" : ota.MemberProfile.ProfileType },
                    new OtherAttribute { Attribute = "TradingAgreementCode", Value = string.IsNullOrWhiteSpace(ota.TradingAgreementCode) ? "" : ota.TradingAgreementCode },
                    new OtherAttribute { Attribute = "MarketingKeyCode", Value = string.IsNullOrWhiteSpace(ota.MemberProfile?.MarketingKeyCode) ? "" : ota.MemberProfile.MarketingKeyCode }
                ]);

            var firstLoyalty = ota.LoyaltyPrograms?.FirstOrDefault();

            if (ota.LoyaltyPrograms.Any())
            {
                otherAttributes.Add(new OtherAttribute
                {
                    Attribute = "Honors",
                    Value = ota.LoyaltyPrograms.FirstOrDefault().MembershipId ?? string.Empty
                });
            }

            return new MemberRequest
            {
                Id = "1004",
                Source = "CLARITY",
                SpecVersion = "1.0",
                Type = "MemberCreated",
                CorrelationId = ota.CorrelationID ?? Guid.NewGuid().ToString(),
                Data = new Data
                {
                    SystemMeta = new SystemMeta
                    {
                        TenantID = "HGV",
                        InsertedBy = "System User",
                        InsertedDateTime = DateTime.UtcNow.ToString(),
                        UpdatedBy = "System User",
                        UpdatedDateTime = DateTime.UtcNow.ToString(),
                        Status = true,
                        StatusReason = "Active"
                    },
                    PersonName = new PersonName
                    {
                        FirstName = ota.MemberProfile?.FirstName ?? string.Empty,
                        LastName = ota.MemberProfile?.LastName ?? string.Empty
                    },

                    ExternalReference = externalReference,
                    MemberResponseMessage = new MemberResponseMessage()
                    {
                        MemberStatus = ota.MemberStatus,
                        Message = "Member details updated"
                    },

                    DigitalAddresses = ota.ContactInfo?.Emails?.Select(e => new DigitalAddresses
                    {
                        DigitalAddress = e?.Email ?? string.Empty,
                        DigitalAddressType = e?.EmailType ?? string.Empty,
                        Status = e?.IsPrimary ?? false
                    }).ToList() ?? [],
                    PhoneNumbers = ota.ContactInfo?.Phones?.Select(p => new PhoneNumber
                    {
                        Number = p?.PhoneNumber ?? string.Empty,
                        PhoneType = p?.PhoneTechType.ToUpper() ?? string.Empty,
                        Extension = p?.AreaCode ?? string.Empty,
                        Status = p?.IsPrimary ?? false,
                    }).ToList() ?? [],
                    PostalAddresses = ota.ContactInfo?.Addresses?.Select(a => new PostalAddress
                    {
                        Line1 = a?.AddressLine1 ?? string.Empty,
                        Line2 = a?.AddressLine2 ?? string.Empty,
                        City = a?.CityName ?? string.Empty,
                        State = a?.State ?? string.Empty,
                        StateCode = a?.StateProv ?? string.Empty,
                        PostalCode = a?.PostalCode ?? string.Empty,
                        Country = a?.Country ?? string.Empty,
                        CountryCode = a?.CountryCode ?? string.Empty,
                        Latitude = a?.Latitude ?? 0,
                        Longitude = a?.Longitude ?? 0,
                        AddressType = a?.AddressType ?? string.Empty,
                        Status = a?.IsPrimary ?? false
                    }).ToList() ?? [],
                    MembershipInformation = new Model.Shared.MembershipInformation
                    {
                        PremierGenCode = int.TryParse(firstLoyalty?.ProgramCode, out var code) ? code : 0,
                        AccountNumber = exchangeSourceSystem?.Id,
                        OwnerType = firstLoyalty?.OwnType ?? string.Empty,
                        MembershipLevel = ota?.MemberProfile?.MemberTier ?? string.Empty
                    },
                    OtherAttributes = otherAttributes
                }
            };
        }

        public static OTAPostMemberResponse MapOnexToOtaResponse(MemberResponse onexResponse)
        {
            if (onexResponse == null || onexResponse.Data == null)
            {
                return null;
            }

            var sourceSystemIds = new List<SourceSystemId>();

            if (onexResponse.Data.ExternalReference != null)
            {
                sourceSystemIds.Add(new SourceSystemId
                {
                    SourceSystem = onexResponse.Data.ExternalReference.SourceSystem ?? string.Empty,
                    Id = onexResponse.Data.ExternalReference.ReferenceId ?? string.Empty
                });
                var memberId = onexResponse.Data.MemberResponseMessage.MemberInfo.MemberId ?? string.Empty;
                sourceSystemIds.Add(new SourceSystemId
                {
                    SourceSystem = "EXCHANGE",
                    Id = memberId
                });
            }

            var otherAttrs = onexResponse.Data.OtherAttributes?.ToDictionary(a => a.Attribute, a => a.Value)
                 ?? [];

            var tradingAgreementCode = otherAttrs.TryGetValue("tradingAgreementCode", out var tacValue)
                                       ? tacValue
                                       : string.Empty;
            return new OTAPostMemberResponse
            {
                CorrelationID = onexResponse.CorrelationId ?? string.Empty,
                TradingAgreementCode = tradingAgreementCode,
                MemberProfile = new OTAMemberProfile
                {
                    SourceSystemIds = sourceSystemIds
                },
                MemberStatus = onexResponse.Data.MemberResponseMessage?.MemberStatus ?? string.Empty,
                Message = onexResponse.Data.MemberResponseMessage?.Message ?? string.Empty
            };
        }

        public static OTAPostMemberResponse MapOnexToUpdateOtaResponse(MemberResponse onexResponse)
        {
            if (onexResponse == null || onexResponse.Data == null)
            {
                return null;
            }

            var sourceSystemIds = new List<SourceSystemId>();

            // OtherAttributes items
            var otherAttrs = onexResponse.Data.OtherAttributes?
                .ToDictionary(a => a.Attribute, a => a.Value, StringComparer.OrdinalIgnoreCase)
                ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            var knownSystems = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "PARTNER", "EXCHANGE", "INTERNAL", "EXTERNAL", "LEGACY", "VOICE_REFERENCE_NUMBER"
            };

            var partnerUsed = false;

            // 1. Use ExternalReference if it's explicitly PARTNER
            var external = onexResponse.Data.ExternalReference;
            if (external?.SourceSystem?.Equals("PARTNER", StringComparison.OrdinalIgnoreCase) == true)
            {
                partnerUsed = true;
                sourceSystemIds.Add(new SourceSystemId
                {
                    SourceSystem = external.SourceSystem ?? string.Empty,
                    Id = external.ReferenceId ?? string.Empty
                });
            }

            // 2. check in OtherAttributes for the source systems
            if (!partnerUsed)
            {
                // Prefer PARTNER if present in attributes, otherwise pick the first source system 
                string matchedKey = null;

                if (otherAttrs.ContainsKey("PARTNER"))
                {
                    matchedKey = "PARTNER";
                }
                else
                {
                    matchedKey = otherAttrs.Keys.FirstOrDefault(knownSystems.Contains);
                }

                if (!string.IsNullOrWhiteSpace(matchedKey) && otherAttrs.TryGetValue(matchedKey, out var matchedId))
                {
                    sourceSystemIds.Add(new SourceSystemId
                    {
                        SourceSystem = matchedKey,
                        Id = matchedId ?? string.Empty
                    });
                }
            }

            var memberId = onexResponse.Data.MemberResponseMessage?.MemberInfo?.MemberId ?? string.Empty;

            // Add EXCHANGE only if not already added
            if (!sourceSystemIds.Any(x => x.SourceSystem.Equals("EXCHANGE", StringComparison.OrdinalIgnoreCase)))
            {
                sourceSystemIds.Add(new SourceSystemId
                {
                    SourceSystem = "EXCHANGE",
                    Id = memberId
                });
            }
            var tradingAgreementCode = otherAttrs.TryGetValue("tradingAgreementCode", out var tacValue)
                                       ? tacValue
                                       : string.Empty;
            return new OTAPostMemberResponse
            {
                CorrelationID = string.IsNullOrWhiteSpace(onexResponse.CorrelationId) ? Guid.NewGuid().ToString() : onexResponse.CorrelationId,
                TradingAgreementCode = tradingAgreementCode,
                MemberProfile = new OTAMemberProfile
                {
                    SourceSystemIds = sourceSystemIds
                },
                MemberStatus = onexResponse.Data.MemberResponseMessage?.MemberStatus ?? string.Empty,
                Message = onexResponse.Data.MemberResponseMessage?.Message ?? string.Empty
            };
        }

        public static OTAPostMemberResponse MapOnexToUpdateOtaResponse(MemberActResponse onexResponse, OTAGetMemberResponse otaGetResponse, bool isActivate)
        {
            if (onexResponse == null || onexResponse.Data == null)
            {
                return null;
            }

            var allowedSystems = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "PARTNER", "EXCHANGE", "INTERNAL", "EXTERNAL", "LEGACY", "VOICE_REFERENCE_NUMBER"
            };

            var sourceSystemIds = new List<SourceSystemId>();

            // 1. Take directly from otaGetResponse.MemberProfile.SourceSystemIds as Boomi is not sending in the update response.
            if (otaGetResponse?.MemberProfile?.SourceSystemIds != null)
            {
                foreach (var ss in otaGetResponse.MemberProfile.SourceSystemIds)
                {
                    if (!string.IsNullOrWhiteSpace(ss.SourceSystem) &&
                        !string.IsNullOrWhiteSpace(ss.Id) &&
                        allowedSystems.Contains(ss.SourceSystem))
                    {
                        sourceSystemIds.Add(new SourceSystemId
                        {
                            SourceSystem = ss.SourceSystem,
                            Id = ss.Id
                        });
                    }
                }
            }

            // 2. Add from AdditionalIdentifiers (only if not already added)
            if (otaGetResponse?.MemberProfile?.AdditionalIdentifiers != null)
            {
                foreach (var id in otaGetResponse.MemberProfile.AdditionalIdentifiers)
                {
                    if (!string.IsNullOrWhiteSpace(id.Type) &&
                        !string.IsNullOrWhiteSpace(id.Value) &&
                        allowedSystems.Contains(id.Type) &&
                        !sourceSystemIds.Any(x => x.SourceSystem.Equals(id.Type, StringComparison.OrdinalIgnoreCase)))
                    {
                        sourceSystemIds.Add(new SourceSystemId
                        {
                            SourceSystem = id.Type,
                            Id = id.Value
                        });
                    }
                }
            }
            string memberStatus;
            if (!string.IsNullOrWhiteSpace(onexResponse.Data.MemberResponseMessage?.MemberStatus))
            {
                memberStatus = onexResponse.Data.MemberResponseMessage.MemberStatus;
            }
            else
            {
                memberStatus = isActivate ? "Active" : "InActive";
            }

            return new OTAPostMemberResponse
            {
                CorrelationID = string.IsNullOrWhiteSpace(onexResponse.CorrelationId) ? Guid.NewGuid().ToString() : onexResponse.CorrelationId,
                TradingAgreementCode = otaGetResponse.TradingAgreementCode ?? string.Empty,
                MemberProfile = new OTAMemberProfile
                {
                    SourceSystemIds = sourceSystemIds
                },
                MemberStatus = memberStatus,

                Message = onexResponse.Data.MemberResponseMessage?.Message ?? string.Empty
            };
        }
        public static OTAGetMemberResponse MapToOTAResponse(OnexGetMemberResponse source)
        {
            if (source == null)
            {
                return null;
            }

            var otherAttrs = source.Data?.OtherAttributes?.ToDictionary(a => a.Attribute, a => a.Value, StringComparer.OrdinalIgnoreCase) ?? [];

            var sourceSystemIds = new List<SourceSystemId>();
            if (source.Data?.ExternalReference != null)
            {
                sourceSystemIds.Add(new SourceSystemId
                {
                    SourceSystem = source.Data.ExternalReference.SourceSystem ?? "",
                    Id = source.Data.ExternalReference.ReferenceId ?? ""
                });
            }

            // Fields that should NOT go into additionalIdentifiers
            var reservedKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "birthDate",
                "gender",
                "profileType",
                "marketingKeyCode",
                "tradingAgreementCode",
                "Honors",
                "VOICE_REFERENCE_NUMBER"
            };

            // Map additionalIdentifiers (exclude reserved keys)
            var additionalIdentifiers = source.Data?.OtherAttributes?
                .Where(a => !reservedKeys.Contains(a.Attribute))
                .Select(a => new AdditionalIdentifier
                {
                    Type = a.Attribute,
                    Value = a.Value
                })
                .ToList() ?? [];

            if (otherAttrs.TryGetValue("VOICE_REFERENCE_NUMBER", out var voiceRef) && !string.IsNullOrWhiteSpace(voiceRef))
            {
                sourceSystemIds.Add(new SourceSystemId
                {
                    SourceSystem = "VOICE_REFERENCE_NUMBER",
                    Id = voiceRef
                });
            }


            var emails = source.Data?.DigitalAddresses
             ?.ConvertAll(d => new Emails
             {
                 Email = d.DigitalAddress ?? "",
                 EmailType = d.DigitalAddressType ?? "",
                 IsPrimary = d.Status ?? false
             }) ?? [];


            var phones = source.Data?.PhoneNumbers?.ConvertAll(p => new Phone
            {
                PhoneNumber = p.Number,
                PhoneTechType = p.PhoneType,
                AreaCode = p.Extension ?? "",
                IsPrimary = p.Status ?? false
            }) ?? [];


            var addresses = source.Data?.PostalAddresses?.ConvertAll(a => new Address
            {
                AddressType = a.AddressType,
                AddressLine1 = a.Line1,
                AddressLine2 = a.Line2,
                CityName = a.City,
                PostalCode = a.PostalCode,
                StateProv = a.StateCode,
                State = a.State,
                CountryCode = a.CountryCode,
                Country = a.Country,
                Latitude = a.Latitude,
                Longitude = a.Longitude,
                IsPrimary = a.Status ?? false
            }) ?? [];

            // Map loyalty programs

            var loyaltyPrograms = new List<LoyaltyProgram>();

            if (source.Data?.MembershipInformation != null)
            {
                var programCode = source.Data.MembershipInformation.PremierGenCode?.ToString();

                // Only add loyalty program if ProgramCode exists
                if (!string.IsNullOrWhiteSpace(programCode))
                {
                    loyaltyPrograms.Add(new LoyaltyProgram
                    {
                        ProgramCode = programCode,
                        OwnType = source.Data.MembershipInformation.OwnerType ?? "",
                        MembershipId = source.Data.MembershipInformation.AccountNumber ?? ""
                    });
                }
            }


            if (otherAttrs.TryGetValue("Honors", out var honorsValue) && !string.IsNullOrWhiteSpace(honorsValue))
            {
                loyaltyPrograms.Add(new LoyaltyProgram
                {
                    ProgramCode = "HONORS",
                    OwnType = "Individual",
                    MembershipId = honorsValue
                });
            }
            // Build the final OTAMemberResponse for Get
            return new OTAGetMemberResponse
            {
                CorrelationID = source.CorrelationId ?? "",
                TradingAgreementCode = otherAttrs.TryGetValue("tradingAgreementCode", out var value4) ? value4 : $"{source.Data.SystemMeta.TenantID}-ARV-{source.Data.SystemMeta.TenantID}",
                MemberProfile = new MemberProfile
                {
                    SourceSystemIds = sourceSystemIds,
                    FirstName = source.Data?.PersonName?.FirstName ?? "",
                    LastName = source.Data?.PersonName?.LastName ?? "",
                    BirthDate = otherAttrs.TryGetValue("birthDate", out var value) ? value : "",
                    Gender = otherAttrs.TryGetValue("gender", out var value1) ? value1 : "",
                    MarketingKeyCode = otherAttrs.TryGetValue("marketingKeyCode", out var value3) ? value3 : "",
                },
                ContactInfo = new ContactInfo
                {
                    Emails = emails,
                    Phones = phones,
                    Addresses = addresses
                },
                MemberStatus = source.Data?.MemberResponseMessage?.MemberStatus ?? "", // To be checked. Added in the user story 851068.
                LoyaltyPrograms = loyaltyPrograms
            };
        }
        public static OTAErrorResponse MapBackendError(Error error = null, HttpResponseMessage backendResponse = null, HttpStatusCode defaultStatusCode = HttpStatusCode.BadRequest, List<string> validationMessages = null)
        {
            // 1️. Determine instance ID
            var instanceId = !string.IsNullOrWhiteSpace(error?.ErrorInstance)
                ? error.ErrorInstance
                : Guid.NewGuid().ToString();

            // 2️. Determine the status code
            var effectiveStatusCode = defaultStatusCode;

            if (error != null)
            {
                // Case: error.ErrorCode is numeric (e.g., "404")
                if (int.TryParse(error.ErrorCode, out var numericCode) &&
                    Enum.IsDefined(typeof(HttpStatusCode), numericCode))
                {
                    effectiveStatusCode = (HttpStatusCode)numericCode;
                }
                // Case: error.ErrorCode is textual (e.g., "NotFound")
                else if (Enum.TryParse<HttpStatusCode>(error.ErrorCode, true, out var parsedStatus))
                {
                    effectiveStatusCode = parsedStatus;
                }
            }
            else if (backendResponse != null)
            {
                effectiveStatusCode = backendResponse.StatusCode;
            }

            // 3️. Build the error response
            return new OTAErrorResponse
            {
                Type = effectiveStatusCode switch
                {
                    HttpStatusCode.BadRequest => "https://api.hgv.com/errors/bad-request",
                    HttpStatusCode.Unauthorized => "https://api.hgv.com/errors/unauthorized",
                    HttpStatusCode.Forbidden => "https://api.hgv.com/errors/forbidden",
                    HttpStatusCode.NotFound => "https://api.hgv.com/errors/not-found",
                    HttpStatusCode.InternalServerError => "https://api.hgv.com/errors/internal-server-error",
                    HttpStatusCode.PaymentRequired => "https://api.hgv.com/errors/payment-required",
                    HttpStatusCode.NotImplemented => "https://api.hgv.com/errors/not-implemented",
                    HttpStatusCode.BadGateway => "https://api.hgv.com/errors/bad-gateway",
                    HttpStatusCode.TooManyRequests => "https://api.hgv.com/errors/too-many-requests",
                    HttpStatusCode.Continue => throw new NotImplementedException(),
                    HttpStatusCode.SwitchingProtocols => throw new NotImplementedException(),
                    HttpStatusCode.Processing => throw new NotImplementedException(),
                    HttpStatusCode.EarlyHints => throw new NotImplementedException(),
                    HttpStatusCode.OK => throw new NotImplementedException(),
                    HttpStatusCode.Created => throw new NotImplementedException(),
                    HttpStatusCode.Accepted => throw new NotImplementedException(),
                    HttpStatusCode.NonAuthoritativeInformation => throw new NotImplementedException(),
                    HttpStatusCode.NoContent => throw new NotImplementedException(),
                    HttpStatusCode.ResetContent => throw new NotImplementedException(),
                    HttpStatusCode.PartialContent => throw new NotImplementedException(),
                    HttpStatusCode.MultiStatus => throw new NotImplementedException(),
                    HttpStatusCode.AlreadyReported => throw new NotImplementedException(),
                    HttpStatusCode.IMUsed => throw new NotImplementedException(),
                    HttpStatusCode.Ambiguous => throw new NotImplementedException(),
                    HttpStatusCode.Moved => throw new NotImplementedException(),
                    HttpStatusCode.Found => throw new NotImplementedException(),
                    HttpStatusCode.RedirectMethod => throw new NotImplementedException(),
                    HttpStatusCode.NotModified => throw new NotImplementedException(),
                    HttpStatusCode.UseProxy => throw new NotImplementedException(),
                    HttpStatusCode.Unused => throw new NotImplementedException(),
                    HttpStatusCode.RedirectKeepVerb => throw new NotImplementedException(),
                    HttpStatusCode.PermanentRedirect => throw new NotImplementedException(),
                    HttpStatusCode.MethodNotAllowed => throw new NotImplementedException(),
                    HttpStatusCode.NotAcceptable => throw new NotImplementedException(),
                    HttpStatusCode.ProxyAuthenticationRequired => throw new NotImplementedException(),
                    HttpStatusCode.RequestTimeout => throw new NotImplementedException(),
                    HttpStatusCode.Conflict => throw new NotImplementedException(),
                    HttpStatusCode.Gone => throw new NotImplementedException(),
                    HttpStatusCode.LengthRequired => throw new NotImplementedException(),
                    HttpStatusCode.PreconditionFailed => throw new NotImplementedException(),
                    HttpStatusCode.RequestEntityTooLarge => throw new NotImplementedException(),
                    HttpStatusCode.RequestUriTooLong => throw new NotImplementedException(),
                    HttpStatusCode.UnsupportedMediaType => throw new NotImplementedException(),
                    HttpStatusCode.RequestedRangeNotSatisfiable => throw new NotImplementedException(),
                    HttpStatusCode.ExpectationFailed => throw new NotImplementedException(),
                    HttpStatusCode.MisdirectedRequest => throw new NotImplementedException(),
                    HttpStatusCode.UnprocessableEntity => throw new NotImplementedException(),
                    HttpStatusCode.Locked => throw new NotImplementedException(),
                    HttpStatusCode.FailedDependency => throw new NotImplementedException(),
                    HttpStatusCode.UpgradeRequired => throw new NotImplementedException(),
                    HttpStatusCode.PreconditionRequired => throw new NotImplementedException(),
                    HttpStatusCode.RequestHeaderFieldsTooLarge => throw new NotImplementedException(),
                    HttpStatusCode.UnavailableForLegalReasons => throw new NotImplementedException(),
                    HttpStatusCode.ServiceUnavailable => throw new NotImplementedException(),
                    HttpStatusCode.GatewayTimeout => throw new NotImplementedException(),
                    HttpStatusCode.HttpVersionNotSupported => throw new NotImplementedException(),
                    HttpStatusCode.VariantAlsoNegotiates => throw new NotImplementedException(),
                    HttpStatusCode.InsufficientStorage => throw new NotImplementedException(),
                    HttpStatusCode.LoopDetected => throw new NotImplementedException(),
                    HttpStatusCode.NotExtended => throw new NotImplementedException(),
                    HttpStatusCode.NetworkAuthenticationRequired => throw new NotImplementedException(),
                    _ => "https://api.hgv.com/errors/unexpected-error"
                },
                Title = effectiveStatusCode switch
                {
                    HttpStatusCode.BadRequest => "Bad Request",
                    HttpStatusCode.Unauthorized => "Unauthorized",
                    HttpStatusCode.PaymentRequired => "Payment Required",
                    HttpStatusCode.Forbidden => "Forbidden",
                    HttpStatusCode.NotFound => "Not Found",
                    HttpStatusCode.NotAcceptable => "Not Acceptable",
                    HttpStatusCode.TooManyRequests => "Too Many Requests",
                    HttpStatusCode.InternalServerError => "Internal Server Error",
                    HttpStatusCode.NotImplemented => "Not Implemented",
                    HttpStatusCode.BadGateway => "Bad Gateway",
                    HttpStatusCode.Continue => throw new NotImplementedException(),
                    HttpStatusCode.SwitchingProtocols => throw new NotImplementedException(),
                    HttpStatusCode.Processing => throw new NotImplementedException(),
                    HttpStatusCode.EarlyHints => throw new NotImplementedException(),
                    HttpStatusCode.OK => throw new NotImplementedException(),
                    HttpStatusCode.Created => throw new NotImplementedException(),
                    HttpStatusCode.Accepted => throw new NotImplementedException(),
                    HttpStatusCode.NonAuthoritativeInformation => throw new NotImplementedException(),
                    HttpStatusCode.NoContent => throw new NotImplementedException(),
                    HttpStatusCode.ResetContent => throw new NotImplementedException(),
                    HttpStatusCode.PartialContent => throw new NotImplementedException(),
                    HttpStatusCode.MultiStatus => throw new NotImplementedException(),
                    HttpStatusCode.AlreadyReported => throw new NotImplementedException(),
                    HttpStatusCode.IMUsed => throw new NotImplementedException(),
                    HttpStatusCode.Ambiguous => throw new NotImplementedException(),
                    HttpStatusCode.Moved => throw new NotImplementedException(),
                    HttpStatusCode.Found => throw new NotImplementedException(),
                    HttpStatusCode.RedirectMethod => throw new NotImplementedException(),
                    HttpStatusCode.NotModified => throw new NotImplementedException(),
                    HttpStatusCode.UseProxy => throw new NotImplementedException(),
                    HttpStatusCode.Unused => throw new NotImplementedException(),
                    HttpStatusCode.RedirectKeepVerb => throw new NotImplementedException(),
                    HttpStatusCode.PermanentRedirect => throw new NotImplementedException(),
                    HttpStatusCode.MethodNotAllowed => throw new NotImplementedException(),
                    HttpStatusCode.ProxyAuthenticationRequired => throw new NotImplementedException(),
                    HttpStatusCode.RequestTimeout => throw new NotImplementedException(),
                    HttpStatusCode.Conflict => throw new NotImplementedException(),
                    HttpStatusCode.Gone => throw new NotImplementedException(),
                    HttpStatusCode.LengthRequired => throw new NotImplementedException(),
                    HttpStatusCode.PreconditionFailed => throw new NotImplementedException(),
                    HttpStatusCode.RequestEntityTooLarge => throw new NotImplementedException(),
                    HttpStatusCode.RequestUriTooLong => throw new NotImplementedException(),
                    HttpStatusCode.UnsupportedMediaType => throw new NotImplementedException(),
                    HttpStatusCode.RequestedRangeNotSatisfiable => throw new NotImplementedException(),
                    HttpStatusCode.ExpectationFailed => throw new NotImplementedException(),
                    HttpStatusCode.MisdirectedRequest => throw new NotImplementedException(),
                    HttpStatusCode.UnprocessableEntity => throw new NotImplementedException(),
                    HttpStatusCode.Locked => throw new NotImplementedException(),
                    HttpStatusCode.FailedDependency => throw new NotImplementedException(),
                    HttpStatusCode.UpgradeRequired => throw new NotImplementedException(),
                    HttpStatusCode.PreconditionRequired => throw new NotImplementedException(),
                    HttpStatusCode.RequestHeaderFieldsTooLarge => throw new NotImplementedException(),
                    HttpStatusCode.UnavailableForLegalReasons => throw new NotImplementedException(),
                    HttpStatusCode.ServiceUnavailable => throw new NotImplementedException(),
                    HttpStatusCode.GatewayTimeout => throw new NotImplementedException(),
                    HttpStatusCode.HttpVersionNotSupported => throw new NotImplementedException(),
                    HttpStatusCode.VariantAlsoNegotiates => throw new NotImplementedException(),
                    HttpStatusCode.InsufficientStorage => throw new NotImplementedException(),
                    HttpStatusCode.LoopDetected => throw new NotImplementedException(),
                    HttpStatusCode.NotExtended => throw new NotImplementedException(),
                    HttpStatusCode.NetworkAuthenticationRequired => throw new NotImplementedException(),
                    _ => "Unexpected Error"
                },
                Status = (int)effectiveStatusCode,
                Detail = effectiveStatusCode switch
                {
                    HttpStatusCode.BadRequest => validationMessages != null && validationMessages.Any()
                        ? $"Invalid or missing parameters - {string.Join("; ", validationMessages)}"
                        : "Invalid or missing parameters.",
                    HttpStatusCode.Unauthorized => "Missing or invalid authentication credentials.",
                    HttpStatusCode.PaymentRequired => "A valid subscription is required to access this resource.",
                    HttpStatusCode.Forbidden => "You do not have permission to access this resource.",
                    HttpStatusCode.NotFound => "No member profile found for the specified memberId.",
                    HttpStatusCode.NotAcceptable => "The requested content type is not supported.",
                    HttpStatusCode.TooManyRequests => "Rate limit exceeded. Please try again after the retry-after period.",
                    HttpStatusCode.InternalServerError => validationMessages != null && validationMessages.Any()
                        ? $"An unexpected error occurred on the server - {string.Join("; ", validationMessages)}"
                        : "An unexpected error occurred on the server.",
                    HttpStatusCode.NotImplemented => "The requested functionality is not supported.",
                    HttpStatusCode.BadGateway => "Invalid response from an upstream server.",
                    HttpStatusCode.Continue => throw new NotImplementedException(),
                    HttpStatusCode.SwitchingProtocols => throw new NotImplementedException(),
                    HttpStatusCode.Processing => throw new NotImplementedException(),
                    HttpStatusCode.EarlyHints => throw new NotImplementedException(),
                    HttpStatusCode.OK => throw new NotImplementedException(),
                    HttpStatusCode.Created => throw new NotImplementedException(),
                    HttpStatusCode.Accepted => throw new NotImplementedException(),
                    HttpStatusCode.NonAuthoritativeInformation => throw new NotImplementedException(),
                    HttpStatusCode.NoContent => throw new NotImplementedException(),
                    HttpStatusCode.ResetContent => throw new NotImplementedException(),
                    HttpStatusCode.PartialContent => throw new NotImplementedException(),
                    HttpStatusCode.MultiStatus => throw new NotImplementedException(),
                    HttpStatusCode.AlreadyReported => throw new NotImplementedException(),
                    HttpStatusCode.IMUsed => throw new NotImplementedException(),
                    HttpStatusCode.Ambiguous => throw new NotImplementedException(),
                    HttpStatusCode.Moved => throw new NotImplementedException(),
                    HttpStatusCode.Found => throw new NotImplementedException(),
                    HttpStatusCode.RedirectMethod => throw new NotImplementedException(),
                    HttpStatusCode.NotModified => throw new NotImplementedException(),
                    HttpStatusCode.UseProxy => throw new NotImplementedException(),
                    HttpStatusCode.Unused => throw new NotImplementedException(),
                    HttpStatusCode.RedirectKeepVerb => throw new NotImplementedException(),
                    HttpStatusCode.PermanentRedirect => throw new NotImplementedException(),
                    HttpStatusCode.MethodNotAllowed => throw new NotImplementedException(),
                    HttpStatusCode.ProxyAuthenticationRequired => throw new NotImplementedException(),
                    HttpStatusCode.RequestTimeout => throw new NotImplementedException(),
                    HttpStatusCode.Conflict => throw new NotImplementedException(),
                    HttpStatusCode.Gone => throw new NotImplementedException(),
                    HttpStatusCode.LengthRequired => throw new NotImplementedException(),
                    HttpStatusCode.PreconditionFailed => throw new NotImplementedException(),
                    HttpStatusCode.RequestEntityTooLarge => throw new NotImplementedException(),
                    HttpStatusCode.RequestUriTooLong => throw new NotImplementedException(),
                    HttpStatusCode.UnsupportedMediaType => throw new NotImplementedException(),
                    HttpStatusCode.RequestedRangeNotSatisfiable => throw new NotImplementedException(),
                    HttpStatusCode.ExpectationFailed => throw new NotImplementedException(),
                    HttpStatusCode.MisdirectedRequest => throw new NotImplementedException(),
                    HttpStatusCode.UnprocessableEntity => throw new NotImplementedException(),
                    HttpStatusCode.Locked => throw new NotImplementedException(),
                    HttpStatusCode.FailedDependency => throw new NotImplementedException(),
                    HttpStatusCode.UpgradeRequired => throw new NotImplementedException(),
                    HttpStatusCode.PreconditionRequired => throw new NotImplementedException(),
                    HttpStatusCode.RequestHeaderFieldsTooLarge => throw new NotImplementedException(),
                    HttpStatusCode.UnavailableForLegalReasons => throw new NotImplementedException(),
                    HttpStatusCode.ServiceUnavailable => throw new NotImplementedException(),
                    HttpStatusCode.GatewayTimeout => throw new NotImplementedException(),
                    HttpStatusCode.HttpVersionNotSupported => throw new NotImplementedException(),
                    HttpStatusCode.VariantAlsoNegotiates => throw new NotImplementedException(),
                    HttpStatusCode.InsufficientStorage => throw new NotImplementedException(),
                    HttpStatusCode.LoopDetected => throw new NotImplementedException(),
                    HttpStatusCode.NotExtended => throw new NotImplementedException(),
                    HttpStatusCode.NetworkAuthenticationRequired => throw new NotImplementedException(),
                    _ => "An unexpected error occurred."
                },
                Instance = $"https://api.hgv.com/errors/{instanceId}"
            };
        }

    }
}

