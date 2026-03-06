# Cloudflare Worker Azure DevOps → Teams Template

This document explains the architecture, logic, and configuration
for the Cloudflare Worker template that sends Microsoft Teams
notifications for Azure DevOps work item updates.

## Architecture Overview

The system follows an **event-driven, serverless architecture**:

1. **Azure DevOps / TFS**  
   - Emits a **Service Hook** when a work item is updated and a PR is linked/removed or anything updated to Development
   - Sends a JSON payload describing the work item.

2. **Cloudflare Worker**  
   - Receives the webhook payload.  
   - Parses work item details (ID, title, state, assigned user, changed by, changed date).  
   - Validates state against **allowed production states**.  
   - Builds an **Adaptive Card payload** for Teams notifications.

3. **Microsoft Teams Webhook**  
   - Receives the Adaptive Card JSON from the Worker.  
   - Posts a notification to the configured Teams channel.

Optional Extensions
- Pull Request detection (currently simplified in template).  
- Additional environment labeling or badges.  
- Further webhook payload validation.


