# PostTweetRequest.cs

`PostTweetRequest` is the serialized v2 tweet creation payload.

It supports plain text tweets and optional `media.media_ids` attachment data populated after a successful v1.1 media upload.

## Add Reply Payload Support (2026-06-15)
- Added optional eply.in_reply_to_tweet_id serialization support so callers can create X reply chains/threads through the v2 tweet creation endpoint.

