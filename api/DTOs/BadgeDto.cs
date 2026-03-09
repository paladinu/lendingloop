using Api.Models;

namespace Api.DTOs;

public class BadgeDto
{
    public string BadgeType { get; set; } = string.Empty;
    public DateTime AwardedAt { get; set; }
    
    public static BadgeDto FromBadgeAward(BadgeAward badgeAward)
    {
        return new BadgeDto
        {
            BadgeType = badgeAward.BadgeType.ToString(),
            AwardedAt = badgeAward.AwardedAt
        };
    }
}
