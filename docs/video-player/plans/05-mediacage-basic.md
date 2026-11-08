# Plan 05: MediaCage Basic DRM on the Bunny library

Why: a browser downloader (Cốc Cốc, extensions) saves the HLS stream the page plays. Bunny's free MediaCage Basic encrypts
the stream per session and makes playback go through Bunny's Embed View only, which stops most of these tools.

## Decisions (confirmed)

| | |
|---|---|
| What | MediaCage Basic (free). Not Enterprise DRM (99 USD/month + per license) |
| Where | The production video library, directly (no separate test library) |
| `freeTillSeconds` preview | Not a concern now (no video uses it). Not verified by this plan |
| Code | None. The web already plays through Bunny's Embed View (`{EmbedBaseUrl}/embed/{libraryId}/{videoId}?token=&expires=`); the admin has no video preview |

## What it does and does not do

- Each playback session gets its own keys; direct access to the files and third-party players are switched off.
- The keys reach the browser in clear form: a determined person can still get the video, and nothing stops screen recording.
- Lost features: **MP4 fallback** (the code uses no MP4 file) and **Early Play** (a newly uploaded video plays only once
  encoding has finished; staff upload before publishing, so customers do not see this).
- Billing is unchanged: storage plus CDN bandwidth. A download costs about one full view at that resolution.

## Steps

1. Bunny dashboard → Stream → the library → **Security** → turn **DRM** (MediaCage) on. Do it at a quiet hour.
2. Right after, on the live site:
   - sign in (customer with access, or a staff account) and open a lesson with a video: it plays, seeking and quality
     switching work, no console errors;
   - a video the account cannot access still answers 403 from `/api/v1/videos/{publicId}/playback` (unchanged);
   - in Cốc Cốc, open the same lesson: the download button is missing or the downloaded file does not play;
   - upload a short test video from the admin and attach it to a draft lesson: it plays once encoding finishes.
3. If playback breaks: turn DRM off again (immediate) and record what failed here.
4. Docs: status row in `../spec.md`, the "Videos" section of `CLAUDE.md` (one line), results below.

## Results

(fill in after step 2)
