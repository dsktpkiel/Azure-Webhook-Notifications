// worker.js
export default {
  async fetch(request, env) {
    if (request.method !== "POST") return new Response("Only POST allowed", { status: 405 });

    let payload;
    try {
      payload = await request.json();
    } catch {
      return new Response("Invalid JSON body", { status: 400 });
    }

    const config = {
      allowedStates: (env.ALLOWED_STATES || "Done,Closed,Resolved")
        .split(",")
        .map(s => s.trim()),
      teamsWebhook: env.TEAMS_WEBHOOK_URL,
      environment: env.ENVIRONMENT_LABEL || "Development",
      maxPRButtons: parseInt(env.MAX_PR_BUTTONS || "3", 10)
    };

    const resource = payload.resource || {};
    const revision = resource.revision || {};
    const fields = revision.fields || {};

    const workItemId = fields["System.Id"] || resource.workItemId || "Unknown";
    const title = fields["System.Title"] || "No Title";
    const state = fields["System.State"] || "—";
    const assignedTo = fields["System.AssignedTo"]?.displayName || fields["System.AssignedTo"]?.uniqueName || "—";
    const changedBy = fields["System.ChangedBy"]?.displayName || "—";
    const changedDate = fields["System.ChangedDate"] || new Date().toISOString();

    const isAllowed = config.allowedStates.includes(state);

    const card = {
      type: "message",
      attachments: [
        {
          contentType: "application/vnd.microsoft.card.adaptive",
          content: {
            $schema: "http://adaptivecards.io/schemas/adaptive-card.json",
            type: "AdaptiveCard",
            version: "1.4",
            msteams: { width: "full" },
            body: [
              {
                type: "Container",
                items: [
                  {
                    type: "TextBlock",
                    text: `🛠️ Work Item #${workItemId} Updated`,
                    size: "Large",
                    weight: "Bolder",
                    wrap: true
                  },
                  {
                    type: "FactSet",
                    facts: [
                      { title: "Title:", value: title },
                      { title: "State:", value: state },
                      { title: "Assigned To:", value: assignedTo },
                      { title: "Changed By:", value: changedBy },
                      { title: "Changed Date:", value: changedDate },
                      { title: "Environment:", value: config.environment }
                    ]
                  },
                  {
                    type: "TextBlock",
                    text: isAllowed
                      ? "Allowed state"
                      : ` Non-production state: ${state}`,
                    wrap: true,
                    weight: "Bolder",
                    color: isAllowed ? "Good" : "Attention",
                    spacing: "Medium"
                  }
                ]
              }
            ],
            actions: [
              {
                type: "Action.OpenUrl",
                title: "Open Work Item",
                url: resource._links?.html?.href || ""
              }
            ]
          }
        }
      ]
    };

    const resp = await fetch(config.teamsWebhook, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(card)
    });

    if (!resp.ok) {
      const text = await resp.text();
      return new Response(`Teams webhook failed: ${resp.status} ${text}`, { status: 502 });
    }

    return new Response("OK", { status: 200 });
  }
};
