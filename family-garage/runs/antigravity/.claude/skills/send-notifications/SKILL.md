---
name: send-notifications
description: Send in-app notifications (the bell/inbox) from a data processor or document behavior via INotificationService — to the user who triggered the work, a specific user, a role, a permission holder, or admins — with optional links to documents and idempotent de-duplication. Use when a processor/behavior should tell someone it finished, failed, or needs attention, including processors that run unattended on the Job Scheduler.
---

# Sending notifications from config code

> **Note:** This recipe's worked example uses `ImportNbuRates`, carried over from a real Kandra configuration; this bare configuration has no entities of its own yet. Treat entity names as illustrative and substitute your own.

A notification is a **stored row** (the user's inbox) that is also pushed live over SignalR to anyone online.
Sending one is an ordinary server-side call, **not** a new entity kind and not a posting-pipeline operation:
`INotificationService` is a plain scoped service, **constructor-injectable** anywhere on the server — unlike
register writers and `IPostingService` (keyed, reachable only through `ISubmitScope`; see
[`post-to-accounting`](../post-to-accounting/SKILL.md)). No attribute, DI registration, migration or Dto is needed.

Design/decisions: `docs/kandra-notifications-and-messaging-design.md` and
`docs/kandra-notifications-and-messaging-implementation-plan.md` in the platform repo. The one real producer in
this repo is `ImportNbuRatesBehavior` (`FamilyGarage.Application/DataProcessors/ImportNbuRates.cs`) — read its tail
end to end; the recipe below is that code generalized.

## 1. When to notify — and when not to

Notify when a person **was not watching the thing happen** and would want to know: a long or scheduled processor
finished (or produced something to review), a batch partly failed, a document needs a follow-up. Do **not**
notify for something the user just did interactively and already sees on screen (a plain Save of their own
document), and don't notify per-line/per-row — one summary per run. Failures of scheduled jobs are already
announced by the engine (`NotifyOnFailure` on the job definition, 24h cooldown, recovery notice) — a processor
need not send its own "I failed" message; throwing is enough.

## 2. Inject the service and build the message

```csharp
public class MyProcessorBehavior(
    /* ... */
    INotificationService notifications,
    IUnitOfWork unitOfWork) : IDataProcessorBehavior<MyProcessorDto, MyProcessorResult>
```

`INotificationService` lives in `Kandra.Application.Abstractions.Services.Notifications`; `NotificationSeverity`/
`NotificationLink`/`NotificationLinkKind` in `Kandra.Domain.Notifications`. The engine's `Notification` record
collides in name with other types in a behavior file — alias it as the real example does:
`using NotificationMessage = Kandra.Application.Abstractions.Services.Notifications.Notification;`.

```csharp
await notifications.NotifyCallerAsync(new NotificationMessage(
    Title: NotificationText.ForKey("NbuRatesImport_Done", dto.Lines.Count, dto.Code),
    Severity: NotificationSeverity.Success,
    Links: [new NotificationLink
    {
        Kind = NotificationLinkKind.Document,
        EntityTypeId = RateImport.TypeId,
        EntityId = created.Id,
        CaptionSnapshot = $"Rate Import {dto.Code}",
    }]), cancellationToken);
```

`Notification` says **what**, never **who** — recipients are chosen by the method you call, so one value can be
sent to a user and a role alike. Fields: `Title` (required), `Body`, `Severity` (`Info` default / `Success` /
`Warning` / `Error`), `Links`, `DeduplicationKey`. Leave `Origin`/`SenderUserId` alone — the engine fills them from
the caller's claims.

**`NotificationText`**: `ForKey(resourceKey, args…)` is resolved through the localization chain in the **reader's**
culture at read time (add the resx key to every `{Config}.Localization` resx — en/ru/uk, with `{0}` placeholders);
`ForLiteral(text)` is stored verbatim and shown to everyone in that one language. Prefer `ForKey`.

Two traps of `ForKey`. **The title and the body share one list of arguments, the title's**: the body's own arguments are
dropped, so give both keys the same arguments and let each use the placeholders it needs (`{0}` and `{1}` in the title,
`{2}` in the body). **The arguments are stored as data and shown as they are**: an enum member or a caption computed in
the sender's language reaches every reader in that one form. Choose the key by the kind instead
(`Reminder_Policy_{kind}_Title`), and pass only names, numbers and dates.

## 3. Pick the recipient method

| Method | Who | Notes |
|---|---|---|
| `NotifyCallerAsync(msg)` | Whoever triggered the current work | **The default for a processor/behavior.** See §4. |
| `NotifyUserAsync(userId, msg)` / `NotifyUsersAsync(ids, msg)` | Named user(s) | e.g. a document's creator. |
| `NotifyRoleAsync(roleName, msg, options?)` | Members of a role | Throws unless the role's **`CanReceiveBroadcasts`** flag is on (Roles admin UI); logs a Warning if the role is empty. Name-based. |
| `NotifyWithPermissionAsync(point, msg, options?)` | Everyone holding an `IAuthorizationPoint` | Direct claim or via role. |
| `NotifyAdminsAsync(msg, options?)` | Everyone the engine's `IsAdmin()` check accepts | No extra permission. |

Audience sends (role/permission/admins) have a spam guard: more than **50** recipients throws unless you pass
`new AudienceOptions { MaxRecipients = … }`, and the sender is excluded from their own send unless
`IncludeSender = true`. All methods return the recipient count (0 is legal: nobody to tell, or all de-duplicated).

## 4. Manual run vs. scheduled run — the thing that's easy to get wrong

Use `NotifyCallerAsync` and **do not branch** on how the code was started. It resolves through `ICallerContext`:
the logged-in user on a manual run, and the job's **`RunAsUserId`** on a scheduled run (the scheduler sets
`ICallerContextOverride`). Hard-coding `NotifyUserAsync(someAdminId)` — or branching on "is this a job?" —
breaks exactly one of the two paths, and a scheduled run's "done" message lands with the wrong person. Never read
`ICallerContext` just to pick a recipient; call `NotifyCallerAsync`.

## 5. Saving: the service stages rows, it never saves

Every `Notify*` call only **adds rows to the current unit of work**.

- Inside a **document behavior hook** (`OnSubmitAsync`, `OnSaveAsync`, …) the document's own save pipeline commits
  them in the same transaction as the document — if the save fails there's no orphan "done" notification. Do
  nothing extra.
- In a **data processor** that doesn't otherwise save (or after a sub-service that already committed, as
  `rateImports.InsertAsync` does), inject `IUnitOfWork` and call `await unitOfWork.SaveAsync(cancellationToken)`
  right after notifying. Forgetting this silently drops the notification when the scope disposes (a real bug the
  engine's own scheduler hit). Send it **after** the work succeeds, so a run that threw never announces success.

The push to online browsers happens only after a successful commit (and is best-effort — a push failure is logged,
never fails your save).

## 6. Links

`Links` attach clickable references. Two kinds: `Document` (set `EntityTypeId` = the document's `TypeId`, and
`EntityId`) and `JobRun` (`EntityId` = the scheduled run id; no `EntityTypeId`). **Dictionary links are not built.**
`CaptionSnapshot` is the text shown for the link, captured **at send time** — build it yourself; it stays correct
if the target is later renamed or deleted.

## 7. `DeduplicationKey` — for producers that re-run

If a producer can run repeatedly over the same facts (a scheduled "documents without a follow-up" scan), set
`DeduplicationKey` to a stable string (`$"hint:InvoiceWithoutWaybill:{invoiceId}"`). A recipient who already has a
row with that key is skipped silently, so re-running the producer doesn't spam. Build the key only from facts that
identify the event and do not move between runs (the vehicle, the work item, the due date or the reading it became due
at), never from a value that changes on every run ("1 240 km left"): the key would differ each time and the "once"
becomes "every run". Leaving the occurrence out goes wrong the other way: a key of the vehicle and the work item alone
blocks the reminder for the next time the same job is due, forever. The key is per recipient, and a
read notification still blocks a repeat (until the 90-day purge removes it). Omit it for one-shot "run finished" messages, which should
always arrive.

## 8. Out of scope / not built

- **Chat** (`IChatService`, direct/support conversations) — not built; revisit when it ships.
- **Workflow hints** (a scheduled processor that nags about missing follow-ups) — the mechanism is exactly §3+§7,
  but no real producer exists yet.
- Read notifications older than 90 days are purged automatically (not configurable); unread ones are kept.
- `create-handler`/local handlers are mid-edit round-trips, not a notification context — don't notify from them.

## 9. Verify

`dotnet build Kandra.slnx`, then live: run the processor from its page and confirm the bell badge increments and
the notification (localized title, working link) appears on the **Notifications** page. For a scheduled processor,
set a distinct **Run As** user on the job, trigger it, and confirm the notification lands for *that* user, not the
admin who scheduled it. Run it twice with a `DeduplicationKey` and confirm only one row.
