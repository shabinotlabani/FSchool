# Sport and development settings

The platform owner chooses the sport in **Klubet → Shto / Ndrysho klubin**. Existing clubs and historical records migrate to Football. Available starting templates: football, basketball, volleyball, handball, tennis and other sport. These are editable starting fields, not age-specific standards or automatic medical/performance judgments.

Club administrators open **Të dhënat e klubit → Sporti · Matjet dhe vlerësimet**. They can add fields, edit labels/units, set numeric limits and increments, manage choice lists and positions, reorder or deactivate fields. Rating fields use 1–5; configurable scales preserve other ranges, including RPE 0–10. Coaches fill assessments but cannot configure club fields.

Settings are scoped by club and sport. Switching sport preserves the previous sport's settings. Basketball has basketball skills, positions and match statistics and no goalkeeper forms. Goalkeeper sections are available for football and handball.

New development records store their sport and the exact field schema. Changes to labels, units, limits or enabled fields do not reinterpret old records. Historical edits use the saved schema. Legacy records use the original football schema. Charts separate sports and differing field definitions; numeric charts shown to parents remain restricted to measurements, testing and skills, not psychological/talent notes. Attendance shows the current sport's latest player role. Nutrition context includes the selected sport.

Migration `SportDevelopmentSettings` adds sport metadata and club metric settings without deleting existing assessments. Settings survive the existing operational data-reset workflow. Migration application occurs on normal application startup; production was not changed during implementation.

Validation: `tests/MultiClub.Checks` covers templates, forged/stale sport forms, custom measurements, deactivation, historical schemas, club isolation, actual admin/coach forms and browser previews. `tests/ParentPortal.Checks` covers existing football assessments and parent progress.
