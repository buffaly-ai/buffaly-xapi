# XClient.cs

`XClient` is the core HTTP client for X/Twitter API operations used by the OpsAgent TwitterXApi skill.

All current operations use OAuth 2.0 user-context bearer authorization. Media upload posts binary files to the X API v2 `/2/media/upload` endpoint, parses the returned media id, and then creates the tweet through `/2/tweets` with a `media.media_ids` payload. OAuth 1.0a signing and the v1.1 `upload.twitter.com` media path are no longer used.
