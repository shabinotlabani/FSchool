# UPNEXT: Multi-Club

The existing data and staff accounts migrate to club ID 1. Its existing name and logo are preserved. The original club remains active without an expiry until the owner sets a date.

The existing `labinot.shabani@gmail.com` account is bootstrapped once as the platform owner. Ownership is a stored account flag, not a runtime email comparison and not the club-level Super Admin role. Ordinary administrators cannot edit this account or grant platform ownership. Passwords are unchanged. The first upgrade refreshes the owner's security stamp, requiring a new login.

After login the owner chooses a club at `/Clubs`. This page also creates clubs and changes their expiry/active status. After entering a club, use **Shfrytëzuesit** to create its administrator and other staff. Each email is globally unique and belongs to exactly one club. The selected club is assigned server-side; clients cannot submit a different club ID. The owner can return through **Klubet · Ndrysho klubin**.

An expiry date includes that whole date in the billing timezone. Access and scheduled work stop the following day, or immediately when the club is deactivated. The owner retains access to club management for renewal. Expiry does not delete data.

## Isolation

- All business entities carry `ClubId`, including sponsorships, settings, payments, inventory, parent notifications, medical records and audit history.
- Context-bound EF filters apply to queries, includes and bulk EF operations. No selected club returns no business data.
- Save guards stamp new records, reject foreign-club changes and validate staff references. Composite database constraints prevent cross-club business relationships.
- Identity lookup is global only during authentication/account management. Operational staff queries are scoped; the platform owner remains resolvable for audit references.
- The owner's selected club cookie is protected and bound to the account ID. Ordinary users always use their database membership. Antiforgery tokens also bind forms to the selected club, preventing stale forms from posting into another club.
- Parent tokens resolve only a club ID before the remaining queries run under that scope. Existing links remain valid. Public logos are addressed by club ID; private records cannot be selected this way.
- Billing, push and nutrition workers enumerate active, unexpired clubs and use independent scopes. Reset deletes only the selected club's operational rows.
- Clubs share the existing origin's VAPID key pair so a parent with children in multiple clubs can use the same browser push subscription. Only this infrastructure credential is reused; subscriptions, messages and notification preferences remain club-scoped.

## New-club defaults

New clubs start without players, payments, sponsors or other operational records. They receive one field, equipment size options, a neutral UPNEXT logo, and the existing tariff defaults: monthly 50; family 40 for the first two children, then 35; waiver 0. Configure tariffs and branding before operational use. Financial values never carry a currency symbol.

## Deployment and checks

Migration: `20261022221802_MultiClub`. Normal Development/Production startup applies it before serving requests. It backfills existing rows to club 1, preserves edited seeded tariffs/fields, and adds club-scoped indexes and relationship constraints. Use the normal database backup procedure before production deployment; this change has not been deployed or pushed automatically.

`dotnet run --project tests/MultiClub.Checks --configuration Sponsorship -- .`

The checks create and remove a separate local PostgreSQL database. They cover upgrade preservation, fail-closed queries, graph inserts, settings/sponsorship isolation, cross-club read/write/FK rejection, family registration and billing with new tariff IDs, parent links, expiry, worker selection, stale forms and scoped reset. They do not use the real club database or send notifications.

They also exercise real MVC login, owner club creation/selection, new administrator assignment and access restrictions through HTTP. `--ui` additionally captures desktop and phone-size browser previews with the local Edge/Playwright runtime. This is browser emulation, not a physical-phone test.
