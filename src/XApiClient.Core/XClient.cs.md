# XClient.cs

`XClient` is the core HTTP client for X/Twitter API operations used by the OpsAgent TwitterXApi skill.

Media upload support posts binary files to `https://upload.twitter.com/1.1/media/upload.json` using OAuth 1.0a user-context authentication, parses `media_id_string`, and then creates the tweet through `/2/tweets` with a `media.media_ids` payload. Multipart upload body fields are intentionally not included in the OAuth signature.
