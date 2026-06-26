# XClient.cs

`XClient` is the core HTTP client for X/Twitter API operations used by the OpsAgent TwitterXApi skill.

All current operations use OAuth 2.0 user-context bearer authorization. Media upload posts binary files to the X API v2 `/2/media/upload` endpoint with `media`, `media_category`, and `media_type` multipart fields, parses the returned media id, and then creates the tweet through `/2/tweets` with a `media.media_ids` payload. OAuth 1.0a signing and the v1.1 `upload.twitter.com` media path are no longer used.

## Add Reply Posting Support (2026-06-23)
- Added PostTweetReplyAsync(...) and PostTweetReplyRawJsonAsync(...) so callers can create reply chains/threads through the X v2 tweet creation endpoint.


## Move Thread Posting Loop Into XClient (2026-06-26)
- Added PostThreadRawJsonAsync(threadJson, cancellationToken) so Buffaly ProtoScript wrappers can delegate thread reply-chain looping to C# instead of using unsupported ProtoScript or statements.

