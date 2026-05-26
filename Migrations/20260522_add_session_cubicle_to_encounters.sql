-- ════════════════════════════════════════════════════════════════════════
--  Migration: add session_id and cubicle_id to public.encounters
--  Date:      2026-05-22
--  Reason:    The C# Encounter model declares [Column("session_id")] and
--             [Column("cubicle_id")], but these columns were missing from
--             the table.  Without them:
--               • EncounterService.FillBookingContextAsync silently failed,
--                 so no encounter ever stored its cubicle.
--               • MatchesSupervisorCubicle always returned false →
--                 supervisor review queue was always empty (Rule 7 broken).
-- ════════════════════════════════════════════════════════════════════════

-- 1. Add the columns (nullable — existing rows keep NULL until backfilled)
ALTER TABLE public.encounters
  ADD COLUMN IF NOT EXISTS session_id integer REFERENCES public.sessions(id),
  ADD COLUMN IF NOT EXISTS cubicle_id integer REFERENCES public.cubicles(id);

-- 2. Backfill existing encounters from their booking context
--    (only rows that have a booking_id and are still NULL)
UPDATE public.encounters e
SET
    session_id = b.session_id,
    cubicle_id = ba.cubicle_id
FROM public.bookings b
LEFT JOIN public.booking_assignments ba
       ON ba.booking_id = b.booking_id
WHERE e.booking_id  = b.booking_id
  AND e.session_id IS NULL;

-- 3. Verify — should return 0 rows if backfill succeeded for all linked encounters
-- SELECT id, booking_id, session_id, cubicle_id
-- FROM public.encounters
-- WHERE booking_id IS NOT NULL
--   AND session_id IS NULL;
