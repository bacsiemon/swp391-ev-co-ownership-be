using FluentValidation;

namespace EvCoOwnership.Repositories.DTOs.UpgradeVoteDTOs
{
    #region Upgrade Vote Request DTOs

    public class CreateUpgradeVoteRequest
    {
        public int UpgradeProposalId { get; set; }
        public string VoteType { get; set; } = string.Empty; // "approve", "reject"
        public string? Comments { get; set; }
    }

    public class CreateUpgradeVoteRequestValidator : AbstractValidator<CreateUpgradeVoteRequest>
    {
        public CreateUpgradeVoteRequestValidator()
        {
            RuleFor(x => x.UpgradeProposalId)
                .GreaterThan(0)
                .WithMessage("Upgrade proposal ID must be valid");

            RuleFor(x => x.VoteType)
                .NotEmpty()
                .WithMessage("Vote type is required")
                .Must(x => x == "approve" || x == "reject")
                .WithMessage("Vote type must be 'approve' or 'reject'");

            RuleFor(x => x.Comments)
                .MaximumLength(500)
                .WithMessage("Comments cannot exceed 500 characters");
        }
    }

    public class UpdateUpgradeVoteRequest
    {
        public string VoteType { get; set; } = string.Empty;
        public string? Comments { get; set; }
    }

    public class UpdateUpgradeVoteRequestValidator : AbstractValidator<UpdateUpgradeVoteRequest>
    {
        public UpdateUpgradeVoteRequestValidator()
        {
            RuleFor(x => x.VoteType)
                .NotEmpty()
                .WithMessage("Vote type is required")
                .Must(x => x == "approve" || x == "reject")
                .WithMessage("Vote type must be 'approve' or 'reject'");

            RuleFor(x => x.Comments)
                .MaximumLength(500)
                .WithMessage("Comments cannot exceed 500 characters");
        }
    }

    #endregion

    #region Upgrade Vote Response DTOs

    public class UpgradeVoteResponse
    {
        public int VoteId { get; set; }
        public int UpgradeProposalId { get; set; }
        public int VoterId { get; set; }
        public string VoterName { get; set; } = string.Empty;
        public string VoteType { get; set; } = string.Empty;
        public string? Comments { get; set; }
        public DateTime VotedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class UpgradeVoteSummaryResponse
    {
        public int UpgradeProposalId { get; set; }
        public string ProposalTitle { get; set; } = string.Empty;
        public int TotalVotes { get; set; }
        public int ApprovalVotes { get; set; }
        public int RejectionVotes { get; set; }
        public decimal ApprovalPercentage { get; set; }
        public string Status { get; set; } = string.Empty; // "pending", "approved", "rejected"
        public bool HasUserVoted { get; set; }
        public string? UserVoteType { get; set; }
        public List<UpgradeVoteResponse> Votes { get; set; } = new();
        public DateTime CreatedAt { get; set; }
        public DateTime? DeadlineAt { get; set; }
    }

    #endregion
}