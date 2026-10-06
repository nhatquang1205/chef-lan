using ChefLan.Domain.Courses.Entities;
using ChefLan.Logic.Courses.Video;
using Microsoft.Extensions.Options;

namespace ChefLan.Infrastructure.Services.Video;

/// <summary>
/// SKELETON — not implemented yet, and the reason this port exists.
///
/// An unlisted YouTube or Vimeo link is obscurity, not access control: the moment a paying
/// customer sees the URL they can paste it into a Facebook group, which undoes the entire
/// migration away from Facebook groups. This is the core business risk of the product.
///
/// When that happens, implement this: mint a short-TTL signed URL per request (Bunny,
/// CloudFront) or a Vimeo domain-restricted private embed, and register it in place of
/// PassthroughVideoLinkResolver. Because every response goes through IVideoLinkResolver,
/// that swap touches no endpoint, no DTO and no table.
///
/// customerId is passed in so the signature can be bound to the viewer, which is what
/// makes a leaked URL useless to anyone else.
/// </summary>
internal sealed class SignedCdnVideoLinkResolver(IOptions<VideoDeliveryOptions> options) : IVideoLinkResolver
{
    private readonly VideoDeliveryOptions _options = options.Value;

    public ValueTask<string?> ResolveAsync(
        Lesson lesson,
        long customerId,
        CancellationToken cancellationToken = default) =>
        throw new NotImplementedException(
            "SignedCdnVideoLinkResolver — not wired up yet; PassthroughVideoLinkResolver is the default.");
}
