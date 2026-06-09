# OAuth 2.0 Migration Notes

XApiClient now uses OAuth 2.0 bearer user-context authorization for all existing read, search, post, and media upload flows.

Media upload targets the X API v2 `/2/media/upload` endpoint and tweet creation targets `/2/tweets`. OAuth 1.0a signing code has been removed.
