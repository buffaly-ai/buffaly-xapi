# XCredentials.cs

`XCredentials` is the OAuth 2.0 credential container for X API calls.

The client requires a user-context bearer access token. `AccessToken` is preferred, while `BearerToken` remains a compatibility alias for callers that already store the bearer value under that name. OAuth 1.0a consumer key/secret and access token secret fields were removed.
