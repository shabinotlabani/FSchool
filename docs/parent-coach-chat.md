# Parent–coach messages

- Parent portal: **Kontakto trajnerin**, scoped to the selected child. Family links use current family membership. Parents can choose the assigned head coach or assistant; each conversation is private to that coach.
- Coach panel and sidebar: **Mesazhet**, with unread counts. Coaches can also start a conversation from attendance or the team list.
- Chat shows the latest 100 messages, older-message navigation, local timestamps and read receipts. Open conversations refresh every 15 seconds while the tab is visible. Sending also works without JavaScript. This feature does not send phone push notifications.
- Every read/send/read-receipt request checks the private parent token or signed-in coach, current team assignment, role and account status. Tenant filters and foreign keys isolate clubs. A former coach loses access after reassignment; the replacement coach does not inherit private messages. Revoked parent links stop working for chat too.
- POST operations require antiforgery tokens. Message bodies are plain text, limited to 2000 characters. A request ID prevents duplicate sends. Message history is included in the selected club's existing data-reset workflow.

## Deactivation

Assigned coaches and assistants can choose **Pasivizo** from attendance or the team list. A reason and explicit confirmation are required. The operation uses the existing billing/team locks, rejects stale revisions, records the coach and reason in `StudentChanges`, and synchronizes pending automatic bank requests. The player leaves the active roster; financial obligations and history are preserved. Reactivation remains in the existing administrator workflow.

## Deployment and verification

Migration `ParentCoachMessages` creates the message table. Normal application startup applies pending migrations; no production database was modified during implementation.

`tests/MultiClub.Checks` covers parent and family authorization, wrong-club/wrong-coach access, revoked links, duplicate sends, read receipts, deactivation audit, preserved obligations and stale attendance. HTTP checks exercise actual forms, antiforgery and HTML escaping. With `--ui`, Edge captures mobile chat and the coach inbox using disposable test data.
