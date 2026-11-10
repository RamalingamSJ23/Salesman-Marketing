using System;

namespace MarketingApi.DTOs.Salesman
{
    public class ActiveTeamAssignmentDto
    {
        public long TeamId { get; set; }
        public string TeamCode { get; set; } = string.Empty;
        public string TeamName { get; set; } = string.Empty;
        public string RoleInTeam { get; set; } = string.Empty; // "Leader" or "Member"
        public string Role => RoleInTeam;
        public bool IsLeader => RoleInTeam == "Leader";
        public bool IsTeamLeader => RoleInTeam == "Leader";
    }

    public class SalesmanDeactivationRequestDto
    {
        public long Id { get; set; }
        public long SalesmanId { get; set; }
        public string SalesmanCode { get; set; } = string.Empty;
        public string SalesmanName { get; set; } = string.Empty;
        public long? TeamId { get; set; }
        public string? TeamCode { get; set; }
        public string? TeamName { get; set; }
        public string RoleInTeam { get; set; } = string.Empty; // "Leader" or "Member"
        public string Role => RoleInTeam;
        public bool IsLeader => RoleInTeam == "Leader";
        public string? Reason { get; set; }
        public string RequestedBy { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty; // "Pending", "Approved", "Rejected"
        public DateTime RequestedDate { get; set; }
        public DateTime? ProcessedDate { get; set; }
        public string? ProcessedBy { get; set; }
        public string? Notes { get; set; }
    }

    public class DeactivateSalesmanResultDto
    {
        public bool DirectDeactivation { get; set; }
        public bool RequestSentToMarketingMaster { get; set; }
        public bool RequiresApproval => RequestSentToMarketingMaster;
        public bool ImmediateDeactivation => DirectDeactivation;
        public string Message { get; set; } = string.Empty;
        public SalesmanDto? Salesman { get; set; }
        public SalesmanDeactivationRequestDto? DeactivationRequest { get; set; }
        public long? RequestId => DeactivationRequest?.Id;
    }

    public class ProcessDeactivationRequestDto
    {
        public string? Notes { get; set; }
    }
}
