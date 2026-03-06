
In-progress:
- Add an additional checking in the Build Pipeline where it automatically logs Work Items that are not "Done" but is about to be deployed in production.
- Leave a comment in the work item on pipeline run, notifying people involved prompting an update from them

This project implements an event-driven DevOps notification system.

When a work item is updated in Azure DevOps, a Service Hook sends
a webhook payload to a Cloudflare Worker. The worker processes
the event, detects state changes and linked pull requests, then
generates a Microsoft Teams Adaptive Card notification.

Azure DevOps / TFS
        │
        │ Service Hook (Work Item Updated)
        ▼
Cloudflare Worker (Serverless Webhook)
        │
        ├─ Parse Work Item JSON
        ├─ Detect State Changes
        ├─ Detect Linked Pull Requests
        └─ Build Adaptive Card
        │
        ▼
Microsoft Teams Webhook
        │
        ▼
Teams Channel Notification

Technologies:
- Azure DevOps Service Hooks
- Power Automate
- Serverless Worker
- Microsoft Teams Webhooks
- Adaptive Cards

Note: Source code is not included due to organizational usage. Template and documentations provided instead.
